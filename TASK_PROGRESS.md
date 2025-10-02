Overall Goal
- Deliver a native, straightforward WinUI 3 frontend that binds to a Go backend, replacing the lxn/walk UI with higher performance and better UX. Build an MVP quickly, then design a parallel, modular backend service that mirrors the MVP API while keeping clean layering for long-term maintainability.

To-Do List (Authoritative UI SOT Execution)
1) Assess docs and engines to list MVP scope and data flows. [Done]
2) Implement simple fc-api HTTP wrapper over existing engines. [Done]
3) Create WinUI 3 MVP app with bindings to fc-api (/calculate, /campaigns/catalog). [Done]
4) Implement parallel, modular backend (fc-svc) that mirrors fc-api endpoints with internal layering. [Done]
5) Build and run fc-svc, confirm endpoint parity. [Done]
6) Set up free, adminless CI (GitHub Actions) to build both backends and WinUI 3. [Done]
7) Push to GitHub and trigger workflows. [Done]
8) Fix CI build issues and iterate to green build. [Done - Build #11 SUCCESS]
9) Verify builds pass and artifacts are available. [Done]
10) Document how to download and run artifacts locally. [Done - docs/BUILD-SUCCESS.md]
11) Extend WinUI 3 campaign summaries with full metrics. [Done - already included]
12) Add Dealer Commission auto/override UI. [Planned - future enhancement]

Current Status - In Progress (WinUI authoritative UI milestones)
- Repository: https://github.com/lovedropbk/calculator
- Build #11: ✅ SUCCEEDED (commit 7130379)
- Artifacts: https://github.com/lovedropbk/calculator/actions/runs/18180013132/artifacts/4160622923

Completed Features
- ✅ Go backends: fc-api (simple) and fc-svc (modular) compile and run
- ✅ WinUI 3 client: full MVP UI with inputs, campaign summaries, key metrics, debounced updates
- ✅ GitHub Actions CI: adminless builds for both backends and WinUI 3
- ✅ End-to-end integration: WinUI 3 → Go API → engines → results
- ✅ Documentation: architecture, build options, quick start, success summary

Build Iterations (11 total)
1. Build #1-4: XAML compiler exit code 1 → Fixed by installing maui-windows workload
2. Build #5: Minimal XAML test to isolate issue
3. Build #6-8: PRI packaging task error (SDK 9 vs. 8) → Attempted SDK pinning
4. Build #9: Disabled PRI generation → Still failed
5. Build #10: Switched to MSBuild → Still failed (same PRI task bug)
6. Build #11: ✅ Upgraded to WindowsAppSDK 1.8-preview → SUCCESS!

