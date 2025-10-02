# Financial Calculator UI — Authoritative SOT and Task Plan

Purpose: unify current implementation (Go Walk app and new WinUI 3 shell), the target design, and the backend/data contracts. This is the authoritative plan to deliver the decision-first UI while preserving pautroven behavior from the Walk MVP and campaign engines.

MARK: Source Documents and Code Anchors

- Specs and fixes:
  - [financial-calculator-ui-redesign.md](docs/financial-calculator-ui-redesign.md)
  - [subsidy-and-idc-fix-understanding.md](docs/subsidy-and-idc-fix-understanding.md)
  - [subdown-column-fix.md](docs/subdown-column-fix.md)
  - [data-linking-fix.md](docs/data-linking-fix.md)
  - [interactive_campaign_manager_sot.md](docs/interactive_campaign_manager_sot.md)

- Walk MVP (Go, production behavior):
  - Entry and layout: [main.go](walk/cmd/fc-walk/main.go)
  - Orchestrator and compute mapping: [ui_orchestrator.go](walk/cmd/fc-walk/ui_orchestrator.go)
  - Progressive edit-state and bottom panels: [deal_inputs_edit_state.go](walk/cmd/fc-walk/deal_inputs_edit_state.go)
  - Details and key metrics panel: [deal_results_bottom.go](walk/cmd/fc-walk/deal_results_bottom.go)
  - Responsive behavior and DPI: [responsive_layout.go](walk/cmd/fc-walk/responsive_layout.go), [dpi_context.go](walk/cmd/fc-walk/dpi_context.go)
  - My Campaigns (MVP SOT and persistence): [my_campaigns_ui.go](walk/cmd/fc-walk/my_campaigns_ui.go), [my_campaigns_handlers.go](walk/cmd/fc-walk/my_campaigns_handlers.go), [campaigns_store.go](walk/cmd/fc-walk/campaigns_store.go)

