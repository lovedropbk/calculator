# Financial Calculator Remediation Plan

**Date:** 2025-10-23
**Status:** Complete (Phases 1-4 Complete)

This plan addresses the critical architectural issues, code quality gaps, and inefficiencies identified in the codebase review.

## Phase 1: Architectural Foundations (High Impact)

**Goal:** Decouple the UI from the Engine and fix critical scalability issues.

1.  **Standardize Infrastructure**
    *   [x] Add `CsvHelper` NuGet package to all projects.
    *   [x] Replace custom `CsvParser` and `RiskParameterRepository.SplitCsvLine` with `CsvHelper`.
    *   [x] Create a shared `IFileService` for abstracting file I/O, enabling better unit testing.

2.  **Engine Facade**
    *   [x] Create `FinancialCalculator.Engine.FinancialFacade` class.
    *   [x] Expose a single, coarse-grained entry point: `ScenarioResult Calculate(ScenarioRequest request)`.
    *   [x] Move orchestration logic (getting risk params, building CofParams, calling DcfModel) from `MainViewModel`/`DealEngine` into this facade.
    *   [x] Ensure `ScenarioResult` contains *everything* the UI needs, formatted as raw data (numbers, not strings).

3.  **Fix Repository Scalability**
    *   [x] Refactor `RiskParameterRepository` to *not* load everything on startup.
    *   [x] Implement an on-demand lookup mechanism (e.g., indexing CSVs on first use, or using SQLite if feasible). For now, lazy-load only needed tables or use streamed reading for lookups if files are large.

## Phase 2: ViewModel Refactoring (Breaking the God Class)

**Goal:** Decompose `MainViewModel` into manageable, testable components.

1.  **Extract Services from ViewModel**
    *   [x] Create `ExportService` to handle XLSX generation.
    *   [x] Create `ComparisonService` to manage the comparison list and waterfall generation logic.
    *   [x] Inject these services into `MainViewModel`.

2.  **Decompose MainViewModel**
    *   [x] Move Goal Seek logic to a dedicated `GoalSeekViewModel`.
    *   [x] Ensure `CampaignManagerViewModel` purely manages campaign state, without knowing about `MainViewModel`'s calculation logic (use events or a mediator if interaction is needed).

## Phase 3: Code Quality & Testing

**Goal:** Improve reliability and maintainability.

1.  **Replace Manual Math**
    *   [x] Evaluate and integrate `MathNet.Numerics` for `Irr`, `Rate`, and interpolation functions.
    *   [x] Refactor `FinancialCalculator.cs`, `GoalSeekEngine.cs`, and `YieldCurve.cs` to use the library.

2.  **Strengthen Test Suite**
    *   [x] Add true unit tests for the new `FinancialFacade` using mocked repositories.
    *   [x] Add property-based testing for financial formulas to cover edge cases.
    *   [x] Refactor `RoRacEndToEndTests` to use known, static input files for reproducible results in CI.

## Phase 4: Cleanup & Optimization

**Status:** Complete

1.  **Remove Obsolete Artifacts**
    *   [x] Audit codebase for any remaining unused classes or files (e.g., legacy DTOs if replaced by Facade models).
        *   Removed `ApiClient.cs`, `BackendLauncher.cs`, `DateTimeSanitizer.cs`, `Dtos.cs` from WinUI3 project as they were remnants of a previous architecture.
        *   Cleaned up `FinancialCalculator.WinUI3.csproj` to remove excluded file references.
        *   Removed unused methods `BuildDealFromInputs` and `LocalScheduleToDto` from `MainViewModel.cs`.
    *   [x] Rename `RefactoredEngineTests.cs` to something more descriptive if it contains valuable tests, or merge into appropriate test classes.
        *   Renamed to `EngineUnitTests.cs` to better reflect its purpose.
2.  **Optimize Re-calculations**
    *   [x] Implement finer-grained change tracking in `DealInputViewModel`. Currently, almost any change triggers a full `RecalculateAsync`.
        *   Implemented `ScenarioRequest` change tracking in `MainViewModel.RecalculateAsync`. Recalculation now only occurs if the generated `ScenarioRequest` differs from the previous one, effectively filtering out pure UI state changes that don't affect the calculation.
        *   Removed redundant `NotifyChanged()` calls in `DealInputViewModel` for properties like `LockMode` that don't immediately impact calculations.
    *   [x] Identify inputs that only affect UI display (e.g., units) vs inputs that affect financial calculation, and avoid re-running `FinancialFacade.Calculate` for purely display changes.
        *   Addressed by the `ScenarioRequest` comparison optimization.
3.  **Enhance Error Handling & Logging**
    *   [x] Ensure all services (especially new ones) have proper error handling and logging.
        *   Updated `VehicleCatalogService`, `StandardRateService`, and `MainViewModel.Campaigns.cs` to use the centralized `Logger` service instead of `System.Diagnostics.Debug.WriteLine`.
        *   Fixed bug in `tests/FinancialCalculator.Tests/Services/VehicleCatalogService.cs` exposed during warning cleanup.
    *   [x] Centralize error reporting in UI (e.g., via a shared InfoBar service or similar).
        *   Updated `MainViewModel.InitializeAsync` to catch critical initialization errors and display them using the main `InfoBar`, ensuring users are aware of startup failures (e.g., missing configuration files).
