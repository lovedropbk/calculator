package profitability

import (
	"errors"
	"fmt"

	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
)

// Engine handles profitability calculations and waterfall generation
type Engine struct {
	parameterSet types.ParameterSet
}

// NewEngine creates a new profitability engine
func NewEngine(params types.ParameterSet) *Engine {
	return &Engine{
		parameterSet: params,
	}
}

// CalculateWaterfall generates the complete profitability waterfall to RoRAC
func (e *Engine) CalculateWaterfall(
	deal types.Deal,
	dealIRR decimal.Decimal,
	idcUpfrontNet decimal.Decimal,
	idcPeriodicNet decimal.Decimal,
) (*types.ProfitabilityWaterfall, error) {

	waterfall := &types.ProfitabilityWaterfall{
		DealIRREffective: dealIRR,
		DealIRRNominal:   e.effectiveToNominal(dealIRR, 12),
		Details:          make(map[string]decimal.Decimal),
	}

	// 1. Cost of Debt (matched funded)
	costOfDebt, err := e.getCostOfDebt(deal.TermMonths)
	if err != nil {
		return nil, err
	}
	waterfall.CostOfDebtMatched = costOfDebt

	// 2. Matched Funded Spread
	waterfall.MatchedFundedSpread = e.parameterSet.MatchedFundedSpread

	// 3. Gross Interest Margin = Deal IRR - Cost of Debt - MF Spread
	waterfall.GrossInterestMargin = dealIRR.Sub(costOfDebt).Sub(waterfall.MatchedFundedSpread)

	// 4. Capital Advantage
	capitalAdvantage := e.calculateCapitalAdvantage(deal)
	waterfall.CapitalAdvantage = capitalAdvantage

	// 5. Net Interest Margin = Gross Interest Margin + Capital Advantage
	waterfall.NetInterestMargin = waterfall.GrossInterestMargin.Add(capitalAdvantage)

	// 6. Cost of Credit Risk (PD * LGD)
	creditRisk := e.calculateCreditRisk(deal)
	waterfall.CostOfCreditRisk = creditRisk

	// 7. OPEX
	opex := e.calculateOPEX(deal)
	waterfall.OPEX = opex

	// 8. IDC Subsidies and Fees
	waterfall.IDCSubsidiesFeesUpfront = idcUpfrontNet
	waterfall.IDCSubsidiesFeesPeriodic = idcPeriodicNet

	// 9. Net EBIT Margin
	waterfall.NetEBITMargin = waterfall.NetInterestMargin.
		Sub(creditRisk).
		Sub(opex).
		Add(idcUpfrontNet).
		Add(idcPeriodicNet).
		Sub(e.parameterSet.CentralHQAddOn)

	// 10. Economic Capital
	economicCapital := e.calculateEconomicCapital(deal)
	waterfall.EconomicCapital = economicCapital

	// 11. Acquisition RoRAC = Net EBIT Margin / Economic Capital
	if economicCapital.GreaterThan(decimal.Zero) {
		waterfall.AcquisitionRoRAC = waterfall.NetEBITMargin.Div(economicCapital)
	} else {
		waterfall.AcquisitionRoRAC = decimal.Zero
	}

	// Round all values to basis points
	waterfall = e.roundWaterfall(waterfall)

	return waterfall, nil
}

// getCostOfDebt looks up the cost of funds for the given term
func (e *Engine) getCostOfDebt(termMonths int) (decimal.Decimal, error) {
	if len(e.parameterSet.CostOfFundsCurve) == 0 {
		return decimal.Zero, errors.New("cost of funds curve not configured")
	}

	// Find the appropriate rate for the term
	var rate decimal.Decimal
	for _, point := range e.parameterSet.CostOfFundsCurve {
		if point.TermMonths == termMonths {
			rate = point.Rate
			break
		}
		// Use interpolation or nearest match
		if point.TermMonths > termMonths {
			if rate.IsZero() {
				rate = point.Rate // Use first available if term is shorter
			}
			break
		}
		rate = point.Rate // Keep updating to use the last lower term
	}

	if rate.IsZero() && len(e.parameterSet.CostOfFundsCurve) > 0 {
		// Use last point if term is longer than curve
		rate = e.parameterSet.CostOfFundsCurve[len(e.parameterSet.CostOfFundsCurve)-1].Rate
	}

	return rate, nil
}

