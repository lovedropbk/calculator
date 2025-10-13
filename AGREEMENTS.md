# Agreements: Simplified Subsidy/IDC Model

Date: 2025-10-13

- Subdown and Cash Discount consume subsidy budget explicitly.
- Remaining (unused) subsidy budget is treated as subsidy income that increases Deal IRR (applied upfront in the simplified model).
- Free Insurance and Free MBSP are IDC costs. They do not consume subsidy budget; they reduce IRR independently.
- Subinterest: derive needed subsidy to meet the target customer nominal rate; set nominal rate to target, apply remaining budget as income if any.
- Dealer Commission and IDC Other are IDC costs (do not consume subsidy); they reduce IRR.
- Campaign Details and Key Metrics show gross costs separately from subsidy utilization/remaining.
- Copy button is handled via code-behind click handler invoking ViewModel command, avoiding DataTemplate binding issues.

Open items:
- Confirm formula for deriving required subsidy for subinterest targets across terms/products.
- Remove or update any remaining docs that assume proportional netting of costs.
