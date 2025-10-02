# Financial Calculator - UI Redesign Draft

Purpose: decision-first layout that lets sales enter a single Subsidy budget and instantly compare campaign options, while making IDCs explicit and overridable.

Author: Roo
Date: 2025-09-19

## Objectives

- Provide a top-level Subsidy budget input used to compute candidate campaign options and their key KPIs.
- Show a campaign comparison area immediately below the budget.
- Selecting a campaign reveals its details and full KPIs on the right.
- "IDCs - Dealer Commissions" auto-calculated from Product (HP, Finance Lease, etc.) with click-to-override and reset to auto.
- "IDCs - Other" free-entry bucket used for any other IDCs; when a campaign is selected, its cost is populated here by default.
- Guardrails enforce deal constraints; non-viable campaigns are greyed out with reasons.

## High-level Layout (ASCII wireframe)

+----------------------------------------------------------------------------------------------+
| Financial Calculator                                                                         |
+----------------------------------------------------------------------------------------------+
| Product & Subsidy                                                                            |
| ------------------------------------------------------------------------------------------- |
| Product: [ HP v ]   Term: [ 36 ]   Timing: [ arrears v ]   Balloon %: [ 0.0 ]               |
| Lock: (*) Amount  ( ) Percent   Down payment: [ 200,000 ] (THB)                             |
|                                                                                              |
| Subsidy budget (THB): [ 100,000 ]    IDCs - Dealer Commissions: [ auto: 1.50% (THB 12,000) ] (click to override)|
|                                                                                              |
| IDCs - Other (THB): [ 0 ]  (Used for campaign fees and any other IDC costs)                  |
| Rate Mode: (*) Fixed Rate  ( ) Target Installment   Customer Rate % p.a.: [ 3.99 ]          |
+----------------------------------------------------------------------------------------------+
| Campaign Options (driven by Subsidy budget and deal inputs)                                  |
| Select one to see details at right                                                           |
| ------------------------------------------------------------------------------------------- |
| ( ) Subdown      | Monthly: 23,616 (eff 4.06%) | Downpayment: 200,000 (20%) | Subsidy / Acq.RoRAC: 60,000 / 13% |
| ( ) Subinterest  | Monthly: 22,950 (eff 3.85%) | Downpayment: 200,000 (20%) | Subsidy / Acq.RoRAC: 95,000 / 12.9% |
| ( ) Free Insur.  | Monthly: 23,990 (eff 4.20%) | Downpayment: 200,000 (20%) | Subsidy / Acq.RoRAC: 80,000 / 12.1% |
| ( ) Free MBSP    | Monthly: 23,920 (eff 4.18%) | Downpayment: 200,000 (20%) | Subsidy / Acq.RoRAC: 75,000 / 12.3% |
| ( ) Cash Discount| Monthly: 24,200 (eff 4.35%) | Downpayment: 200,000 (20%) | Subsidy / Acq.RoRAC: 100,000 / 11.7% |
| ------------------------------------------------------------------------------------------- |
| Note: "Subsidy" will populate IDCs - Other automatically when selected; initial value equals Subsidy budget but can be edited per row. |
+-------------------------------------------+--------------------------------------------------+
| Campaign Details                          | Key Metrics & Summary                            |
| (contextual, appears when a row selected)  | (recomputes for selection)                       |
| ----------------------------------------  | -----------------------------------------------  |
| Campaign: Subinterest                      | Monthly Installment:   THB 22,950                |
| Summary: Uses subsidy to buy-down rate.    | Nominal Customer Rate: 3.50%                     |
|                                            | Effective Rate:        3.85%                     |
| Configurable parameters (if allowed):      | Financed Amount:       THB 800,000               |
|  - Buy-down bps: [ 49 ]                    | Acquisition RoRAC:     3.67%                     |
|  - Insurance:    [ none ]                  | IDC Total:             THB 95,000                |
|  - Fees:          calculated                | IDC - Dealer Comm.:    THB 12,000 (auto/overr.)  |
|                                            | IDC - Other:           THB 83,000                |
|                                            | Parameter Version:      2025.08                  |
|                                            | Calc Time:              1 ms                     |
+-------------------------------------------+--------------------------------------------------+
| [ Recalculate ]   [ Save Scenario ]   [ Export PDF ]                                         |
+----------------------------------------------------------------------------------------------+


