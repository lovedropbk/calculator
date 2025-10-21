# Audit Report: Legacy VBA vs New C# Engine

**Date:** 2025-10-21
**Auditor:** Kilo Code

## Executive Summary

A deep-dive audit comparing the legacy VBA implementation (`hq_logic_vba.md`) with the new C# engine (`FinancialCalculator.Engine`) reveals significant architectural differences. The C# engine currently implements a simplified, annualized approach for RoRAC, whereas the VBA utilizes a detailed, period-by-period Discounted Cash Flow (DCF) model and includes full Basel II risk parameter calculations.

Unless the strategic intention was to offload Risk and detailed DCF calculations to an external service, the current C# engine **does not match** the legacy HQ logic in depth or precision.

## 1. Basel II Risk Parameters

### Legacy VBA (`BASEL_II.bas`)
The VBA contains a complete implementation of Basel II Internal Ratings-Based (IRB) formulas:
*   **Correlation (`r`) & Maturity Adjustment (`b`)**: Explicit formulas dependent on Probability of Default (PD).
*   **Asset Class Specifics**: Distinct correlation formulas for `Corporate`, `Dealer`, and `Retail` exposures.
*   **Unexpected Loss (UL) / Economic Capital (EC)**: calculated using the Basel asymptotic single risk factor (ASRF) model: `UL = (LGD * N[(1-r)^-0.5 * G(PD) + (r/(1-r))^0.5 * G(0.999)] - PD*LGD) * ...`
*   **Expected Loss (EL)**: `EL = LGD * PD`

### C# Engine (`RoracCalculator.cs`)
The C# engine **lacks these calculations entirely**.
*   It treats `CostOfRisk` (equivalent to EL in annualized terms) and `EconCapRatio` (related to UL) as **simple inputs** via `CofParams`.
*   There is no logic to derive these from PD, LGD, term, or asset class.
--> you can implement same logic as in hq tool. parameters are @parameters_live_... don't use the parameters from the vba directly as they are not applicable for TH

**Recommendation:**
If the C# engine is intended to be standalone, it MUST implement the Basel II formulas from `BASEL_II.bas`. Porting the `UL`, `EL`, `r`, and `b` functions is required.

## 2. DCF and IRR/NPV Calculations

### Legacy VBA (`DCF_Excel.bas` & `mdlCalculation.bas`)
*   **DCF Factors**: `DCF_Excel.bas` implements linear and exponential interpolation to derive discount factors from a yield curve for every period of the deal. (ok, we can change.)
*   **Valuation**: `mdlCalculation.bas` uses these factors to calculate the Present Value (PV) of *each* income/expense stream over the deal's life (Capital Advantage, Cost of Credit Risk, EC, etc.) to arrive at a lifetime RoRAC. (ok, let's implement.)

### C# Engine (`FinancialCalculator.cs` & `RoracCalculator.cs`)
*   **IRR**: `FinancialCalculator.cs` has a robust Newton-Raphson IRR implementation for the deal itself. This aligns with standard practices but needs verification against VBA's implicitly used Excel `IRR`/`XIRR` functions for edge cases. (not an issue. we can leave as is.)
*   **RoRAC**: `RoracCalculator.cs` uses a **simplified annualized margin model**. It calculates `Net EBIT Margin` by subtracting annualized costs (MFR, Opex, Risk) from the effective annual IRR. It does *not* perform a full DCF valuation of all components. (ok, implement full DCF model then)

**Recommendation:**
The current C# RoRAC calculation is an approximation compared to the VBA's detailed DCF model. For exact matching, the C# engine needs to:
1.  Implement yield curve interpolation (port `DCF_Excel.bas` logic).
2.  Generate a full period-by-period cashflow for *all* RoRAC components (not just deal principal/interest), matching the logic in `mdlCalculation.bas`.
3.  Calculate RoRAC based on `PV(Net Income) / PV(Economic Capital)` rather than just annualized margins.

## 3. Hidden Assumptions & Edge Cases

*   **Day Count Conventions**: VBA uses `fct_DiffDays30` (likely 30/360) and `Act/Act`. C# mostly assumes standard monthly periods. This will lead to precision mismatches.
*   **Asset Classes**: VBA has specific handling and formulas for "Corporate", "Dealer", "Retail", "Bank Branch", and "US Operating Lease". The C# engine is currently generic and lacks these specializations.
*   **Grace Periods**: Implied support in VBA; not explicitly seen in C# `FinancialCalculator.cs` standard schedule generation.

## 4. Summary of Discrepancies

| Feature | Legacy VBA | New C# Engine | Status |
| :--- | :--- | :--- | :--- |
| **Basel II Formulas** | Full Implementation (Corp, Dealer, Retail) | Missing (Inputs required) | 🔴 Critical Gap |
| **RoRAC Model** | Full Lifetime DCF | Simplified Annualized Margin | 🟠 Major Difference |
| **DCF Interpolation**| Custom Linear/Exponential | Missing | 🔴 Critical Gap |
| **Day Count** | Mixed (30/360, Act/Act) | Standard Monthly | 🟡 Minor Precision Risk|
