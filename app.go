package main

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	"financial-calculator/parameters"

	"github.com/financial-calculator/engines/campaigns"
	"github.com/financial-calculator/engines/cashflow"
	"github.com/financial-calculator/engines/pricing"
	"github.com/financial-calculator/engines/profitability"
	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
)

// App struct
type App struct {
	ctx          context.Context
	parameterSet types.ParameterSet
	paramService *parameters.Service
}

// NewApp creates a new App application struct
func NewApp() *App {
	// Initialize parameter service
	paramService, err := parameters.NewService()
	if err != nil {
		// Fall back to default parameters if service fails
		fmt.Printf("Warning: Failed to initialize parameter service: %v\n", err)
		paramService = nil
	}

	// Load parameters from service or use defaults
	var parameterSet types.ParameterSet
	if paramService != nil {
		// Load latest parameters from service
		params, err := paramService.LoadLatest()
		if err != nil {
			fmt.Printf("Warning: Failed to load parameters: %v\n", err)
			parameterSet = createDefaultParameterSet()
		} else {
			// Convert from parameters.ParameterSet to types.ParameterSet
			parameterSet = convertParametersToEngineTypes(params)
		}
	} else {
		// Use default parameter set
		parameterSet = createDefaultParameterSet()
	}

	return &App{
		parameterSet: parameterSet,
		paramService: paramService,
	}
}

// startup is called when the app starts. The context is saved
// so we can call the runtime methods
func (a *App) startup(ctx context.Context) {
	a.ctx = ctx
}

// convertParametersToEngineTypes converts parameters.ParameterSet to types.ParameterSet
func convertParametersToEngineTypes(params *parameters.ParameterSet) types.ParameterSet {
	if params == nil {
		return createDefaultParameterSet()
	}

	// Build rate curve from map
	var cofCurve []types.RateCurvePoint
	for term, rate := range params.CostOfFunds {
		cofCurve = append(cofCurve, types.RateCurvePoint{
			TermMonths: term,
			Rate:       decimal.NewFromFloat(rate),
		})
	}

	// Build PD/LGD map
	pdlgd := make(map[string]types.PDLGDEntry)
	for key, p := range params.PDLGDTables {
		pdlgd[key] = types.PDLGDEntry{
			Product: p.Product,
			Segment: p.Segment,
			PD:      decimal.NewFromFloat(p.PD),
			LGD:     decimal.NewFromFloat(p.LGD),
		}
	}

	// Build OPEX rates map - need to add "_opex" suffix for engine compatibility
	opexRates := make(map[string]decimal.Decimal)
	for product, rate := range params.OPEXRates {
		key := product + "_opex"
		opexRates[key] = decimal.NewFromFloat(rate)
	}

	return types.ParameterSet{
		ID:                 params.ID,
		Version:            params.ID,
		EffectiveDate:      params.EffectiveDate,
		DayCountConvention: params.DayCountConvention,

		CostOfFundsCurve:    cofCurve,
		MatchedFundedSpread: decimal.NewFromFloat(params.MatchedSpread),
		PDLGD:               pdlgd,
		OPEXRates:           opexRates,

		EconomicCapitalParams: types.EconomicCapitalParams{
			BaseCapitalRatio:     decimal.NewFromFloat(params.EconomicCapital.BaseCapitalRatio),
			CapitalAdvantage:     decimal.NewFromFloat(params.EconomicCapital.CapitalAdvantage),
			DTLAdvantage:         decimal.NewFromFloat(params.EconomicCapital.DTLAdvantage),
			SecurityDepAdvantage: decimal.NewFromFloat(params.EconomicCapital.SecurityDepAdvantage),
			OtherAdvantage:       decimal.NewFromFloat(params.EconomicCapital.OtherAdvantage),
		},

		CentralHQAddOn: decimal.NewFromFloat(params.CentralHQAddOn),

		RoundingRules: types.RoundingRules{
			Currency:    params.RoundingRules.Currency,
			MinorUnits:  params.RoundingRules.MinorUnits,
			Method:      params.RoundingRules.Method,
			DisplayRate: params.RoundingRules.DisplayRate,
		},
	}
}

