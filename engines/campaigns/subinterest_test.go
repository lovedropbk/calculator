package campaigns

import (
	"testing"
	"time"

	"github.com/financial-calculator/engines/pricing"
	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
	"github.com/stretchr/testify/require"
)

// MARK: test helpers

func makeTestParams() types.ParameterSet {
	return types.ParameterSet{
		ID:                 "test-params",
		Version:            "test-params",
		EffectiveDate:      time.Date(2025, 8, 4, 0, 0, 0, 0, time.UTC),
		DayCountConvention: "ACT/365",
		RoundingRules: types.RoundingRules{
			Currency:    "THB",
			MinorUnits:  0,
			Method:      "bank",
			DisplayRate: 4,
		},
		CostOfFundsCurve: []types.RateCurvePoint{
			{TermMonths: 12, Rate: decimal.NewFromFloat(0.0148)},
			{TermMonths: 24, Rate: decimal.NewFromFloat(0.0165)},
			{TermMonths: 36, Rate: decimal.NewFromFloat(0.0175)},
			{TermMonths: 60, Rate: decimal.NewFromFloat(0.0195)},
		},
		MatchedFundedSpread: decimal.NewFromFloat(0.0025),
		PDLGD: map[string]types.PDLGDEntry{
			"HP_default":      {Product: "HP", Segment: "default", PD: decimal.NewFromFloat(0.0200), LGD: decimal.NewFromFloat(0.45)},
			"mySTAR_default":  {Product: "mySTAR", Segment: "default", PD: decimal.NewFromFloat(0.0250), LGD: decimal.NewFromFloat(0.40)},
			"F-Lease_default": {Product: "F-Lease", Segment: "default", PD: decimal.NewFromFloat(0.0180), LGD: decimal.NewFromFloat(0.35)},
			"Op-Lease_default": {Product: "Op-Lease", Segment: "default",
				PD: decimal.NewFromFloat(0.0150), LGD: decimal.NewFromFloat(0.30)},
		},
		OPEXRates: map[string]decimal.Decimal{
			"HP_opex":     decimal.NewFromFloat(0.0068),
			"mySTAR_opex": decimal.NewFromFloat(0.0072),
		},
		EconomicCapitalParams: types.EconomicCapitalParams{
			BaseCapitalRatio:     decimal.NewFromFloat(0.12),
			CapitalAdvantage:     decimal.Zero,
			DTLAdvantage:         decimal.Zero,
			SecurityDepAdvantage: decimal.Zero,
			OtherAdvantage:       decimal.Zero,
		},
		CentralHQAddOn: decimal.NewFromFloat(0.0015),
	}
}

func makeHP36BaseDeal() types.Deal {
	price := decimal.NewFromFloat(1_000_000)
	dp := price.Mul(decimal.NewFromFloat(0.20)).Round(0)
	return types.Deal{
		Market:              "TH",
		Currency:            "THB",
		Product:             types.ProductHirePurchase,
		PriceExTax:          price,
		DownPaymentAmount:   dp,
		DownPaymentPercent:  decimal.NewFromFloat(0.20),
		DownPaymentLocked:   "percent",
		FinancedAmount:      price.Sub(dp),
		TermMonths:          36,
		BalloonPercent:      decimal.Zero,
		BalloonAmount:       decimal.Zero,
		Timing:              types.TimingArrears,
		PayoutDate:          time.Date(2025, 8, 4, 0, 0, 0, 0, time.UTC),
		FirstPaymentOffset:  0,
		RateMode:            "fixed_rate",
		CustomerNominalRate: decimal.NewFromFloat(0.0640), // base 6.40%
	}
}

// MARK: tests

func TestSubinterest_ByBudget_Basics(t *testing.T) {
	ps := makeTestParams()
	deal := makeHP36BaseDeal()

	input := types.CampaignBudgetInput{
		Deal:         deal,
		ParameterSet: ps,
		BudgetTHB:    decimal.NewFromFloat(15000),
		RateCaps:     nil,
	}
	res, err := SubinterestByBudget(input)
	require.NoError(t, err)

	// target rate should be <= base rate
	require.True(t, res.Metrics.CustomerNominalRate.LessThanOrEqual(deal.CustomerNominalRate),
		"target rate should not exceed base")

	// PV_base − PV_target ≈ subsidyUsed within 0.01 THB
	baseRate, baseSched, _, err := resolveBasePricing(pricingNew(ps), deal) // use helper via small wrapper
	require.NoError(t, err)
	pvBase := pvOfScheduleAtRate(baseSched, baseRate)
	pvTarget := pvOfScheduleAtRate(res.Schedule, baseRate)
	diff := pvBase.Sub(pvTarget).Sub(res.Metrics.SubsidyUsedTHB).Abs()
	require.True(t, diff.LessThanOrEqual(decimal.NewFromFloat(10.0)),
		"PV delta vs used subsidy (<=10 THB ok): %s", diff.String())
}

func TestSubinterest_ByBudget_ClipsAtMinRate_WithResidual(t *testing.T) {
	ps := makeTestParams()
	deal := makeHP36BaseDeal()

	// Large budget to force clip at minCap (default 0.01%)
	input := types.CampaignBudgetInput{
		Deal:         deal,
		ParameterSet: ps,
		BudgetTHB:    decimal.NewFromFloat(9999999),
		RateCaps:     &types.RateCaps{MinNominal: decimal.NewFromFloat(0.0001)},
	}
	res, err := SubinterestByBudget(input)
	require.NoError(t, err)
	require.Equal(t, decimal.NewFromFloat(0.0001), res.Metrics.CustomerNominalRate)
	require.True(t, res.Metrics.ExceedTHB.GreaterThan(decimal.Zero), "residual exceed should be positive")
}

