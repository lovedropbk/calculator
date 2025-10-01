# Subsidy & IDC Fix - Understanding

## Tool Purpose
Sales tool for comparing campaign profitability. User enters ONE vehicle deal, compares different campaign options side-by-side to choose which to run.

## Key Concepts

### Subsidy Budget
- **200k available PER campaign row** (each is independent)
- Only ONE campaign will actually be chosen
- Show how much budget each option uses + leftover

### IDCs (Initial Direct Costs)
- **NOT financed to customer** - customer monthly is based on vehicle price only
- **Upfront costs paid by finance company** (dealer commission, insurance, MBSP, etc.)
- **Reduce profitability** via Acquisition RoRAC calculation
- `Financed: false` always

### Subsidies
- **Offset IDC costs** to improve finance company profitability
- Can be applied as T0 upfront OR periodic (amortized over term)
- **Net IDC = Total IDC Costs - Subsidies**
- Net IDC impacts Deal IRR → Acquisition RoRAC

## Gross Presentation (Key Fix)

**Show costs SEPARATELY from subsidies:**

```
Free MBSP Campaign:
├─ IDC - MBSP Cost: 150,000 (actual cost, ALWAYS shown)
├─ Subsidy Utilized: 150,000 (periodic income, amortized)
├─ Net Effect: 0 (fully covered)
└─ RoRAC: HIGH
```

**If cost > budget:**
```
Free MBSP (high cost):
├─ IDC - MBSP Cost: 250,000 (actual cost)
├─ Subsidy Utilized: 200,000 (budget limit)
├─ Net Cost: 50,000 (finance company absorbs)
└─ RoRAC: LOWER (50k loss impact)
```

## Campaign Type Effects

| Type | Changes | Customer Impact | Finance Company Impact |
|------|---------|-----------------|----------------------|
| Subdown | DP ↑ by subsidy | Financed ↓, Monthly ↓ | Uses subsidy budget |
| Subinterest | Nominal rate ↓ | Monthly ↓, Rate ↓ | Uses subsidy budget (periodic) |
| Free Insurance | Finance pays insurance IDC | Monthly unchanged | IDC cost - subsidy = net impact |
| Free MBSP | Finance pays MBSP IDC | Monthly unchanged | IDC cost - subsidy = net impact |
| Cash Discount | Price ↓ (non-finance) | No monthly (cash sale) | N/A |

## Required Columns

**Default Campaigns Table:**
```
[Copy] [Select] [Campaign] [Monthly] [DP] [Subdown] [Cash] [Free Ins] [MBSP] [Subsidy Used] [RoRAC] [Dealer] [Notes]
```

**My Campaigns Table:** Same minus Copy column

## My Campaigns - Full Interactivity

**User can combine ANY adjustments:**
```
Example:
├─ Subdown: 50,000
├─ Free Insurance Cost: 80,000
├─ Free MBSP Cost: 150,000
Total needed: 280,000
Budget: 200,000

Calculation:
├─ All costs applied as IDCs
├─ Subsidy utilized: 200,000 (budget limit)
├─ Net Cost: 80,000 (reduces RoRAC)
```

**Priority when budget insufficient:** Apply all costs, cap subsidy at budget, show net cost impact.

## Real-Time Updates Required

When user edits ANY field in My Campaigns edit mode:
1. Recalculate subsidy utilized
2. Recalculate net IDC
3. Recalculate cashflows
4. Recalculate monthly installment
5. Recalculate customer rates
6. Recalculate Acquisition RoRAC
7. **Update all 3 panels:** Deal Inputs, Campaign Details, Key Metrics

## Display Requirements

**Campaign Details Panel:**
- Show actual costs (Free Insurance, MBSP)
- Show Subsidy Budget / Utilized / Remaining
- Show campaign-specific fields (Subdown, Cash Discount)

**Key Metrics Panel:**
- Monthly Installment
- Customer Nominal & Effective Rate
- Financed Amount
- Acquisition RoRAC
- IDC Total / breakdown

**Both panels must reflect selected campaign at all times.**

## Current Issues

1. ❌ Hardcoded costs (50k insurance, 150k MBSP) instead of actual/configurable
2. ❌ No Free Insurance column in tables
3. ❌ Incorrect subsidy calculation (proportional scaling instead of budget cap)
4. ❌ Costs shown netted instead of gross
5. ❌ Subsidy Utilized field doesn't reflect actual usage
6. ❌ No "Subsidy Remaining" display

## Implementation Steps

1. Add Free Insurance column to both tables
2. Add `FreeInsuranceCostTHB` field to data structures
3. Update `computeMyCampaignRow()` to use gross presentation
4. Fix subsidy calculation: `min(total_costs, budget)` not proportional
5. Update responsive layout to include new column
6. Ensure real-time updates work for all campaign types
7. Update Campaign Details to show gross costs + subsidy tracking
