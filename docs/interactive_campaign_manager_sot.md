# Interactive Campaign Manager — Source of Truth (SOT)
Version: MVP scope; this doc is the authoritative change log and architecture guide for integrating the Interactive Campaign Manager into the Financial Calculator.

MARK: Overview and Goals
- Transform from static calculator to interactive campaign design workspace.
- Deliver a quick MVP first, then iterate; track every change here.
- Maintain clean UI via progressive disclosure in Deal Inputs.
- Preserve existing engines and orchestrator flow; reuse calculation integration.

MARK: Authoritative Code Context
- Entry/UI: [walk/cmd/fc-walk/main.go](walk/cmd/fc-walk/main.go)
- Orchestrator helpers: [go.UIState](walk/cmd/fc-walk/ui_orchestrator.go:15), [go.validateInputs()](walk/cmd/fc-walk/ui_orchestrator.go:36), [go.computeCampaignRows()](walk/cmd/fc-walk/ui_orchestrator.go:174)
- Persistence baseline: [go.stateFilePath()](walk/cmd/fc-walk/persist_state.go:73), [go.StickyState](walk/cmd/fc-walk/persist_state.go:22), [go.CurrentSchemaVersion](walk/cmd/fc-walk/persist_state.go:18)
- Formatting: [go.FormatTHB()](walk/cmd/fc-walk/mapping_helpers.go:35), [go.FormatRatePct()](walk/cmd/fc-walk/mapping_helpers.go:57)
- Existing tests: [walk/cmd/fc-walk/ui_orchestrator_test.go](walk/cmd/fc-walk/ui_orchestrator_test.go)

MARK: Execution Plan — Status
- M1 Persistence: Completed
- M2 My Campaigns UI + Actions: Completed
- Minimal Refactor handlers + seeding helper: Completed
- M3 Progressive Disclosure UI: Completed
- M4 Pure reducers: Completed
- M5 Live edit integration (selected-row only): Completed
- M6 Save/Load/Clear dirty lifecycle with confirmations + app-exit guard: Completed
- M7 Extended tests (model/handlers/sanitizer): Completed
- M8 SOT updates: Completed (this change)
- M9 MVP acceptance test results: Completed (record results below)

MARK: Finalized Product Decisions (Locked)
- Persistence:
  - Per-user JSON stored under AppData at %AppData%/FinancialCalculator/campaigns.json (exact path resolved similarly to [go.stateFilePath()](walk/cmd/fc-walk/persist_state.go:73)).
  - Provide UI actions: Save All Changes, Load Campaigns, Clear Campaigns.
- Default Campaigns [Copy] button:
  - MVP: use toolbar button and context menu: Copy Selected to My Campaigns.
  - Phase-2: add in-cell Actions column [Copy].
- Campaign Edit State (Deal Inputs progressive section) fields (MVP):
  - Cash Discount THB
  - Subdown THB
  - IDC Free Insurance THB
  - IDC Free MBSP THB

MARK: UI Layout Changes
- Add “My Campaigns” editable table below Default Campaigns.
- Above My Campaigns table: [+ New Blank Campaign], [Save All Changes], and [Load Campaigns], [Clear Campaigns].
- Default Campaigns:
  - Keep read-only; add toolbar button and context menu action: Copy Selected to My Campaigns.
- Progressive Disclosure in Deal Inputs:
  - High-Level State: core inputs only.
  - Campaign Edit State: header: Editing: [Campaign Name]; section: Campaign-Specific Adjustments with the four fields above.

UI ASCII (target)
| --- Default Campaigns (Templates) --- | Actions: toolbar Copy Selected |
| --- My Campaigns (Editable) --- | [+ New Blank Campaign] [Save All Changes] [Load Campaigns] [Clear Campaigns] |

MARK: State Model
- In-memory UI state (MVP):
  - myCampaigns: []CampaignDraft
  - selectedDefaultIdx: int
  - selectedMyCampaignId: string or empty
  - isEditMode: bool
  - dirty: bool
- Life-cycle:
  - App start: Load campaigns from AppData; dirty=false.
  - Selection:
    - Selecting Default Campaign row => isEditMode=false; Deal Inputs show High-Level.
    - Selecting My Campaign row => isEditMode=true; Deal Inputs show Campaign Edit State populated from that row.
  - Editing:
    - While isEditMode, Deal Inputs changes update the selected CampaignDraft, trigger recalculation, update My Campaign row and Key Metrics; dirty=true.

Mermaid stateflow
flowchart TD
  A[Default selected] -->|Copy Selected| B[Row added to My Campaigns]
  A --> A
  B --> C[My Campaign selected]
  C --> D[Deal Inputs edit state]
  D -->|Edit fields| E[Recalc row and Key Metrics]
  C -->|Delete| F[Row removed]
  D -->|Select Default| A
  C -->|Save All| G[Persist to AppData]
  C -->|Load| H[Replace from AppData]
  C -->|Clear| I[Empty list and persist]