4.  **Zero-Warning Policy**
    *   [x] Address all build warnings.
        *   Fixed unused variable warnings in tests.
        *   Fixed nullability warnings in test services by adding proper null checks and null-forgiving operators where appropriate.
        *   Verified clean build with `dotnet test`.

## Execution Order

1.  Phase 1 (Standardize Infra -> Engine Facade -> Repo Fix)
2.  Phase 2 (Extract Services -> Decompose VM)
3.  Phase 3 (Math replacement can be parallel with Phase 2)
4.  Phase 4 (Cleanup)

---
Critical Codebase Review Report
Executive Summary
The codebase demonstrates a functional but fragile implementation of a complex financial calculator. While it successfully implements advanced concepts like DCF and Basel II risk modeling, it suffers from significant architectural debts, code duplication, and maintainability issues that will severely hamper future scaling and stability.

Overall Score: C- (Functional but technically debt-ridden)

1. Critical Architectural Issues
1.1. The "God ViewModel" Anti-Pattern
Issue: MainViewModel.cs (and its partials) is a massive, monolithic class (>1500 lines combined) that violates the Single Responsibility Principle. It manages UI state, orchestrates calculation logic, handles file I/O for exports, manages sub-viewmodels, and even contains raw business logic for budget utilization and campaign comparisons.
Impact: Extremely difficult to test, maintain, or extend. Any change risks breaking unrelated features.
Recommendation: Refactor immediately.
Move export logic to a dedicated ExportService.
Move comparison logic to a ComparisonService.
Delegate orchestration to a CalculationWorkflowCoordinator.
1.2. Leaky Abstractions & Tight Coupling
Issue: The UI layer (MainViewModel) knows too much about the engine's internals. It manually constructs CofParams (indirectly via DealEngine), interprets raw engine outputs, and even re-implements some logic like commission resolution that should be core engine responsibilities.
Impact: Changes to the engine require lock-step changes in the UI, defeating the purpose of a layered architecture.
Recommendation: Introduce a strongly-typed FinancialFacade in the Engine layer that exposes coarse-grained operations (e.g., CalculateScenario(ScenarioRequest request)) returning fully populated, UI-agnostic result models.
1.3. In-Memory Scalability Bottleneck
Issue: RiskParameterRepository.cs loads entire CSV files (PD.csv, LGD_OneEC.csv) into in-memory dictionaries on startup.
Impact: While acceptable for an MVP with small files, this will cause slow startup times and high memory usage as parameter files grow (common in real-world financial systems).
Recommendation: Switch to a real database (SQLite for local, SQL Server for server) or implement lazy-loading/indexing for CSVs if they must remain the source of truth.
2. Code Quality & Inconsistencies
2.1. Duplicated Infrastructure Logic
Issue: multiple ad-hoc CSV parsers exist. RiskParameterRepository has its own SplitCsvLine implementation, while FinancialCalculator.WinUI3.Services has a separate CsvParser class.
Impact: Double maintenance burden. A bug fixed in one parser remains in the other.
Recommendation: Standardize on a single, robust CSV library (e.g., CsvHelper) or at least a single shared utility class.
2.2. Fragile Math Implementations
Issue: Key financial algorithms are manually implemented with basic numerical methods:
GoalSeekEngine.cs uses a simple bisection/Newton hybrid that may fail on complex non-monotonic pricing functions.
FinancialCalculator.cs implements its own Irr (Newton-Raphson) and Rate functions.
Impact: High risk of edge-case failures (non-convergence, multiple roots) compared to battle-tested financial libraries.
Recommendation: Replace manual implementations with a trusted numerical library (e.g., Math.NET Numerics) where possible.
2.3. Confusing Naming & Obsolete Artifacts
Issue: Files like RefactoredEngineTests.cs imply incomplete refactoring work. Documentation refers to LocalEngineService.cs and LocalScenarioService.cs, which appear to be deleted or renamed, causing confusion for new developers.
Impact: Increases onboarding time and cognitive load.
3. Test Suite Gaps
Issue: Tests are largely integration tests (RoRacEndToEndTests.cs) that depend on external files existing in specific relative paths. Unit tests for individual components (like strictly testing math functions without file I/O) are sparse.
Impact: Flaky tests that fail depending on execution environment (e.g., CI pipelines missing generic file paths).
Recommendation: Mock file system access for unit tests and use clearly defined artifacts for integration tests.
4. Inefficiencies
Redundant Recalculations: The MainViewModel uses debouncing to mitigate frequent updates, but the coarse-grained RecalculateAsync method re-runs the entire DCF model even for minor UI changes that might only affect display formatting or non-financial metadata.
Tightly Coupled Services: Services often manually instantiate their dependencies (new DealEngine(new RiskParameterRepository())), preventing effective dependency injection and making them hard to swap out for testing or different configurations.

