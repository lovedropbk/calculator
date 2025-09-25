# Profitability Modeling v2 — Annualized, Comparable Components

Status: Implemented in code (see file references at end).  
Scope: Align Acquisition RoRAC and Campaign Options table to annualized, fully comparable basis with strict separation of IDC costs vs Subsidy incomes.

MARK: Why
- Full comparability: All rows/columns that express profitability components are on an annualized basis, enabling apples-to-apples evaluation versus Deal IRR.
- Clarification of terms:
  - Deal IRR is the single annualized IRR of the whole cashflow stream.
  - IDC Upfront (cost) % and Subsidy Upfront (income) % are annualized equivalents (rate-form) derived from T0 cashflows over ALB (Average Loan Balance).
  - These rate-form components stack with other annualized components (e.g., Net Interest Margin) to derive RoRAC.
- Strict separation of costs vs incomes:
  - IDC lines are costs (no “IDC revenue”).
  - Subsidies are incomes. Upfront subsidies are shown as a positive annualized rate in “Subsidy Upfront (income) %”.
- Deal inputs are authoritative:
  - “IDC - Other (THB)” is always treated as an upfront T0 IDC cost (non-financed).
  - “Financed IDCs” input does not exist yet (future optional enhancement).

MARK: Data Model Changes (v2)
- ProfitabilityWaterfall (new fields)
  - IDCUpfront (cost) %: Annualized rate of all upfront IDC costs at T0 derived on ALB basis.
  - Subsidy Upfront (income) %: Annualized rate of all upfront subsidy income at T0 on ALB basis.
  - IDCPeriodicCostPct, SubsidyPeriodicPct: Reserved; currently zero in MVP (periodic modeling later).
  - Legacy compatibility: IDCSubsidiesFeesUpfront and IDCSubsidiesFeesPeriodic retained as “net” lines (income − cost). Engines continue using these in NIM→EBIT plumbing, while UIs use the separated fields for display clarity.
- Request Options recognized by calculator engine
  - derive_idc_from_cf: when true, the calculator derives ALB and computes annualized upfront components (separated costs vs subsidy).
  - add_subsidy_upfront_thb: injects a synthetic T0 subsidy inflow (used for “Base (subsidy included)” and other benchmark rows).
  - add_subsidy_periodic_thb: retained for Subinterest/Free Insurance/MBSP modeling variants that treat subsidy as periodic support (MVP).

MARK: Annualized Conversion (ALB basis)
- Numerators (both rounded to THB per spec rounding):
  - IDCUpfrontCostPct numerator: sum of T0 IDC outflows (direction=out, type=IDC) at PayoutDate T0.
  - SubsidyUpfrontPct numerator: sum of T0 Subsidy inflows (direction=in, type=Subsidy) at PayoutDate T0.
- Denominator (ALB):
  - Monthly start-of-period balances from the rounded periodic schedule are summed and averaged (term-months). When no schedule entries exist, fallback to FinancedAmount. ALB aligns with HQ methodology.
- Sign convention:
  - Cost lines are positive “cost” rates (displayed as positive absolute values with a negative economic impact).
  - Subsidy lines are positive “income” rates (increase to margin stack).
- Net provided to EBIT/EBITDA staging:
  - NetUpfront = SubsidyUpfrontPct − IDCUpfrontCostPct
  - NetPeriodic = SubsidyPeriodicPct − IDCPeriodicCostPct
  - Net values feed into NetEBIT margin along with OPEX, CoR, HQ add-on.

MARK: Campaign Table Reconfiguration
Rows (reference logic implemented):
1) Base (no subsidy)
   - IDC: Dealer Commission + IDC - Other as T0 IDC costs.
   - Subsidy: ignored (0).
   - Outputs: Subsidy Upfront % = 0; IDC Upfront (cost) % reflects true costs.

2) Base (subsidy included)
   - IDC: Dealer Commission + IDC - Other as T0 IDC costs.
   - Subsidy: full Subsidy Budget injected as T0 subsidy inflow (income).
   - Outputs: Both new lines show true separation; RoRAC reflects IDC cost vs subsidy income.

3) Cash Discount (reference only)
   - Entire Subsidy Budget used as cash discount off the vehicle price (effective cash price in Downpayment column).
   - No IDCs (Dealer Commission, IDC - Other) applied.
   - Financing columns blank (—). Notes: “No financing (reference only)”.
   - New “Cash Discount” column shows the discount THB.