// GetCurrentParameterVersion returns the current parameter version
func (a *App) GetCurrentParameterVersion() string {
	if a.paramService != nil {
		return a.paramService.GetCurrentVersion()
	}
	return a.parameterSet.Version
}

// LoadParameterSet loads a specific parameter set version
func (a *App) LoadParameterSet(version string) error {
	if a.paramService == nil {
		return fmt.Errorf("parameter service not available")
	}

	params, err := a.paramService.LoadByVersion(version)
	if err != nil {
		return fmt.Errorf("failed to load parameter set %s: %w", version, err)
	}

	// Convert and update
	a.parameterSet = convertParametersToEngineTypes(params)
	return nil
}

// RefreshParameters refreshes parameters from storage
func (a *App) RefreshParameters() error {
	if a.paramService == nil {
		return fmt.Errorf("parameter service not available")
	}

	params, err := a.paramService.LoadLatest()
	if err != nil {
		return fmt.Errorf("failed to refresh parameters: %w", err)
	}

	// Convert and update
	a.parameterSet = convertParametersToEngineTypes(params)
	return nil
}

// GetParameterVersions returns available parameter versions
func (a *App) GetParameterVersions() ([]map[string]interface{}, error) {
	if a.paramService == nil {
		return nil, fmt.Errorf("parameter service not available")
	}

	versions, err := a.paramService.GetAvailableVersions()
	if err != nil {
		return nil, err
	}

	// Convert to simple map for frontend
	var result []map[string]interface{}
	for _, v := range versions {
		result = append(result, map[string]interface{}{
			"id":            v.ID,
			"effectiveDate": v.EffectiveDate,
			"createdAt":     v.CreatedAt,
			"description":   v.Description,
			"isDefault":     v.IsDefault,
		})
	}

	return result, nil
}

// ==============================================
// MAIN CALCULATION METHODS
// ==============================================

// CalculateQuote is the simplified main API for UI integration
func (a *App) CalculateQuote(
	product string,
	priceExTax float64,
	downPaymentAmount float64,
	downPaymentPercent float64,
	downPaymentLocked string,
	termMonths int,
	balloonPercent float64,
	timing string,
	customerNominalRate float64,
	targetInstallment float64,
	rateMode string,
	campaignTypes []string,
	idcItemsJSON string,
) (string, error) {

	// Build Deal object
	deal := types.Deal{
		Market:              "TH",
		Currency:            "THB",
		Product:             types.Product(product),
		PriceExTax:          decimal.NewFromFloat(priceExTax),
		DownPaymentAmount:   decimal.NewFromFloat(downPaymentAmount),
		DownPaymentPercent:  decimal.NewFromFloat(downPaymentPercent),
		DownPaymentLocked:   downPaymentLocked,
		TermMonths:          termMonths,
		BalloonPercent:      decimal.NewFromFloat(balloonPercent),
		BalloonAmount:       decimal.NewFromFloat(priceExTax * balloonPercent),
		Timing:              types.PaymentTiming(timing),
		PayoutDate:          time.Now(),
		FirstPaymentOffset:  0,
		RateMode:            rateMode,
		CustomerNominalRate: decimal.NewFromFloat(customerNominalRate),
		TargetInstallment:   decimal.NewFromFloat(targetInstallment),
	}

	// Calculate financed amount
	if downPaymentLocked == "percent" {
		deal.DownPaymentAmount = deal.PriceExTax.Mul(deal.DownPaymentPercent)
	} else {
		deal.DownPaymentPercent = deal.DownPaymentAmount.Div(deal.PriceExTax)
	}
	deal.FinancedAmount = deal.PriceExTax.Sub(deal.DownPaymentAmount)

	// Parse IDC items
	var idcItems []types.IDCItem
	if idcItemsJSON != "" {
		if err := json.Unmarshal([]byte(idcItemsJSON), &idcItems); err != nil {
			return "", fmt.Errorf("failed to parse IDC items: %w", err)
		}
	}

	// Build campaigns from types
	campaigns := a.buildCampaignsFromTypes(campaignTypes)

	// Process the calculation pipeline
	result, err := a.processCalculationPipeline(deal, campaigns, idcItems)
	if err != nil {
		return "", err
	}

	// Convert to JSON
	resultJSON, err := json.Marshal(result)
	if err != nil {
		return "", fmt.Errorf("failed to marshal result: %w", err)
	}

	return string(resultJSON), nil
}

