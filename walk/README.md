# Walk UI — Native Windows App (Canonical)

This directory contains the native Windows (Walk) UI for the Financial Calculator. This is the canonical UI. The previous Wails/React UI under /frontend is legacy and should be ignored.

- Canonical UI: Walk (Windows-native)
- Legacy: Wails (unmaintained, not tested)

Structure
- bin/ — built executables (fc-walk.exe)
- build/windows/ — Windows resources (manifest)
- scripts/ — PowerShell helper scripts (build, run-dev, clean)
- versioninfo.json — version and icon resource configuration (read by goversioninfo)

Prerequisites
- Windows 10/11
- Go 1.21+
- MSVC redistributables

Build and Run

Option A: Development (fastest)
- Uses the root walk entrypoint with a build tag (keeps legacy main.go excluded)
- Command:
  - go run -tags walkui .

Option B: Release-style build (GUI subsystem + resources)
1) Install goversioninfo (once):
   - go install github.com/josephspurrier/goversioninfo/cmd/goversioninfo@latest
2) From repo root, generate resources and build:
   - & "$env:USERPROFILE\go\bin\goversioninfo.exe"
   - go build -tags walkui -ldflags "-H=windowsgui" -o walk/bin/fc-walk.exe .
3) Run:
   - ./walk/bin/fc-walk.exe

Notes
- If you encounter side-by-side (SxS) errors launching the exe, re-run the goversioninfo step so resources are embedded before building.

How it works

UI entrypoint
- The Walk entrypoint is defined in the root at:
  - [walk_main.go](../walk_main.go:1)
- It creates a split-pane window:
  - Left pane → inputs (product, price, down payment %/amount with lock, term, timing, balloon, rate mode)
  - Right pane → outputs (monthly installment, nominal/effective customer rate, acquisition RoRAC, financed amount), metadata

Backend binding (no HTTP, direct call)
- The Walk UI calls [App.CalculateQuote()](../app.go:198) which uses the calculation engines under /engines:
  - Campaign → [campaigns.Engine.ApplyCampaigns()](../engines/campaigns/campaigns.go:24)
  - Pricing → [pricing.Engine.ProcessDeal()](../engines/pricing/pricing.go:248)
  - Cashflow → [cashflow.Engine.*](../engines/cashflow/cashflow.go:1)
  - Profitability → [profitability.Engine.CalculateWaterfall()](../engines/profitability/profitability.go:23)

Design decisions
- Entry-point placement: [walk_main.go](../walk_main.go:1) remains at the repo root (package main). Go cannot import package main from a subfolder. We keep all Walk assets (manifest, scripts, binaries) under /walk but leave the executable entrypoint at root until a future refactor extracts shared app glue into an importable package.
- Manifest: [walk.exe.manifest](./build/windows/walk.exe.manifest:1) enables Common Controls v6 and DPI awareness.
- Version/icon config: [versioninfo.json](./versioninfo.json:1) is read by goversioninfo to generate resource.syso.

What’s implemented vs. TODO

Implemented
- Split-pane layout (no overlapping)
- Core inputs: product, price, DP %/amount with lock, term, timing, balloon, rate mode
- Outputs: monthly installment, nominal/effective rate, acquisition RoRAC, financed amount, metadata

TODO (prioritized)
1) Campaign selection UI; pass to engines; show audit results
2) IDC editor (dialog/table); feed engines; display impacts
3) Waterfall details panel (expandable)
4) Scenario save/export; scenario compare
5) Parameter service in Walk (load latest set like Wails did)
6) Schedule/cashflows view and CSV export

Troubleshooting

- “TTM_ADDTOOL failed”: Rare Walk tooltip init issue. Ensure resources are embedded and Common Controls v6 is active (manifest). If encountered under go run, prefer the release build path.
- Side-by-side configuration error: Re-run goversioninfo and rebuild.

References
- Engines API mapping in root README:
  - [README.md](../README.md:1)
- Architecture doc (Walk migration section 14):
  - [docs/financial-calculator-architecture.md](../docs/financial-calculator-architecture.md:1)