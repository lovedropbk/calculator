package calculator

import (
	"crypto/sha256"
	"encoding/json"
	"errors"
	"fmt"
	"time"

	"github.com/financial-calculator/engines/campaigns"
	"github.com/financial-calculator/engines/cashflow"
	"github.com/financial-calculator/engines/pricing"
	"github.com/financial-calculator/engines/profitability"
	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
)

// Calculator is the main API entrypoint for all calculations
type Calculator struct {
	pricingEngine       *pricing.Engine
	campaignEngine      *campaigns.Engine
	cashflowEngine      *cashflow.Engine
	profitabilityEngine *profitability.Engine
}

// New creates a new calculator instance
func New(parameterSet types.ParameterSet) *Calculator {
	return &Calculator{
		pricingEngine:       pricing.NewEngine(parameterSet),
		campaignEngine:      campaigns.NewEngine(parameterSet),
		cashflowEngine:      cashflow.NewEngine(parameterSet),
		profitabilityEngine: profitability.NewEngine(parameterSet),
	}
}

// Calculate is the main API entrypoint that accepts a calculation request and returns the result
func (c *Calculator) Calculate(request types.CalculationRequest) (*types.CalculationResult, error) {
	startTime := time.Now()

	// Generate input hash for audit
	inputHash, err := c.generateHash(request)
	if err != nil {
		return nil, fmt.Errorf("failed to generate input hash: %w", err)
	}

	// Initialize result
	result := &types.CalculationResult{
		Success:   false,
		InputHash: inputHash,
		Metadata:  make(map[string]interface{}),
	}

	// Validate request
	if err := c.validateRequest(request); err != nil {
		result.Errors = append(result.Errors, err.Error())
		return result, err
	}

	// Process the deal through the calculation pipeline
	quote, err := c.processDeal(request)
	if err != nil {
		result.Errors = append(result.Errors, err.Error())
		return result, err
	}

	// Generate output hash
	outputHash, err := c.generateHash(quote)
	if err != nil {
		result.Warnings = append(result.Warnings, "Failed to generate output hash")
	} else {
		result.OutputHash = outputHash
	}

	// Set result
	result.Quote = *quote
	result.Success = true

	// Add metadata
	result.Metadata["calculation_time_ms"] = time.Since(startTime).Milliseconds()
	result.Metadata["parameter_set_version"] = request.ParameterSet.Version
	result.Metadata["engine_version"] = "1.0.0"

	return result, nil
}

// processDeal handles the complete calculation pipeline
func (c *Calculator) processDeal(request types.CalculationRequest) (*types.Quote, error) {
	deal := request.Deal

	// Step 1: Calculate financed amount if not set
	if deal.FinancedAmount.IsZero() {
		deal.FinancedAmount = c.calculateFinancedAmount(deal, request.IDCItems)
	}

	// Step 2: Apply campaigns in stacking order
	campaignResult, err := c.campaignEngine.ApplyCampaigns(deal, request.Campaigns)
	if err != nil {
		return nil, fmt.Errorf("campaign application failed: %w", err)
	}
	deal = campaignResult.TransformedDeal

	// Step 3: Process pricing (calculate installment or solve for rate)
	pricingResult, err := c.pricingEngine.ProcessDeal(deal)
	if err != nil {
		return nil, fmt.Errorf("pricing calculation failed: %w", err)
	}

	// Step 4: Build complete cashflows
	t0Flows := c.cashflowEngine.ConstructT0Flows(deal, campaignResult.T0Flows, request.IDCItems)
	periodicSchedule := c.cashflowEngine.BuildPeriodicSchedule(
		deal,
		pricingResult.MonthlyInstallment,
		pricingResult.CustomerRateNominal,
	)

	// Add balloon if applicable
	if deal.BalloonAmount.GreaterThan(decimal.Zero) {
		periodicSchedule = c.cashflowEngine.AddBalloonPayment(deal, periodicSchedule)
	}

	// Step 5: Calculate IRR
	allCashflows := c.cashflowEngine.MergeCashflows(t0Flows, periodicSchedule)
	dealIRR, err := c.cashflowEngine.CalculateDealIRR(t0Flows, periodicSchedule, []types.Cashflow{})
	if err != nil {
		dealIRR = decimal.Zero // Use zero if IRR calculation fails
	}

	// Step 6: Calculate profitability waterfall
	idcUpfrontNet, idcPeriodicNet := c.profitabilityEngine.CalculateIDCImpact(request.IDCItems)
	waterfall, err := c.profitabilityEngine.CalculateWaterfall(
		deal,
		dealIRR,
		idcUpfrontNet,
		idcPeriodicNet,
	)
	if err != nil {
		return nil, fmt.Errorf("profitability calculation failed: %w", err)
	}

	// Build quote result
	quote := &types.Quote{
		DealID:                fmt.Sprintf("DEAL-%d", time.Now().Unix()),
		ParameterSetID:        request.ParameterSet.ID,
		CalculatedAt:          time.Now(),
		MonthlyInstallment:    pricingResult.MonthlyInstallment,
		CustomerRateNominal:   pricingResult.CustomerRateNominal,
		CustomerRateEffective: pricingResult.CustomerRateEffective,
		Schedule:              periodicSchedule,
		Cashflows:             allCashflows,
		Profitability:         *waterfall,
		CampaignAudit:         campaignResult.AuditEntries,
	}

	return quote, nil
}