- WinUI 3 MVP (C#):
  - App resources and theme: [App.xaml](winui3-mvp/FinancialCalculator.WinUI3/App.xaml)
  - Shell window (Deal Inputs, Tables, Insights): [MainWindow.xaml](winui3-mvp/FinancialCalculator.WinUI3/MainWindow.xaml)
  - Window setup (Mica, titlebar): [MainWindow.xaml.cs](winui3-mvp/FinancialCalculator.WinUI3/MainWindow.xaml.cs)
  - View model and API client: [MainViewModel.cs](winui3-mvp/FinancialCalculator.WinUI3/ViewModels/MainViewModel.cs)
  - Property change binding: [PropertyChangedHandlers.cs](winui3-mvp/FinancialCalculator.WinUI3/ViewModels/PropertyChangedHandlers.cs)
  - Helper: [StringEqualsToVisibilityConverter.cs](winui3-mvp/FinancialCalculator.WinUI3/Converters/StringEqualsToVisibilityConverter.cs)

MARK: Current State Snapshot

- Go Walk MVP (authoritative behavior today)
  - Has full tables for Default Campaigns and My Campaigns.
  - Implements progressive disclosure in Deal Inputs for My Campaign selection (edit mode).
  - Bottom panels (Campaign Details and Key Metrics) update on selection changes (see data-linking fixes).
  - Responsive and DPI logic validated.
  - Subdown column implemented and subsidy totals corrected. See:
    - [subdown-column-fix.md](docs/subdown-column-fix.md)
    - [data-linking-fix.md](docs/data-linking-fix.md)
  - Interactive Campaign Manager (SOT) established with persistence, copy-to-My Campaigns, and edit loop:
    - [interactive_campaign_manager_sot.md](docs/interactive_campaign_manager_sot.md)

- WinUI 3 shell (modern desktop UI, new)
  - Left pane: Deal Input Mask (Product, Term, Timing, Balloon, Lock Mode, Price/DP).
  - Right pane: two stacked tables (Standard Campaigns, My Campaigns) and an Insights area (Details Key Metrics and Cashflows) — structure present.
  - ViewModel has state for IDC Dealer Commission (auto/override), IDC Other, rate mode switching, Subsidy Budget, and collections for StandardCampaigns, MyCampaigns, Active selection, and Cashflows.
  - Backend endpoints in use:
    - GET api/v1/campaigns/catalog
    - POST api/v1/campaigns/summaries
    - POST api/v1/calculate
  - Status: compiles and runs; shell and bindings in place; deep wiring to engines (dealer commission policies, campaign catalog, and selected-row details) to be completed.

MARK: Target Architecture (UI and Engines)

- Decision-first UI
  - One deal entered once (left pane); Subsidy Budget and IDC treatment are explicit.
  - Center-right shows Standard (templates) and My Campaigns (user variants).
  - Selection drives:
    - Gross cost vs Subsidy Utilized vs Remaining (not netting).
    - Key metrics: Monthly, Nominal, Effective, Financed, Acq RoRAC, IDC breakdown.
    - Cashflows view.

- Engines and services
  - Campaigns engine produces rows with subsidy use, dealer commission context, and viability reasons:
    - Row attributes include: Monthly, Effective, DP amount, DealerComm THB and pct, Subsidy Utilized, Acq RoRAC, Notes.
  - Calculator provides quote (monthly, schedule, profitability).
  - Parameters service provides Dealer Commission policy (by product; extensible to term/DP%/channel).
  - Persistence: My Campaigns drafts (future in WinUI) mirroring Walk’s AppData JSON schema (MVP).

MARK: Spec vs Implementation Crosswalk (Gaps and Confirmations)

- Subsidy and IDCs (gross presentation)
  - Spec: Show cost and subsidy separately; cap subsidy at budget; Net effect for finance company in profitability only.
  - Walk: Implemented gross presentation for subdown and included in Subsidy utilized; My Campaigns supports combined adjustments. Confirmed via [subdown-column-fix.md](docs/subdown-column-fix.md) and [data-linking-fix.md](docs/data-linking-fix.md).
  - WinUI status: Grid columns present at a summary level; detailed gross breakdown and budget remaining to be computed shown in Insights; pending wiring.

- Dealer Commission auto policy
  - Spec: Auto: X% of financed amount with reset-to-auto and override.
  - Walk: Uses parameterized values (via engines or config) and displays in bottom panels.
  - WinUI: State model has auto/override; computation via parameters service not yet wired; currently uses summaries API fields.

- Data linkage (selection and panels)
  - Spec: Selecting Standard or My Campaign updates Campaign Details, Key Metrics, Cashflows.
  - Walk: Fixed (mutual exclusion, bottom panels update for both tables) — see [data-linking-fix.md](docs/data-linking-fix.md).
  - WinUI: Selections bound; RefreshActiveSelectionAsync prepares schedule mapping; needs confirmation on schedule JSON shape.

- Columns and sorting
  - Spec: Include Subdown, Cash Discount, Free Insurance, MBSP, Subsidy Used, Dealer, Acq RoRAC, Notes.
  - Walk: Implemented Subdown column; Free Insurance still flagged as missing in some places (see Known Issues).
  - WinUI: Current Standard and My Campaigns templates show Monthly, Eff, DP, Dealer, Subsidy, RoRAC; need Free Insurance MBSP columns and sorting logic.

- My Campaigns
  - Spec: Copy from Standard; editable adjustments; Save/Load/Clear; selections drive edit state.
  - Walk: Fully implemented per SOT with persistence and handlers.
  - WinUI: Collections exist; Copy action temporarily disabled during XAML stabilization; persistence not implemented.

- Cashflows
  - Spec: Cashflows for the selected row shown in bottom tab.
  - Walk: Present and wired; refresh on selection.
  - WinUI: ListView and mapping ready; needs binding to API schedule with safe parsing.

MARK: API Contract Alignment (WinUI ↔ Backend)

- Current WinUI API Client: [MainViewModel.cs](winui3-mvp/FinancialCalculator.WinUI3/ViewModels/MainViewModel.cs)
  - Endpoints
    - GET api/v1/campaigns/catalog ⇒ list of templates (id, type, funder, description, parameters).
    - POST api/v1/campaigns/summaries ⇒ returns CampaignSummaryDto with dealer commission pct amt.
    - POST api/v1/calculate ⇒ returns quote: monthly_installment, customer_rate_nominal, customer_rate_effective, schedule array balances, profitability.acquisition_rorac.
  - Required confirmations
    1) Summaries payload: confirm it also exposes sufficient per-row subsidy utilization fields needed for grid (Subdown, Free Insurance, MBSP, Cash Discount) or whether calculation is needed per row to derive Subsidy Utilized. If not provided, we need a richer summaries endpoint or a batch calculate.
    2) Calculate payload: confirm schedule array element shape: contains principal, interest, fees, balance, cashflow; WinUI now expects those keys when populating Cashflows.
    3) Parameters endpoint: Dealer Commission policy lookup endpoint (by product); currently not called from WinUI. Confirm path and response shape for policy version and percent.
    4) Row viability reasons: Do summaries provide reasons and flags for disabled rows? If yes, confirm property names to surface in Notes and disabled state.

