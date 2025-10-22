# DCF Model Architecture

**Date:** 2025-10-22
**Based on:** `hq_logic_vba.md` (Legacy VBA)

## Overview

The goal is to replace the simplified annualized margin RoRAC calculation in `FinancialCalculator.Engine` with a period-by-period Discounted Cash Flow (DCF) model that aligns with the legacy VBA implementation.

## 1. Core Components

### 1.1. Yield Curve & Interpolation (`YieldCurve.cs`)
Must port logic from `DCF_Excel.bas`.
- **Linear Interpolation**: Used for short-term or extrapolation if needed.
  - Formula: `y = y1 + (y2 - y1) * (x - x1) / (x2 - x1)`
- **Exponential Interpolation**: Used between grid points on the yield curve.
  - Formula: `y = Exp( (lambda * Ln(y1) + (1 - lambda) * Ln(y2)) )` where `lambda = (x2 - x) / (x2 - x1)`
  - *Note*: The VBA `fct_Exponential` implementation needs careful checking. It seems to interpolate the *Discount Factor* or *Rate* exponentially.
  - VBA `fct_Exponential`: `Exp((ldblLambda * Log(pdblValueLast) + (1 - ldblLambda) * Log(pdblValueNext)))` - This is log-linear interpolation of the values (equivalent to exponential interpolation).

### 1.2. Discount Factors
- Calculate a Discount Factor (DCF) for each period $t$.
- $DCF_t = \frac{1}{(1 + r_t)^t}$ where $r_t$ is the interpolated zero-coupon rate for term $t$.
- Alternatively, if the curve provides discount factors directly, interpolate those.

### 1.3. RoRAC Component Schedules
Instead of single annualized percentages, we calculate the actual cash flow effect of each component for every period $t$:

*   **Outstanding Balance ($BAL_{t-1}$)**: From standard amortization schedule.
*   **Net Income Components ($NI_t$)**:
    *   (+) Interest Income: derived from deal payments.
    *   (-) Funding Cost: $BAL_{t-1} \times MFR \times \Delta t$
    *   (-) Cost of Risk (EL): $BAL_{t-1} \times CoR_{annual} \times \Delta t$ (until full Basel II is implemented)
    *   (-) Operating Expenses: $InitialAmount \times Opex_{annual} \times \Delta t$ (or based on balance depending on specific opex definition)
    *   (+) Capital Benefit: $EC_{t-1} \times (FundingRate + Spread) \times \Delta t$ (Earnings on allocated capital)
*   **Economic Capital ($EC_t$)**:
    *   $EC_t = BAL_{t-1} \times EconCapRatio$ (until full Basel II implemented)

## 2. RoRAC Calculation Formula

Matches `mdlCalculation.bas` approach of normalizing by PV of Outstanding.

$$ \text{PV\_Outstanding} = \sum_{t=1}^N (BAL_{t-1} \times DCF_t \times \Delta t) $$

*Note: VBA might use $BAL_t$ or different timing. `arrCalculation_Generation(j, 21)` uses `arrCalculation_Generation(j - 1, 15)` which is previous period balance.*

$$ \text{RoRAC} = \frac{\sum \text{PV}(NI_t)}{\sum \text{PV}(EC_t)} $$

VBA uses a slightly more complex looking formula that mathematically simplifies to a ratio of PVs, often normalizing individual components by `PV_Outstanding` first to get "annualized equivalent rates" for reporting.

$$ \text{ComponentRate}_i = \frac{\sum \text{PV}(\text{ComponentCashflow}_{i,t})}{\text{PV\_Outstanding}} $$

$$ \text{RoRAC} = \frac{\sum \text{ComponentRate}_{\text{income}} - \sum \text{ComponentRate}_{\text{expense}}}{\text{Average EC Rate}} $$

Where $\text{Average EC Rate} = \frac{\sum \text{PV}(EC_t)}{\text{PV\_Outstanding}}$.

## 3. Implementation Plan

1.  **Create `FinancialCalculator.Engine.Core.YieldCurve`**: Handles rate interpolation.
2.  **Create `FinancialCalculator.Engine.Core.DcfModel`**:
    *   Takes `CalculatorOutputs` (schedule) and `CofParams` (rates/risk params).
    *   Generates full period-by-period RoRAC component streams.
    *   Calculates PVs for all streams.
    *   Computes final RoRAC.
3.  **Update `RoracCalculator`**: Switch from simple formula to using `DcfModel`.
