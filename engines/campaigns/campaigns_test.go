package campaigns

import (
	"testing"

	"github.com/financial-calculator/engines/types"
)

type stubLookup map[string]float64

func (s stubLookup) CommissionPercentByProduct(p string) float64 {
	if v, ok := s[p]; ok {
		return v
	}
	return 0
}

func TestGenerateCampaignSummaries(t *testing.T) {
	ps := types.ParameterSet{Version: "test"}
	eng := NewEngine(ps).SetCommissionLookup(stubLookup{"HP": 0.015})

	deal := types.Deal{
		Product:           types.ProductHirePurchase,
		PriceExTax:        types.NewDecimal(1_000_000),
		DownPaymentAmount: types.NewDecimal(200_000),
		// Simulate financed IDCs present by setting a larger FinancedAmount; engine must ignore this
		// for commission base and use PriceExTax - DownPaymentAmount instead.
		FinancedAmount: types.NewDecimal(810_000),
		TermMonths:     36,
		Timing:         types.TimingArrears,
	}
	state := types.DealState{
		DealerCommission: types.DealerCommission{
			Mode: types.DealerCommissionModeAuto,
		},
	}
	campaigns := []types.Campaign{
		{ID: "A", Type: types.CampaignSubdown},
		{ID: "B", Type: types.CampaignSubinterest},
		{ID: "C", Type: types.CampaignFreeInsurance},
		{ID: "D", Type: types.CampaignFreeMBSP},
		{ID: "E", Type: types.CampaignCashDiscount},
	}

	summaries := eng.GenerateCampaignSummaries(deal, state, campaigns)
	if len(summaries) < 5 {
		t.Fatalf("expected at least 5 summaries, got %d", len(summaries))
	}

	for _, row := range summaries {
		if row.CampaignType == types.CampaignCashDiscount {
			if row.DealerCommissionPct != 0 || row.DealerCommissionAmt != 0 {
				t.Errorf("cash discount commission expected 0 pct / 0 amt, got %v / %v",
					row.DealerCommissionPct, row.DealerCommissionAmt)
			}
			continue
		}
		if row.DealerCommissionPct != 0.015 {
			t.Errorf("finance commission pct = %v, want 0.015", row.DealerCommissionPct)
		}
		if row.DealerCommissionAmt != 12000 {
			t.Errorf("finance commission amt = %v, want 12000", row.DealerCommissionAmt)
		}
	}

}

func TestCommissionBaseExcludesFinancedIDCs_InSummaries(t *testing.T) {
	ps := types.ParameterSet{Version: "test"}
	eng := NewEngine(ps).SetCommissionLookup(stubLookup{"HP": 0.03}) // 3%

	price := 1_000_000.0
	dp := 200_000.0
	// FinancedAmount includes a financed IDC of 10,000 (810,000) but base must be price - dp = 800,000
	deal := types.Deal{
		Product:           types.ProductHirePurchase,
		PriceExTax:        types.NewDecimal(price),
		DownPaymentAmount: types.NewDecimal(dp),
		FinancedAmount:    types.NewDecimal(price - dp + 10_000),
		TermMonths:        36,
		Timing:            types.TimingArrears,
	}
	state := types.DealState{DealerCommission: types.DealerCommission{Mode: types.DealerCommissionModeAuto}}

	summaries := eng.GenerateCampaignSummaries(deal, state, []types.Campaign{{ID: "A", Type: types.CampaignSubdown}})
	if len(summaries) == 0 {
		t.Fatalf("expected at least one summary")
	}
	gotAmt := summaries[0].DealerCommissionAmt
	// 3% of 800,000 = 24,000 expected
	if gotAmt != 24000 {
		t.Fatalf("commission amount = %v, want 24000 (3%% of 800,000 base)", gotAmt)
	}
}

func TestOverridePrecedence(t *testing.T) {
	ps := types.ParameterSet{Version: "test"}
	eng := NewEngine(ps).SetCommissionLookup(stubLookup{"HP": 0.015})

	deal := types.Deal{
		Product:           types.ProductHirePurchase,
		PriceExTax:        types.NewDecimal(1_000_000),
		DownPaymentAmount: types.NewDecimal(200_000),
		FinancedAmount:    types.NewDecimal(800_000),
		TermMonths:        36,
		Timing:            types.TimingArrears,
	}
	overrideAmt := 9000.0
	state := types.DealState{
		DealerCommission: types.DealerCommission{
			Mode: types.DealerCommissionModeOverride,
			Amt:  &overrideAmt,
		},
	}
	campaigns := []types.Campaign{
		{ID: "A", Type: types.CampaignSubdown},
		{ID: "B", Type: types.CampaignSubinterest},
		{ID: "C", Type: types.CampaignFreeInsurance},
		{ID: "D", Type: types.CampaignFreeMBSP},
		{ID: "E", Type: types.CampaignCashDiscount},
	}

	summaries := eng.GenerateCampaignSummaries(deal, state, campaigns)
	if len(summaries) < 5 {
		t.Fatalf("expected at least 5 summaries, got %d", len(summaries))
	}

	for _, row := range summaries {
		if row.CampaignType == types.CampaignCashDiscount {
			if row.DealerCommissionPct != 0 || row.DealerCommissionAmt != 0 {
				t.Errorf("cash discount commission expected 0 pct / 0 amt, got %v / %v",
					row.DealerCommissionPct, row.DealerCommissionAmt)
			}
			continue
		}
		if row.DealerCommissionPct != 0 {
			t.Errorf("override commission pct = %v, want 0", row.DealerCommissionPct)
		}
		if row.DealerCommissionAmt != 9000 {
			t.Errorf("override commission amt = %v, want 9000", row.DealerCommissionAmt)
		}
	}
}