## Interaction Model

- Product changes:
  - Recompute "IDCs - Dealer Commissions" auto value from the parameters-backed commission policy (product-level percent). If policy or product is missing, default to 0% and surface a non-blocking warning badge.
  - If previously overridden, keep the override but show "overridden" with a "Reset to auto" control.
- Subsidy budget:
  - Requests candidate campaign options from the campaign engine; options exceeding budget are disabled with reason tooltips.
  - Updates KPIs in the grid in real time (debounced).
- Campaign selection:
  - Sets "IDCs - Other" to the selected campaign's Subsidy by default.
  - If "IDCs - Other" was user-edited, prompt to replace with the campaign's Subsidy amount (Yes/No).
  - Shows right-side details and recomputes full metrics for the selection.
- IDCs - Dealer Commissions:
  - Displayed as a pill: "auto: X% (THB Y)" sourced from the commission policy. Click to toggle override editor (percent or amount).
  - Provide "Reset to auto" button and indicate overridden state.
- IDCs - Other:
  - Free numeric input; used for campaign fees and miscellaneous IDCs.
  - When campaign changes, behavior as above (auto-fill unless user-edited).
- Rate Mode:
  - Fixed Rate: show Customer Rate; hide Target Installment.
  - Target Installment: show Target Installment; hide Customer Rate; recompute feasibility.

## Campaign Options Grid

Columns:
- Select (radio)
- Campaign
- Monthly Installment (with Effective Rate in brackets)
- Downpayment Amount (and % of vehicle price)
- Dealer Comm.: THB X (Y%) [policy-based; falls back to 0% if unknown]
- Subsidy / Acq.RoRAC (Subsidy maps to IDCs - Other; initial value = Subsidy budget)
- Notes (reasons, warnings, inclusions like "free insurance")

Note: Cash Discount shows Dealer Comm.: THB 0 (0%).

Sorting and filters:
- Default sort by Monthly Installment ascending.
- Secondary sort by Effective Rate.
- Toggle to show only viable campaigns within budget.

## IDCs Logic

- Dealer Commissions:
  - Auto-calculated from the commission policy stored in the parameters service (product-level percent; extensible later for term, DP%, timing, channel).
  - Base amount = Financed Amount = Vehicle Price − Down Payment (aligns with engines). Resolved amount rounds to nearest THB.
  - Overrides: percent or amount; amount takes precedence if both provided; "Reset to auto" restores policy-based value; overrides persist for the session.
  - Fallbacks and guardrails: unknown product or missing policy defaults to 0%; negative values disallowed; Cash Discount tiles always show THB 0 (0%).
- Other:
  - Holds the selected campaign subsidy amount when a campaign is selected.
  - May be edited freely; flagged as "user-edited" to control auto-fill behavior on campaign change.
- IDC Total = Dealer Commissions (amount) + Other.

## State Model (conceptual)

- DealState
  - Product, Term, Timing, Balloon, DownPayment, LockMode, RateMode, CustomerRate, TargetInstallment
  - SubsidyBudget
  - DealerCommission: { mode: auto|override, pct?: number, amt?: number, resolvedAmt: number }
  - IDC_Other: { value: number, userEdited: boolean }
  - SelectedCampaign?: CampaignId
- Derived
  - CampaignSummaries[] from campaigns engine (budget-applied), including dealerCommissionAmt and dealerCommissionPct for tile display
  - SelectedCampaignDetail
  - Metrics from pricing/cashflow/profitability

## Data Flow

1) Inputs change -> validate -> throttle/debounce -> recompute auto Dealer Commission resolvedAmt (if mode=auto) -> query campaigns engine for summaries (include per-option Dealer Commission).
2) Grid updates including Dealer Comm.: THB X (Y%); disabled rows show reasons when not viable.
3) Selecting a campaign sets IDC_Other from the selected row's Subsidy and triggers full recompute via pricing/cashflow/profitability.
4) Right pane shows computed KPIs and details; IDC Total remains Dealer Commission + Other with both components shown.

## Validation & Guardrails

- Numeric ranges checked inline; show unit-aware messages.
- For Target Installment, compute feasibility; if infeasible, suggest nearest feasible rate.
- Budget guardrails: campaign Subsidy must not exceed Subsidy budget; if combined IDCs exceed policy caps, show warnings and disable actions as needed.

## Commission Policy Defaults