// calculateCapitalAdvantage calculates the total capital advantage
func (e *Engine) calculateCapitalAdvantage(deal types.Deal) decimal.Decimal {
	params := e.parameterSet.EconomicCapitalParams

	totalAdvantage := params.CapitalAdvantage.
		Add(params.DTLAdvantage).
		Add(params.SecurityDepAdvantage).
		Add(params.OtherAdvantage)

	return totalAdvantage
}

// calculateCreditRisk calculates PD * LGD for the deal
func (e *Engine) calculateCreditRisk(deal types.Deal) decimal.Decimal {
	// Look up PD and LGD for the product
	key := fmt.Sprintf("%s_default", string(deal.Product))
	if pdlgd, exists := e.parameterSet.PDLGD[key]; exists {
		return pdlgd.PD.Mul(pdlgd.LGD)
	}

	// Default values if not found
	defaultPD := decimal.NewFromFloat(0.02)  // 2% PD
	defaultLGD := decimal.NewFromFloat(0.45) // 45% LGD

	return defaultPD.Mul(defaultLGD)
}

// calculateOPEX calculates operating expenses for the deal
func (e *Engine) calculateOPEX(deal types.Deal) decimal.Decimal {
	// Look up OPEX rate for the product
	key := fmt.Sprintf("%s_opex", string(deal.Product))
	if opexRate, exists := e.parameterSet.OPEXRates[key]; exists {
		return opexRate
	}

	// Default OPEX rate
	return decimal.NewFromFloat(0.0068) // 0.68% default
}

// calculateEconomicCapital calculates the economic capital requirement
func (e *Engine) calculateEconomicCapital(deal types.Deal) decimal.Decimal {
	baseCapitalRatio := e.parameterSet.EconomicCapitalParams.BaseCapitalRatio

	if baseCapitalRatio.IsZero() {
		// Default capital ratio
		baseCapitalRatio = decimal.NewFromFloat(0.12) // 12% default
	}

	// Economic capital as percentage of financed amount
	// In practice, this would be more sophisticated
	return baseCapitalRatio
}

// effectiveToNominal converts effective annual rate to nominal rate
func (e *Engine) effectiveToNominal(effectiveRate decimal.Decimal, compoundingPeriods int) decimal.Decimal {
	if compoundingPeriods <= 0 {
		return effectiveRate
	}

	// Nominal = n * ((1 + effective)^(1/n) - 1)
	onePlusEffective := decimal.NewFromFloat(1).Add(effectiveRate)
	periodicRate := decimal.NewFromFloat(1).Div(decimal.NewFromInt(int64(compoundingPeriods)))

	// Use approximation for small rates
	if effectiveRate.LessThan(decimal.NewFromFloat(0.5)) {
		// For small rates, use approximation to avoid precision issues
		nominal := effectiveRate.Mul(decimal.NewFromInt(int64(compoundingPeriods))).
			Div(decimal.NewFromInt(int64(compoundingPeriods)).Add(
				effectiveRate.Mul(decimal.NewFromInt(int64(compoundingPeriods - 1))).Div(decimal.NewFromInt(2)),
			))
		return nominal
	}

	// For larger rates, use exact formula
	periodicFactor := onePlusEffective.Pow(periodicRate).Sub(decimal.NewFromFloat(1))
	nominal := periodicFactor.Mul(decimal.NewFromInt(int64(compoundingPeriods)))

	return nominal
}

