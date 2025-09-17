# Financial Calculator — Architecture and UX Spec (Thailand MVP)

Author: Roo (Technical Architecture) | Date: 2025-09-15

Scope confirmed by business:
- Products: Hire Purchase, Finance Lease, mySTAR balloon finance, Operating Lease
- Geography and currency: Thailand, THB
- Campaigns: Subdown, Subinterest, Free Insurance, Free MBSP, Cash Discount
- Parameters: Average parameter set for MVP, seeded from current reporting
- Tech stack: Cross-platform desktop app built with Wails v2 framework; React + TypeScript frontend with Tailwind CSS; Go calculation and profitability engines with direct in-process integration. Optional local HTTP facade for future multi-client support.

Contents
1. Executive summary
2. Architecture overview
3. Component responsibilities
4. Data and configuration model
5. Calculation pipeline
6. Campaign mechanics
7. Product-specific specs
8. IDC taxonomy and handling
9. UI wireframes (ASCII)
10. Parameter catalog and governance
11. Non-functional targets
12. Implementation roadmap and acceptance tests
13. Orchestrated implementation plan — four subprojects and testing

## 1. Executive summary
The calculator provides monthly installment, nominal and effective customer rates, and a profitability waterfall to Acquisition RoRAC. It supports campaign types and multiple upfront expenses IDCs. Sales uses a guided UI; all numerical truth is computed by a deterministic backend engine that is versioned and auditable.

## 2. Architecture overview
High-level flow:

```mermaid
flowchart LR
  Sales[Sales user] --> App[Desktop App (Wails + React)]
  App --> Eng[Engines: Pricing • Campaign • Cashflow+IRR • Profitability]
  App --> Cfg[Config store local cache]
  Cfg <---> Sync[Param sync over HTTPS]
  App --> Audit[Scenario and audit store]
  Audit --> Export[PDF/XLSX and Reporting]
  Sync --> Back[Parameter publisher backend]
```

Key principles: single source of parameters; deterministic outputs; versioning of configs; exports with signatures for audit.

## 3. Component responsibilities
- Desktop app (Wails v2): Cross-platform native application with React frontend and TypeScript; modern UI with Tailwind CSS styling, guided forms, inline validation, numeric controls, and adaptive theming; orchestrates calculations and displays results. No business math in the UI layer.
- Engines (in-process Go libraries): Pricing, Campaign rules, Cashflow and IRR, Profitability and RoRAC. Deterministic and versioned; callable directly from the app via Wails runtime bindings.
- Config service (local cache with sync): Versioned parameters stored locally; background sync over HTTPS from the publisher backend; each calculation pins to a specific version id for reproducibility.
- Scenario and audit store: Local scenario persistence and immutable audit log; export to PDF/XLSX; optional BI feed or server upload.

### 3.1 Wails frontend implementation notes
- Application framework: Wails v2 providing native desktop wrapper with embedded web view; React 18+ for component-based UI architecture; TypeScript for type safety and better IDE support; direct Go backend integration through Wails runtime bindings enabling seamless function calls from frontend to backend.
- Modern UI with Tailwind CSS: Tailwind CSS for utility-first styling with custom design system; glassmorphism effects with backdrop blur for depth and hierarchy; CSS variables for dynamic light/dark theme switching; smooth transitions and micro-interactions; custom components matching modern financial application patterns.
- React architecture: Functional components with React hooks for state management; Context API for global state (theme, user preferences, parameter versions); Custom hooks for business logic encapsulation; React Query for async state management and caching; Component composition for reusable UI patterns.
- UI components: Custom number input with formatting, validation, and currency display; Toggle switches and segmented controls for options; Collapsible panels with smooth animations for Details section; Modern date pickers and select dropdowns; Loading states and error boundaries for resilience.
- Cross-platform capabilities: Single codebase runs on Windows (primary), macOS, and Linux; Platform-specific styling adjustments via CSS; Native file dialogs through Wails runtime; System tray integration and notifications; Keyboard shortcuts with platform-appropriate key bindings.
- Frontend-backend integration: Wails bindings auto-generate TypeScript types from Go structs; Direct function calls from React to Go without HTTP overhead; Structured error handling with typed responses; Event system for backend-to-frontend push updates; Efficient binary data transfer for large datasets.
- State management: React state for UI-specific data (form inputs, UI toggles); Go backend maintains authoritative calculation state; Optimistic UI updates with rollback on errors; Session persistence via browser storage APIs; Undo/redo capability for user actions.

