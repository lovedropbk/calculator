#!/usr/bin/env python3
import sys
import os
import json
import argparse
import datetime
import re
from collections import defaultdict, Counter
from mitmproxy.io import FlowReader
from mitmproxy.http import HTTPFlow


def shorten_token(t, keep=6):
    if not isinstance(t, str):
        return t
    if len(t) <= keep * 2:
        return t
    return f"{t[:keep]}...{t[-keep:]}({len(t)} chars)"


def pick_auth_headers(headers):
    res = {}
    for k in headers.keys():
        lk = k.lower()
        if lk in ("authorization", "www-authenticate", "proxy-authenticate"):
            res[k] = headers[k]
        if lk == "cookie":
            res["Cookie"] = headers[k]
        if lk == "set-cookie":
            res["Set-Cookie"] = headers[k]
        if lk in ("x-xsrf-token", "x-csrf-token"):
            res[k] = headers[k]
    return res


def sanitize_headers(d):
    out = {}
    for k, v in d.items():
        lk = k.lower()
        if lk in ("authorization", "cookie", "set-cookie"):
            out[k] = shorten_token(v, keep=8)
        else:
            out[k] = v
    return out


def is_interesting(flow, host_filters):
    h = flow.request.host
    if not host_filters:
        return True
    return any(h.endswith(f) or h == f for f in host_filters)


def build_summary(flows, host_filters):
    by_host = defaultdict(lambda: {"count": 0, "methods": Counter(), "status": Counter(), "endpoints": Counter()})
    for f in flows:
        if not getattr(f, "response", None):
            continue
        if not is_interesting(f, host_filters):
            continue
        host = f.request.host
        path = f.request.path.split("?")[0]
        by_host[host]["count"] += 1
        by_host[host]["methods"][f.request.method] += 1
        by_host[host]["status"][f.response.status_code] += 1
        by_host[host]["endpoints"][f"{f.request.method} {path}"] += 1
    out = {}
    for host, info in by_host.items():
        out[host] = {
            "total_flows": info["count"],
            "methods": info["methods"].most_common(),
            "status_codes": info["status"].most_common(),
            "top_endpoints": info["endpoints"].most_common(25),
        }
    return out


def format_timeline_line(flow):
    req = flow.request
    resp = getattr(flow, "response", None)
    line = {}
    line["time"] = datetime.datetime.fromtimestamp(flow.timestamp_start).isoformat()
    line["host"] = req.host
    line["method"] = req.method
    line["url"] = f"{req.scheme}://{req.host}{req.path}"
    if resp:
        line["status"] = resp.status_code
        rh = pick_auth_headers(resp.headers)
        rh = sanitize_headers(rh)
        if resp.headers.get("Location"):
            line["location"] = resp.headers.get("Location")
    rq = pick_auth_headers(req.headers)
    rq = sanitize_headers(rq)
    line["req_headers"] = rq
    if resp:
        line["res_headers"] = rh
    return line


def write_markdown_timeline(timeline, out_md):
    with open(out_md, "w", encoding="utf-8") as f:
        f.write("# Auth/Endpoint Timeline\n\n")
        for i, ln in enumerate(timeline, 1):
            f.write(f"- [{i}] {ln['time']} {ln['method']} {ln['url']}")
            if "status" in ln:
                f.write(f" -> {ln['status']}")
            f.write("\n")
            if ln.get("req_headers"):
                f.write("  - req: " + json.dumps(ln["req_headers"]) + "\n")
            if ln.get("res_headers"):
                f.write("  - res: " + json.dumps(ln["res_headers"]) + "\n")
            if ln.get("location"):
                f.write(f"  - redirect: {ln['location']}\n")


def extract_interesting(flows, host_filters):
    details = []
    for f in flows:
        if not is_interesting(f, host_filters):
            continue
        d = {}
        req = f.request
        d["time"] = datetime.datetime.fromtimestamp(f.timestamp_start).isoformat()
        d["method"] = req.method
        d["url"] = f"{req.scheme}://{req.host}{req.path}"
        d["host"] = req.host
        d["req_headers"] = sanitize_headers(dict(req.headers))
        if req.content:
            d["req_body_len"] = len(req.content)
        resp = getattr(f, "response", None)
        if resp:
            d["status"] = resp.status_code
            d["res_headers"] = sanitize_headers(dict(resp.headers))
            if resp.content:
                d["res_body_len"] = len(resp.content)
        details.append(d)
    return details


def main():
    ap = argparse.ArgumentParser(description="Analyze mitmproxy flows for auth workflow")
    ap.add_argument("input", help="Path to flows.mitm")
    ap.add_argument(
        "--hosts",
        default="api.atlassian.com,as.atlassian.com,mcp.atlassian.com,api.statsig.com",
        help="Comma-separated host filters. Empty to include all.",
    )
    ap.add_argument("--outdir", default=None, help="Output directory. Default: alongside input in 'analysis'")
    args = ap.parse_args()

    in_path = os.path.abspath(args.input)
    if not os.path.isfile(in_path):
        print(f"Input not found: {in_path}", file=sys.stderr)
        sys.exit(1)
    base_dir = os.path.dirname(in_path)
    outdir = args.outdir or os.path.join(base_dir, "analysis")
    os.makedirs(outdir, exist_ok=True)

    host_filters = [h.strip() for h in args.hosts.split(",") if h.strip()]

    flows = []
    with open(in_path, "rb") as f:
        reader = FlowReader(f)
        for flow in reader.stream():
            flows.append(flow)

    summary = build_summary(flows, host_filters)
    with open(os.path.join(outdir, "summary_by_host.json"), "w", encoding="utf-8") as f:
        json.dump(summary, f, indent=2)

    timeline = []
    for f in flows:
        if not is_interesting(f, host_filters):
            continue
        timeline.append(format_timeline_line(f))
    with open(os.path.join(outdir, "timeline.json"), "w", encoding="utf-8") as f:
        json.dump(timeline, f, indent=2)
    write_markdown_timeline(timeline, os.path.join(outdir, "timeline.md"))

    details = extract_interesting(flows, host_filters)
    with open(os.path.join(outdir, "details.jsonl"), "w", encoding="utf-8") as f:
        for d in details:
            f.write(json.dumps(d) + "\n")

    print("Wrote outputs to", outdir)
    print(" - summary_by_host.json: top endpoints, status distribution per host")
    print(" - timeline.md: chronological view with auth-related headers")
    print(" - timeline.json: same data as JSON")
    print(" - details.jsonl: per-flow headers and sizes (sanitized)")


if __name__ == "__main__":
    main()