- Dealer Commission percents are parameterized and versioned under the parameters service (commissionPolicy).
- Values shown in this document and wireframes are illustrative. Actual defaults come from the active ParameterSet version and may change via parameter sync.
- The UI surfaces the active ParameterSet version in the header; Dealer Commission auto values reflect that same version.

## Walk UI Implementation Plan (phased)

Phase 1 - Structure
- Replace campaign checkboxes with a ListView grid and radio selection.
- Add "Subsidy budget" input and the IDCs section (Dealer Commissions with auto/override; Other).
- Add right pane Splitter with "Key Metrics" and "Campaign Details".

Phase 2 - Wiring
- Introduce DealState view-model; bind form fields.
- Implement Dealer Commission auto lookup from parameters by product; support override.
- Integrate campaigns engine to produce CampaignSummaries given Subsidy budget and DealState.

Phase 3 - Metrics and Details
- On selection, compute metrics using pricing/cashflow/profitability with campaign modifiers.
- Populate right pane; update IDC_Other default and user-edited logic.

Phase 4 - UX Polish
- Debounce recomputes; show loading and warnings.
- Add Export/Save actions; print-ready summary.
- Keyboard shortcuts and accessibility improvements.

## Open Questions

- Dealer Commission policy exact drivers (product-only vs. product+term+DP%+channel?).
- Campaign catalog: which parameters are user-tunable per campaign vs. fixed?
- Are there global caps for total IDCs by product or deal size?
- Should campaign cost ever be split between Dealer Commission and Other?

## Notes

- "Subsidy" in the grid is written to "IDCs - Other". Users can override afterward.
- The right pane is authoritative for the currently selected campaign. The top headline can mirror Monthly Installment and Effective Rate for quick reading.
- Keep computations deterministic and traceable; surface parameter version and calc time for auditability.
## Implementation Addendum — WinUI 3 Backend Mapping, Inline Editing, and Foldable Metrics (Authoritative)

Decision
- Use backend engines for all calculations; WinUI 3 is a native UI shell with per-row calculate requests (Option A).
- Map C# DTOs to the engine JSON contract (snake_case) and parse schedule using engine field names.
- Provide default IDC costs for Free Insurance and MBSP from the campaign catalog; allow inline editing in both Standard and My Campaigns grids.
- Implement foldable Acquisition RoRAC and Campaign Metrics as a details section bound to engine profitability outputs.

References (authoritative contracts and behavior)
- Engine request/response DTOs: [engines/types/types.go](engines/types/types.go:78), [engines/types/types.go](engines/types/types.go:282)
- Cashflow fields: [engines/types/types.go](engines/types/types.go:135)
- Profitability Waterfall fields: [engines/types/types.go](engines/types/types.go:223)
- Campaign catalog maps insurance and MBSP costs: [internal/services/enginesvc/engine_service.go](internal/services/enginesvc/engine_service.go:89)
- Walk campaign rows and IDC handling for parity: [walk/cmd/fc-walk/ui_orchestrator.go](walk/cmd/fc-walk/ui_orchestrator.go:169)
- Cashflow table shaping in Walk (for display parity): [walk/cmd/fc-walk/cashflow_tab.go](walk/cmd/fc-walk/cashflow_tab.go:11)

1) API and JSON Mapping (WinUI 3)
- Requests:
  - CalculationRequest (POST /api/v1/calculate) fields (snake_case):
    - deal: product, price_ex_tax, down_payment_amount, down_payment_percent, down_payment_locked, financed_amount, term_months, balloon_percent, balloon_amount, timing, rate_mode, customer_nominal_rate, target_installment
    - campaigns: array of { id, type, parameters?, eligibility?, funder?, stacking? }
    - idc_items: array of IDC items (see IDC mapping below)
    - options: optional map (derive_idc_from_cf=true; add_subsidy_upfront_thb for subinterest)
  - Campaign Summaries (POST /api/v1/campaigns/summaries):
    - deal: same snake_case
    - state: { dealerCommission: { mode, pct?, amt?, resolvedAmt }, idcOther: { value, userEdited } } mapped to snake_case: dealerCommission → dealerCommission, idcOther → idcOther (fields inside keep camel for now unless server expects snake)
    - campaigns: from catalog (see below)