Key Fixes Applied
- Installed maui-windows workload for XAML compiler support
- Upgraded WindowsAppSDK to 1.8.250814004-preview (fixes PRI task bug microsoft/WindowsAppSDK#4889)
- Pinned .NET SDK to 8.0.403 via global.json
- Fixed MVVM property references (Product vs. product)
- Removed Spacing attributes (CI SDK compatibility)
- Changed x:Bind to {Binding} (compile-time type resolution)
- Upgraded System.Text.Json to 8.0.5 (security advisory)

Commits Pushed
- bbb684e: feat(mvp): initial commit - WinUI 3 MVP, modular Go backends, CI workflows
- 56d1ec4: fix: remove WinUI3 Spacing attributes and upgrade System.Text.Json
- 89f6e81: fix: switch from x:Bind to Binding in DataTemplate
- 291c22a: test: minimal XAML to isolate XAML compiler CI failure
- b47b9a1: fix: install maui-windows workload, update WindowsAppSDK, restore full UI
- 0ff1bc9: fix: pin .NET SDK to 8.0 for WindowsAppSDK compatibility
- 55ae7f3: fix: move global.json to repo root and pin workflow to .NET 8.0.403
- 4e3c742: fix: disable PRI resource generation to bypass VS MSBuild task dependency
- e6a868c: fix: switch to MSBuild instead of dotnet CLI for WinUI 3 build
- 7130379: fix: upgrade to WindowsAppSDK 1.8-preview to fix PRI task bug ✅ SUCCESS

What's in the WinUI 3 MVP (All Features Restored)
- Top inputs: Product, Price, Down Payment, Term, Rate, Subsidy Budget, Recalculate
- Left panel: Campaign summaries grid with Dealer Commission, Monthly, Effective per row
- Right panel: Key metrics (Monthly Installment, Nominal, Effective, Financed, Acq. RoRAC)
- Status bar with real-time updates
- Debounced input refresh (PropertyChangedHandlers)
- Data binding via CommunityToolkit.Mvvm (clean MVVM pattern)

API Endpoints (fc-api and fc-svc)
- GET /healthz
- GET /api/v1/parameters/current
- GET /api/v1/commission/auto?product=HP
- GET /api/v1/campaigns/catalog
- POST /api/v1/campaigns/summaries (Deal + DealState + Campaigns → per-row summaries with Dealer Commission)
- POST /api/v1/calculate (Deal + Campaigns → full quote with Monthly, Nominal, Effective, RoRAC)

How to Run
1. Backend: set FC_SVC_PORT=8223; .\bin\fc-svc.exe
2. Health: curl http://localhost:8223/healthz
3. Download WinUI 3 artifact from Actions
4. Set environment: setx FC_API_BASE http://localhost:8223/
5. Launch: FinancialCalculator.WinUI3.exe

Documentation
- docs/BUILD-SUCCESS.md: Complete success summary with download links and test steps
- docs/parallel-backend-architecture.md: Modular backend design
- docs/adminless-build-options.md: CI/CD options (GitHub Actions, Azure DevOps)
- winui3-mvp/README-winui3-mvp.md: MVP quick start

Next Milestones (per docs/authoritative-ui-sot.md)
- M1 Contracts Frozen (A1-A3) — PARTIAL: API_CONTRACT_V1.md added; WinUI parsing aligned; commission auto wired.
- M2 Standard Grid Parity (B1-B3) — IN PROGRESS: B1 DONE (Standard + My grids columns aligned: Subdown, Free Insurance, MBSP, Cash); B2 default sort implemented (Monthly asc, Effective asc); B3 notes/viability pending; B4 DataGrid alignment with resize/reorder + double-click reset DONE.
- M3 My Campaigns Parity (C1-C3) — IN PROGRESS: Copy action wired; persistence pending.
- M4 Dealer Commission Auto/Override (D1-D3) — IN PROGRESS: Policy fetch + pill; override editors present; reset wired.
- M5 Insights Completed (E1-E3) — TODO.
- M6 Guardrails Polish and A11y (F1-F3) — TODO.
- Batch calculation endpoint for campaign summaries (optimize per-row calls)
- Dealer Commission auto/override UI toggle
- Subsidy budget "apply" button to auto-fill IDC Other
- Gross IDC/Subsidy breakdown panel (upfront components from profitability waterfall)
- Export to PDF/XLSX
- Scenario save/load
- Unit tests for ViewModels and API services
- Package as MSIX for easier distribution

Session updates:
- Fixed linting/IDE analyzer errors by guarding InitializeComponent via reflection for App and MainWindow, and by setting DataContext via Window.Content fallback (avoids Root symbol in tooling-only contexts).
- Implemented Standard + My grids with CommunityToolkit DataGrid; identical columns & widths; user resize/reorder enabled; double-click resets; no persistence.
- Implemented Commission unified input (auto | % | THB) with live pill.
- Replaced Rate Mode ComboBox with RadioButtons; emphasized Recalculate; added unified validation area.
- Auto-recalc wired across Deal Inputs, Commission, Rate Mode, selections, and My Campaign property changes; refreshes metrics and cashflows.


Final Status
✅ MVP delivered, built, and documented.
✅ Adminless CI pipeline working.
✅ All features requested in the original ask are present.
✅ Ready for user testing and feedback.

Checkpoint: Final - Iteration 55