// processCalculationPipeline executes the complete calculation flow
func (a *App) processCalculationPipeline(
	deal types.Deal,
	campaignList []types.Campaign,
	idcItems []types.IDCItem,
) (*QuoteResult, error) {

	// Step 1: Calculate financed amount including financed IDCs
	financedAmount := a.calculateFinancedAmount(deal, idcItems)
	deal.FinancedAmount = financedAmount

	// Step 2: Apply campaigns in stacking order
	campaignEngine := campaigns.NewEngine(a.parameterSet)
	campaignResult, err := campaignEngine.ApplyCampaigns(deal, campaignList)
	if err != nil {
		return nil, fmt.Errorf("campaign application failed: %w", err)
	}
	deal = campaignResult.TransformedDeal

	// Step 3: Process pricing
	pricingEngine := pricing.NewEngine(a.parameterSet)
	pricingResult, err := pricingEngine.ProcessDeal(deal)
	if err != nil {
		return nil, fmt.Errorf("pricing calculation failed: %w", err)
	}

	// Step 4: Build cashflows
	cashflowEngine := cashflow.NewEngine(a.parameterSet)
	t0Flows := cashflowEngine.ConstructT0Flows(deal, campaignResult.T0Flows, idcItems)
	periodicSchedule := cashflowEngine.BuildPeriodicSchedule(
		deal,
		pricingResult.MonthlyInstallment,
		pricingResult.CustomerRateNominal,
	)

	// Add balloon if applicable
	if deal.BalloonAmount.GreaterThan(decimal.Zero) {
		periodicSchedule = cashflowEngine.AddBalloonPayment(deal, periodicSchedule)
	}

	// Step 5: Calculate IRR
	dealIRR, err := cashflowEngine.CalculateDealIRR(t0Flows, periodicSchedule, []types.Cashflow{})
	if err != nil {
		dealIRR = decimal.Zero
	}

	// Step 6: Calculate profitability waterfall
	profitEngine := profitability.NewEngine(a.parameterSet)
	idcUpfrontNet, idcPeriodicNet := profitEngine.CalculateIDCImpact(idcItems)
	waterfall, err := profitEngine.CalculateWaterfall(
		deal,
		dealIRR,
		idcUpfrontNet,
		idcPeriodicNet,
	)
	if err != nil {
		return nil, fmt.Errorf("profitability calculation failed: %w", err)
	}

	// Build result matching TypeScript interface
	result := &QuoteResult{
		MonthlyInstallment:    pricingResult.MonthlyInstallment.InexactFloat64(),
		CustomerRateNominal:   pricingResult.CustomerRateNominal.Mul(decimal.NewFromInt(100)).InexactFloat64(),
		CustomerRateEffective: pricingResult.CustomerRateEffective.Mul(decimal.NewFromInt(100)).InexactFloat64(),
		AcquisitionRoRAC:      waterfall.AcquisitionRoRAC.Mul(decimal.NewFromInt(100)).InexactFloat64(),

		// Profitability waterfall
		DealRateIRREffective:        waterfall.DealIRREffective.Mul(decimal.NewFromInt(100)).InexactFloat64(),
		CostOfDebtMatchedFunded:     waterfall.CostOfDebtMatched.Mul(decimal.NewFromInt(100)).InexactFloat64(),
		GrossInterestMargin:         waterfall.GrossInterestMargin.Mul(decimal.NewFromInt(100)).InexactFloat64(),
		CapitalAdvantage:            waterfall.CapitalAdvantage.Mul(decimal.NewFromInt(100)).InexactFloat64(),
		NetInterestMargin:           waterfall.NetInterestMargin.Mul(decimal.NewFromInt(100)).InexactFloat64(),
		StandardCostOfCreditRisk:    waterfall.CostOfCreditRisk.Mul(decimal.NewFromInt(100)).InexactFloat64(),
		OPEX:                        waterfall.OPEX.Mul(decimal.NewFromInt(100)).InexactFloat64(),
		IDCSubsidiesAndFeesPeriodic: waterfall.IDCSubsidiesFeesPeriodic.Mul(decimal.NewFromInt(100)).InexactFloat64(),
		NetEBITMargin:               waterfall.NetEBITMargin.Mul(decimal.NewFromInt(100)).InexactFloat64(),

		// Schedule and cashflows
		Schedule:  convertCashflowsToSchedule(periodicSchedule),
		Cashflows: convertCashflowsToDisplay(cashflowEngine.MergeCashflows(t0Flows, periodicSchedule)),

		// Campaign audit
		CampaignAudit: convertCampaignAudit(campaignResult.AuditEntries),

		// Metadata
		ParameterSetVersion:  a.GetCurrentParameterVersion(),
		CalculationTimestamp: time.Now().Format(time.RFC3339),

		// Financed amount info
		FinancedAmount: deal.FinancedAmount.InexactFloat64(),
		TotalPayments:  pricingResult.TotalPayments.InexactFloat64(),
		TotalInterest:  pricingResult.TotalInterest.InexactFloat64(),
	}

	return result, nil
}