MARK: Gaps Requiring Decisions

1) Summaries richness vs batch calculate
   - Option A: Expand api/v1/campaigns/summaries to include all display columns (Monthly, Eff, DP, Dealer pct amt, Subsidy Used split, Acq RoRAC, Notes, viability).
   - Option B: Keep summaries minimal; WinUI calls calculate per row (or batch) to derive all metrics. Tradeoff: latency vs server cost.

2) Dealer Commission policy wiring
   - Confirm endpoint and shape. Are there additional drivers (term, dp%) that must be included at MVP? Spec suggests product-level percent first.

3) Free Insurance column and IDC catalog consistency
   - Ensure both tables include Free Insurance column and that engines produce the values consistently for both Standard and My Campaigns.

4) Subsidy Remaining and breakdown
   - Where should Subsidy Remaining be computed? Engine or UI? Prefer engine for determinism and guardrails messaging.

5) My Campaigns persistence in WinUI
   - Follow Walk’s AppData schema v1 (see SOT) or introduce a platform-appropriate location. Recommendation: mirror schema for parity.

MARK: Execution Plan — Work Breakdown Structure

Epic A — Backend Contract Confirmation and Stubs (Blocking)
- A1. Confirm campaigns summaries schema has fields required by redesigned grid; if not, propose extended DTO (SubdownTHB, CashDiscountTHB, FreeInsuranceTHB, MBSPTHB, SubsidyUsedTHB, DealerPct, DealerAmt, RoRAC, Notes, ViabilityFlag, ViabilityReason).
- A2. Confirm calculate schedule item keys and profitability object shape; align WinUI mapping.
- A3. Confirm Dealer Commission policy endpoint and drivers (initially by Product; extendable).

Acceptance: One page “API Contract v1” with example JSON, and WinUI consuming code updated to new shape (or documented fallback to per-row calculate).

Epic B — WinUI Standard Campaigns Grid Completion
- B1. Add missing columns: Subdown, Free Insurance, MBSP, Cash Discount; ensure widths and order match specs. [DONE for Standard grid]
- B2. Implement default sorting: Monthly asc, then Effective asc. Add toggle (viable only). [Default sort implemented; viable toggle pending]
- B3. Display Notes and disabled row visuals with tooltip for reason.

Acceptance: Visual parity with spec grid; sort toggle works; disabled rows show reason.

Epic C — WinUI My Campaigns Parity
- C1. Re-enable “Copy to My Campaigns” action (Standard → My) with command route in row template.
- C2. Add persistence: Save All, Load, Clear using Walk’s JSON schema v1 under per-user app data path.
- C3. Implement edit-state progressive disclosure in Deal Inputs for selected My Campaign (subdown, cash discount, free insurance, free MBSP, idc other).

Acceptance: Copy flow works; reload survives app restart; editing updates row and insights live.

