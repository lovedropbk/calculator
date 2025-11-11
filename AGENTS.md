# Repository Guidelines

## Project Structure & Module Organization
- Root: `financial_calculator.sln`, `NuGet.config`, `global.json`, `azure-pipelines.yml`.
- Engine: `winui3-mvp/FinancialCalculator.Engine` (core calculation library).
- UI: `winui3-mvp/FinancialCalculator.WinUI3` (WinUI 3 desktop app, Windows).
- Tests: `tests/FinancialCalculator.Tests` (xUnit unit/integration tests).
- Docs: `docs/` and `winui3-mvp/docs/` (design, parameters, architecture).
- Scripts: `scripts/` (PowerShell helpers like `run-tests.ps1`, `run-app.ps1`).

## Build, Test, and Development Commands
- Restore: `dotnet restore financial_calculator.sln`
- Build (Debug): `dotnet build financial_calculator.sln -c Debug`
- Tests: `dotnet test tests/FinancialCalculator.Tests -c Debug`
- Run UI (Windows): `dotnet run --project winui3-mvp\FinancialCalculator.WinUI3`
- Scripts (alternatives): `./scripts/run-tests.ps1`, `./scripts/run-app.ps1`

## Coding Style & Naming Conventions
- Language: C# (.NET). Indentation: 4 spaces; UTF-8; LF/CRLF as OS default.
- Names: PascalCase for types/methods/properties; camelCase for locals/params; `_camelCase` or `camelCase` for private fields; interfaces prefixed with `I`; async methods end with `Async`.
- Layout: file-scoped namespaces preferred; one public type per file; keep classes focused.
- Formatting: keep diffs minimal and consistent. You may run `dotnet format` locally if available.

## UI Styling (Fluent UI v2)

### Localization & x:Uid guidelines (WinUI 3)
- All user-visible UI strings must be centralized in resw files. Do not hardcode `Text`, `Content`, `Header`, `Title`, `PlaceholderText`, or tooltips.
- Use `x:Uid` in XAML to localize strings and map to the correct property in the `.resw` keys based on control type:
  - `TextBlock` -> `.Text`
  - `Button` -> `.Content`
  - `MenuFlyoutItem` -> `.Text`
  - `Expander` -> `.Header`
  - `InfoBar` -> `.Title`
  - `ToolTip` -> `.Content` (or `ToolTipService.ToolTip` on the owning element)
  - `ToggleSwitch` -> `.OnContent`, `.OffContent`
- Avoid using `x:Uid` on `Window` to set `Title` (varies across SDK versions). Set AppWindow title programmatically via ResourceLoader.
- Units and placeholders:
  - `%` -> `x:Uid="Unit_Percent"`
  - `THB` suffix -> `x:Uid="Unit_THB"`
  - `THB` placeholder -> `x:Uid="Unit_THB_Placeholder"`
- Runtime language switching:
  - Update qualifier: `Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", tag)`.
  - Recreate main window on the UI thread (`DispatcherQueue.TryEnqueue`) to re-apply `x:Uid` strings.
  - Persist: `ApplicationLanguages.PrimaryLanguageOverride = tag`.
- Programmatic strings: use `Microsoft.Windows.ApplicationModel.Resources.ResourceLoader` via `Services.ResourceHelper.GetString(key)`.

### What we’ve changed (Nov 2025)
- Fixed startup crashes by deferring WinRT calls to `OnLaunched`, removing static `ResourceLoader`, and lazy-initializing `App.Settings`.
- Implemented runtime language switching with window recreation and global qualifier update.
- Centralized large portions of the UI (Deal Inputs, Cashflows, Campaign Details, Budget Utilization, footer, App Settings, loading overlay, Risk dialog).
- Standardized units and placeholders (Unit_Percent, Unit_THB, Unit_THB_Placeholder) and localized the commission summary " THB)" Run.
- Removed `x:Uid` on `Window` and `App_MainWindow.Title` keys to prevent XAML loader failures.
- Added a temporary localization validator under `scripts/tmp_rovodev_validate_localization.ps1` to spot x:Uid/property mismatches before runtime.


- Mandatory: adhere to Fluent UI v2 for WinUI 3. Do not hardcode margins, paddings, colors, or radii in views; use shared tokens/styles.
- Tokens live in `winui3-mvp/FinancialCalculator.WinUI3/Styles/FluentTheme.xaml` (e.g., `SpaceS`, `SpaceM`, `CompactControlHeight`) and styles in `Styles/DefaultStyles.xaml` (e.g., `DenseTextBoxStyle`, `TokenCardStyle`).
- Use `StaticResource`/`ThemeResource` in XAML and prefer reusable `Style`/`ControlTemplate` over inline setters.
- ViewModels must stay presentation-agnostic (no UI metrics/formatting); expose data/state and bind to styles/converters in XAML.
- Example: `<StackPanel Margin='{StaticResource SpaceS}'><TextBox Style='{StaticResource DenseTextBoxStyle}'/></StackPanel>`

## Testing Guidelines
- Framework: xUnit (`using Xunit;`).
- Location: `tests/FinancialCalculator.Tests` mirrors engine namespaces when practical.
- Naming: `MethodName_State_ExpectedResult` (e.g., `RateConverter_FlatToNominal_MatchesExpected`).
- Run tests: `dotnet test` or `./scripts/run-tests.ps1`.

## Commit & Pull Request Guidelines
- Commits: imperative mood, concise scope first line (<= 72 chars). Example: `Engine: fix nominal rate conversion for arrears`.
- Include brief context and rationale in the body; reference issues (`#123`).
- PRs: clear description (What/Why), linked issues, test evidence; include screenshots/GIFs for UI changes; note any data/parameter updates in `docs/parameters`.

## Security & Configuration Tips
- Do not commit secrets; sample data lives under `docs/parameters/`.
- Respect `NuGet.config` sources; prefer SDK-managed restores.
- Large artifacts and generated outputs should not be committed.
