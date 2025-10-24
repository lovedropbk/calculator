# Financial Calculator Unified Documentation

## Overview
The Financial Calculator is a WinUI 3 application with a .NET 8 Calculation Engine. It performs complex financial deal structuring, including DCF analysis and Basel II RoRAC calculations.

## Architecture
The solution follows a clean architecture with a clear separation between UI (WinUI3) and Core Logic (Engine).

### Engine Layer (`FinancialCalculator.Engine`)
The Engine is a self-contained .NET 8 library responsible for all financial and risk calculations.

*   **FinancialFacade**: The single, coarse-grained entry point for all Engine operations.
    *   `Calculate(ScenarioRequest)`: Performs full deal structuring, DCF analysis, and RoRAC calculation.
    *   `GoalSeek(...)`: Solves for a target metric (e.g., RoRAC) by varying an input (e.g., Customer Rate).
*   **Core Logic**:
    *   `DealEngine`: Orchestrates the calculation flow, including risk parameter lookup and COF parameter construction.
    *   `FinancialCalculator`: Generates cashflow schedules based on product type and payment mode.
    *   `DcfModel`: Implements period-by-period Discounted Cash Flow to calculate RoRAC and profitability waterfall components.
    *   `BaselIIEngine`: Implements Basel II AIRB formulas for Expected Loss (EL) and Economic Capital (EC).
*   **Data Access**:
    *   `RiskParameterRepository`: Loads PD, LGD, and EC parameters from CSV files using `CsvHelper`.

### UI Layer (`FinancialCalculator.WinUI3`)
The UI is a native WinUI 3 application following the MVVM pattern. It holds NO financial calculation logic, delegating entirely to the Engine.

*   **MainViewModel**: The root ViewModel that initializes the `FinancialFacade` and orchestrates sub-ViewModels. It implements change tracking to optimize re-calculations.
*   **Sub-ViewModels**:
    *   `DealInputViewModel`: Manages user inputs, validation, and auto-population from catalogs.
    *   `CampaignManagerViewModel`: Manages standard and custom campaigns.
    *   `ResultsViewModel`: Holds calculation results for display.
*   **Services**:
    *   `CampaignCalculationService`: Bridges `CampaignManagerViewModel` and `FinancialFacade` for campaign scenario analysis.
    *   `VehicleCatalogService`, `StandardRateService`: Load UI-specific reference data from CSVs.

## Key Workflows & Integration

### 1. Deal Calculation Pipeline
1.  User modifies inputs in `DealInputViewModel`.
2.  `MainViewModel` detects changes (debounced).
3.  `DealInputViewModel` builds a `ScenarioRequest` DTO.
4.  `MainViewModel` calls `FinancialFacade.Calculate(ScenarioRequest)`.
5.  Engine performs: Risk Lookup -> Schedule Generation -> DCF Analysis.
6.  Engine returns `ScenarioResult` DTO.
7.  `MainViewModel` populates `ResultsViewModel` with data from `ScenarioResult`.

### 2. Campaign Analysis Pipeline
1.  `CampaignManagerViewModel` iterates through standard campaigns.
2.  For each campaign, `CampaignCalculationService` modifies the base `ScenarioRequest` according to campaign rules (e.g., apply subsidy, set target rate).
3.  `CampaignCalculationService` calls `FinancialFacade.Calculate()` for the modified scenario.
4.  Results are aggregated and displayed in the Campaigns grid.

## Integration Verification
A comprehensive code audit confirms that the WinUI 3 frontend exclusively utilizes the `FinancialCalculator.Engine` for all financial calculations.
*   **Main Calculator**: `MainViewModel.RecalculateAsync` directly calls `FinancialFacade.Calculate`.
*   **Campaigns**: `CampaignCalculationService` injects and uses `FinancialFacade` for all campaign scenario simulations.
*   **Goal Seek**: `GoalSeekViewModel` uses `FinancialFacade.GoalSeek`.
*   **No Duplicate Logic**: There is no shadow calculation logic remaining in the UI layer. All legacy embedded calculations have been removed.

## Testing
- **Unit Tests**: `FinancialFacadeTests` use mocked `IRiskParameterRepository` to test orchestration logic in isolation.
- **Integration Tests**: `RoRacEndToEndTests` use mocked `IFileService` with static CSV data to test the full calculation pipeline reproducibly.