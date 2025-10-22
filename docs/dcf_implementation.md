# DCF and Risk Engine Implementation Documentation

**Date:** 2025-10-22
**Status:** Fully Implemented (DCF + Basel II Risk + UI)

## Overview

The financial calculator engine has been upgraded to include a full period-by-period Discounted Cash Flow (DCF) model for RoRAC calculation, integrated with a Basel II Internal Ratings-Based (IRB) risk engine that dynamically looks up parameters (PD, LGD) from CSV files.

## Key Components

### 1. DCF Model (`DcfModel.cs`)
- **Location**: `FinancialCalculator.Engine.Core`
- **Functionality**: Replaces simplified annualized margin calculations.
- **Logic**:
    - Generates cashflow streams for Gross Interest, Funding Cost (MFR), Risk Cost (EL), Opex, and Capital Benefit.
    - Discounts all streams to $T=0$ using MFR as the discount rate.
    - Calculates RoRAC as $\frac{\sum PV(\text{Net Income})}{\sum PV(\text{Economic Capital})}$.
    - **Sign Convention**: All costs (Funding, Risk, Opex) are stored and calculated as negative values internally. Net Income is the simple sum of all components.

### 2. Yield Curve Interpolation (`YieldCurve.cs`)
- **Location**: `FinancialCalculator.Engine.Core`
- **Functionality**: Provides rates for any term using Linear (extrapolation) and Exponential (interpolation) methods, matching legacy VBA.

### 3. Basel II Risk Engine (`BaselIIEngine.cs`)
- **Location**: `FinancialCalculator.Engine.Core`
- **Functionality**: Implements standard Basel II AIRB formulas.
- **Formulas Verified against `BASEL_II.bas`**:
    - **Correlation (R)**: Different formulas for Corporate/Dealer/Fleet vs. Retail.
    - **Maturity Adjustment (b)**: Applied to Corporate exposures.
    - **Capital Requirement (K)**: Full Gaussian copula formula implemented with normal distribution approximations.
    - **Expected Loss (EL)**: $PD \times LGD_{DCF}$.

### 4. Risk Parameter Repository (`RiskParameterRepository.cs`)
- **Location**: `FinancialCalculator.Engine.Core`
- **Functionality**: Loads `PD.csv`, `LGD_OneEC.csv`, and `EC_TOTAL.csv` into memory.
- **Lookups**:
    - `GetPd(CustomerType, Rating)`
    - `GetLgd(CustomerType, AssetState, AssetValuationCurve)` returns both `DcfLgd` (for EL) and `DownturnLgd` (for EC).
    - `GetEcTotal()` provides a pragmatic total EC ratio (~8.67%) from `EC_TOTAL.csv`, used as the base EC ratio.

### 5. Integration (`LocalScenarioService.cs`)
- **Location**: `FinancialCalculator.WinUI3.Services`
- **Flow**:
    1.  Receives `ScenarioInput` including new risk fields (`CustomerType`, `Rating`, etc.).
    2.  Calls `RiskParameterRepository` to get PD and LGDs.
    3.  Calls `BaselIIEngine` to calculate annualized `CostOfRisk` (EL).
    4.  Uses `EC_TOTAL` from repository for `EconCapRatio` (pragmatic approach matching expected ~8-12% range).
    5.  Populates `CofParams` with these dynamic values.
    6.  Calls `DcfModel.Compute` directly with the dynamic parameters.

## User Interface
- **New Section**: "Risk Parameters" added to the main calculator input area (expandable, folded by default).
- **Inputs**:
    - Customer Type (Default: RETAIL PRIVATE)
    - Asset State (Default: New)
    - Asset Class (Default: MBPC)
    - Credit Rating (Default: 5, 5.0)
- **Behavior**: Changes to these inputs automatically trigger a recalculation of RoRAC.

## Usage
Ensure the `docs/parameters` directory contains valid `PD.csv`, `LGD_OneEC.csv`, and `EC_TOTAL.csv` files and is accessible to the application (currently hardcoded path in `LocalScenarioService` for development environment).