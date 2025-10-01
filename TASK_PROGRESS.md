Overall Goal
- Deliver a native, straightforward WinUI 3 frontend that binds to a Go backend, replacing the lxn/walk UI with higher performance and better UX. Build an MVP quickly, then design a parallel, modular backend service that mirrors the MVP API while keeping clean layering for long-term maintainability.

To-Do List
1) Assess docs and engines to list MVP scope and data flows. [Done]
2) Implement simple fc-api HTTP wrapper over existing engines. [Done]
3) Create WinUI 3 MVP app with bindings to fc-api (/calculate, /campaigns/catalog). [Done]
4) Implement parallel, modular backend (fc-svc) that mirrors fc-api endpoints with internal layering. [Done]
5) Build and run fc-svc, confirm endpoint parity. [Done]
6) Extend WinUI 3 to optionally target fc-svc via FC_API_BASE, add campaign summaries grid and IDC/Subsidy mapping. [Planned]
7) Add Dealer Commission auto/override UI (policy integration). [Planned]
8) Add debounced inputs and scenario persistence/export. [Planned]

Current Status
- fc-api: implemented (cmd/fc-api) with endpoints for health, parameters, commission/auto, campaigns/catalog, campaigns/summaries, calculate.
- WinUI 3 MVP: implemented with top inputs, campaign list, metrics; hitting /calculate and /campaigns/catalog.
- fc-svc (parallel backend): implemented and built to bin/fc-svc.exe. Modular layers created: internal/config, internal/server, internal/services (adapters, enginesvc), internal/ports/httpserver/router.go.

Next Task
- Expand WinUI 3 to call /campaigns/summaries and render per-row metrics per redesign; implement Dealer Commission auto/override UI.

Checkpoint: Iteration 30 of 30
- Progress doc updated per mandate after 5th iteration and at the end of this cycle.
