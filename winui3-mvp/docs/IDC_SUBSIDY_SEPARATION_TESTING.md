# IDC vs Subsidy Separation Testing Checklist

## Overview
This document provides a comprehensive testing checklist for validating the IDC (Internal Deal Costs) vs Subsidy separation implementation in the Financial Calculator WinUI3 application.

## Testing Prerequisites

### Backend Requirements
- [ ] Ensure backend API is running on port 8123 (or configured port)
- [ ] Verify backend supports the new separated fields in profitability response
- [ ] Confirm backend accepts `idc_items` array in calculation requests
- [ ] Validate parameter set endpoint is accessible

### Test Data Preparation
- [ ] Create test campaigns with various subsidy types
- [ ] Prepare test cases with different IDC configurations
- [ ] Set up scenarios with both dealer commission and IDC Other values

## Functional Testing Checklist

### 1. IDC Items Creation and Submission

#### Dealer Commission IDC Item
- [ ] **Test Case 1.1**: Verify dealer commission creates broker_commission IDC item
  - Set dealer commission mode to "override"
  - Enter commission value (% or THB)
  - Trigger calculation
  - **Expected**: Request includes IDC item with category="broker_commission"

- [ ] **Test Case 1.2**: Auto dealer commission handling
  - Set dealer commission mode to "auto"
  - Verify auto commission fetches from policy
  - **Expected**: IDC item amount matches calculated commission

#### IDC Other Item
- [ ] **Test Case 1.3**: IDC Other creates internal_processing item
  - Enter IDC Other value > 0
  - Trigger calculation
  - **Expected**: Request includes IDC item with category="internal_processing"

- [ ] **Test Case 1.4**: Zero IDC values excluded
  - Set both dealer commission and IDC Other to 0
  - **Expected**: `idc_items` array is empty or contains no items

### 2. Campaign Parameters for My Campaigns

#### SubDown Campaign
- [ ] **Test Case 2.1**: SubDown parameter mapping
  - Create custom SubDown campaign in My Campaigns
  - Set FSSubDownAmount value
  - **Expected**: Parameters include `subsidy_amount` key

#### Free Insurance Campaign
- [ ] **Test Case 2.2**: Insurance parameter mapping
  - Create custom Free Insurance campaign
  - Set FSSubInterestAmount value
  - **Expected**: Parameters include `insurance_cost` key

#### Free MBSP Campaign
- [ ] **Test Case 2.3**: MBSP parameter mapping
  - Create custom Free MBSP campaign
  - Set FSFreeMBSPAmount or IDC_MBSP_CostAmount
  - **Expected**: Parameters include `mbsp_cost` key

#### Cash Discount Campaign
- [ ] **Test Case 2.4**: Cash discount parameter mapping
  - Create custom Cash Discount campaign
  - Set CashDiscountAmount value
  - **Expected**: Parameters include `discount_amount` key

### 3. Profitability Waterfall Display

#### Separated IDC/Subsidy Fields
- [ ] **Test Case 3.1**: New fields display correctly
  - Perform calculation with IDC and subsidies
  - Check Profitability Details panel
  - **Expected**: Shows separated values for:
    - IDC Upfront Cost %
    - IDC Periodic Cost %
    - Subsidy Upfront %
    - Subsidy Periodic %

- [ ] **Test Case 3.2**: Backward compatibility
  - Test with backend that doesn't return new fields
  - **Expected**: Fields default to 0.00% without errors

### 4. Export Functionality

- [ ] **Test Case 4.1**: Excel export includes separated values
  - Export calculation to Excel
  - **Expected**: CSV includes all four separated IDC/Subsidy fields

### 5. UI State Management

#### IDC Other Behavior
- [ ] **Test Case 5.1**: Initial value mapping
  - Start application
  - **Expected**: IDC Other initially equals Subsidy Budget

- [ ] **Test Case 5.2**: User editing tracking
  - Manually change IDC Other value
  - **Expected**: `userEdited` flag set to true in requests