- Parsing:
  - quote.monthly_installment, quote.customer_rate_nominal, quote.customer_rate_effective
  - quote.schedule (periodic schedule as Cashflow): use principal, interest, amount, balance (there is no field named "cashflow")
  - quote.profitability: use fields listed in section 4
- Campaign Catalog (GET /api/v1/campaigns/catalog):
  - For Free Insurance and Free MBSP default costs, read parameters insurance_cost and mbsp_cost from catalog item or mapped fields [internal/services/enginesvc/engine_service.go](internal/services/enginesvc/engine_service.go:89).

2) Default IDC Cost Mapping (Free Insurance and MBSP)
- Source of defaults:
  - Free Insurance (campaign type free_insurance): default cost = insurance_cost from catalog item; if missing, treat default as 0.
  - Free MBSP (campaign type free_mbsp): default cost = mbsp_cost from catalog item; if missing, treat default as 0.
- Write path into per-row state:
  - Standard Campaigns row: initialize editable "Free Insurance THB" and "MBSP THB" cells with these defaults.
  - My Campaigns row: when copying, carry these values into the draft’s adjustments where applicable (FreeInsuranceCostTHB, FreeMBSPCostTHB).

3) Inline Editing Rules (Standard and My Campaigns)
- Editable columns:
  - Free Insurance THB (number, ≥ 0)
  - MBSP THB (number, ≥ 0)
  - Subdown THB (number, ≥ 0)
  - Cash Discount THB (number, ≥ 0)
  - Notes (free text)
- Behavior:
  - Standard Campaigns:
    - Inline edits are ephemeral (session-scoped). They immediately trigger a per-row calculate using edited values as part of IDC items (see section 5), but are not persisted across app restarts.
    - When "Copy to My Campaigns" is used, the edited values are carried into the new draft.
  - My Campaigns:
    - Inline edits update the selected CampaignDraft.Adjustments fields and persist on Save All (AppData JSON v1 schema mirrors Walk; see [walk/cmd/fc-walk/campaigns_store.go](walk/cmd/fc-walk/campaigns_store.go:21)).
- Validation:
  - Reject negatives; clamp to 0 and show a lightweight tooltip.
  - Very large numbers allowed but display warning if exceeding reasonable budget caps (UX only).

4) Foldable "Acquisition RoRAC and Campaign Metrics" Panel (Details)
- Location: Right pane, collapsible/expandable panel below Key Metrics.
- Data: Bind from quote.profitability fields:
  - Deal IRR Effective (deal_irr_effective)
  - Deal IRR Nominal (deal_irr_nominal)
  - Cost of Debt Matched (cost_of_debt_matched)
  - Matched Funded Spread (matched_funded_spread)
  - Gross Interest Margin (gross_interest_margin)
  - Capital Advantage (capital_advantage)
  - Net Interest Margin (net_interest_margin)
  - Cost of Credit Risk (cost_of_credit_risk)
  - OPEX (opex)
  - IDC Upfront Cost Pct (idc_upfront_cost_pct), Subsidy Upfront Pct (subsidy_upfront_pct)
  - IDC Periodic Pct (idc_subsidies_fees_periodic or idc_periodic_cost_pct when available), Subsidy Periodic Pct (subsidy_periodic_pct)
  - Net EBIT Margin (net_ebit_margin)
  - Economic Capital (economic_capital)
  - Acquisition RoRAC (acquisition_rorac)
- UX:
  - Panel shows human-friendly percent formatting. "—" when zero or not available.
  - Header summarizes Monthly Installment and RoRAC to mirror the selected row headline.

5) IDC Items Construction for Calculate Requests
- Always include derive_idc_from_cf=true in options.
- Dealer Commission (if resolved THB > 0):
  - Add IDC item:
    - category: broker_commission
    - amount: resolved THB
    - financed: false
    - timing: upfront
    - is_revenue: false
    - is_cost: true
    - description: "Dealer commission"
- Other IDC (campaign costs and manual bucket):
  - For Subdown and Cash Discount: treat value as an upfront "subsidy" inflow rather than IDC cost when applicable:
    - Use options.add_subsidy_upfront_thb = subdownTHB or cashDiscountTHB for the subinterest-like impacts when applicable.
    - If not subinterest/cash-discount logic: write as IDC admin_fee (cost) instead as UI fallback.
  - For Free Insurance and Free MBSP editable THB:
    - Add IDC item(s) with category: admin_fee (or product-specific category when introduced), amount = edited THB, financed=false, timing=upfront, is_cost=true.
  - IDCs - Other (global input):
    - Add as separate IDC item with category: admin_fee, timing=upfront, is_cost=true.
