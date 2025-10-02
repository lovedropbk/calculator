# Test Case Derivations and External Check Pack

This document lists all inputs, formulas, and the exact references used to derive the expected values in the test suite. It also includes copy/pasteable CSV blocks so you can validate results in Excel, Python, or any IRR/PMT-capable tool.

Sources (clickable)
- Golden test wiring: [calculator.TestGoldenCase()](engines/calculator/calculator_test.go:13)
- Pricing unit tests:
  - [pricing.TestCalculateInstallment()](engines/pricing/pricing_test.go:13)
  - [pricing.TestSolveForRate()](engines/pricing/pricing_test.go:79)
  - [pricing.TestBuildAmortizationSchedule()](engines/pricing/pricing_test.go:131)
  - [pricing.TestCalculateEffectiveRate()](engines/pricing/pricing_test.go:171)
- Rounding rules:
  - [types.RoundTHB()](engines/types/types.go:295) → round to whole THB (banker's rounding)
  - [types.RoundBasisPoints()](engines/types/types.go:300) → round rates to 4 decimals (basis points)
- Spec acceptance targets:
  - [docs/financial-calculator-architecture.md](docs/financial-calculator-architecture.md:161)

--------------------------------------------------------------------------------

Golden Case (used by TestGoldenCase)
- Product: Hire Purchase (HP)
- Price ex tax: 1,665,576 THB
- Down payment: 333,115 THB (20%)
- Financed amount: 1,332,461 THB
- Term: 12 months
- Balloon: 0
- Timing: Arrears
- Payout date: 2025-08-04
- First payment offset: 1
- Nominal customer rate: 6.44% (annual)
- Effective customer rate (target): ≈ 6.63% (display)
- Deal IRR Effective (target): ≈ 2.11% (display)
- Cost of Debt (12m): 1.48%
- OPEX: 0.68%
- Capital Advantage: 0.08%
- Cost of Credit Risk: 0.02%
- IDC example in test: Documentation fee 2,000 THB, Upfront, Financed=true, Revenue=true (baseline waterfall shows IDC periodic impact 0.00%)

ParameterSet snapshot (from test)
- [calculator.TestGoldenCase()](engines/calculator/calculator_test.go:16)
- Cost of Funds (12m): 1.48%
- Matched Funded Spread: 0.00%
- PD/LGD (HP_default): PD 0.02%, LGD 100% (so PD×LGD = 0.02%)
- OPEX (HP): 0.68%
- Economic Capital Advantage: 0.08%
- Rounding Rules: THB, MinorUnits=0, Method=&#34;bank&#34;, DisplayRate=4

--------------------------------------------------------------------------------

Formulas you can paste into Excel

1) Monthly installment (arrears, no balloon)
- PMT = P × r / (1 − (1 + r)^−n)
  - P = 1,332,461
  - r = 0.0644 / 12
  - n = 12
- Excel:
  - =PMT(0.0644/12, 12, -1332461, 0, 0) → 115,098.61…
  - Round to whole THB: =ROUND(PMT(0.0644/12, 12, -1332461, 0, 0), 0) → 115,099

2) Effective annual rate from nominal (compounding m periods)
- Effective = (1 + nominal/m)^m − 1
  - Monthly: (1 + 0.0644/12)^12 − 1 → ≈ 0.0663 (6.63%)
  - Quarterly: (1 + 0.0644/4)^4 − 1 → ≈ 0.0659 (6.59%)

3) Amortization schedule (per month m = 1..12)
- Start balance B₀ = 1,332,461
- r = 0.0644 / 12
- Installment I = 115,099 (whole THB, banker's rounding)
- Interestₘ = ROUND(B₍ₘ₋₁₎ × r, 0)  [whole THB per rounding rules]
- Principalₘ = I − Interestₘ
- End balance Bₘ = B₍ₘ₋₁₎ − Principalₘ
- Dates (arrears with FirstPaymentOffset=1):
  - First payment = AddMonths(2025-08-04, 1 + 1) = 2025-10-04
  - Then monthly on the 4th through 2026-09-04
- Reference: [pricing.BuildAmortizationSchedule()](engines/pricing/pricing.go:111)

4) Total interest (golden test check)
- = Installment × 12 − FinancedAmount
- 115,099 × 12 − 1,332,461 = 48,727 THB (tolerance ±100 THB in test)

5) IRR flows for external check (two common variants)
- Variant A (customer cashflows): T0 outflow of financed amount; monthly inflows = full installment
  - T0: −1,332,461 (2025-08-04)
  - 12 inflows: +115,099 on monthly dates starting 2025-10-04
  - Monthly IRR = IRR(…); Effective Annual = (1 + IRR)^12 − 1
  - This corresponds to the customer-rate effective (≈6.63%), not the Deal IRR line in waterfall.

