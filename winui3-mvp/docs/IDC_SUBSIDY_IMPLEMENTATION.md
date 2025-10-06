# IDC vs Subsidy Separation Implementation Summary

## Overview
This document summarizes the implementation of IDC (Internal Deal Costs) vs Subsidy separation in the Financial Calculator WinUI3 application. The implementation enables distinct tracking and reporting of IDC costs and subsidies in profitability calculations.

## Architecture Changes

### Data Transfer Objects (DTOs)

#### New DTO: `IdcItemDto`
Located in: [`Models/Dtos.cs`](../FinancialCalculator.WinUI3/Models/Dtos.cs:48-57)

```csharp
public class IdcItemDto
{
    [JsonPropertyName("category")] public string Category { get; set; }
    [JsonPropertyName("amount")] public double Amount { get; set; }
    [JsonPropertyName("financed")] public bool Financed { get; set; }
    [JsonPropertyName("timing")] public string Timing { get; set; }
    [JsonPropertyName("is_revenue")] public bool IsRevenue { get; set; }
    [JsonPropertyName("is_cost")] public bool IsCost { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; }
}
```

#### Enhanced `CalculationRequestDto`
- Added `IdcItems` property: `List<IdcItemDto>`
- Enables submission of multiple IDC items with calculations

#### Extended `ProfitabilityDto`
New fields for separated tracking:
- `IDCUpfrontCostPct`: IDC upfront costs as percentage
- `IDCPeriodicCostPct`: IDC periodic costs as percentage
- `SubsidyUpfrontPct`: Subsidy upfront as percentage
- `SubsidyPeriodicPct`: Subsidy periodic as percentage

### API Contract Mappings

#### IDC Categories
| UI Element | API Category | Status |
|------------|--------------|--------|
| Dealer Commission | `broker_commission` | ⚠️ Needs verification |
| IDC Other | `internal_processing` | ⚠️ Needs verification |

#### Campaign Parameters
| Campaign Type | UI Field | API Parameter | Status |
|---------------|----------|---------------|--------|
| SubDown | FSSubDownAmount | `subsidy_amount` | ⚠️ Needs verification |
| Free Insurance | FSSubInterestAmount | `insurance_cost` | ⚠️ Needs verification |
| Free MBSP | FSFreeMBSPAmount | `mbsp_cost` | ⚠️ Needs verification |
| Cash Discount | CashDiscountAmount | `discount_amount` | ⚠️ Needs verification |

### ViewModels Changes

#### MainViewModel Enhancements
Located in: [`ViewModels/MainViewModel.cs`](../FinancialCalculator.WinUI3/ViewModels/MainViewModel.cs)

1. **New Properties for Separated Display**:
   ```csharp
   // Lines 518-522
   private double _wfIDCUpfrontCostPct;
   private double _wfIDCPeriodicCostPct;
   private double _wfSubsidyUpfrontPct;
   private double _wfSubsidyPeriodicPct;
   ```

2. **IDC Items Construction** (Lines 298-327, 356-384):
   ```csharp
   var idcItems = new List<IdcItemDto>();
   
   // Add Dealer Commission IDC item
   if (DealerCommissionResolvedAmt > 0)
   {
       idcItems.Add(new IdcItemDto
       {
           Category = "broker_commission", // TODO: Verify with backend
           Amount = DealerCommissionResolvedAmt,
           Financed = true,
           Timing = "upfront",
           IsCost = true,
           IsRevenue = false
       });
   }
   
   // Add IDC Other item
   if (IdcOther > 0)
   {
       idcItems.Add(new IdcItemDto
       {
           Category = "internal_processing", // TODO: Verify with backend
           Amount = IdcOther,
           Financed = true,
           Timing = "upfront",
           IsCost = true,
           IsRevenue = false
       });
   }
   ```

3. **Campaign Parameters Building** (Lines 431-471):
   - Method: `BuildCampaignParameters(CampaignSummaryViewModel campaign)`
   - Maps UI values to API parameters for My Campaigns
   - Only applies to user-edited campaigns

### API Client Updates