// calculateFinancedAmount calculates the financed amount including IDCs
func (c *Calculator) calculateFinancedAmount(deal types.Deal, idcItems []types.IDCItem) decimal.Decimal {
	financed := deal.PriceExTax.Sub(deal.DownPaymentAmount)

	// Add financed IDC items that are costs (not revenues)
	for _, idc := range idcItems {
		if idc.Financed && idc.Timing == types.IDCTimingUpfront {
			if idc.IsCost {
				financed = financed.Add(idc.Amount)
			}
		}
	}

	return types.RoundTHB(financed)
}

// validateRequest validates the calculation request
func (c *Calculator) validateRequest(request types.CalculationRequest) error {
	// Validate deal
	if request.Deal.PriceExTax.LessThanOrEqual(decimal.Zero) {
		return errors.New("price must be positive")
	}

	if request.Deal.TermMonths <= 0 {
		return errors.New("term must be positive")
	}

	// Validate down payment
	if request.Deal.DownPaymentLocked == "percent" {
		if request.Deal.DownPaymentPercent.LessThan(decimal.Zero) ||
			request.Deal.DownPaymentPercent.GreaterThan(decimal.NewFromFloat(0.8)) {
			return errors.New("down payment must be between 0% and 80%")
		}
		// Calculate amount from percent
		request.Deal.DownPaymentAmount = request.Deal.PriceExTax.Mul(request.Deal.DownPaymentPercent)
		request.Deal.DownPaymentAmount = types.RoundTHB(request.Deal.DownPaymentAmount)
	} else if request.Deal.DownPaymentLocked == "amount" {
		if request.Deal.DownPaymentAmount.LessThan(decimal.Zero) {
			return errors.New("down payment amount must be non-negative")
		}
		// Calculate percent from amount
		if request.Deal.PriceExTax.GreaterThan(decimal.Zero) {
			request.Deal.DownPaymentPercent = request.Deal.DownPaymentAmount.Div(request.Deal.PriceExTax)
		}
	}

	// Validate balloon
	if request.Deal.BalloonPercent.LessThan(decimal.Zero) ||
		request.Deal.BalloonPercent.GreaterThanOrEqual(decimal.NewFromFloat(1)) {
		return errors.New("balloon must be between 0% and 100%")
	}

	// Calculate balloon amount if percent is specified
	if request.Deal.BalloonPercent.GreaterThan(decimal.Zero) && request.Deal.BalloonAmount.IsZero() {
		request.Deal.BalloonAmount = request.Deal.PriceExTax.Mul(request.Deal.BalloonPercent)
		request.Deal.BalloonAmount = types.RoundTHB(request.Deal.BalloonAmount)
	}

	// Validate rate mode
	if request.Deal.RateMode != "fixed_rate" && request.Deal.RateMode != "target_installment" {
		return errors.New("invalid rate mode: must be 'fixed_rate' or 'target_installment'")
	}

	if request.Deal.RateMode == "fixed_rate" && request.Deal.CustomerNominalRate.LessThanOrEqual(decimal.Zero) {
		return errors.New("customer nominal rate must be positive for fixed rate mode")
	}

	if request.Deal.RateMode == "target_installment" && request.Deal.TargetInstallment.LessThanOrEqual(decimal.Zero) {
		return errors.New("target installment must be positive for target installment mode")
	}

	// Validate parameter set
	if request.ParameterSet.ID == "" {
		return errors.New("parameter set ID is required")
	}

	if request.ParameterSet.Version == "" {
		return errors.New("parameter set version is required")
	}

	return nil
}

