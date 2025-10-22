# Risk Architecture (CoR/PD/LGD)

**Date:** 2025-10-22
**Goal:** Implement Basel II-based Cost of Risk (CoR) and Economic Capital (EC) calculations using TH-specific parameters.

## 1. New Inputs Required
To lookup parameters correctly, `CalculatorInputs` needs expansion:

*   **Customer Type**: `RETAIL PRIVATE`, `RETAIL SMALL BUSINESS`, `FLEET`, `DEALER`.
*   **Asset State**: `New` (N), `Used` (U).
*   **Asset Brand/Class**: Maps to `AssetValuationCurve` (e.g., `MBPC` for Mercedes Passenger Cars).
*   **Credit Rating**: Internal rating (e.g., "3.5", "4.0") to lookup PD.

## 2. Parameter Repository
A service to load and query the provided CSVs in `winui3-mvp/docs/parameters/`:

*   `PD.csv`: Lookup `PD` based on `CustomerType` and `Rating`.
*   `LGD_OneEC.csv`: Primary source for LGD.
    *   Use `DCF` column for Expected Loss (CoR) calculations.
    *   Use `downturn` column for Economic Capital (EC) calculations.
*   `FIXED_LGD.csv`: Overrides for specific products (especially Dealer/Wholesale).

## 3. Basel II Engine
Must implement standard IRB formulas (replacing `BASEL_II.bas`):

### 3.1. Correlation (R)
Formulas depend on asset class (Corporate, Retail, etc.).
*   Example (Corporate): $R = 0.12 \times \frac{1 - e^{-50 \times PD}}{1 - e^{-50}} + 0.24 \times [1 - \frac{1 - e^{-50 \times PD}}{1 - e^{-50}}]$

### 3.2. Maturity Adjustment (b)
*   $b = (0.11852 - 0.05478 \times \ln(PD))^2$

### 3.3. Capital Requirement (K)
*   $K = [LGD \times N(\frac{G(PD) + \sqrt{R} \times G(0.999)}{\sqrt{1-R}}) - PD \times LGD] \times \frac{1 + (M - 2.5) \times b}{1 - 1.5 \times b}$
*   Where $N(\cdot)$ is standard normal CDF, $G(\cdot)$ is inverse standard normal CDF.
*   $M$ is Effective Maturity (usually calculated from schedule).

### 3.4. Economic Capital (EC)
*   $EC = K \times EAD$ (Exposure at Default).

### 3.5. Expected Loss (EL) & Cost of Risk (CoR)
*   $EL = PD \times LGD \times EAD$
*   $CoR_{annual} = \frac{EL}{EAD} = PD \times LGD$ (simplified annualized rate).

## 4. Integration with DCF Model
1.  **Pre-calculation**: Before running DCF, use `BaselIIEngine` to calculate deal-specific `CoR_scalar` ($PD \times LGD$) and `EC_ratio` ($K$).
2.  **Pass to DCF**: Populate `CofParams.CostOfRisk` and `CofParams.EconCapRatio` with these calculated values.
3.  **Execution**: `DcfModel` runs as currently implemented, using these deal-specific risk parameters instead of generic defaults.

## 5. Next Steps (Implementation Mode)
1.  Add required fields to `CalculatorInputs` (or a new `RiskInputs` model).
2.  Implement `CsvParameterLoader` to read the files.
3.  Implement `BaselIIEngine` with the formulas.
4.  Wire it all up in `LocalScenarioService` to populate `CofParams` dynamically.