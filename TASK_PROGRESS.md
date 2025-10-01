Overall Goal
- Deliver a native, straightforward WinUI 3 frontend that binds to a Go backend, replacing the lxn/walk UI with higher performance and better UX. Build an MVP quickly, then design a parallel, modular backend service that mirrors the MVP API while keeping clean layering for long-term maintainability.

To-Do List
1) Assess docs and engines to list MVP scope and data flows. [Done]
2) Implement simple fc-api HTTP wrapper over existing engines. [Done]
3) Create WinUI 3 MVP app with bindings to fc-api (/calculate, /campaigns/catalog). [Done]
4) Implement parallel, modular backend (fc-svc) that mirrors fc-api endpoints with internal layering. [Done]
5) Build and run fc-svc, confirm endpoint parity. [Done]
6) Set up free, adminless CI (GitHub Actions) to build both backends and WinUI 3. [Done]
7) Push to GitHub and trigger workflows. [Done]
8) Fix CI build issues (XAML Spacing attributes, System.Text.Json vulnerability). [Done]
9) Verify builds pass and artifacts are available. [In Progress]
10) Document how to download and run artifacts locally. [Planned]
11) Extend WinUI 3 to call /campaigns/summaries and render per-row metrics per redesign. [Planned]
12) Add Dealer Commission auto/override UI (policy integration). [Planned]
13) Add debounced inputs and scenario persistence/export. [Planned]

Current Status
- fc-api: implemented (cmd/fc-api) with endpoints for health, parameters, commission/auto, campaigns/catalog, campaigns/summaries, calculate.
- fc-svc (parallel backend): implemented and built to bin/fc-svc.exe. Modular layers created: internal/config, internal/server, internal/services (adapters, enginesvc), internal/ports/httpserver/router.go.
- WinUI 3 MVP: implemented with top inputs, campaign list, metrics; hitting /calculate and /campaigns/catalog. Debounced inputs via PropertyChangedHandlers.
- GitHub Actions CI: workflows created for Go backends and WinUI 3. 
- Repository: https://github.com/lovedropbk/calculator
- Commits pushed:
  - bbb684e: feat(mvp): initial commit - WinUI 3 MVP, modular Go backends (fc-api, fc-svc), and CI workflows
  - 56d1ec4: fix: remove WinUI3 Spacing attributes and upgrade System.Text.Json to fix CI build

Next Task
- Monitor https://github.com/lovedropbk/calculator/actions to confirm builds pass.
- Download artifacts (go-backends and FinancialCalculator.WinUI3).
- Test end-to-end: run backend, launch WinUI 3 client, verify campaign summaries and metrics.

Checkpoint: Iteration 4 completed
- Progress doc updated after CI setup and first push/fix cycle.
- Next update after build verification and end-to-end smoke test.