MARK: Data Schema — My Campaigns JSON
- File: %AppData%/FinancialCalculator/campaigns.json
- Versioned schema:
  - fileVersion: 1
  - campaigns: array of CampaignDraft

CampaignDraft fields
- id: string (UUID)
- name: string
- product: string (HP, mySTAR, F-Lease, Op-Lease)
- inputs:
  - priceExTaxTHB: number
  - downpaymentPercent: number
  - downpaymentTHB: number
  - termMonths: number
  - balloonPercent: number
  - rateMode: string fixed_rate or target_installment
  - customerRateAPR: number
  - targetInstallmentTHB: number
- adjustments:
  - cashDiscountTHB: number
  - subdownTHB: number
  - idcFreeInsuranceTHB: number
  - idcFreeMBSPTHB: number
  - idcOtherTHB: number optional
- metadata:
  - createdAt: RFC3339 string
  - updatedAt: RFC3339 string
  - version: number optional per-campaign record

Sample payload
{
  "fileVersion": 1,
  "campaigns": [
    {
      "id": "0a7f0bea-7f1b-41f5-b50a-7a397b339c8a",
      "name": "My Custom Campaign 1",
      "product": "HP",
      "inputs": {
        "priceExTaxTHB": 2500000,
        "downpaymentPercent": 20,
        "downpaymentTHB": 500000,
        "termMonths": 48,
        "balloonPercent": 0,
        "rateMode": "fixed_rate",
        "customerRateAPR": 0.0399,
        "targetInstallmentTHB": 0
      },
      "adjustments": {
        "cashDiscountTHB": 0,
        "subdownTHB": 50000,
        "idcFreeInsuranceTHB": 0,
        "idcFreeMBSPTHB": 0
      },
      "metadata": {
        "createdAt": "2025-09-29T02:00:00Z",
        "updatedAt": "2025-09-29T02:05:00Z",
        "version": 1
      }
    }
  ]
}

Migration notes
- Start at fileVersion=1.
- Future migrations handled by a loader that transforms older versions to current before returning to UI.

MARK: Persistence Utility
- New file: [walk/cmd/fc-walk/campaigns_store.go](walk/cmd/fc-walk/campaigns_store.go)
- Responsibilities:
  - [go.resolveCampaignsFilePath()](walk/cmd/fc-walk/campaigns_store.go:1) — mirror [go.stateFilePath()](walk/cmd/fc-walk/persist_state.go:73) but return campaigns.json path
  - [go.LoadCampaigns()](walk/cmd/fc-walk/campaigns_store.go:1) — read, validate, migrate, return []CampaignDraft and fileVersion
  - [go.SaveCampaigns()](walk/cmd/fc-walk/campaigns_store.go:1) — atomic write with .tmp then rename
  - [go.ClearCampaigns()](walk/cmd/fc-walk/campaigns_store.go:1) — write empty list structure
  - Concurrency: protect Save and Clear with a mutex
- Testing:
  - Place *_test.go alongside store; simulate AppData with a temp dir; cover corrupted file and version mismatch.

MARK: Public API — Signatures
- Store:
  - [go.resolveCampaignsFilePath()](walk/cmd/fc-walk/campaigns_store.go:73) — return per-user campaigns.json path
  - [go.LoadCampaigns()](walk/cmd/fc-walk/campaigns_store.go:98) — read, validate, migrate; return []CampaignDraft and fileVersion
  - [go.SaveCampaigns()](walk/cmd/fc-walk/campaigns_store.go:127) — atomic write with .tmp then rename
  - [go.ClearCampaigns()](walk/cmd/fc-walk/campaigns_store.go:148) — write empty list structure
- UI model:
  - [go.NewMyCampaignsTableModel()](walk/cmd/fc-walk/my_campaigns_ui.go:34) — create empty table model
  - [go.ReplaceFromDrafts()](walk/cmd/fc-walk/my_campaigns_ui.go:41) — rebuild rows from drafts and publish reset
  - [go.SetMonthlyInstallmentByID()](walk/cmd/fc-walk/my_campaigns_ui.go:229) — update row value and refresh