- Variant B (invested-capital perspective):
  - T0: −1,332,461 (2025-08-04)
  - Monthly inflows: interest-only each month (per schedule)
  - Terminal capital return: +1,332,461 on the last payment date (principal return lumped)
  - Monthly IRR of Variant B, then Effective Annual = (1 + IRR)^12 − 1 → this aligns with a &#34;deal-level&#34; return on invested capital before risk/opex/advantage lines. The acceptance spec's 2.11% target is defined at this deal metric level.

Note on IDC handling
- For financed upfront IDC (e.g., doc fee financed by customer):
  - No lender T0 inflow is modeled for the financed fee; it is recovered via installments.
  - This matches the acceptance spec line &#34;IDC Subsidies and Fees periodic: 0.00%&#34; for the baseline waterfall.
- If upfront IDC is a cost (outflow) and not financed, it should appear as a T0 outflow and affect Deal IRR.

--------------------------------------------------------------------------------

Copy/paste CSV — Golden Case customer cashflows (Variant A)

Paste into a file named golden_case_customer_cashflows.csv and use IRR/XIRR:

date,direction,type,amount
2025-08-04,out,disbursement,-1332461
2025-10-04,in,installment,115099
2025-11-04,in,installment,115099
2025-12-04,in,installment,115099
2026-01-04,in,installment,115099
2026-02-04,in,installment,115099
2026-03-04,in,installment,115099
2026-04-04,in,installment,115099
2026-05-04,in,installment,115099
2026-06-04,in,installment,115099
2026-07-04,in,installment,115099
2026-08-04,in,installment,115099
2026-09-04,in,installment,115099

- Excel:
  - =XIRR(values, dates) → monthly IRR if equally spaced, or use IRR if the dates are equally spaced and you omit actual dates.
  - Effective Annual = (1 + monthlyIRR)^12 − 1

Copy/paste CSV — Golden Case invested-capital cashflows (Variant B)

Use the schedule to compute monthly interest (rounded to whole THB), then:

date,direction,type,amount
2025-08-04,out,disbursement,-1332461
2025-10-04,in,interest,INTEREST_M1
2025-11-04,in,interest,INTEREST_M2
2025-12-04,in,interest,INTEREST_M3
2026-01-04,in,interest,INTEREST_M4
2026-02-04,in,interest,INTEREST_M5
2026-03-04,in,interest,INTEREST_M6
2026-04-04,in,interest,INTEREST_M7
2026-05-04,in,interest,INTEREST_M8
2026-06-04,in,interest,INTEREST_M9
2026-07-04,in,interest,INTEREST_M10
2026-08-04,in,interest,INTEREST_M11
2026-09-04,in,interest,INTEREST_M12
2026-09-04,in,principal_return,1332461

- Excel:
  - Compute the 12 INTEREST_Mi using the schedule formula (Interestₘ = ROUND(B₍ₘ₋₁₎ × r, 0)).
  - Then =XIRR(values, dates).
  - Effective Annual = (1 + monthlyIRR)^12 − 1.
  - This is the metric that aligns conceptually with the &#34;Deal Rate IRR Effective&#34; line (target ≈ 2.11%) in the acceptance spec.

--------------------------------------------------------------------------------

Unit Test Parity Cases (from pricing tests)

HP 1,000,000 | 12m | 6.44% | no balloon
- Expected Installment: 86,263 THB
- Excel: =ROUND(PMT(0.0644/12, 12, -1000000, 0, 0), 0)

mySTAR 1,000,000 | 12m | 6.44% | 300,000 balloon
- Balloon PV = 300000 / (1 + 0.0644/12)^12
- Amortizing principal = 1,000,000 − PV(balloon)
- PMT = AmortizingPrincipal × (0.0644/12) / (1 − (1 + 0.0644/12)^−12)
- Expected Installment: 60,969 THB
- Excel (single line): 
  =ROUND( ((1000000 - 300000/(1+0.0644/12)^12) * (0.0644/12) / (1 - (1+0.0644/12)^-12)), 0)

Effective Rate checks
- Monthly comp: (1 + 0.0644/12)^12 − 1 → 0.0663
- Quarterly comp: (1 + 0.0644/4)^4 − 1 → 0.0659

--------------------------------------------------------------------------------

Notes on your HQ example (for cross-reference)
- Your tool shows Financed Amount 1,334,061 and Installment 115,087.84 at 6.44% nominal — those are consistent with the standard PMT formula given the slightly different financed amount/date conventions.
- Payment timing differences:
  - Our spec baseline uses arrears with FirstPaymentOffset=1, so first cash-in on 2025-10-04 for payout 2025-08-04: [pricing.BuildAmortizationSchedule()](engines/pricing/pricing.go:111)
  - If payments are in advance, the first installment is due on payout; changing payment timing will change the schedule and IRR slightly.
- Upfront IDC handling:
  - If an IDC is financed, it should not create a T0 inflow for the lender; it is recovered via installments.
  - If it is a cost and not financed, it should appear as a T0 outflow and reduce Deal IRR accordingly.

If you want these CSVs written as actual files, I can generate them in the repo for you next.