// calculateFinancedAmount calculates the financed amount including financed IDCs
func (a *App) calculateFinancedAmount(deal types.Deal, idcItems []types.IDCItem) decimal.Decimal {
	financed := deal.PriceExTax.Sub(deal.DownPaymentAmount)

	// Add financed IDC items
	for _, idc := range idcItems {
		if idc.Financed && idc.Timing == types.IDCTimingUpfront && idc.IsCost {
			financed = financed.Add(idc.Amount)
		}
	}

	return types.RoundTHB(financed)
}

// buildCampaignsFromTypes creates Campaign objects from campaign type strings
func (a *App) buildCampaignsFromTypes(campaignTypes []string) []types.Campaign {
	campaignList := []types.Campaign{}

	for i, ct := range campaignTypes {
		var campaign types.Campaign
		campaign.ID = fmt.Sprintf("CAMP-%d", i+1)

		switch ct {
		case "Subdown":
			campaign.Type = types.CampaignSubdown
			campaign.SubsidyPercent = decimal.NewFromFloat(0.05) // 5% subdown
			campaign.Funder = "Dealer"
			campaign.Stacking = 1
		case "Subinterest":
			campaign.Type = types.CampaignSubinterest
			campaign.TargetRate = decimal.NewFromFloat(0.0299) // 2.99% target rate
			campaign.Funder = "Manufacturer"
			campaign.Stacking = 2
		case "FreeInsurance":
			campaign.Type = types.CampaignFreeInsurance
			campaign.InsuranceCost = decimal.NewFromFloat(15000) // 15k THB insurance
			campaign.Funder = "Insurance Partner"
			campaign.Stacking = 3
		case "FreeMBSP":
			campaign.Type = types.CampaignFreeMBSP
			campaign.MBSPCost = decimal.NewFromFloat(5000) // 5k THB MBSP
			campaign.Funder = "Manufacturer"
			campaign.Stacking = 4
		case "CashDiscount":
			campaign.Type = types.CampaignCashDiscount
			campaign.DiscountPercent = decimal.NewFromFloat(0.02) // 2% cash discount
			campaign.Funder = "Dealer"
			campaign.Stacking = 5
		}

		campaignList = append(campaignList, campaign)
	}

	return campaignList
}

// ==============================================
// RESULT TYPES AND CONVERTERS
// ==============================================

