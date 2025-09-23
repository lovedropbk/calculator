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