#### Subsidy Budget Enable/Disable
- [ ] **Test Case 5.3**: Budget field enablement
  - Select My Campaign with subsidy > budget
  - **Expected**: Subsidy Budget field becomes enabled

### 6. Integration Testing

#### End-to-End Scenarios
- [ ] **Test Case 6.1**: Complete workflow test
  1. Load standard campaigns
  2. Copy to My Campaigns
  3. Edit campaign parameters
  4. Set IDC values
  5. Calculate
  6. View cashflows
  7. Export results
  - **Expected**: All steps complete without errors

- [ ] **Test Case 6.2**: Mixed IDC and subsidies
  - Configure both IDC items and campaign subsidies
  - **Expected**: Both appear correctly in profitability breakdown

## Validation Steps

### API Request Validation
1. Enable network logging or use debugging proxy
2. Capture `/api/v1/calculate` requests
3. Verify request structure:
```json
{
  "deal": { ... },
  "campaigns": [ ... ],
  "idc_items": [
    {
      "category": "broker_commission",
      "amount": 50000,
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
  ]
}
```

### API Response Validation
1. Capture `/api/v1/calculate` responses
2. Verify profitability object includes:
```json
{
  "profitability": {
    "idc_subsidies_fees_upfront": 0.05,
    "idc_subsidies_fees_periodic": 0.02,
    "idc_upfront_cost_pct": 0.03,
    "idc_periodic_cost_pct": 0.01,
    "subsidy_upfront_pct": 0.02,
    "subsidy_periodic_pct": 0.01
  }
}
```

### UI Display Validation
1. Navigate to Profitability Details panel
2. Verify all percentage values display with 2 decimal places
3. Check that IDC Total = Dealer Commission + IDC Other
4. Confirm subsidy calculations match displayed values

## Known Limitations and TODOs

### Pending Backend Verification
The following items require confirmation with the backend team:

1. **IDC Category Names**
   - Current: `"broker_commission"` for dealer commission
   - Current: `"internal_processing"` for IDC Other
   - Status: ⚠️ Needs backend team verification

2. **Campaign Parameter Keys**
   - SubDown: `"subsidy_amount"`
   - Free Insurance: `"insurance_cost"`
   - Free MBSP: `"mbsp_cost"`
   - Cash Discount: `"discount_amount"`
   - Status: ⚠️ Needs backend team verification

3. **IDC Item Properties**
   - Assumption: All IDC items are `financed=true`, `timing="upfront"`, `is_cost=true`
   - Status: ⚠️ Verify if other configurations are needed

### Current Assumptions
- IDC items are always treated as upfront costs
- Campaign parameters only apply to My Campaigns (user-edited)
- Standard campaigns rely on backend catalog definitions
- Separated IDC/Subsidy fields may be null in responses (handled with defaults)

## Test Execution Log

| Test Case | Date | Tester | Result | Notes |
|-----------|------|--------|--------|-------|
| 1.1 | | | | |
| 1.2 | | | | |
| 1.3 | | | | |
| 1.4 | | | | |
| 2.1 | | | | |
| 2.2 | | | | |
| 2.3 | | | | |
| 2.4 | | | | |
| 3.1 | | | | |
| 3.2 | | | | |
| 4.1 | | | | |
| 5.1 | | | | |
| 5.2 | | | | |
| 5.3 | | | | |
| 6.1 | | | | |
| 6.2 | | | | |

## Regression Testing

### Areas to Monitor
- [ ] Standard campaign loading and display
- [ ] Campaign summaries grid population
- [ ] Cashflow calculations accuracy
- [ ] Export functionality completeness
- [ ] Parameter set caching behavior
- [ ] Commission policy fetching

## Performance Testing

### Metrics to Track
- [ ] API response times with IDC items
- [ ] UI responsiveness with multiple campaigns
- [ ] Export generation time
- [ ] Memory usage with parameter set caching

## Sign-off Criteria

- [ ] All test cases pass
- [ ] Backend team confirms field names and categories
- [ ] No regression issues identified
- [ ] Performance metrics within acceptable ranges
- [ ] Documentation updated with final field mappings