- Subinterest (rate buy-down):
  - When computing subinterest rows, set options.add_subsidy_upfront_thb to the subsidy used amount to reflect periodic subsidy impacts in profitability; see Walk solver flow [walk/cmd/fc-walk/ui_orchestrator.go](walk/cmd/fc-walk/ui_orchestrator.go:407).
- Cash Discount:
  - Force Dealer Commission to 0% (engine summaries already enforce this). No dealer IDC item is sent.

6) Campaign Tile Metric Population (Per-row Calculate)
- For each Standard row, perform a per-row calculate with the row’s edited values and IDC items composed as above.
- Populate:
  - Monthly Installment (quote.monthly_installment)
  - Effective Rate (quote.customer_rate_effective)
  - Downpayment string "THB X (Y% DP)" from inputs
  - Dealer Commission label "THB X (Y%)" using auto policy or overrides; Cash Discount forced to "THB 0 (0%)"
  - Subsidy / RoRAC: "THB S / R%" where S is budget or edited subsidy used; R is acquisition_rorac (quote.profitability.acquisition_rorac)
- Sorting defaults: Monthly ascending, then Effective ascending.
- Toggle "Viable only": hides rows that exceed budget (server viability optional; else UI heuristics).

7) Dealer Commission Auto Policy
- Endpoint: GET /api/v1/commission/auto?product={HP|mySTAR|F-Lease|Op-Lease}
- Display "auto: X% (THB Y)" pill where:
  - X = percent from endpoint
  - Y = Financed Amount × X (rounded to THB)
- Override:
  - Either Pct or Amt; Amt has precedence.
  - "Reset to auto" clears overrides and recomputes resolved Amt.
- Policy version:
  - Show policyVersion returned by endpoint in a tooltip or beside the pill.
- Backend references: [cmd/fc-api/main.go](cmd/fc-api/main.go:48)

8) Standard vs My Campaigns Semantics
- Standard Campaigns:
  - Read-only templates except for inline-editable cost columns (Subdown, Cash Discount, Free Insurance THB, MBSP THB, Notes).
  - Edits are session-scoped (not persisted). Copying to My Campaigns transfers edited values.
- My Campaigns:
  - Fully editable; persisted via AppData JSON (schema v1).
  - Save/Load/Clear maps to per-user file per Walk (see [walk/cmd/fc-walk/campaigns_store.go](walk/cmd/fc-walk/campaigns_store.go:84)).

9) Insights: Gross Presentation and Subsidy Tracking
- Compute and display (for selected row):
  - Subsidy Budget (from input), Subsidy Utilized (from per-row calculation or from options), Remaining = max(0, Budget − Utilized).
  - IDC Total = Dealer Commission (resolved) + Other (including Free Insurance THB and MBSP THB).
- Parameter Version and Calc Time:
  - Fetch parameter_set.version via GET /api/v1/parameters/current and display.
  - If calculate response includes metadata timestamps, show "Calc Time" summary.

10) UX, Performance, and Accessibility
- Debounce recomputes by ~300 ms on input changes.
- Avoid UI stalls by updating tiles with a "busy" shimmer while awaiting per-row calculate.
- Keyboard navigation: Enter commits inline edit; Esc cancels; Tab moves across editable cells.
- Tooltips for disabled rows and policy notes.
- Minimum contrast and content labels for screen readers.

11) Acceptance Criteria (WinUI 3)
- Standard grid inline edits to Free Insurance THB / MBSP THB update the row’s Monthly, Effective, RoRAC within 300–600 ms.
- Copying a Standard row with edits creates a My Campaign draft carrying the edited costs and recomputes metrics.
- Foldable metrics show the profitability waterfall fields and update on selection changes.
- Dealer Commission pill reflects policy percent and resolved THB; override and reset work.
- Insights show correct Subsidy Budget/Utilized/Remaining and IDC components consistent with engine results.

Open Items (tracked separately)
- Viability flags and reasons in /campaigns/summaries (server authoritative), otherwise client heuristics.
- Optional richer summaries to reduce N×calculate calls.
- Category specialization for Free Insurance and MBSP IDC items if/when introduced.