## 4. Data and configuration model
Entities
- Deal: market, currency, product, price ex tax, down payment value or percent, financed amount, term, balloon, timing, dates, campaign ids.
- Campaign: id, type, parameters, eligibility, funder, stacking.
- IDC item: category, amount, payer, financed or withheld, timing upfront or periodic, tax flags, revenue or cost.
- Cashflow: date, direction, type, amount, memo.
- ParameterSet: id, effective date, category tables.

Parameter categories
- Cost of funds matched funded curve by term
- Matched funded spread
- PD LGD by product and segment
- OPEX rates
- Economic capital parameters and advantage
- Central HQ add-on
- Rounding rules and day count ACT 365 Thai

## 5. Calculation pipeline
Inputs
- Commercial: price ex tax; down payment dual input THB and percent (linked, optional [lock] to fix one as primary); term months; balloon percent or value; timing arrears or advance; payout date; first payment offset.
- Rate mode: fixed customer nominal rate or target installment.
- Campaign selection: multi select.
- IDCs: upfront and periodic items with flags.

Steps
1. Financed amount = price after cash discount + financed IDCs − down payment.
2. Apply campaigns in order: Subdown → Subinterest → Free insurance → Free MBSP → Cash discount validation.
3. Build cashflows: t0 flows (dealer disbursement, subsidies, fees, costs); periodic schedule per product; balloon at maturity if applicable.
4. Compute monthly IRR and effective annual rate; compute or solve nominal rate as needed.
5. Profitability waterfall to RoRAC: deal IRR − cost of debt ± capital advantage − risk − opex + net fees and subsidies − HQ add-ons → Net EBIT Margin; RoRAC = EBIT ÷ economic capital.

Conventions
- Frequency monthly; Thai calendar ACT 365; arrears default.
- Rounding: bank round to currency minor units; display to basis points.

## 6. Campaign mechanics
Stacking order and effects
- Subdown: upfront subsidy from OEM or dealer; increases effective down payment or modeled as t0 inflow.
- Subinterest: reduce customer rate to target; t0 subsidy equals PV difference between base and target schedules.
- Free insurance: if free, t0 inflow from funder and pass-through to partner as required; if capitalized, increase financed amount and add periodic items.
- Free MBSP: same mechanics as insurance, different cost curve and duration.
- Cash discount: price reduction applied before financing; impacts financed amount and installment; subject to constraints.

## 7. Product-specific specs
Hire Purchase
- Fixed amortizing schedule; arrears default; monthly.
- Fees configurable as upfront or periodic.

mySTAR balloon finance
- Same as HP with balloon value or percent at maturity.
- Rate or installment solved under balloon constraint.

Finance Lease
- Rental schedule economically loan-like for pricing; title remains with lessor.
- VAT presentation shown separately for customer and internal profitability.
- Optional security deposit t0 inflow and matched outflow at end.

Operating Lease
- Rental equals depreciation plus funding cost on net invested capital plus opex plus risk plus target profit.
- MVP uses simple linear residual and opex model.

## 8. IDC taxonomy and handling
Categories
- Upfront revenue: documentation fee, acquisition fee.
- Upfront cost: broker commission, stamp, internal processing.
- Periodic fees: admin monthly; maintenance for leases.
Flags
- Revenue or cost; financed or withheld; taxable; disclosure class.

## 9. UI wireframes (ASCII)
9.1 Quote screen

+----------------------------------------------------------------------------------+
| Header: Financial Calculator  | Market TH | Currency THB | Scenario [Save] [Export] |
+----------------------------------------------------------------------------------+
|                                 |                                              |
| Inputs (left)                   | Results (right)                               |
|---------------------------------|----------------------------------------------|
| Product: [HP] [mySTAR] [F-Lease]| Monthly Installment:   THB 22,198.61         |
|          [Op-Lease]            | Customer Rate Nominal: 6.44 percent          |
| Price ex tax: [ 1,665,576 ]    | Customer Rate Effective: 6.63 percent        |
| Down payment: THB [ 333,115 ]  | Acquisition RoRAC:    5.58 percent           |
|              or  [ 20 percent ]| [Details ▼] Waterfall hidden by default       |
| Term months: [ 12 ]            |                                              |
| Balloon percent: [ 0 ]         |                                              |
| Timing: [Arrears v]            |                                              |
| Campaigns: [Subdown] [Subint]  |                                              |
|            [Free Ins] [MBSP]   |                                              |
|            [Cash Discount]     |                                              |
| IDC Quick Add [+]              |                                              |
| [Advanced ▸]                   |                                              |
+----------------------------------------------------------------------------------+

