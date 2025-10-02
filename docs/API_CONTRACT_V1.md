# API Contract v1 — WinUI 3 ↔ Go Services

This document captures the frozen contract for the WinUI 3 client and the Go services (fc-api / fc-svc) for the MVP plus decision-first UI wiring.

Endpoints
- GET /api/v1/campaigns/catalog → []Campaign
- POST /api/v1/campaigns/summaries → []CampaignSummary
- POST /api/v1/calculate → CalculationResult
- GET /api/v1/commission/auto?product=HP → { product, percent, policyVersion }

Shapes (selected)
- CampaignSummary: { campaign_id: string, campaign_type: string, dealerCommissionAmt: number, dealerCommissionPct: number }
- Quote.schedule[]: elements have at least { principal, interest, balance, fee? or fees?, amount? or cashflow? }
- Quote.profitability: { acquisition_rorac: number }

Notes
- WinUI requests calculate with options.derive_idc_from_cf=true to populate separation lines for insights; it ignores periodic IDC separation for now.
- Dealer Commission policy is resolved via GET /commission/auto. Negative percentages are clamped to 0 at source; Cash Discount rows force 0 THB and 0% in summaries.
