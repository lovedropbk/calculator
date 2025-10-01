WinUI 3 MVP — Financial Calculator (Complete)

Overview
- Native WinUI 3 app that calls Go HTTP API (cmd/fc-api or cmd/fc-svc modular).
- Implements the full MVP requested:
  - Inputs: Product, Price, Down Payment, Term, Rate, Subsidy Budget.
  - Campaign Summaries grid: fetched from /campaigns/summaries with Deal + DealState; shows Dealer Commission and per-row Monthly/Eff metrics (via per-row calculate calls; can be batched later).
  - Right metrics: Monthly, Nominal, Effective, Financed, RoRAC from /calculate.
  - Debounced inputs to keep UI responsive.

Run backends
1) Simple wrapper (fc-api)
   set FC_API_PORT=8123
   go run ./cmd/fc-api

2) Modular service (fc-svc)
   set FC_SVC_PORT=8223
   go run ./cmd/fc-svc

Run WinUI 3 app
- Visual Studio 2022: open winui3-mvp/FinancialCalculator.WinUI3/FinancialCalculator.WinUI3.csproj and Run.
- Or dotnet build: dotnet build winui3-mvp/FinancialCalculator.WinUI3/FinancialCalculator.WinUI3.csproj
- Point the app at a backend: setx FC_API_BASE http://localhost:8123/ (or http://localhost:8223/)

Notes
- Campaign summaries call calculate per row to show Monthly/Eff. This is acceptable for MVP and can be optimized with a batch endpoint.
- Dealer Commission auto mode is used by default; override UI can be added next.
- Subsidy Budget currently maps to idcOther.value in DealState per doc; selection can auto-fill IDC Other on next iteration with userEdited logic.