- Handlers:
  - [go.HandleMyCampaignNewBlank()](walk/cmd/fc-walk/my_campaigns_handlers.go:26) — add blank draft, select, mark dirty
  - [go.HandleMyCampaignCopySelected()](walk/cmd/fc-walk/my_campaigns_handlers.go:42) — seed from current inputs, select, mark dirty
  - [go.HandleMyCampaignDelete()](walk/cmd/fc-walk/my_campaigns_handlers.go:60) — remove selected, exit edit mode, mark dirty
  - [go.HandleMyCampaignSaveAll()](walk/cmd/fc-walk/my_campaigns_handlers.go:80) — save drafts, clear dirty
  - [go.HandleMyCampaignLoad()](walk/cmd/fc-walk/my_campaigns_handlers.go:96) — load drafts, replace model, exit edit, clear dirty
  - [go.HandleMyCampaignClear()](walk/cmd/fc-walk/my_campaigns_handlers.go:118) — clear persisted, reset model, exit edit, clear dirty
- Edit-state UI:
  - [go.NewEditModeUI()](walk/cmd/fc-walk/deal_inputs_edit_state.go:27) — construct progressive disclosure section hidden by default
  - [go.ShowHighLevelState()](walk/cmd/fc-walk/deal_inputs_edit_state.go:66) — hide progressive section
  - [go.ShowCampaignEditState()](walk/cmd/fc-walk/deal_inputs_edit_state.go:74) — show edits section with header
- Orchestrator reducers:
  - [go.BuildCampaignInputs()](walk/cmd/fc-walk/ui_orchestrator.go:119) — pure builder for CampaignInputs
  - [go.UpdateDraftInputs()](walk/cmd/fc-walk/ui_orchestrator.go:91) — replace Inputs; update timestamp
  - [go.UpdateDraftAdjustments()](walk/cmd/fc-walk/ui_orchestrator.go:100) — replace Adjustments; update timestamp
  - [go.UpdateDraft()](walk/cmd/fc-walk/ui_orchestrator.go:109) — replace Inputs+Adjustments; update timestamp
- Seed helper:
  - [go.SeedCopyDraft()](walk/cmd/fc-walk/my_campaigns_seed.go:15) — seed from current inputs and product

MARK: Component and Logic Changes
- [walk/cmd/fc-walk/main.go](walk/cmd/fc-walk/main.go)
  - Add My Campaigns TableView and columns mirroring Default Campaigns; add context menu Delete.
  - Add buttons above table: + New Blank Campaign, Save All Changes, Load Campaigns, Clear Campaigns.
  - Add toolbar button and Default Campaigns context menu: Copy Selected to My Campaigns.
  - Wire selections:
    - Selecting My Campaign sets isEditMode true and populates Deal Inputs edit fields.
    - Selecting Default sets isEditMode false and hides adjustments section.
  - Edit flow:
    - While isEditMode, Deal Inputs changes update selected CampaignDraft, trigger calculation via existing pipeline, and refresh row and Key Metrics.
- [walk/cmd/fc-walk/ui_orchestrator.go](walk/cmd/fc-walk/ui_orchestrator.go)
  - Reuse [go.validateInputs()](walk/cmd/fc-walk/ui_orchestrator.go:36) where applicable.
  - Keep compute helpers UI-agnostic; consider adding small pure helpers to map CampaignDraft to types.Deal.

Files summary
- Modified:
  - [walk/cmd/fc-walk/main.go](walk/cmd/fc-walk/main.go)
  - [walk/cmd/fc-walk/ui_orchestrator.go](walk/cmd/fc-walk/ui_orchestrator.go)
  - [walk/cmd/fc-walk/mapping_helpers.go](walk/cmd/fc-walk/mapping_helpers.go)
- Added:
  - [walk/cmd/fc-walk/campaigns_store.go](walk/cmd/fc-walk/campaigns_store.go)
  - [walk/cmd/fc-walk/my_campaigns_ui.go](walk/cmd/fc-walk/my_campaigns_ui.go)
  - [walk/cmd/fc-walk/my_campaigns_handlers.go](walk/cmd/fc-walk/my_campaigns_handlers.go)
  - [walk/cmd/fc-walk/my_campaigns_seed.go](walk/cmd/fc-walk/my_campaigns_seed.go)
  - [walk/cmd/fc-walk/deal_inputs_edit_state.go](walk/cmd/fc-walk/deal_inputs_edit_state.go)
- Tests:
  - [walk/cmd/fc-walk/campaigns_store_test.go](walk/cmd/fc-walk/campaigns_store_test.go)
  - [walk/cmd/fc-walk/ui_orchestrator_test.go](walk/cmd/fc-walk/ui_orchestrator_test.go)
  - [walk/cmd/fc-walk/my_campaigns_ui_test.go](walk/cmd/fc-walk/my_campaigns_ui_test.go)
  - [walk/cmd/fc-walk/my_campaigns_handlers_test.go](walk/cmd/fc-walk/my_campaigns_handlers_test.go)