4) Subdown
   - Subsidy added to down payment; no T0 subsidy inflow created.
   - IDC: Dealer Commission + IDC - Other as T0 IDC cost.
   - Output: Lower Monthly; Subsidy/Acq.RoRAC column shows “THB used / RoRAC”.

5) Subinterest
   - Subsidy buys down the interest rate (solver budgeted or clipped to floor).
   - IDC: Dealer Commission + IDC - Other as T0 IDC cost.
   - Subsidy: “Used” subsidy recognized; modeled as periodic support in current MVP for parity with existing solver workflows.
   - Output: Lower Monthly; Subsidy/Acq.RoRAC shows “THB used / RoRAC”.

6) Free Insurance
   - IDC: Dealer Commission + IDC - Other + Placeholder IDC (50,000 THB).
   - Subsidy: Full Subsidy Budget treated as income (modeled periodic in MVP).
   - Output: Customer rates unchanged; RoRAC reflects net impact.

7) Free MBSP
   - IDC: Dealer Commission + IDC - Other + Placeholder IDC (150,000 THB).
   - Subsidy: Full Subsidy Budget treated as income (periodic in MVP).
   - Output: Customer rates unchanged; RoRAC reflects net impact.

MARK: UI Changes
- Profitability Details panel split:
  - “IDC Upfront (cost) %”: annualized rate from T0 cost components.
  - “Subsidy Upfront (income) %”: annualized rate from T0 subsidy inflows.
  - Periodic lines (net or split) remain placeholders for future.
- Campaign Options table:
  - New column “Cash Discount”.
  - Populated only for Cash Discount row as “THB X”, “—” elsewhere (phase 1).
- Dealer Commission & IDC - Other:
  - Dealer Commission resolved/presented per policy or override (already v1).
  - IDC - Other is always added as T0 IDC cost (non-financed) in every financed scenario (Base, Subdown, Subinterest, Free Insurance, Free MBSP).

MARK: Engine Integration Summary
- Profitability engine:
  - Accepts annualized net IDC/Subsidy lines (unchanged function signature).
  - Computes NIM on nominal annual basis; RoRAC remains NetEBIT / EconomicCapital.
  - New separated fields surfaced for UI (kept backward-compatible net fields).
- Calculator engine:
  - Derives ALB and separated upfront components when “derive_idc_from_cf” is true.
  - Supports synthetic subsidy T0 injection for “Base (subsidy included)” via “add_subsidy_upfront_thb”.
  - Supports periodic subsidy addition via “add_subsidy_periodic_thb” (for Subinterest/Free* flows in MVP).
- Campaign engine:
  - Adds Base benchmark types and stacking; “Base (subsidy included)” injects synthetic T0 subsidy via the new option.
  - Cash Discount row treated as reference-only (no financing metrics).

MARK: Verification Plan
1) Build and run:
   - go build ./...
2) Sanity checks in the UI:
   - Toggle Base (no subsidy) and Base (subsidy included) and verify:
     - IDC Upfront (cost) % increases as Dealer Commission / IDC - Other increase.
     - Subsidy Upfront (income) % is zero for Base (no subsidy), positive for Base (subsidy included).
     - Cash Discount row shows only “Cash Discount” column populated and all financing metrics “—”.
3) Reference tests:
   - Manually run four reference test cases from docs/reference_tests_hq_results.json in the UI.
   - Compare display values; document deviations by screenshot or CSV; confirm consistent annualized treatment.
4) Edge cases:
   - Zero Budget, zero IDC Other, and short terms (6m) with rounding; confirm stable ALB denominators and no divide-by-zero.

MARK: Future Enhancements
- Add “Financed” toggle per IDC item. When financed, include cost in principal rather than T0 cashflow and exclude from IDC Upfront (cost) %.
- Implement periodic split derivation: IDCPeriodicCostPct and SubsidyPeriodicPct, enabling separated periodic lines in the waterfall details.
- Extend XLSX export to include the two new separated lines in the summary sheet.

MARK: Code Artifacts
- Engines (data model and plumbing)
  - engines/types/types.go
  - engines/calculator/calculator.go
  - engines/campaigns/campaigns.go
  - engines/profitability/profitability.go (unchanged signature; nominal basis confirmed)
- UI (Walk)
  - walk/cmd/fc-walk/main.go
  - walk/cmd/fc-walk/ui_orchestrator.go