Interaction notes
- Results default shows only Monthly Installment, Customer Rate Nominal and Effective, and Acquisition RoRAC. The detailed profitability waterfall is hidden and revealed via [Details ▼]. The expansion state persists per user session.
- Down payment supports dual entry: THB and percent fields are linked; editing one updates the other. A [lock] toggle lets users fix one field as the source of truth. Validation: 0 to 80 percent by default; rounding to whole THB.
- Changing product toggles visible fields e.g., balloon for mySTAR only.
- Campaign chips open parameter popovers.
- IDC Quick Add opens mini modal with presets.

9.1.1 Results details (expanded)
+---------------- Results Details ----------------+
| Customer Rate Nominal: 6.44 percent            |
| Customer Rate Effective: 6.63 percent          |
| Deal Rate IRR Effective: 2.11 percent          |
| Cost of Debt Matched Funded: 1.48 percent      |
| Gross Interest Margin: 0.62 percent            |
| Capital Advantage: 0.08 percent                |
| Net Interest Margin: 0.70 percent              |
| Standard Cost of Credit Risk: 0.02 percent     |
| OPEX: 0.68 percent                              |
| IDC Subsidies and Fees periodic: 0.00 percent  |
| Net EBIT Margin: 0.00 percent                  |
| Acquisition RoRAC: 5.58 percent                |
+------------------------------------------------+

9.2 Advanced drawer

+------------------------------------------------ Advanced -------------------------------+
| Dates: Payout [04 08 2025]  First payment offset [1] months                           |
| Payment mode: [In arrears]  Frequency: [Monthly]                                      |
| Additional financed items: [ + Add ]                                                   |
| Security deposit: [ 0 ]                                                                |
| IDC table:                                                                             |
|  Item                 Type      Timing   Financed  Amount   Tax   Payer               |
|  Documentation fee    Revenue   Upfront  Yes       2,000    VAT   Customer           |
|  Broker commission    Cost      Upfront  No        10,000   -     Dealer             |
|  Admin fee            Revenue   Periodic n a       200 mth VAT   Customer           |
+----------------------------------------------------------------------------------------+

9.3 IDC Quick Add modal

+----------------- IDC Quick Add -----------------+
| Presets: [Doc fee 2,000] [Broker 1.5 percent]  |
|          [Stamp 0.5 percent] [Admin 200 mth]   |
| Custom:  Category [..] Amount [..] Flags [...]  |
| [Add] [Cancel]                                 |
+-------------------------------------------------+

9.4 Scenario compare

+---------------------------------+---------------------------------+---------------------------------+
| Baseline                         | Variant A                        | Variant B                        |
+---------------------------------+---------------------------------+---------------------------------+
| Installment    22,198.61        | 21,950.00  (-248.61)            | 22,600.00  (+401.39)            |
| Customer Nom   6.44 percent     | 6.20 percent (-24 bp)           | 6.70 percent (+26 bp)           |
| Deal IRR Eff   2.11 percent     | 2.05 percent                    | 2.25 percent                    |
| Net EBIT Mgn   0.00 percent     | 0.10 percent                    | -0.05 percent                   |
| Acq RoRAC      5.58 percent     | 5.80 percent                    | 5.30 percent                    |
+---------------------------------+---------------------------------+---------------------------------+

9.5 Config and parameter publishing

+----------------- Parameters (version 2025 08) ------------------+
| Cost of funds curve           [Upload CSV] [Publish]            |
| Matched funded spread         [Upload CSV] [Publish]            |
| PD LGD tables                 [Upload CSV] [Publish]            |
| OPEX rates                    [Upload CSV] [Publish]            |
| Economic capital parameters   [Upload CSV] [Publish]            |
| Campaign catalog              [Edit]       [Publish]            |
| Rounding and calendars        [Edit]       [Publish]            |
+------------------------------------------------------------------+

9.6 Audit and scenarios

+----------------- Scenarios ------------------+  +----------------- Audit log ------------------+
| [Open] Retail HP baseline                    |  2025 08 03 12 04  Created scenario by user A  |
| [Open] Fleet mySTAR 12m with subint          |  2025 08 03 12 04  Parameters published v45    |
| [Open] Lease OpL simple residual             |  2025 08 03 12 05  Scenario computed v45       |
+----------------------------------------------+  +----------------------------------------------+

## 10. Parameter catalog and governance
Sources and categories mapped to screenshot fields
- Matched funded interest rate and spread → cost of debt and MF spread
- Economic capital advantage
- Deferred tax liabilities advantage
- Customer security deposits advantage
- Other non interest bearing positions advantage
- Fee income
- Other expenses
- Cost of risk
- OPEX
- Central HQ add on