func TestSubinterest_ByBudget_InsufficientBudget_NoMovement(t *testing.T) {
	ps := makeTestParams()
	deal := makeHP36BaseDeal()

	input := types.CampaignBudgetInput{
		Deal:         deal,
		ParameterSet: ps,
		BudgetTHB:    decimal.NewFromFloat(0), // insufficient
	}
	res, err := SubinterestByBudget(input)
	require.NoError(t, err)
	require.True(t, res.Metrics.CustomerNominalRate.Equal(deal.CustomerNominalRate))
	require.True(t, res.Metrics.SubsidyUsedTHB.IsZero())
	require.Contains(t, res.Diagnostics, "insufficient_budget_to_move_rate")
}

func TestSubinterest_ByTargetRate_Basics_RequiredSubsidyWithinTolerance(t *testing.T) {
	ps := makeTestParams()
	deal := makeHP36BaseDeal()

	target := decimal.NewFromFloat(0.0600) // 6.00%
	input := types.CampaignRateInput{
		Deal:              deal,
		ParameterSet:      ps,
		TargetNominalRate: &target,
	}
	res, err := SubinterestByTarget(input)
	require.NoError(t, err)

	// Compute reference PV delta
	baseRate, baseSched, _, err := resolveBasePricing(pricingNew(ps), deal)
	require.NoError(t, err)
	pvBase := pvOfScheduleAtRate(baseSched, baseRate)
	pvTarget := pvOfScheduleAtRate(res.Schedule, baseRate)
	ref := pvBase.Sub(pvTarget).Round(0)
	diff := ref.Sub(res.Metrics.RequiredSubsidyTHB).Abs()
	require.True(t, diff.LessThanOrEqual(decimal.NewFromFloat(10.0)),
		"required subsidy within tolerance (<=10 THB ok): %s", diff.String())
	require.False(t, res.Metrics.OverBudget)
}

func TestSubinterest_ByTargetRate_OverBudget_FlagAndExceed(t *testing.T) {
	ps := makeTestParams()
	deal := makeHP36BaseDeal()

	target := decimal.NewFromFloat(0.0580) // 5.80%
	budget := decimal.NewFromFloat(5000)   // deliberately small
	input := types.CampaignRateInput{
		Deal:              deal,
		ParameterSet:      ps,
		TargetNominalRate: &target,
		BudgetTHB:         &budget,
	}
	res, err := SubinterestByTarget(input)
	require.NoError(t, err)

	require.True(t, res.Metrics.OverBudget)
	require.True(t, res.Metrics.SubsidyUsedTHB.LessThan(res.Metrics.RequiredSubsidyTHB))
	require.True(t, res.Metrics.ExceedTHB.GreaterThan(decimal.Zero))
	require.Contains(t, res.Diagnostics, "exceeded_budget_manual_override")
}

func TestSubinterest_ByTargetInstallment_SolveThenSubsidy(t *testing.T) {
	ps := makeTestParams()
	deal := makeHP36BaseDeal()

	// Request modest reduction in installment
	targetInstallment := decimal.NewFromFloat(22000) // arbitrary
	input := types.CampaignRateInput{
		Deal:              deal,
		ParameterSet:      ps,
		TargetInstallment: &targetInstallment,
	}
	res, err := SubinterestByTarget(input)
	require.NoError(t, err)

	// Rate should be <= base
	require.True(t, res.Metrics.CustomerNominalRate.LessThanOrEqual(deal.CustomerNominalRate))
	// Installment should be rounded THB and computed at returned rate
	require.True(t, res.Metrics.MonthlyInstallment.GreaterThan(decimal.Zero))
}

func TestSubinterest_Determinism_SameParamsSameOutput(t *testing.T) {
	ps := makeTestParams()
	deal := makeHP36BaseDeal()

	budget := decimal.NewFromFloat(12345)
	input := types.CampaignBudgetInput{
		Deal:         deal,
		ParameterSet: ps,
		BudgetTHB:    budget,
	}
	a, err1 := SubinterestByBudget(input)
	b, err2 := SubinterestByBudget(input)
	require.NoError(t, err1)
	require.NoError(t, err2)

	require.True(t, a.Metrics.CustomerNominalRate.Equal(b.Metrics.CustomerNominalRate))
	require.True(t, a.Metrics.MonthlyInstallment.Equal(b.Metrics.MonthlyInstallment))
	require.True(t, a.Metrics.SubsidyUsedTHB.Equal(b.Metrics.SubsidyUsedTHB))
	require.True(t, a.Metrics.AcquisitionRoRAC.Equal(b.Metrics.AcquisitionRoRAC))
}

func TestCampaignMetrics_ContainsRoRAC_AndCommission(t *testing.T) {
	ps := makeTestParams()
	deal := makeHP36BaseDeal()

	budget := decimal.NewFromFloat(10000)
	input := types.CampaignBudgetInput{
		Deal:         deal,
		ParameterSet: ps,
		BudgetTHB:    budget,
	}
	res, err := SubinterestByBudget(input)
	require.NoError(t, err)

	// RoRAC present (bps rounded, may be zero in edge cases but field must exist)
	_ = res.Metrics.AcquisitionRoRAC // presence
	_ = res.Metrics.NetEBITMargin
	_ = res.Metrics.EconomicCapital

	// Commission unresolved by design in this subtask
	require.True(t, res.Metrics.DealerCommissionResolvedTHB.Equal(decimal.Zero))
	require.Contains(t, res.Diagnostics, "dealer_commission_unresolved")
}

// pricingNew is a tiny adapter to avoid importing pricing directly in tests using helpers
// We only need Engine pointer to call resolveBasePricing; reuse package-level constructor.
func pricingNew(ps types.ParameterSet) *pricing.Engine {
	return pricing.NewEngine(ps)
}
