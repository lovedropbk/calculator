package types

import (
	"time"

	"github.com/shopspring/decimal"
)

// Product represents the financial product type
type Product string

const (
	ProductHirePurchase   Product = "HP"
	ProductMySTAR         Product = "mySTAR"
	ProductFinanceLease   Product = "F-Lease"
	ProductOperatingLease Product = "Op-Lease"
)

// PaymentTiming represents when payments are made
type PaymentTiming string

const (
	TimingArrears PaymentTiming = "arrears"
	TimingAdvance PaymentTiming = "advance"
)

// CampaignType represents different campaign types
type CampaignType string

const (
	CampaignSubdown       CampaignType = "subdown"
	CampaignSubinterest   CampaignType = "subinterest"
	CampaignFreeInsurance CampaignType = "free_insurance"
	CampaignFreeMBSP      CampaignType = "free_mbsp"
	CampaignCashDiscount  CampaignType = "cash_discount"
)

// IDCTiming represents when IDC items are applied
type IDCTiming string

const (
	IDCTimingUpfront  IDCTiming = "upfront"
	IDCTimingPeriodic IDCTiming = "periodic"
)

// IDCCategory represents different IDC categories
type IDCCategory string

const (
	IDCDocumentationFee   IDCCategory = "documentation_fee"
	IDCAcquisitionFee     IDCCategory = "acquisition_fee"
	IDCBrokerCommission   IDCCategory = "broker_commission"
	IDCStampDuty          IDCCategory = "stamp_duty"
	IDCInternalProcessing IDCCategory = "internal_processing"
	IDCAdminFee           IDCCategory = "admin_fee"
	IDCMaintenanceFee     IDCCategory = "maintenance_fee"
)

// CashflowType represents different types of cashflows
type CashflowType string

const (
	CashflowPrincipal    CashflowType = "principal"
	CashflowInterest     CashflowType = "interest"
	CashflowFee          CashflowType = "fee"
	CashflowSubsidy      CashflowType = "subsidy"
	CashflowDisbursement CashflowType = "disbursement"
	CashflowDownPayment  CashflowType = "down_payment"
	CashflowBalloon      CashflowType = "balloon"
	CashflowIDC          CashflowType = "idc"
)

// Deal represents a financial deal
type Deal struct {
	Market             string          `json:"market"`
	Currency           string          `json:"currency"`
	Product            Product         `json:"product"`
	PriceExTax         decimal.Decimal `json:"price_ex_tax"`
	DownPaymentAmount  decimal.Decimal `json:"down_payment_amount"`
	DownPaymentPercent decimal.Decimal `json:"down_payment_percent"`
	DownPaymentLocked  string          `json:"down_payment_locked"` // "amount" or "percent"
	FinancedAmount     decimal.Decimal `json:"financed_amount"`
	TermMonths         int             `json:"term_months"`
	BalloonPercent     decimal.Decimal `json:"balloon_percent"`
	BalloonAmount      decimal.Decimal `json:"balloon_amount"`
	Timing             PaymentTiming   `json:"timing"`
	PayoutDate         time.Time       `json:"payout_date"`
	FirstPaymentOffset int             `json:"first_payment_offset"`
	CampaignIDs        []string        `json:"campaign_ids"`

	// Rate mode
	RateMode            string          `json:"rate_mode"` // "fixed_rate" or "target_installment"
	CustomerNominalRate decimal.Decimal `json:"customer_nominal_rate,omitempty"`
	TargetInstallment   decimal.Decimal `json:"target_installment,omitempty"`
}

// Campaign represents a marketing campaign
type Campaign struct {
	ID          string                 `json:"id"`
	Type        CampaignType           `json:"type"`
	Parameters  map[string]interface{} `json:"parameters"`
	Eligibility map[string]interface{} `json:"eligibility"`
	Funder      string                 `json:"funder"`
	Stacking    int                    `json:"stacking"` // Order in the stack

	// Type-specific fields
	SubsidyAmount   decimal.Decimal `json:"subsidy_amount,omitempty"`
	SubsidyPercent  decimal.Decimal `json:"subsidy_percent,omitempty"`
	TargetRate      decimal.Decimal `json:"target_rate,omitempty"`
	DiscountAmount  decimal.Decimal `json:"discount_amount,omitempty"`
	DiscountPercent decimal.Decimal `json:"discount_percent,omitempty"`
	InsuranceCost   decimal.Decimal `json:"insurance_cost,omitempty"`
	MBSPCost        decimal.Decimal `json:"mbsp_cost,omitempty"`
}

