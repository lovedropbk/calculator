package profitability

import (
	"math"
	"testing"

	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
	"github.com/stretchr/testify/require"
)

func effToNominal(eff decimal.Decimal, n int) decimal.Decimal {
	if n <= 0 {
		return eff
	}
	onePlus := decimal.NewFromFloat(1).Add(eff)
	periodic := decimal.NewFromFloat(math.Pow(onePlus.InexactFloat64(), 1.0/float64(n)) - 1)
	return periodic.Mul(decimal.NewFromInt(int64(n)))
}

func lookupCostOfDebt(ps types.ParameterSet, termMonths int) decimal.Decimal {
	var rate decimal.Decimal
	for _, pt := range ps.CostOfFundsCurve {
		if pt.TermMonths == termMonths {
			return pt.Rate
		}
		if pt.TermMonths > termMonths {
			if rate.IsZero() {
				rate = pt.Rate
			}
			break
		}
		rate = pt.Rate
	}
	if rate.IsZero() && len(ps.CostOfFundsCurve) > 0 {
		rate = ps.CostOfFundsCurve[len(ps.CostOfFundsCurve)-1].Rate
	}
	return rate
}

func TestProfitability_FixedCoR_025Percent_NominalBasis(t *testing.T) {
	// Arrange: minimal parameter set on nominal basis
	ps := types.ParameterSet{
		ID:                 "TEST",
		Version:            "test",
		DayCountConvention: "ACT/365",
		CostOfFundsCurve: []types.RateCurvePoint{
			{TermMonths: 36, Rate: decimal.NewFromFloat(0.0175)}, // 1.75%
		},
		MatchedFundedSpread: decimal.NewFromFloat(0.0025), // 25 bps
		OPEXRates: map[string]decimal.Decimal{
			"HP_opex": decimal.NewFromFloat(0.0068),
		},
		EconomicCapitalParams: types.EconomicCapitalParams{
			BaseCapitalRatio: decimal.NewFromFloat(0.08),
			CapitalAdvantage: decimal.NewFromFloat(0.0008), // 8 bps
		},
	}

	deal := types.Deal{
		Market:             "TH",
		Currency:           "THB",
		Product:            types.ProductHirePurchase,
		PriceExTax:         decimal.NewFromInt(1_000_000),
		DownPaymentPercent: decimal.NewFromFloat(0.20),
		DownPaymentAmount:  decimal.NewFromInt(200_000),
		DownPaymentLocked:  "percent",
		FinancedAmount:     decimal.NewFromInt(800_000),
		TermMonths:         36,
		Timing:             types.TimingArrears,
	}

	eng := NewEngine(ps)

	// We pass an effective annual IRR and let the engine convert to nominal internally.
	dealIRREff := decimal.NewFromFloat(0.0663) // ~6.63% effective
	idcUpfront, idcPeriodic := decimal.Zero, decimal.Zero

	// Act
	wf, err := eng.CalculateWaterfall(deal, dealIRREff, idcUpfront, idcPeriodic)
	require.NoError(t, err)

	// Assert: CoR fixed at 0.25% nominal p.a. with strict relative tolerance 2%
	expectedCoR := decimal.NewFromFloat(0.0025)
	absDiffCoR := wf.CostOfCreditRisk.Sub(expectedCoR).Abs()
	relTol := decimal.NewFromFloat(0.02) // 2%
	allowedCoR := expectedCoR.Mul(relTol)
	require.Truef(t, absDiffCoR.LessThanOrEqual(allowedCoR),
		"CoR expected=%s actual=%s absΔ=%s allowed=%s",
		expectedCoR.StringFixed(4), wf.CostOfCreditRisk.StringFixed(4),
		absDiffCoR.StringFixed(6), allowedCoR.StringFixed(6),
	)

	// Assert: Nominal basis consistency for Net Interest Margin
	nominal := effToNominal(dealIRREff, 12)
	cod := lookupCostOfDebt(ps, deal.TermMonths)
	mfs := ps.MatchedFundedSpread
	cadv := ps.EconomicCapitalParams.CapitalAdvantage.
		Add(ps.EconomicCapitalParams.DTLAdvantage).
		Add(ps.EconomicCapitalParams.SecurityDepAdvantage).
		Add(ps.EconomicCapitalParams.OtherAdvantage)

	expectedNIM := nominal.Sub(cod).Sub(mfs).Add(cadv).Round(4)
	absDiffNIM := wf.NetInterestMargin.Sub(expectedNIM).Abs()
	// tight absolute tolerance at 1 bp (0.0001)
	require.Truef(t, absDiffNIM.LessThanOrEqual(decimal.NewFromFloat(0.0001)),
		"NIM mismatch expected=%s actual=%s absΔ=%s",
		expectedNIM.StringFixed(4), wf.NetInterestMargin.StringFixed(4),
		absDiffNIM.StringFixed(6),
	)
}
