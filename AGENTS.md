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