#### Enhanced Response Parsing
Located in: [`Services/ApiClient.cs`](../FinancialCalculator.WinUI3/Services/ApiClient.cs:82-87)

```csharp
// Safely extract the new separated IDC/Subsidy fields
// These fields may not exist in older responses, so we default to 0
IDCUpfrontCostPct = prof.TryGetProperty("idc_upfront_cost_pct", out var iucPct) 
    && iucPct.ValueKind == JsonValueKind.Number ? iucPct.GetDouble() : 0.0,
IDCPeriodicCostPct = prof.TryGetProperty("idc_periodic_cost_pct", out var ipcPct) 
    && ipcPct.ValueKind == JsonValueKind.Number ? ipcPct.GetDouble() : 0.0,
SubsidyUpfrontPct = prof.TryGetProperty("subsidy_upfront_pct", out var suPct) 
    && suPct.ValueKind == JsonValueKind.Number ? suPct.GetDouble() : 0.0,
SubsidyPeriodicPct = prof.TryGetProperty("subsidy_periodic_pct", out var spPct) 
    && spPct.ValueKind == JsonValueKind.Number ? spPct.GetDouble() : 0.0,
```

### UI Display Updates

#### Profitability Details Panel
New fields displayed:
- IDC Upfront Cost %
- IDC Periodic Cost %
- Subsidy Upfront %
- Subsidy Periodic %

#### Export Functionality
Enhanced export includes separated values (Lines 698-702):
```csharp
sb.AppendLine("Separated Values:");
sb.AppendLine($"IDC Upfront Cost %,{_wfIDCUpfrontCostPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
sb.AppendLine($"IDC Periodic Cost %,{_wfIDCPeriodicCostPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
sb.AppendLine($"Subsidy Upfront %,{_wfSubsidyUpfrontPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
sb.AppendLine($"Subsidy Periodic %,{_wfSubsidyPeriodicPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
```

## Configuration Requirements

### Environment Variables
- `FC_API_BASE`: Backend API base URL (default: `http://localhost:8123/`)

### Backend Requirements
1. Support for `idc_items` array in calculation requests
2. Return separated IDC/Subsidy fields in profitability response
3. Accept campaign parameters for custom campaigns
4. Provide parameter set via `/api/v1/parameters/current`

## Request/Response Examples

### Sample Calculation Request
```json
{
  "deal": {
    "product": "HP",
    "price_ex_tax": 1000000,
    "down_payment_amount": 200000,
    "term_months": 36,
    "rate_mode": "fixed_rate",
    "customer_nominal_rate": 0.0399
  },
  "campaigns": [
    {
      "id": "custom_subdown_01",
      "type": "subdown",
      "parameters": {
        "subsidy_amount": 50000
      }
    }
  ],
  "idc_items": [
    {
      "category": "broker_commission",
      "amount": 24000,
      "financed": true,
      "timing": "upfront",
      "is_cost": true,
      "is_revenue": false
    },
    {
      "category": "internal_processing",
      "amount": 100000,
      "financed": true,
      "timing": "upfront",
      "is_cost": true,
      "is_revenue": false
    }
  ],
  "parameter_set": { /* cached parameters */ },
  "options": {
    "derive_idc_from_cf": true
  }
}
```

### Sample Calculation Response
```json
{
  "quote": {
    "monthly_installment": 23500,
    "customer_rate_nominal": 0.0399,
    "customer_rate_effective": 0.0407,
    "profitability": {
      "deal_irr_effective": 0.0523,
      "deal_irr_nominal": 0.0510,
      "idc_subsidies_fees_upfront": 0.05,
      "idc_subsidies_fees_periodic": 0.02,
      "idc_upfront_cost_pct": 0.03,
      "idc_periodic_cost_pct": 0.01,
      "subsidy_upfront_pct": 0.02,
      "subsidy_periodic_pct": 0.01,
      "net_ebit_margin": 0.0123,
      "acquisition_rorac": 0.1234
    }
  },
  "schedule": [ /* cashflow rows */ ]
}
```

## Troubleshooting Guide

### Common Issues and Solutions

