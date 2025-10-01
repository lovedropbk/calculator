Adminless Build Options (Free)

Goal: Let you build and ship the WinUI 3 client and Go backends without requiring local admin rights.

1) GitHub Actions (already configured)
- Files: .github/workflows/build-winui3.yml, .github/workflows/build-go.yml
- Pros: Free for public repos; works for private repos within free minutes; no local admin; artifacts downloadable per run.
- How to use: push/PR or manual workflow_dispatch; download artifacts from the Actions run.

2) Azure DevOps Pipelines (already configured)
- File: azure-pipelines.yml
- Pros: Free tier with Microsoft-hosted Windows agents; adminless; easy artifact management.
- How to use: import the repo into Azure DevOps, enable pipelines, point to azure-pipelines.yml, run; download artifacts.

3) GitHub Codespaces (optional)
- Pros: Cloud dev environment; you can run the Go backends; WinUI 3 GUI cannot be run, but the CI can build the app. Adminless.
- How to use: open the repo in Codespaces; use GitHub Actions to build WinUI 3; run Go server(s) inside Codespaces.

4) Local-only Go builds (no admin)
- The Go backends (fc-api, fc-svc) build fine with stock Go and do not require Visual Studio or admin access:
  - go build ./cmd/fc-api
  - go build ./cmd/fc-svc
- You can iterate on backend logic locally while using CI to produce the WinUI 3 artifacts.

5) Windows App Runtime considerations
- The WinUI 3 app requires Windows App Runtime at runtime. On dev machines without admin, per-user installation is often allowed, but may require sideloading policy.
- If sideloading is blocked, you can still test the app by running it where Windows App Runtime is already installed (VM, secondary machine), or by asking IT to push the runtime via Intune.

Recommended flow
- Develop locally: implement Go and app code.
- Push to a branch; Actions builds both backends and WinUI 3; download artifacts.
- Run backends locally (no admin required).
- Run WinUI 3 artifact on a machine with Windows App Runtime, adminless if policy allows per-user install; otherwise use a VM.

Next enhancements (I can add on request)
- GitHub Release workflow that builds and attaches artifacts to tagged releases.
- CI job to run end-to-end tests by starting fc-svc, sending sample requests, and comparing outputs.
- Packaging (MSIX) via CI with side-loadable per-user install packages, subject to org policy.
