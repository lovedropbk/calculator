# WinUI 3 MVP - Build Success! 🎉

## Status
- **Build #11**: ✅ SUCCEEDED (commit 7130379)
- **Build time**: 3m 7s
- **Artifacts**: Ready to download

## Download Artifacts
**WinUI 3 Client:**
https://github.com/lovedropbk/calculator/actions/runs/18180013132/artifacts/4160622923

**Go Backends:**
- fc-api.exe and fc-svc.exe are already compiled locally in `bin/`
- Or download from the "Build Go Backends" workflow artifacts

## What's Included in the WinUI 3 MVP

### UI Features (All Restored)
- **Top Input Bar:**
  - Product selector (HP, mySTAR, F-Lease, Op-Lease)
  - Price (THB)
  - Down Payment (THB)
  - Term (months)
  - Rate % p.a.
  - Subsidy Budget (THB)
  - Recalculate button

- **Left Panel - Campaign Summaries Grid:**
  - Campaign title/type
  - Dealer Commission (Amount & Percentage)
  - Monthly installment per option
  - Effective rate per option
  - Notes field

- **Right Panel - Key Metrics:**
  - Monthly Installment
  - Nominal Rate
  - Effective Rate
  - Financed Amount
  - Acquisition RoRAC (Return on Risk-Adjusted Capital)

- **Status Bar:**
  - Real-time status messages

### Backend Features
- **fc-api** (simple wrapper): Minimal HTTP API over existing engines
- **fc-svc** (modular production): Clean layered architecture
  - internal/config
  - internal/server
  - internal/services (adapters, enginesvc)
  - internal/ports/httpserver

### API Endpoints
Both backends expose:
- GET /healthz
- GET /api/v1/parameters/current
- GET /api/v1/commission/auto?product=HP
- GET /api/v1/campaigns/catalog
- POST /api/v1/campaigns/summaries
- POST /api/v1/calculate

## How to Run End-to-End

### 1. Start a Backend
```powershell
# Option A: Modular service (recommended)
set FC_SVC_PORT=8223
.\bin\fc-svc.exe

# Option B: Simple wrapper
set FC_API_PORT=8123
.\bin\fc-api.exe
```

Health check:
```powershell
curl http://localhost:8223/healthz
# Should return: {"status":"ok"}
```

### 2. Run the WinUI 3 App
1. Download the artifact from the link above
2. Extract the ZIP file
3. Set the API endpoint environment variable:
   ```powershell
   setx FC_API_BASE http://localhost:8223/
   ```
4. Launch `FinancialCalculator.WinUI3.exe` from the extracted folder

**Note:** The app requires Windows App Runtime. If you get a runtime error, install it per-user from:
https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads

### 3. Test the MVP
1. Select a product (e.g., HP)
2. Enter deal parameters:
   - Price: 1000000
   - Down Payment: 200000
   - Term: 36
   - Rate: 3.99
   - Subsidy Budget: 50000
3. Click **Recalculate** or wait for debounced refresh
4. Observe:
   - Campaign summaries populate with dealer commission and monthly/effective per option
   - Right panel shows key metrics (Monthly, Nominal, Effective, Financed, RoRAC)
   - Status bar shows "Done"

## Build Iterations (What We Fixed)
1. **Build #1-4**: XAML compiler exit code 1 → Fixed by installing `maui-windows` workload
2. **Build #5**: Minimal XAML test to isolate SDK vs. XAML syntax issue
3. **Build #6-8**: PRI packaging task error → Attempted SDK pinning, global.json placement
4. **Build #9**: Disabled PRI generation → Didn't work, task still invoked
5. **Build #10**: Switched to MSBuild → Still failed (same PRI task issue)
6. **Build #11**: ✅ **Upgraded to WindowsAppSDK 1.8-preview** → **SUCCESS!**

## Key Fixes Applied
- Installed `maui-windows` workload for XAML compiler
- Upgraded WindowsAppSDK to 1.8.250814004-preview (fixes PRI task bug microsoft/WindowsAppSDK#4889)
- Pinned .NET SDK to 8.0.403
- Fixed MVVM property references in BuildDealFromInputs
- Removed unsupported `Spacing` attributes
- Changed `x:Bind` to `{Binding}` for CI compatibility

## What's Next (Optional Enhancements)
- [ ] Batch calculation endpoint to optimize per-row summaries
- [ ] Dealer Commission auto/override UI toggle
- [ ] Subsidy budget "apply" button to auto-fill IDC Other
- [ ] Gross IDC/Subsidy breakdown panel
- [ ] Export to PDF/XLSX
- [ ] Scenario save/load
- [ ] Unit tests for ViewModels and API services

## Repository
https://github.com/lovedropbk/calculator

## Commits
- Initial: bbb684e
- Fixes: 56d1ec4, 89f6e81, 291c22a, b47b9a1, 0ff1bc9, 55ae7f3, 4e3c742, e6a868c
- **Success**: 7130379

## Architecture Docs
- docs/parallel-backend-architecture.md
- docs/adminless-build-options.md
- winui3-mvp/README-winui3-mvp.md