Governance
- Draft and publish workflow with maker checker.
- ParameterSet version id is attached to every calculation and export.

## 11. Non functional targets
- Performance p95 under 300 ms per calculation.
- Accuracy parity within 0.01 THB on installment and 1 bp on rates and margins.
- Availability 99.9 percent; read only mode if importer is down.
- Security SSO and RBAC; minimal PII; redacted logs.

## 12. Implementation roadmap and acceptance tests
Phase 1 MVP
- Engine foundations and parameter service
- HP and mySTAR products
- Subdown and Subinterest and Free Insurance and Free MBSP and Cash Discount
- IDC modeling core categories
- Profitability waterfall and RoRAC
- Quick quote UI and advanced drawer
- PDF and spreadsheet export
- Regression tests vs curated Excel cases and reference outputs

Phase 2
- Finance Lease and Operating Lease calculators
- Scenario compare and save share and audit UI
- Security deposit and periodic admin fee
- BI export with scenario metadata

Acceptance checks
- Installment parity on baseline cases within 0.01 THB
- Rate and margin parity within 1 bp for all waterfall lines
- Campaign stacks examples match expectation for subdown and subinterest and cash discount and free insurance
- RoRAC matches reference with fixed ParameterSet
- UI default view: Results panel shows Monthly Installment, Customer Rate Nominal and Effective, and Acquisition RoRAC only; [Details ▼] reveals full profitability waterfall; collapsed by default and remembered per user session.
- Down payment dual entry: THB and percent inputs are synchronized; constraints applied (e.g., 0 to 80 percent) and currency rounded to whole THB; lock toggle allows fixing either field as the source of truth.
- Windows desktop packaging: App runs as a GUI application without console window; on first run, parameter sync succeeds; when offline, the app uses the last cached ParameterSet and clearly shows the active version id in the header.

## 13. Orchestrated implementation plan — four subprojects and testing

This MVP will be delivered as four larger subprojects executed in parallel with stable, versioned interfaces. Each calculation pins to a specific ParameterSet version for full reproducibility (see sections 3–5 and 10–12).

13.1 Engine libraries (Go) — pricing, campaigns, cashflow+IRR, profitability
- Scope
  - Deterministic, versioned in-process engines that compute all numerical truths.
  - Products: Hire Purchase and mySTAR balloon finance for MVP; Finance Lease and Operating Lease in Phase 2.
  - Campaigns for MVP: Subdown, Subinterest, Free Insurance, Free MBSP, Cash Discount, with the stacking order and mechanics defined in sections 5–6.
  - IDC core categories per section 8, including upfront vs periodic, financed vs withheld, tax flags.
- Deliverables
  - Pricing engine: amortizing schedule, arrears default, balloon handling, support for fixed customer nominal rate or solving for installment under balloon constraints.
  - Campaign engine: deterministic application in the defined stack order with explicit audit of each transformation and the time-zero impacts.
  - Cashflow and rate engine: construction of t0 flows, periodic schedule, maturity balloon, monthly IRR, effective annual, and nominal rate computation/solution.
  - Profitability engine: waterfall to Acquisition RoRAC consistent with sections 5 and Appendix B, including cost of debt (matched funded curve and spread), capital advantage, risk (PD LGD), OPEX, HQ add-on, and subtotals.
  - Public API contract (conceptual): one entrypoint that accepts a Deal, selected campaign set, IDC items, and a pinned ParameterSet and returns a structured result containing schedule, cashflows, rates, and profitability waterfall with RoRAC. The API must not rely on UI state.
  - Deterministic serialization of inputs and outputs for audit (e.g., canonical JSON), including the ParameterSet version id.
- Interfaces and integration
  - Inputs: typed entities as per section 4 (Deal, Campaign, IDC, ParameterSet).
  - Outputs: Quote result with schedule, cashflows, nominal and effective customer rates, profitability lines, and RoRAC; all amounts as THB minor units accuracy with rounding conventions from section 5.
  - Error model: validation failures return explicit codes/messages; math failures (e.g., unsolvable target) return structured diagnostics; no panics in normal operation.
- Non-functional targets
  - Performance: p95 under 300 ms per calculation on a typical laptop; no shared global mutable state; concurrency-safe pure computations.
  - Determinism: identical inputs with same ParameterSet version yield identical outputs; stable ordering of cashflows and waterfall lines.
