package calculator

import (
	"testing"
	"time"

	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

// MARK: IRR parity without fees/IDCs
func TestHP36m_NoFees_RateParity(t *testing.T) {
	calc := New(createTestParameterSet())

	// Baseline deal: 1,000,000 THB price; 20% DP; 36m; arrears; no balloon; fixed nominal 3.99%
	price := decimal.NewFromFloat(1_000_000)
	dpPct := decimal.NewFromFloat(0.20)
	dpAmt := price.Mul(dpPct).Round(0)
	financed := price.Sub(dpAmt)

	deal := types.Deal{
		Market:              "TH",
		Currency:            "THB",
		Product:             types.ProductHirePurchase,
		PriceExTax:          price,
		DownPaymentAmount:   dpAmt,
		DownPaymentPercent:  dpPct,
		DownPaymentLocked:   "percent",
		FinancedAmount:      financed,
		TermMonths:          36,
		BalloonPercent:      decimal.Zero,
		BalloonAmount:       decimal.Zero,
		Timing:              types.TimingArrears,
		PayoutDate:          time.Date(2025, 8, 4, 0, 0, 0, 0, time.UTC),
		FirstPaymentOffset:  0,
		RateMode:            "fixed_rate",
		CustomerNominalRate: decimal.NewFromFloat(0.0399), // 3.99%
	}

	req := types.CalculationRequest{
		Deal:         deal,
		Campaigns:    []types.Campaign{},
		IDCItems:     []types.IDCItem{},
		ParameterSet: createTestParameterSet(),
	}

	res, err := calc.Calculate(req)
	require.NoError(t, err)
	require.True(t, res.Success)

	// Effective annual from pricing vs Deal IRR Effective should be within 5 bps with no upfront items
	rEff := res.Quote.CustomerRateEffective
	irr := res.Quote.Profitability.DealIRREffective
	diff := irr.Sub(rEff).Abs()
	assert.True(t, diff.LessThan(decimal.NewFromFloat(0.0005)),
		"Deal IRR Effective should be ~ Customer Effective (diff=%s bps)", diff.Mul(decimal.NewFromInt(10000)).String())
}

// MARK: IRR ordering with 3% upfront commission (non-financed)
func TestHP36m_CommissionReducesIRR(t *testing.T) {
	calc := New(createTestParameterSet())

	price := decimal.NewFromFloat(1_000_000)
	dpPct := decimal.NewFromFloat(0.20)
	dpAmt := price.Mul(dpPct).Round(0)
	financed := price.Sub(dpAmt)

	deal := types.Deal{
		Market:              "TH",
		Currency:            "THB",
		Product:             types.ProductHirePurchase,
		PriceExTax:          price,
		DownPaymentAmount:   dpAmt,
		DownPaymentPercent:  dpPct,
		DownPaymentLocked:   "percent",
		FinancedAmount:      financed,
		TermMonths:          36,
		BalloonPercent:      decimal.Zero,
		BalloonAmount:       decimal.Zero,
		Timing:              types.TimingArrears,
		PayoutDate:          time.Date(2025, 8, 4, 0, 0, 0, 0, time.UTC),
		FirstPaymentOffset:  0,
		RateMode:            "fixed_rate",
		CustomerNominalRate: decimal.NewFromFloat(0.0399), // 3.99%
	}

	// 3% upfront dealer commission at T0, NOT financed (reduces IRR)
	commission := financed.Mul(decimal.NewFromFloat(0.03)).Round(0)
	idc := types.IDCItem{
		Category:    types.IDCAcquisitionFee,
		Amount:      commission,
		Payer:       "Dealer",
		Financed:    false,
		Withheld:    false,
		Timing:      types.IDCTimingUpfront,
		TaxFlags:    []string{},
		IsRevenue:   false,
		IsCost:      true,
		Description: "Dealer commission 3%",
	}

	req := types.CalculationRequest{
		Deal:         deal,
		Campaigns:    []types.Campaign{},
		IDCItems:     []types.IDCItem{idc},
		ParameterSet: createTestParameterSet(),
	}

	res, err := calc.Calculate(req)
	require.NoError(t, err)
	require.True(t, res.Success)

	rEff := res.Quote.CustomerRateEffective
	irr := res.Quote.Profitability.DealIRREffective
	assert.True(t, irr.LessThan(rEff),
		"Deal IRR Effective (%s) should be less than Customer Effective (%s) when upfront cost exists",
		irr.Mul(decimal.NewFromInt(100)).StringFixed(4), rEff.Mul(decimal.NewFromInt(100)).StringFixed(4))
}

// MARK: Waterfall arithmetic and RoRAC normalization
func TestWaterfallArithmeticAndRoRAC(t *testing.T) {
	calc := New(createTestParameterSet())

	price := decimal.NewFromFloat(1_000_000)
	dpPct := decimal.NewFromFloat(0.20)
	dpAmt := price.Mul(dpPct).Round(0)
	financed := price.Sub(dpAmt)

	deal := types.Deal{
		Market:              "TH",
		Currency:            "THB",
		Product:             types.ProductHirePurchase,
		PriceExTax:          price,
		DownPaymentAmount:   dpAmt,
		DownPaymentPercent:  dpPct,
		DownPaymentLocked:   "percent",
		FinancedAmount:      financed,
		TermMonths:          36,
		BalloonPercent:      decimal.Zero,
		BalloonAmount:       decimal.Zero,
		Timing:              types.TimingArrears,
		PayoutDate:          time.Date(2025, 8, 4, 0, 0, 0, 0, time.UTC),
		FirstPaymentOffset:  0,
		RateMode:            "fixed_rate",
		CustomerNominalRate: decimal.NewFromFloat(0.0399), // 3.99%
	}

	req := types.CalculationRequest{
		Deal:         deal,
		Campaigns:    []types.Campaign{},
		IDCItems:     []types.IDCItem{},
		ParameterSet: createTestParameterSet(),
	}

	res, err := calc.Calculate(req)
	require.NoError(t, err)
	require.True(t, res.Success)

	wf := res.Quote.Profitability

	// Gross Interest Margin = Deal IRR Nominal − Cost of Debt − Matched Funded Spread
	expectedGIM := wf.DealIRRNominal.Sub(wf.CostOfDebtMatched).Sub(wf.MatchedFundedSpread)
	diffGIM := wf.GrossInterestMargin.Sub(expectedGIM).Abs()
	assert.True(t, diffGIM.LessThan(decimal.NewFromFloat(0.001)),
		"Gross Interest Margin mismatch: got %s, want %s (diff=%s bps)",
		wf.GrossInterestMargin.Mul(decimal.NewFromInt(100)).StringFixed(4),
		expectedGIM.Mul(decimal.NewFromInt(100)).StringFixed(4),
		diffGIM.Mul(decimal.NewFromInt(10000)).StringFixed(2),
	)

	// RoRAC normalization: AcquisitionRoRAC = NetEBITMargin / EconomicCapital (both decimals)
	if wf.EconomicCapital.GreaterThan(decimal.Zero) {
		expectedRoRAC := wf.NetEBITMargin.Div(wf.EconomicCapital)
		diffRo := wf.AcquisitionRoRAC.Sub(expectedRoRAC).Abs()
		assert.True(t, diffRo.LessThan(decimal.NewFromFloat(0.0005)),
			"RoRAC mismatch: got %s, want %s (diff=%s bps)",
			wf.AcquisitionRoRAC.Mul(decimal.NewFromInt(100)).StringFixed(4),
			expectedRoRAC.Mul(decimal.NewFromInt(100)).StringFixed(4),
			diffRo.Mul(decimal.NewFromInt(10000)).StringFixed(2),
		)
	} else {
		assert.True(t, wf.AcquisitionRoRAC.IsZero(), "RoRAC should be zero when EconomicCapital is zero")
	}
}