// generateHash generates a deterministic hash for audit
func (c *Calculator) generateHash(data interface{}) (string, error) {
	jsonData, err := json.Marshal(data)
	if err != nil {
		return "", err
	}

	hash := sha256.Sum256(jsonData)
	return fmt.Sprintf("%x", hash), nil
}

// CalculateWithDefaults performs a calculation with default parameter set
func (c *Calculator) CalculateWithDefaults(deal types.Deal, campaigns []types.Campaign, idcItems []types.IDCItem) (*types.CalculationResult, error) {
	// Create default parameter set
	parameterSet := c.createDefaultParameterSet()

	request := types.CalculationRequest{
		Deal:         deal,
		Campaigns:    campaigns,
		IDCItems:     idcItems,
		ParameterSet: parameterSet,
	}

	return c.Calculate(request)
}

// createDefaultParameterSet creates a default parameter set for testing
func (c *Calculator) createDefaultParameterSet() types.ParameterSet {
	return types.ParameterSet{
		ID:                 "DEFAULT-001",
		Version:            "2025.08",
		EffectiveDate:      time.Now(),
		DayCountConvention: "ACT/365",

		// Cost of funds curve
		CostOfFundsCurve: []types.RateCurvePoint{
			{TermMonths: 6, Rate: decimal.NewFromFloat(0.0120)},
			{TermMonths: 12, Rate: decimal.NewFromFloat(0.0148)},
			{TermMonths: 24, Rate: decimal.NewFromFloat(0.0165)},
			{TermMonths: 36, Rate: decimal.NewFromFloat(0.0175)},
			{TermMonths: 48, Rate: decimal.NewFromFloat(0.0185)},
			{TermMonths: 60, Rate: decimal.NewFromFloat(0.0195)},
		},

		// Matched funded spread
		MatchedFundedSpread: decimal.NewFromFloat(0.0025), // 25 bps

		// PD/LGD by product
		PDLGD: map[string]types.PDLGDEntry{
			"HP_default": {
				Product: "HP",
				Segment: "default",
				PD:      decimal.NewFromFloat(0.02), // 2%
				LGD:     decimal.NewFromFloat(0.45), // 45%
			},
			"mySTAR_default": {
				Product: "mySTAR",
				Segment: "default",
				PD:      decimal.NewFromFloat(0.025), // 2.5%
				LGD:     decimal.NewFromFloat(0.40),  // 40%
			},
		},

		// OPEX rates by product
		OPEXRates: map[string]decimal.Decimal{
			"HP_opex":     decimal.NewFromFloat(0.0068), // 68 bps
			"mySTAR_opex": decimal.NewFromFloat(0.0072), // 72 bps
		},

		// Economic capital parameters
		EconomicCapitalParams: types.EconomicCapitalParams{
			BaseCapitalRatio:     decimal.NewFromFloat(0.12),   // 12%
			CapitalAdvantage:     decimal.NewFromFloat(0.0008), // 8 bps
			DTLAdvantage:         decimal.NewFromFloat(0.0003), // 3 bps
			SecurityDepAdvantage: decimal.NewFromFloat(0.0002), // 2 bps
			OtherAdvantage:       decimal.NewFromFloat(0.0001), // 1 bp
		},

		// Central HQ add-on
		CentralHQAddOn: decimal.NewFromFloat(0.0015), // 15 bps

		// Rounding rules
		RoundingRules: types.RoundingRules{
			Currency:    "THB",
			MinorUnits:  0,      // Round to whole THB
			Method:      "bank", // Banker's rounding
			DisplayRate: 4,      // Display to basis points
		},
	}
}

// GetPerformanceMetrics returns performance metrics for monitoring
func (c *Calculator) GetPerformanceMetrics(result *types.CalculationResult) map[string]interface{} {
	metrics := make(map[string]interface{})

	if result != nil && result.Metadata != nil {
		if calcTime, ok := result.Metadata["calculation_time_ms"].(int64); ok {
			metrics["calculation_time_ms"] = calcTime
			metrics["meets_sla"] = calcTime < 300 // p95 under 300ms target
		}
	}

	metrics["engine_version"] = "1.0.0"
	metrics["timestamp"] = time.Now().Unix()

	return metrics
}