Epic D — Dealer Commission Auto/Override
- D1. Wire auto policy fetch on Product and compute resolved THB from Financed Amount; update pill (“auto: X percent THB Y”).
- D2. Add override editors (pct, amt) with precedence rules; add “Reset to auto”.
- D3. Ensure Cash Discount tiles always show Dealer 0 THB 0 percent per spec.

Acceptance: Switching products updates auto pill; overrides persist for session and reset works.

Epic E — Insights Panel (Details, Metrics, Cashflows)
- E1. Populate “Campaign Details” with gross costs, Subsidy Budget Utilized Remaining; show policy version and calc time.
- E2. Fill “Key Metrics” from calculate response; ensure feasibility feedback for Target Installment.
- E3. Cashflows: render schedule for selected row; ensure per-change recompute and binding.

Acceptance: Selecting a row updates all three; Target Installment infeasible shows guidance.

Epic F — Guardrails and UX Polish
- F1. Debounce inputs and show busy indicator during recompute.
- F2. Warnings and badges for unknown product or missing policy (Dealer Commission 0 percent fallback).
- F3. Accessibility: keyboard nav across grids and inputs; tooltips for disabled rows.

Acceptance: Smooth typing, no stutter; guardrails visible and non-blocking; a11y check basic pass.

MARK: Dependency Matrix

- A → B,C,D,E (API confirmation blocks deep wiring)
- D → E (Dealer commission display affects insights)
- C → E (My Campaign selection drives edit state and insights)

MARK: Proposed Milestones

- M1 Contracts Frozen (A1-A3)
- M2 Standard Grid Parity (B1-B3)
- M3 My Campaigns Parity Persistence and Edit State (C1-C3)
- M4 Dealer Commission Auto Override (D1-D3)
- M5 Insights Completed (E1-E3)
- M6 Guardrails Polish and A11y (F1-F3)

MARK: Clarifications Required (Decision List)

1) Summaries DTO scope (minimal vs rich): choose A or B strategy.
2) Policy endpoint path naming (e.g., GET api/v1/policy/dealer-commission?product=HP), and whether to return version string for display.
3) Target Installment feasibility calculation on backend or UI? Recommendation: backend suggests nearest feasible rate target.
4) Persist location for WinUI campaigns.json (AppData equivalent on Windows) and whether to share with Walk’s file.
5) Free Insurance “actual cost” source of truth: parameters or per-campaign template row? Confirm catalog schema.

MARK: Mermaid — UI Information Flow

flowchart LR
  DealInputs[Deal Inputs] --> Summaries[Campaigns Summaries API]
  Summaries --> StandardGrid[Standard Campaigns Grid]
  StandardGrid --> SelectStandard[Select Standard]
  MyCampaigns[My Campaigns List] --> SelectMy[Select My Campaign]
  SelectStandard --> Insights[Insights: Details, Metrics, Cashflows]
  SelectMy --> EditState[Edit Inputs for My Campaign]
  EditState --> Calculate[Calculate API]
  Calculate --> Insights
  Insights --> Guardrails[Warnings and Policy Badges]

MARK: Acceptance Criteria Summary (Per Epic)

- A: API contract doc committed; sample payloads; WinUI parsing aligned.
- B: Grid shows Subdown, Free Insurance, MBSP, Cash, Subsidy Used, Dealer, RoRAC, Notes; sorting and viability toggle.
- C: Copy action works; persistence roundtrip; edit state shows adjustments and live updates.
- D: Dealer Commission pill auto resolves and can be overridden; reset to auto consistent; Cash Discount zeroes dealer commission.
- E: Insights shows gross costs and subsidy tracking; metrics and cashflows recompute reliably; target installment feasibility UX added.
- F: Debounced UI; guardrail tooltips; keyboard navigation and basic a11y available.

MARK: Out-of-Scope (for this pass)

- Cloud sync of campaigns; team sharing.
- Advanced channel drivers for commissions.
- Print-ready PDF and advanced export formats (add after final metrics stabilization).

Status: Ready for execution per milestones. This file is the SOT until superseded.