// roundWaterfall rounds all waterfall values to basis points
func (e *Engine) roundWaterfall(waterfall *types.ProfitabilityWaterfall) *types.ProfitabilityWaterfall {
	waterfall.DealIRREffective = types.RoundBasisPoints(waterfall.DealIRREffective)
	waterfall.DealIRRNominal = types.RoundBasisPoints(waterfall.DealIRRNominal)
	waterfall.CostOfDebtMatched = types.RoundBasisPoints(waterfall.CostOfDebtMatched)
	waterfall.MatchedFundedSpread = types.RoundBasisPoints(waterfall.MatchedFundedSpread)
	waterfall.GrossInterestMargin = types.RoundBasisPoints(waterfall.GrossInterestMargin)
	waterfall.CapitalAdvantage = types.RoundBasisPoints(waterfall.CapitalAdvantage)
	waterfall.NetInterestMargin = types.RoundBasisPoints(waterfall.NetInterestMargin)
	waterfall.CostOfCreditRisk = types.RoundBasisPoints(waterfall.CostOfCreditRisk)
	waterfall.OPEX = types.RoundBasisPoints(waterfall.OPEX)
	waterfall.IDCSubsidiesFeesUpfront = types.RoundBasisPoints(waterfall.IDCSubsidiesFeesUpfront)
	waterfall.IDCSubsidiesFeesPeriodic = types.RoundBasisPoints(waterfall.IDCSubsidiesFeesPeriodic)
	waterfall.NetEBITMargin = types.RoundBasisPoints(waterfall.NetEBITMargin)
	waterfall.EconomicCapital = types.RoundBasisPoints(waterfall.EconomicCapital)
	waterfall.AcquisitionRoRAC = types.RoundBasisPoints(waterfall.AcquisitionRoRAC)

	return waterfall
}

// CalculateIDCImpact calculates the net impact of IDC items
// NOTE: HQ-aligned semantics for MVP:
//   - Do not convert absolute THB to a rate at this stage (no financed amount context here).
//   - Treat IDC rate adjustments as zero to avoid distorting margins.
//     Profitability rate impacts from IDC can be modeled explicitly later with proper base.
func (e *Engine) CalculateIDCImpact(idcItems []types.IDCItem) (upfrontNet, periodicNet decimal.Decimal) {
	return decimal.Zero, decimal.Zero
}

// GenerateWaterfallSummary creates a summary of the waterfall for display
func (e *Engine) GenerateWaterfallSummary(waterfall *types.ProfitabilityWaterfall) map[string]string {
	summary := make(map[string]string)

	summary["Deal IRR Effective"] = fmt.Sprintf("%.2f%%", waterfall.DealIRREffective.Mul(decimal.NewFromInt(100)).InexactFloat64())
	summary["Cost of Debt Matched"] = fmt.Sprintf("%.2f%%", waterfall.CostOfDebtMatched.Mul(decimal.NewFromInt(100)).InexactFloat64())
	summary["Gross Interest Margin"] = fmt.Sprintf("%.2f%%", waterfall.GrossInterestMargin.Mul(decimal.NewFromInt(100)).InexactFloat64())
	summary["Capital Advantage"] = fmt.Sprintf("%.2f%%", waterfall.CapitalAdvantage.Mul(decimal.NewFromInt(100)).InexactFloat64())
	summary["Net Interest Margin"] = fmt.Sprintf("%.2f%%", waterfall.NetInterestMargin.Mul(decimal.NewFromInt(100)).InexactFloat64())
	summary["Cost of Credit Risk"] = fmt.Sprintf("%.2f%%", waterfall.CostOfCreditRisk.Mul(decimal.NewFromInt(100)).InexactFloat64())
	summary["OPEX"] = fmt.Sprintf("%.2f%%", waterfall.OPEX.Mul(decimal.NewFromInt(100)).InexactFloat64())
	summary["Net EBIT Margin"] = fmt.Sprintf("%.2f%%", waterfall.NetEBITMargin.Mul(decimal.NewFromInt(100)).InexactFloat64())
	summary["Acquisition RoRAC"] = fmt.Sprintf("%.2f%%", waterfall.AcquisitionRoRAC.Mul(decimal.NewFromInt(100)).InexactFloat64())

	return summary
}

// ValidateParameters checks if all required parameters are present
func (e *Engine) ValidateParameters() []error {
	var errors []error

	if len(e.parameterSet.CostOfFundsCurve) == 0 {
		errors = append(errors, fmt.Errorf("cost of funds curve is missing"))
	}

	if e.parameterSet.MatchedFundedSpread.IsZero() {
		errors = append(errors, fmt.Errorf("matched funded spread is not configured"))
	}

	if e.parameterSet.EconomicCapitalParams.BaseCapitalRatio.IsZero() {
		errors = append(errors, fmt.Errorf("base capital ratio is not configured"))
	}

	return errors
}