#### Issue 1: IDC items not appearing in backend requests
**Symptoms**: IDC values show in UI but not in API requests
**Possible Causes**:
- Zero or negative values (excluded by design)
- Calculation not triggered after value changes
**Solution**: 
- Ensure values > 0
- Click Calculate button after changes

#### Issue 2: Separated fields show 0.00%
**Symptoms**: All separated IDC/Subsidy fields display as 0.00%
**Possible Causes**:
- Backend doesn't support new fields yet
- Response parsing issues
**Solution**:
- Verify backend version supports separated fields
- Check network response for field presence
- Implementation handles missing fields gracefully (defaults to 0)

#### Issue 3: Campaign parameters not sent
**Symptoms**: My Campaigns calculations don't reflect custom values
**Possible Causes**:
- Campaign not recognized as "My Campaign"
- Zero values in amount fields
**Solution**:
- Ensure campaign is in MyCampaigns collection
- Verify amount fields have positive values

#### Issue 4: Parameter set load fails
**Symptoms**: "Parameter set load failed" message
**Impact**: Non-blocking - calculations proceed without parameter set
**Solution**:
- Verify `/api/v1/parameters/current` endpoint availability
- Check backend logs for parameter service issues

### Debug Logging Points

To troubleshoot issues, add logging at these key points:

1. **IDC Items Creation** ([`MainViewModel.cs:298-327`](../FinancialCalculator.WinUI3/ViewModels/MainViewModel.cs))
   - Log created IDC items before adding to request

2. **Campaign Parameters Building** ([`MainViewModel.cs:431-471`](../FinancialCalculator.WinUI3/ViewModels/MainViewModel.cs))
   - Log campaign type and parameters

3. **API Response Parsing** ([`ApiClient.cs:82-87`](../FinancialCalculator.WinUI3/Services/ApiClient.cs))
   - Log presence/absence of separated fields

4. **Profitability Details Update** ([`MainViewModel.cs:573-576`](../FinancialCalculator.WinUI3/ViewModels/MainViewModel.cs))
   - Log received values before display

## TODO Items Requiring Backend Team Coordination

### Critical Items
1. **Confirm IDC Category Names**
   - Current implementation uses:
     - `"broker_commission"` for dealer commission
     - `"internal_processing"` for IDC Other
   - Location: [`MainViewModel.cs:305, 320`](../FinancialCalculator.WinUI3/ViewModels/MainViewModel.cs)

2. **Verify Campaign Parameter Keys**
   - SubDown: `"subsidy_amount"`
   - Free Insurance: `"insurance_cost"`
   - Free MBSP: `"mbsp_cost"`
   - Cash Discount: `"discount_amount"`
   - Location: [`MainViewModel.cs:442, 449, 458, 465`](../FinancialCalculator.WinUI3/ViewModels/MainViewModel.cs)

3. **Validate IDC Item Properties**
   - Current assumptions:
     - All IDC items: `financed=true`
     - All IDC items: `timing="upfront"`
     - All IDC items: `is_cost=true`, `is_revenue=false`
   - Confirm if other configurations needed

### Nice-to-Have Items
1. Support for periodic IDC items (currently all upfront)
2. Revenue-type IDC items support
3. Dynamic IDC category list from backend
4. Validation rules for IDC amounts

## Migration Notes

### For Existing Installations
1. No database migrations required
2. UI will gracefully handle old backend responses
3. Separated fields default to 0 if not present

### For Development Teams
1. Update API documentation with new fields
2. Add integration tests for IDC items flow
3. Update backend logging for IDC processing
4. Consider feature flag for gradual rollout

## References

- [Testing Checklist](./IDC_SUBSIDY_SEPARATION_TESTING.md)
- [API Contract V1](./API_CONTRACT_V1.md)
- [Financial Calculator UI Redesign](./financial-calculator-ui-redesign.md)
- [Backend Implementation](../../engines/types/types.go)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-10-06 | Initial implementation of IDC/Subsidy separation |

## Contact

For questions or clarifications:
- Frontend: [UI Team]
- Backend: [API Team]
- Product: [Product Owner]