// QuoteResult matches the TypeScript interface
type QuoteResult struct {
	// Key metrics
	MonthlyInstallment    float64 `json:"monthlyInstallment"`
	CustomerRateNominal   float64 `json:"customerRateNominal"`
	CustomerRateEffective float64 `json:"customerRateEffective"`
	AcquisitionRoRAC      float64 `json:"acquisitionRoRAC"`

	// Profitability waterfall
	DealRateIRREffective        float64 `json:"dealRateIRREffective"`
	CostOfDebtMatchedFunded     float64 `json:"costOfDebtMatchedFunded"`
	GrossInterestMargin         float64 `json:"grossInterestMargin"`
	CapitalAdvantage            float64 `json:"capitalAdvantage"`
	NetInterestMargin           float64 `json:"netInterestMargin"`
	StandardCostOfCreditRisk    float64 `json:"standardCostOfCreditRisk"`
	OPEX                        float64 `json:"opex"`
	IDCSubsidiesAndFeesPeriodic float64 `json:"idcSubsidiesAndFeesPeriodic"`
	NetEBITMargin               float64 `json:"netEBITMargin"`

	// Schedule and cashflows
	Schedule  []ScheduleEntry `json:"schedule"`
	Cashflows []CashflowEntry `json:"cashflows"`

	// Campaign audit
	CampaignAudit []CampaignAuditDisplay `json:"campaignAudit"`

	// Metadata
	ParameterSetVersion  string `json:"parameterSetVersion"`
	CalculationTimestamp string `json:"calculationTimestamp"`

	// Additional info
	FinancedAmount float64 `json:"financedAmount"`
	TotalPayments  float64 `json:"totalPayments"`
	TotalInterest  float64 `json:"totalInterest"`
}

// ScheduleEntry represents a payment in the schedule
type ScheduleEntry struct {
	Month     int     `json:"month"`
	Date      string  `json:"date"`
	Payment   float64 `json:"payment"`
	Principal float64 `json:"principal"`
	Interest  float64 `json:"interest"`
	Balance   float64 `json:"balance"`
}

// CashflowEntry represents a cashflow for display
type CashflowEntry struct {
	Date      string  `json:"date"`
	Type      string  `json:"type"`
	Direction string  `json:"direction"`
	Amount    float64 `json:"amount"`
	Memo      string  `json:"memo"`
}

// CampaignAuditDisplay represents campaign audit for display
type CampaignAuditDisplay struct {
	CampaignID   string  `json:"campaignId"`
	CampaignType string  `json:"campaignType"`
	Applied      bool    `json:"applied"`
	Impact       float64 `json:"impact"`
	Description  string  `json:"description"`
}

// Converter functions
func convertCashflowsToSchedule(cashflows []types.Cashflow) []ScheduleEntry {
	schedule := []ScheduleEntry{}
	for i, cf := range cashflows {
		if cf.Type == types.CashflowPrincipal || cf.Type == types.CashflowBalloon {
			entry := ScheduleEntry{
				Month:     i + 1,
				Date:      cf.Date.Format("2006-01-02"),
				Payment:   cf.Amount.InexactFloat64(),
				Principal: cf.Principal.InexactFloat64(),
				Interest:  cf.Interest.InexactFloat64(),
				Balance:   cf.Balance.InexactFloat64(),
			}
			schedule = append(schedule, entry)
		}
	}
	return schedule
}

func convertCashflowsToDisplay(cashflows []types.Cashflow) []CashflowEntry {
	display := []CashflowEntry{}
	for _, cf := range cashflows {
		entry := CashflowEntry{
			Date:      cf.Date.Format("2006-01-02"),
			Type:      string(cf.Type),
			Direction: cf.Direction,
			Amount:    cf.Amount.InexactFloat64(),
			Memo:      cf.Memo,
		}
		display = append(display, entry)
	}
	return display
}

func convertCampaignAudit(entries []types.CampaignAuditEntry) []CampaignAuditDisplay {
	display := []CampaignAuditDisplay{}
	for _, entry := range entries {
		d := CampaignAuditDisplay{
			CampaignID:   entry.CampaignID,
			CampaignType: string(entry.CampaignType),
			Applied:      entry.Applied,
			Impact:       entry.Impact.InexactFloat64(),
			Description:  entry.Description,
		}
		display = append(display, d)
	}
	return display
}

// ==============================================
// PARAMETER SET MANAGEMENT
// ==============================================