- Testing (required)
  - Unit tests for formula components (payment formula; IRR convergence; balloon and day count ACT 365 Thai; rounding to whole THB where required).
  - Property-based tests around amortization and IRR (monotonicity of payment vs rate; invariants on schedule sums).
  - Golden tests (regressions) using curated Excel reference cases; assert parity within 0.01 THB on installment and 1 basis point on all rate/margin lines.
  - Campaign stack tests covering Subdown, Subinterest, Free Insurance, Free MBSP, and Cash Discount permutations; verify t0 flows and PV-based subsidies.
  - IDC taxonomy tests for financed vs withheld, revenue vs cost, tax flags and periodic fee propagation.
  - Performance benchmarks on representative cases; fail CI if p95 exceeds target by more than 20 percent.
  - Determinism tests: repeated runs produce byte-identical serialized outputs (excluding approved volatile fields like timestamp where applicable).

13.2 Desktop app (Wails v2) — cross-platform UI and orchestration
- Scope
  - Cross-platform desktop app built with Wails v2, React, TypeScript, and Tailwind CSS with the UI, layout, and interaction patterns described in section 3.1 and wireframes in section 9.
  - Modern design system with glassmorphism effects, smooth animations, and adaptive light/dark themes.
  - React component architecture with hooks, Context API, and proper separation of concerns.
  - No business math in the UI: all computations delegated to the Go engine libraries via Wails runtime bindings with pinned ParameterSet version id displayed in header.
  - Offline-first operation with last cached ParameterSet; background async operations with proper state management.
- Deliverables
  - React components: App shell with navigation and layout management; Input panel with form controls and validation; Results panel with dynamic metric display; Collapsible Details component for profitability waterfall; Reusable UI primitives (NumberInput, Select, Toggle, DatePicker); Theme provider for light/dark mode switching.
  - TypeScript types and interfaces: Auto-generated types from Go structs via Wails; Frontend models matching backend entities (Deal, Campaign, IDC); Type-safe props and component interfaces; Validation schemas using Zod or similar; Error and response type definitions.
  - Tailwind CSS styling: Custom design tokens for colors, spacing, typography; Component-specific styles with utility classes; Responsive breakpoints for different screen sizes; Animation classes for transitions and micro-interactions; Dark mode variants using CSS variables.
  - Wails integration: Go struct bindings exposed to frontend; Direct function calls without HTTP overhead; Event listeners for backend push updates; File dialog integration for imports/exports; System tray and notification support.
  - Application packaging: Wails build for Windows (NSIS installer), macOS (DMG), Linux (AppImage); Code signing for Windows and macOS; Auto-update mechanism using GitHub releases or custom server; Single-instance application support; Custom application icon and metadata.
  - Modern UI features: Glassmorphism with backdrop-filter effects; Smooth scroll and parallax effects where appropriate; Skeleton loaders during async operations; Toast notifications for user feedback; Keyboard shortcuts with visual indicators; Responsive design adapting to window size.
- Interfaces and integration
  - Engine invocation through Wails runtime with Promise-based API; React Query for caching and state synchronization; Optimistic updates with error rollback.
  - Parameter service client for versioned fetch and cache; local scenario store and audit log writer using Wails file system APIs.
- Accessibility, usability, and localization
  - ARIA attributes for screen reader support; keyboard navigation with visible focus indicators; semantic HTML structure; color contrast meeting WCAG AA standards; THB formatting using Intl.NumberFormat; React-i18n for future localization support; responsive design for various screen sizes.
- Testing (required)
  - React Testing Library tests for component behavior and user interactions (e.g., baseline HP quote flow; mySTAR with balloon; campaign selection; collapsible panel state).
  - Component unit tests: Input validation for 0–80 percent down payment range with whole THB rounding; two-way binding and lock behavior for dual inputs; conditional rendering based on product selection.
  - Visual regression tests using Playwright or Cypress for key UI states across different themes and screen sizes.
  - Integration tests: React components tested against mocked Wails runtime with fixed responses; verify correct data flow and display values matching expected outputs.
  - Offline-first tests: simulate offline mode; verify app uses cached ParameterSet and computes quotes; verify version id display in header.
  - Build and packaging tests: verify executables run without external dependencies; test installer flows on target platforms; validate auto-update mechanism; test file export functionality (PDF/XLSX).

13.3 Parameter service and publisher — versioned parameters, cache, and sync
- Scope
  - Local parameter cache with versioned ParameterSet; background sync over HTTPS from a publisher backend; maker-checker publishing workflow.
  - Categories per section 4 and "Parameter catalog and governance" (section 10), including curves, spreads, PD LGD, OPEX, capital advantage, rounding rules, calendars, and campaign catalog.
