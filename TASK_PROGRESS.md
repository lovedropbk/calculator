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
8) Fix CI build issues and iterate to green build. [In Progress]
9) Verify builds pass and artifacts are available. [Pending]
10) Document how to download and run artifacts locally. [Planned]
11) Extend WinUI 3 campaign summaries with full metrics. [Planned]
12) Add Dealer Commission auto/override UI. [Planned]

Current Status - Build #8
- Repository: https://github.com/lovedropbk/calculator
- Go backends: fc-api and fc-svc build successfully in CI
- WinUI 3: iterating on CI build configuration

Build iterations and fixes applied:
1. Build #1-4: XAML compiler exit code 1 → Fixed by installing maui-windows workload
2. Build #5: Minimal XAML test to isolate issue
3. Build #6: PRI packaging task error (SDK 9.0.305) → Added global.json pinning .NET 8.0
4. Build #7: PRI task still using SDK 9 → Moved global.json to repo root
5. Build #8 (current): Explicit workflow pin to .NET 8.0.403 + global.json at root

Commits pushed:
- bbb684e: feat(mvp): initial commit
- 56d1ec4: fix: remove WinUI3 Spacing attributes and upgrade System.Text.Json
- 89f6e81: fix: switch from x:Bind to Binding in DataTemplate
- 291c22a: test: minimal XAML to isolate XAML compiler CI failure
- b47b9a1: fix: install maui-windows workload, update WindowsAppSDK, restore full UI
- 0ff1bc9: fix: pin .NET SDK to 8.0 for WindowsAppSDK compatibility
- 55ae7f3: fix: move global.json to repo root and pin workflow to .NET 8.0.403

Next Task
- Await Build #8 result from user
- If pass: download artifacts, test end-to-end, document usage
- If fail: debug next error and fix immediately

Checkpoint: Iteration 38 of active development cycle
- Multiple CI fixes applied autonomously with each build failure
- Progress doc updated