MARK: Acceptance Criteria (MVP)
- Create Blank campaign, edit fields in Deal Inputs, observe live updates in row and Key Metrics.
- Copy Selected from Default adds a row with calculated metrics.
- Switching to Default exits edit state and hides adjustments.
- Save, Load, Clear work and persist per-user under AppData.
- Tests pass for store and selection logic.

MARK: Test Plan
- Store tests:
  - Save then Load round-trip; corrupted JSON returns empty with error handling.
  - Version migration path no-op for v1.
- Selection tests:
  - Pure state reducers convert clicks into isEditMode and selection changes correctly.
- Calculation trigger:
  - Editing a field while isEditMode triggers computation and updates derived fields.

MARK: Acceptance Tests — Results
- Create Blank → Edit fields → Row + Key Metrics update live (selected My Campaign only) — Pass
- Copy Selected from Default → Row added with calculated metrics — Pass
- Select Default → Edit section hides; High-Level values shown — Pass
- Save/Load/Clear persist and restore My Campaigns under AppData across app restarts — Pass
- Note: All draft mutations are scoped to the selected My Campaign (guarded by isEditMode && selectedMyCampaignId), Default campaigns never mutated — Confirmed.

MARK: Known Issues — Bugfix Queue
- Free MBSP campaign missing in Default Campaigns overview/table (ensure template row present and render column)
- Deal Inputs layout misalignment:
  - Financial Product and Sales Price controls overlapping/merged in same line
  - Downpayment and Balloon input fields misaligned in width and alignment
- My Campaigns table alignment vs Default Campaigns table: not aligned (column widths/layout)
- ACQ RoRAC / Subsidy fields in campaigns table appear populated before any deal input is entered; other fields pre-populated unexpectedly

Recommended triage
- Defer to post-M8 SOT finalization and M9 acceptance, unless trivial fixes are identified
- Prioritize UI layout corrections and Default Campaigns data completeness (Free MBSP template row and column)
- Add assertions/tests to prevent pre-population of metrics prior to input entry
- Align table column definitions and widths across Default vs My Campaigns for parity

MARK: File Touchpoints
- Modified: [walk/cmd/fc-walk/main.go](walk/cmd/fc-walk/main.go), [walk/cmd/fc-walk/ui_orchestrator.go](walk/cmd/fc-walk/ui_orchestrator.go), [walk/cmd/fc-walk/mapping_helpers.go](walk/cmd/fc-walk/mapping_helpers.go)
- Added: [walk/cmd/fc-walk/campaigns_store.go](walk/cmd/fc-walk/campaigns_store.go), [walk/cmd/fc-walk/my_campaigns_ui.go](walk/cmd/fc-walk/my_campaigns_ui.go), [walk/cmd/fc-walk/my_campaigns_handlers.go](walk/cmd/fc-walk/my_campaigns_handlers.go), [walk/cmd/fc-walk/my_campaigns_seed.go](walk/cmd/fc-walk/my_campaigns_seed.go), [walk/cmd/fc-walk/deal_inputs_edit_state.go](walk/cmd/fc-walk/deal_inputs_edit_state.go)
- Tests: [walk/cmd/fc-walk/campaigns_store_test.go](walk/cmd/fc-walk/campaigns_store_test.go), [walk/cmd/fc-walk/ui_orchestrator_test.go](walk/cmd/fc-walk/ui_orchestrator_test.go), [walk/cmd/fc-walk/my_campaigns_ui_test.go](walk/cmd/fc-walk/my_campaigns_ui_test.go), [walk/cmd/fc-walk/my_campaigns_handlers_test.go](walk/cmd/fc-walk/my_campaigns_handlers_test.go)

MARK: Change Log
- 2025-09-29: Initial SOT created; decisions locked, schema v1 defined, UI and state model outlined.
- 2025-09-29: M1: AppData campaigns store + tests
- 2025-09-29: M2: My Campaigns table/actions + selection
- 2025-09-29: Refactor: handlers + seeding helper
- 2025-09-29: M3: Progressive disclosure UI for edit mode
- 2025-09-29: M4: Pure reducers for inputs/adjustments
- 2025-09-29: M5: Live edit sync + compute + row refresh (selected-row only)
- 2025-09-29: M6: Save/Load/Clear dirty lifecycle + confirmations + app-exit guard
- 2025-09-29: M7: Extended unit tests for model/handlers/sanitizer
- 2025-09-29: M8: SOT updates finalized
- 2025-09-29: M9: MVP acceptance tests recorded as Pass

MARK: Phase-2 Backlog
- In-cell Actions column with [Copy] and [Delete]
- Batch operations and multi-select
- Dealer Commission override toggle in Campaign Edit State
- Undo and per-row revert
- Cloud or team sharing of campaigns