// IDCItem represents an Initial Direct Cost item
type IDCItem struct {
	Category    IDCCategory     `json:"category"`
	Amount      decimal.Decimal `json:"amount"`
	Payer       string          `json:"payer"`
	Financed    bool            `json:"financed"`
	Withheld    bool            `json:"withheld"`
	Timing      IDCTiming       `json:"timing"`
	TaxFlags    []string        `json:"tax_flags"`
	IsRevenue   bool            `json:"is_revenue"`
	IsCost      bool            `json:"is_cost"`
	Description string          `json:"description"`
}

// Cashflow represents a single cashflow
type Cashflow struct {
	Date      time.Time       `json:"date"`
	Direction string          `json:"direction"` // "in" or "out"
	Type      CashflowType    `json:"type"`
	Amount    decimal.Decimal `json:"amount"`
	Memo      string          `json:"memo"`
	Principal decimal.Decimal `json:"principal,omitempty"`
	Interest  decimal.Decimal `json:"interest,omitempty"`
	Balance   decimal.Decimal `json:"balance,omitempty"`
}

// ParameterSet represents a versioned set of calculation parameters
type ParameterSet struct {
	ID            string                 `json:"id"`
	Version       string                 `json:"version"`
	EffectiveDate time.Time              `json:"effective_date"`
	Categories    map[string]interface{} `json:"categories"`

	// Specific parameter categories
	CostOfFundsCurve      []RateCurvePoint           `json:"cost_of_funds_curve"`
	MatchedFundedSpread   decimal.Decimal            `json:"matched_funded_spread"`
	PDLGD                 map[string]PDLGDEntry      `json:"pd_lgd"`
	OPEXRates             map[string]decimal.Decimal `json:"opex_rates"`
	EconomicCapitalParams EconomicCapitalParams      `json:"economic_capital_params"`
	CentralHQAddOn        decimal.Decimal            `json:"central_hq_addon"`
	RoundingRules         RoundingRules              `json:"rounding_rules"`
	DayCountConvention    string                     `json:"day_count_convention"` // "ACT/365"
}

// RateCurvePoint represents a point on the interest rate curve
type RateCurvePoint struct {
	TermMonths int             `json:"term_months"`
	Rate       decimal.Decimal `json:"rate"`
}

// PDLGDEntry represents Probability of Default and Loss Given Default
type PDLGDEntry struct {
	Product string          `json:"product"`
	Segment string          `json:"segment"`
	PD      decimal.Decimal `json:"pd"`  // Probability of Default
	LGD     decimal.Decimal `json:"lgd"` // Loss Given Default
}

// EconomicCapitalParams represents economic capital parameters
type EconomicCapitalParams struct {
	BaseCapitalRatio     decimal.Decimal `json:"base_capital_ratio"`
	CapitalAdvantage     decimal.Decimal `json:"capital_advantage"`
	DTLAdvantage         decimal.Decimal `json:"dtl_advantage"` // Deferred Tax Liabilities
	SecurityDepAdvantage decimal.Decimal `json:"security_dep_advantage"`
	OtherAdvantage       decimal.Decimal `json:"other_advantage"`
}

// RoundingRules defines rounding behavior
type RoundingRules struct {
	Currency    string `json:"currency"`
	MinorUnits  int    `json:"minor_units"`
	Method      string `json:"method"`       // "bank" for banker's rounding
	DisplayRate int    `json:"display_rate"` // basis points for rate display
}

// Quote represents the complete calculation result
type Quote struct {
	// Input references
	DealID         string    `json:"deal_id"`
	ParameterSetID string    `json:"parameter_set_id"`
	CalculatedAt   time.Time `json:"calculated_at"`

	// Core outputs
	MonthlyInstallment    decimal.Decimal `json:"monthly_installment"`
	CustomerRateNominal   decimal.Decimal `json:"customer_rate_nominal"`
	CustomerRateEffective decimal.Decimal `json:"customer_rate_effective"`

	// Cashflows and schedule
	Schedule  []Cashflow `json:"schedule"`
	Cashflows []Cashflow `json:"cashflows"`

	// Profitability waterfall
	Profitability ProfitabilityWaterfall `json:"profitability"`

	// Campaign impacts
	CampaignAudit []CampaignAuditEntry `json:"campaign_audit"`

	// Validation and errors
	Errors   []string `json:"errors,omitempty"`
	Warnings []string `json:"warnings,omitempty"`
}

