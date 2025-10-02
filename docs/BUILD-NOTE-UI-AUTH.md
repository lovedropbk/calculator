Implementation log for Authoritative UI SOT

- Extracted DTOs into WinUI Models (winui3-mvp/FinancialCalculator.WinUI3/Models/Dtos.cs)
- Introduced Services/ApiClient.cs with resilient JSON parsing for schedule and campaign audit
- Wired commission auto policy via GET /api/v1/commission/auto; pill now shows mode, pct derived, and resolved THB
- Added Dealer Commission controls: Reset to auto, Override toggle; reactive resolution on product change
- Enabled Copy Selected → My Campaigns; command parameter and CanExecute
- Created docs/API_CONTRACT_V1.md to freeze current contract alignment
- Updated TASK_PROGRESS.md status and milestones

Next
- B1-B3: Add missing grid columns (Subdown, Free Insurance, MBSP, Cash Discount), sort toggle, viability/notes tooltip
- C2: My Campaigns persistence (AppData JSON parity with Walk)
- E1-E3: Campaign Details fill-outs, cashflow bindings verification, target installment guidance