// GetParameterSet returns the current parameter set
func (a *App) GetParameterSet() (string, error) {
	result, err := json.Marshal(a.parameterSet)
	if err != nil {
		return "", fmt.Errorf("failed to marshal parameter set: %w", err)
	}
	return string(result), nil
}

// UpdateParameterSet updates the parameter set
func (a *App) UpdateParameterSet(paramSetJSON string) error {
	var paramSet types.ParameterSet
	if err := json.Unmarshal([]byte(paramSetJSON), &paramSet); err != nil {
		return fmt.Errorf("failed to parse parameter set: %w", err)
	}

	a.parameterSet = paramSet
	return nil
}

// createDefaultParameterSet creates a mock parameter set for testing
func createDefaultParameterSet() types.ParameterSet {
	return types.ParameterSet{
		ID:                 "TH-2025-08-DEFAULT",
		Version:            "2025.08",
		EffectiveDate:      time.Now(),
		DayCountConvention: "ACT/365",

		// Cost of funds curve (Thai market rates)
		CostOfFundsCurve: []types.RateCurvePoint{
			{TermMonths: 6, Rate: decimal.NewFromFloat(0.0120)},  // 1.20%
			{TermMonths: 12, Rate: decimal.NewFromFloat(0.0148)}, // 1.48%
			{TermMonths: 24, Rate: decimal.NewFromFloat(0.0165)}, // 1.65%
			{TermMonths: 36, Rate: decimal.NewFromFloat(0.0175)}, // 1.75%
			{TermMonths: 48, Rate: decimal.NewFromFloat(0.0185)}, // 1.85%
			{TermMonths: 60, Rate: decimal.NewFromFloat(0.0195)}, // 1.95%
			{TermMonths: 72, Rate: decimal.NewFromFloat(0.0205)}, // 2.05%
			{TermMonths: 84, Rate: decimal.NewFromFloat(0.0215)}, // 2.15%
		},

		// Matched funded spread
		MatchedFundedSpread: decimal.NewFromFloat(0.0025), // 25 bps

		// PD/LGD tables by product and segment
		PDLGD: map[string]types.PDLGDEntry{
			"HP_default": {
				Product: "HP",
				Segment: "default",
				PD:      decimal.NewFromFloat(0.0200), // 2.00%
				LGD:     decimal.NewFromFloat(0.4500), // 45.00%
			},
			"mySTAR_default": {
				Product: "mySTAR",
				Segment: "default",
				PD:      decimal.NewFromFloat(0.0250), // 2.50%
				LGD:     decimal.NewFromFloat(0.4000), // 40.00%
			},
			"F-Lease_default": {
				Product: "F-Lease",
				Segment: "default",
				PD:      decimal.NewFromFloat(0.0180), // 1.80%
				LGD:     decimal.NewFromFloat(0.3500), // 35.00%
			},
			"Op-Lease_default": {
				Product: "Op-Lease",
				Segment: "default",
				PD:      decimal.NewFromFloat(0.0150), // 1.50%
				LGD:     decimal.NewFromFloat(0.3000), // 30.00%
			},
		},

		// OPEX rates by product
		OPEXRates: map[string]decimal.Decimal{
			"HP_opex":       decimal.NewFromFloat(0.0068), // 68 bps
			"mySTAR_opex":   decimal.NewFromFloat(0.0072), // 72 bps
			"F-Lease_opex":  decimal.NewFromFloat(0.0065), // 65 bps
			"Op-Lease_opex": decimal.NewFromFloat(0.0070), // 70 bps
		},

		// Economic capital parameters
		EconomicCapitalParams: types.EconomicCapitalParams{
			BaseCapitalRatio:     decimal.NewFromFloat(0.1200), // 12.00%
			CapitalAdvantage:     decimal.NewFromFloat(0.0008), // 8 bps
			DTLAdvantage:         decimal.NewFromFloat(0.0003), // 3 bps (Deferred Tax Liabilities)
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
			DisplayRate: 4,      // Display rates to basis points (4 decimal places)
		},
	}
}