- Deliverables
  - ParameterSet schema (documented) with strict validation; stable ids for categories and tables.
  - Local cache location and layout; atomic writes with checksum; corruption detection and recovery.
  - Sync protocol: fetch latest, fetch by version id, etag/if-modified-since semantics; exponential backoff; signed payloads; minimal PII in logs.
  - Publisher tools: CSV importers with header and unit validation; draft/publish with maker-checker; version notes; audit of who published what and when.
  - UI surfaces in app to show current version id and last sync time; read-only mode if importer or network is down.
- Interfaces and integration
  - App and engine consume a pinned ParameterSet via dependency injection; each calculation records the version id into audit payloads and exports.
- Testing (required)
  - JSON/schema validation tests across all categories; reject malformed or out-of-range data with precise errors.
  - Migration tests when schema evolves; ensure backward compatibility and explicit migrations.
  - Sync tests: normal, not-modified, network failures, partial outages; offline-first behavior; resume after reconnection.
  - Security tests for signature verification and permission checks in publisher workflow.
  - Determinism tests: identical ParameterSet version ids produce identical engine results on the same inputs.

13.4 Scenario, audit, and exports — persistence, reporting, and BI hooks
- Scope
  - Scenario save/open with complete input set and pinned ParameterSet id; immutable audit log for each computation.
  - Exports: PDF and XLSX that reflect UI and engine outputs with consistent rounding and presentation; scenario compare view per section 9.4.
  - BI feed stub in Phase 2 with metadata enrichment.
- Deliverables
  - Scenario file format (documented) and storage path policy; retention, naming, and user-level isolation.
  - Audit record structure: who, when, what inputs, which ParameterSet id, outputs hash, and summary metrics.
  - Export templates for PDF and XLSX (brand-compliant); watermarks and signatures; native file dialogs integration.
  - Scenario compare rendering: three-column layout with deltas and bp changes consistent with section 9.4.
- Testing (required)
  - Golden-file tests: exported PDF and XLSX parsed and numerically compared to engine outputs (allowing formatting variance but not numeric drift beyond thresholds).
  - Deterministic export tests: same input and ParameterSet yield byte-stable or hash-stable exports where feasible.
  - File permission and path tests; overwrite prompts; non-ASCII filenames; long paths where applicable.
  - Audit integrity tests: tamper-evident records; verified linkage between scenario, audit, and exports.

13.5 Cross-cutting quality gates and CI
- Test pyramid
  - Broad unit tests in engine; focused UI/integration tests; fewer but comprehensive end-to-end acceptance tests seeded by curated Excel baselines.
- Acceptance tests (build must fail on breach)
  - Installment parity within 0.01 THB on baseline cases.
  - Rate and margin parity within 1 basis point for all waterfall lines.
  - Campaign stacks match expected t0 and PV impacts for Subdown, Subinterest, Free Insurance, Free MBSP, Cash Discount.
  - RoRAC matches reference given a fixed ParameterSet.
  - UI defaults: headline-only results by default; [Details ▼] reveals full waterfall; expansion state remembered per user session.
  - Offline-first: app uses last cached ParameterSet and shows active version id in header.
- CI execution matrix
  - Run unit and golden tests on Windows; include performance benchmarks for engines; publish coverage and performance artifacts; gated merges via pull requests only.
- Definitions of Done (per subproject)
  - Engines: green unit/property/golden/benchmark suite; deterministic outputs; documented API and serialization.
  - Desktop app (Wails): green React Testing Library/component/integration tests; accessibility validation passes; platform-specific packaging tests successful; installer artifacts properly signed.
  - Parameter service and publisher: schema and migration tests green; sync and security tests green; maker-checker flow demonstrated.
  - Scenario/audit/exports: golden exports validated; deterministic hashing; audit integrity verified; scenario compare renders correctly.