// ProfitabilityWaterfall represents the profitability analysis
type ProfitabilityWaterfall struct {
	// Deal metrics
	DealIRREffective decimal.Decimal `json:"deal_irr_effective"`
	DealIRRNominal   decimal.Decimal `json:"deal_irr_nominal"`

	// Cost components
	CostOfDebtMatched   decimal.Decimal `json:"cost_of_debt_matched"`
	MatchedFundedSpread decimal.Decimal `json:"matched_funded_spread"`
	GrossInterestMargin decimal.Decimal `json:"gross_interest_margin"`

	// Capital adjustments
	CapitalAdvantage  decimal.Decimal `json:"capital_advantage"`
	NetInterestMargin decimal.Decimal `json:"net_interest_margin"`

	// Risk and costs
	CostOfCreditRisk         decimal.Decimal `json:"cost_of_credit_risk"`
	OPEX                     decimal.Decimal `json:"opex"`
	IDCSubsidiesFeesUpfront  decimal.Decimal `json:"idc_subsidies_fees_upfront"`
	IDCSubsidiesFeesPeriodic decimal.Decimal `json:"idc_subsidies_fees_periodic"`

	// Final metrics
	NetEBITMargin    decimal.Decimal `json:"net_ebit_margin"`
	EconomicCapital  decimal.Decimal `json:"economic_capital"`
	AcquisitionRoRAC decimal.Decimal `json:"acquisition_rorac"`

	// Detailed breakdown
	Details map[string]decimal.Decimal `json:"details,omitempty"`
}

// CampaignAuditEntry tracks campaign application
type CampaignAuditEntry struct {
	CampaignID            string                 `json:"campaign_id"`
	CampaignType          CampaignType           `json:"campaign_type"`
	Applied               bool                   `json:"applied"`
	Impact                decimal.Decimal        `json:"impact"`
	T0Flow                decimal.Decimal        `json:"t0_flow"`
	Description           string                 `json:"description"`
	TransformationDetails map[string]interface{} `json:"transformation_details,omitempty"`
}

// CalculationRequest represents the main API input
type CalculationRequest struct {
	Deal         Deal                   `json:"deal"`
	Campaigns    []Campaign             `json:"campaigns"`
	IDCItems     []IDCItem              `json:"idc_items"`
	ParameterSet ParameterSet           `json:"parameter_set"`
	Options      map[string]interface{} `json:"options,omitempty"`
}

// CalculationResult represents the main API output
type CalculationResult struct {
	Quote    Quote                  `json:"quote"`
	Success  bool                   `json:"success"`
	Errors   []string               `json:"errors,omitempty"`
	Warnings []string               `json:"warnings,omitempty"`
	Metadata map[string]interface{} `json:"metadata,omitempty"`

	// Deterministic hash for audit
	InputHash  string `json:"input_hash"`
	OutputHash string `json:"output_hash"`
}

// ValidationError represents a validation failure
type ValidationError struct {
	Field   string `json:"field"`
	Message string `json:"message"`
	Code    string `json:"code"`
}

// Helper functions

// NewDecimal creates a decimal from a float64
func NewDecimal(value float64) decimal.Decimal {
	return decimal.NewFromFloat(value)
}

// RoundTHB rounds to whole Thai Baht
func RoundTHB(amount decimal.Decimal) decimal.Decimal {
	return amount.Round(0)
}

// RoundBasisPoints rounds to basis points (0.01%)
func RoundBasisPoints(rate decimal.Decimal) decimal.Decimal {
	return rate.Round(4)
}

// DaysInYear returns days in year for Thai ACT/365 convention
func DaysInYear() int {
	return 365
}

// MonthsBetween calculates months between two dates
func MonthsBetween(start, end time.Time) int {
	years := end.Year() - start.Year()
	months := int(end.Month()) - int(start.Month())
	days := end.Day() - start.Day()

	totalMonths := years*12 + months
	if days < 0 {
		totalMonths--
	}

	return totalMonths
}

// AddMonths adds months to a date, preserving day where possible
func AddMonths(date time.Time, months int) time.Time {
	year := date.Year()
	month := date.Month()
	day := date.Day()

	// Add months
	totalMonths := int(month) + months
	yearDelta := (totalMonths - 1) / 12
	monthResult := ((totalMonths - 1) % 12) + 1

	year += yearDelta

	// Handle day overflow
	result := time.Date(year, time.Month(monthResult), 1,
		date.Hour(), date.Minute(), date.Second(),
		date.Nanosecond(), date.Location())

	lastDay := result.AddDate(0, 1, -1).Day()
	if day > lastDay {
		day = lastDay
	}

	return time.Date(year, time.Month(monthResult), day,
		date.Hour(), date.Minute(), date.Second(),
		date.Nanosecond(), date.Location())
}