13.6 Technology Migration Notes
- Previous considerations: Earlier architecture iterations considered WinUI 3 for Windows-native implementation and a Windigo-based approach. These explorations informed the current architecture but were superseded by the Wails solution.
- Current implementation: The Wails v2 framework was selected as the optimal solution, combining native desktop performance with modern web technologies.
- Rationale for Wails v2 adoption:
  - Cross-platform capability: Single codebase runs natively on Windows (primary target), macOS, and Linux, enabling future market expansion without rewriting.
  - Modern web stack: Leverages React ecosystem, TypeScript type safety, and Tailwind CSS for rapid UI development with extensive component libraries and tooling.
  - Direct Go integration: Eliminates IPC overhead through direct bindings between React frontend and Go backend, maintaining sub-300ms calculation performance targets.
  - Smaller binary size: Wails produces compact executables (typically 10-15MB) compared to Electron alternatives (100MB+), important for distribution in bandwidth-constrained regions.
  - Native OS integration: Access to native file dialogs, system tray, notifications, and OS-specific features while maintaining cross-platform compatibility.
  - Developer productivity: Hot reload during development, excellent debugging tools, and familiar web development patterns reduce time-to-market.
  - Future flexibility: Web-based UI can be easily adapted for future web deployment or mobile apps using React Native, protecting technology investment.
- Implementation approach: The functional requirements, calculation engines, and business logic remain unchanged. The Wails framework provides the optimal balance of native performance, modern UI capabilities, and cross-platform support while maintaining direct integration with the existing Go calculation engines.

Appendix A: Display formulas for reference
- Amortizing payment Pmt = r * PV / (1 − (1 + r)^{−n}) where r is periodic nominal rate.
- Effective annual rate from monthly IRR r_m: (1 + r_m)^{12} − 1.

Appendix B: Waterfall line mapping
- Customer Rate → shown nominal and effective
- IDCs and subsidies upfront → included in deal IRR
- Deal Rate IRR → effective and nominal display
- Cost of Debt matched funded → curve lookup
- Gross Interest Margin → difference of above
- Capital Advantage → per config
- Net Interest Margin → subtotal
- Cost of Credit Risk → PD LGD applied to profile
- OPEX → per product and segment
- IDC subsidies and fees periodic → net add
- Net EBIT Margin → subtotal
- Economic Capital and Acquisition RoRAC → final KPI

---

## 14. Walk UI migration — native Windows split-pane design (WIP)

Purpose
- Replace the current Wails/React shell with a native Windows UI using Walk, per stakeholder request.
- Preserve all calculation functionality by wiring UI events directly into the Go engines via existing application services.
- Deliver a modern, robust, non-overlapping split-pane layout with inputs on the left and outputs on the right.

Scope and status
- Walk entrypoint added behind build tag: [main.go](main.go:1) is disabled when building with walk tag.
- New Windows-native entrypoint: [main.main()](walk_main.go:1) created (build tag `walkui`).
- Uses existing backend app/service: [NewApp()](app.go:27) and [App.CalculateQuote()](app.go:198) to compute results with pinned ParameterSet version.

Build targets and switching UIs
- Wails UI (default): `go run .` or `wails dev` (still available).
- Walk UI: `go run -tags walkui .` (native, no webview).
- Build tags:
  - Wails entrypoint has build constraint [!walkui](main.go:1).
  - Walk entrypoint has build constraints `//go:build walkui` and `// +build walkui` in [walk_main.go](walk_main.go:1).

High-level layout (Walk)
- MainWindow
  - Root layout: VBox
  - Body: HSplitter (two panes)
    1) Left/Inputs (VBox)
       - GroupBox “Deal Inputs” (Grid 2 columns)
         - Product ComboBox: HP, mySTAR, FinanceLease, OperatingLease
         - Price (THB) LineEdit
         - Down payment percent LineEdit
         - Down payment amount LineEdit
         - Lock mode (RadioButton “Percent” / “Amount”)
         - Term months LineEdit
         - Timing ComboBox: Arrears/Advance
         - Balloon percent LineEdit
       - GroupBox “Rate Mode”
         - Radio “Fixed Rate” → enable Customer rate (% p.a.)
         - Radio “Target Installment” → enable Target installment (THB)
         - Customer rate (% p.a.) LineEdit
         - Target installment (THB) LineEdit
       - Calculate button
    2) Right/Outputs (VBox)
       - GroupBox “Key Metrics”
         - Monthly Installment
         - Customer Rate Nominal
         - Customer Rate Effective
         - Acquisition RoRAC
         - Financed Amount
       - GroupBox “Metadata”
         - Parameter Version
         - Calculated timestamp
- Initial window size 1280×860, minimum 1100×700; splitter resizable; no overlap.

Event and data flow
- All inputs write to in-memory UI state. The “lock” semantics maintain two-way binding between down payment percent and amount.
- On any EditingFinished/Selection change, call [App.CalculateQuote()](app.go:198). The function arguments mirror the existing React flow, including rate mode and IDC items (IDC UI is deferred; pass empty JSON for now).
- The JSON result is decoded to [QuoteResult](app.go:426) and displayed on the right panel (monthly installment, rates, RoRAC, financed amount, version, timestamp).

Rounding and invariants
- Display formatting:
  - Currency: use fixed 2 decimals with thousand separators.
  - Percent: use fixed 2 decimals with % sign.
- Two-way down payment lock:
  - When locked on percent, amount = price × percent/100
  - When locked on amount, percent = amount/price × 100
  - Guard against division by zero.
- Validate minimum sane value ranges in UI (e.g., term ≥ 1, price ≥ 0). Failing constraints still trigger calculation but clamp to defaults.

Compatibility notes and known issues (current blockers)
- Runtime crash observed on Windows 11/Go 1.23 with `TTM_ADDTOOL failed` originating from Walk’s tooltip handling during widget initialization:
  - Stack trace shows failure inside `walk.(*ToolTip).addTool` called by `WidgetBase.init`.
  - This appears to be a compatibility regression in `github.com/lxn/walk` (archived) with newer Windows/Go/ComCtl stacks.
- Mitigation plan:
  1) Attempt to disable any implicit tooltip creation:
     - Avoid declarative properties that might set ToolTipText (none are set today).
     - Evaluate creating the MainWindow with minimal styles and no tooltip associations.
     - If feasible, fork Walk and guard tooltip creation when text is empty (skip `TTM_ADDTOOL`).
  2) Ensure proper Common Controls v6 activation:
     - Embed a manifest enabling ComCtl32 v6 for “go build” artifacts (not effective for `go run`).
     - Integrate a manifest pipeline (e.g., goversioninfo) and test if it stabilizes tooltip behavior.
  3) Evaluate maintained forks (community forks that addressed tooltip/ComCtl changes).
  4) If Walk cannot be reliably stabilized, re-evaluate alternate native toolkits (WinUI3 via CGo bridge, Fyne, Gio) while preserving the split-pane UX and keeping the engines unchanged.

Build & run instructions
- Prereqs: Windows, Go toolchain, MSVC runtime (as required by engines); no NPM needed for Walk build.
- Run Walk UI (native):
  - Development: `go run -tags walkui .`
  - Build: `go build -tags walkui -o financial-calculator-walk.exe .`
- Switch back to Wails UI:
  - `wails dev` or `go run .` (Wails is compiled only when `walkui` tag is absent).

Mapping from React components to Walk controls
- Input panel (React) → “Deal Inputs” + “Rate Mode” group boxes on left pane:
  - Product selector: React ToggleGroup/Select → Walk ComboBox
  - Price, down payment fields, balloon: React NumberInput → Walk LineEdit with numeric parsing
  - Lock semantics: React local state → Walk RadioButtons + SetChecked() initialization
  - Timing: React Select → Walk ComboBox
  - Rate mode and fields: React radio + inputs → Walk RadioButtons + enable/disable LineEdits
- Results panel (React) → “Key Metrics” group box on right pane:
  - Monthly installment, rates, RoRAC, financed amount
  - Metadata version and timestamp

Extensibility (next phases)
- Add IDC dialog/table on Walk side mirroring the React Advanced Drawer behavior:
  - IDC items (type, timing, financed, amount, tax, payer)
  - Add/Update/Remove; pass serialized IDC list to [App.CalculateQuote()](app.go:198)
- Add details waterfall (deal IRR, cost of debt matched funded, gross/net margins, risk, opex, EBIT, RoRAC) in an expandable group.
- Export and scenario save:
  - Reuse JSON export format created in the React UI and bound Go methods for scenario IO.

Acceptance criteria for Walk UI
- No overlapping controls at minimum and common sizes; splitter allows comfortable resizing.
- All inputs cause deterministic recalculation within sub-300ms in typical cases.
- Metric values match Wails UI for identical inputs (numeric parity within 0.01 THB / 1 bp where applicable).
- Display is stable under rapid edits (no panic, no crash).
- Clearly visible ParameterSet version and calculation timestamp.

Files added/changed
- New entrypoint [walk_main.go](walk_main.go:1) with `//go:build walkui` and legacy build tag for compatibility.
- Build exclusion tag added at top of [main.go](main.go:1) to disable Wails when `walkui` is used.

Open issues tracker (Walk)
- [ ] Fix TTM_ADDTOOL crash by disabling tooltip creation or applying a manifest/controls fix.
- [ ] Evaluate maintained forks for Walk; vendor if necessary.
- [ ] Implement IDC UI and Details waterfall after baseline UI stabilization.
- [ ] Add unit tests for input parsing/locking and output formatting at the UI boundary.
