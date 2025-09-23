package parameters

import (
	"encoding/json"
	"fmt"
	"time"

	"github.com/shopspring/decimal"
)

// ParameterSet represents a versioned set of calculation parameters
type ParameterSet struct {
	// Versioning and metadata
	ID            string    `json:"id"`             // Version ID like "2025-08"
	EffectiveDate time.Time `json:"effective_date"` // When this version becomes effective
	CreatedAt     time.Time `json:"created_at"`     // When this version was created
	CreatedBy     string    `json:"created_by"`     // Who created this version
	Description   string    `json:"description"`    // Version description/notes
	IsDefault     bool      `json:"is_default"`     // Whether this is the default/fallback version

	// Core parameter categories
	CostOfFunds        map[int]float64        `json:"cost_of_funds"`        // Term (months) -> Rate curve
	MatchedSpread      float64                `json:"matched_spread"`       // Matched funded spread
	PDLGDTables        map[string]PDLGDParams `json:"pd_lgd_tables"`        // Product -> Risk parameters
	OPEXRates          map[string]float64     `json:"opex_rates"`           // Product -> OPEX rate
	EconomicCapital    EconomicCapitalParams  `json:"economic_capital"`     // Capital parameters
	CentralHQAddOn     float64                `json:"central_hq_addon"`     // HQ add-on rate
	RoundingRules      RoundingParams         `json:"rounding_rules"`       // Rounding configuration
	CampaignCatalog    []CampaignDefinition   `json:"campaign_catalog"`     // Available campaigns
	DayCountConvention string                 `json:"day_count_convention"` // e.g., "ACT/365"

	// Commission policy: product-level commission percentages
	CommissionPolicy    CommissionPolicy      `json:"commissionPolicy,omitempty"` // See docs/financial-calculator-architecture.md (policy section)
}

// PDLGDParams represents Probability of Default and Loss Given Default parameters
type PDLGDParams struct {
	Product     string  `json:"product"`     // Product code (HP, mySTAR, etc.)
	Segment     string  `json:"segment"`     // Customer segment
	PD          float64 `json:"pd"`          // Probability of Default
	LGD         float64 `json:"lgd"`         // Loss Given Default
	Description string  `json:"description"` // Optional description
}

// EconomicCapitalParams represents economic capital parameters
type EconomicCapitalParams struct {
	BaseCapitalRatio     float64 `json:"base_capital_ratio"`     // Base capital ratio
	CapitalAdvantage     float64 `json:"capital_advantage"`      // Capital advantage rate
	DTLAdvantage         float64 `json:"dtl_advantage"`          // Deferred Tax Liabilities advantage
	SecurityDepAdvantage float64 `json:"security_dep_advantage"` // Security deposit advantage
	OtherAdvantage       float64 `json:"other_advantage"`        // Other advantages
}

// RoundingParams defines rounding behavior
type RoundingParams struct {
	Currency      string `json:"currency"`       // Currency code (THB)
	MinorUnits    int    `json:"minor_units"`    // Decimal places for currency
	Method        string `json:"method"`         // Rounding method (bank, floor, ceil)
	DisplayRate   int    `json:"display_rate"`   // Decimal places for rate display
	InstallmentTo int    `json:"installment_to"` // Round installment to nearest N
}

// CommissionPolicy defines commission percentages by product.
// JSON shape aligns with docs/financial-calculator-architecture.md policy examples.
//
// Example JSON:
// {
//   "commissionPolicy": {
//     "version": "2025-09-draft",
//     "byProductPct": { "HP": 0.01, "mySTAR": 0.0125 },
//     "notes": "Illustrative only"
//   }
// }
type CommissionPolicy struct {
	ByProductPct map[string]float64 `json:"byProductPct"` // Product code -> commission percent (0..1). Missing means 0%.
	Version      string             `json:"version"`      // Semantic/date version tag for the policy set
	Notes        string             `json:"notes,omitempty"`
}

// CampaignDefinition defines a campaign in the catalog
type CampaignDefinition struct {
	ID            string                 `json:"id"`             // Campaign ID
	Name          string                 `json:"name"`           // Display name
	Type          string                 `json:"type"`           // Campaign type
	Description   string                 `json:"description"`    // Campaign description
	ValidFrom     time.Time              `json:"valid_from"`     // Campaign start date
	ValidUntil    time.Time              `json:"valid_until"`    // Campaign end date
	Parameters    map[string]interface{} `json:"parameters"`     // Campaign-specific parameters
	Eligibility   map[string]interface{} `json:"eligibility"`    // Eligibility criteria
	Funder        string                 `json:"funder"`         // Who funds the campaign
	StackingOrder int                    `json:"stacking_order"` // Order in campaign stack
	Active        bool                   `json:"active"`         // Whether campaign is active
}

// ValidationError represents a parameter validation error
type ValidationError struct {
	Field   string `json:"field"`
	Message string `json:"message"`
	Code    string `json:"code"`
}

// Validate performs validation on the ParameterSet
func (ps *ParameterSet) Validate() []ValidationError {
	var errors []ValidationError

	// Validate ID
	if ps.ID == "" {
		errors = append(errors, ValidationError{
			Field:   "id",
			Message: "Parameter set ID is required",
			Code:    "REQUIRED_FIELD",
		})
	}

	// Validate EffectiveDate
	if ps.EffectiveDate.IsZero() {
		errors = append(errors, ValidationError{
			Field:   "effective_date",
			Message: "Effective date is required",
			Code:    "REQUIRED_FIELD",
		})
	}

	// Validate CostOfFunds curve
	if len(ps.CostOfFunds) == 0 {
		errors = append(errors, ValidationError{
			Field:   "cost_of_funds",
			Message: "Cost of funds curve is required",
			Code:    "REQUIRED_FIELD",
		})
	} else {
		for term, rate := range ps.CostOfFunds {
			if term <= 0 {
				errors = append(errors, ValidationError{
					Field:   fmt.Sprintf("cost_of_funds[%d]", term),
					Message: "Term must be positive",
					Code:    "INVALID_VALUE",
				})
			}
			if rate < 0 || rate > 1 {
				errors = append(errors, ValidationError{
					Field:   fmt.Sprintf("cost_of_funds[%d]", term),
					Message: "Rate must be between 0 and 1",
					Code:    "OUT_OF_RANGE",
				})
			}
		}
	}

	// Validate MatchedSpread
	if ps.MatchedSpread < 0 || ps.MatchedSpread > 1 {
		errors = append(errors, ValidationError{
			Field:   "matched_spread",
			Message: "Matched spread must be between 0 and 1",
			Code:    "OUT_OF_RANGE",
		})
	}

	// Validate PDLGDTables
	if len(ps.PDLGDTables) == 0 {
		errors = append(errors, ValidationError{
			Field:   "pd_lgd_tables",
			Message: "PD/LGD tables are required",
			Code:    "REQUIRED_FIELD",
		})
	} else {
		for key, params := range ps.PDLGDTables {
			if params.PD < 0 || params.PD > 1 {
				errors = append(errors, ValidationError{
					Field:   fmt.Sprintf("pd_lgd_tables[%s].pd", key),
					Message: "PD must be between 0 and 1",
					Code:    "OUT_OF_RANGE",
				})
			}
			if params.LGD < 0 || params.LGD > 1 {
				errors = append(errors, ValidationError{
					Field:   fmt.Sprintf("pd_lgd_tables[%s].lgd", key),
					Message: "LGD must be between 0 and 1",
					Code:    "OUT_OF_RANGE",
				})
			}
		}
	}

	// Validate OPEXRates
	if len(ps.OPEXRates) == 0 {
		errors = append(errors, ValidationError{
			Field:   "opex_rates",
			Message: "OPEX rates are required",
			Code:    "REQUIRED_FIELD",
		})
	} else {
		for product, rate := range ps.OPEXRates {
			if rate < 0 || rate > 1 {
				errors = append(errors, ValidationError{
					Field:   fmt.Sprintf("opex_rates[%s]", product),
					Message: "OPEX rate must be between 0 and 1",
					Code:    "OUT_OF_RANGE",
				})
			}
		}
	}

	// Validate EconomicCapital
	if ps.EconomicCapital.BaseCapitalRatio < 0 || ps.EconomicCapital.BaseCapitalRatio > 1 {
		errors = append(errors, ValidationError{
			Field:   "economic_capital.base_capital_ratio",
			Message: "Base capital ratio must be between 0 and 1",
			Code:    "OUT_OF_RANGE",
		})
	}

	// Validate CentralHQAddOn
	if ps.CentralHQAddOn < 0 || ps.CentralHQAddOn > 1 {
		errors = append(errors, ValidationError{
			Field:   "central_hq_addon",
			Message: "Central HQ add-on must be between 0 and 1",
			Code:    "OUT_OF_RANGE",
		})
	}

	// Validate RoundingRules
	if ps.RoundingRules.Currency == "" {
		errors = append(errors, ValidationError{
			Field:   "rounding_rules.currency",
			Message: "Currency is required",
			Code:    "REQUIRED_FIELD",
		})
	}

	// Validate CampaignCatalog
	for i, campaign := range ps.CampaignCatalog {
		if campaign.ID == "" {
			errors = append(errors, ValidationError{
				Field:   fmt.Sprintf("campaign_catalog[%d].id", i),
				Message: "Campaign ID is required",
				Code:    "REQUIRED_FIELD",
			})
		}
		if campaign.Type == "" {
			errors = append(errors, ValidationError{
				Field:   fmt.Sprintf("campaign_catalog[%d].type", i),
				Message: "Campaign type is required",
				Code:    "REQUIRED_FIELD",
			})
		}
	}

	return errors
}

// Clone creates a deep copy of the ParameterSet
func (ps *ParameterSet) Clone() *ParameterSet {
	// Marshal to JSON and unmarshal to create a deep copy
	data, _ := json.Marshal(ps)
	var clone ParameterSet
	json.Unmarshal(data, &clone)
	return &clone
}

// ToEngineTypes converts to the engine's types.ParameterSet format
func (ps *ParameterSet) ToEngineTypes() interface{} {
	// This would convert to the engine's expected format
	// For now, returning a simplified structure
	engineParams := make(map[string]interface{})

	// Convert cost of funds curve
	cofCurve := make([]map[string]interface{}, 0)
	for term, rate := range ps.CostOfFunds {
		cofCurve = append(cofCurve, map[string]interface{}{
			"term_months": term,
			"rate":        decimal.NewFromFloat(rate),
		})
	}
	engineParams["cost_of_funds_curve"] = cofCurve

	// Convert other parameters
	engineParams["matched_funded_spread"] = decimal.NewFromFloat(ps.MatchedSpread)
	engineParams["central_hq_addon"] = decimal.NewFromFloat(ps.CentralHQAddOn)
	engineParams["day_count_convention"] = ps.DayCountConvention

	// Convert PD/LGD tables
	pdlgd := make(map[string]interface{})
	for key, params := range ps.PDLGDTables {
		pdlgd[key] = map[string]interface{}{
			"product": params.Product,
			"segment": params.Segment,
			"pd":      decimal.NewFromFloat(params.PD),
			"lgd":     decimal.NewFromFloat(params.LGD),
		}
	}
	engineParams["pd_lgd"] = pdlgd

	// Convert OPEX rates
	opexRates := make(map[string]interface{})
	for product, rate := range ps.OPEXRates {
		opexRates[product] = decimal.NewFromFloat(rate)
	}
	engineParams["opex_rates"] = opexRates

	// Convert economic capital
	engineParams["economic_capital_params"] = map[string]interface{}{
		"base_capital_ratio":     decimal.NewFromFloat(ps.EconomicCapital.BaseCapitalRatio),
		"capital_advantage":      decimal.NewFromFloat(ps.EconomicCapital.CapitalAdvantage),
		"dtl_advantage":          decimal.NewFromFloat(ps.EconomicCapital.DTLAdvantage),
		"security_dep_advantage": decimal.NewFromFloat(ps.EconomicCapital.SecurityDepAdvantage),
		"other_advantage":        decimal.NewFromFloat(ps.EconomicCapital.OtherAdvantage),
	}

	// Convert rounding rules
	engineParams["rounding_rules"] = map[string]interface{}{
		"currency":     ps.RoundingRules.Currency,
		"minor_units":  ps.RoundingRules.MinorUnits,
		"method":       ps.RoundingRules.Method,
		"display_rate": ps.RoundingRules.DisplayRate,
	}

	engineParams["id"] = ps.ID
	engineParams["version"] = ps.ID
	engineParams["effective_date"] = ps.EffectiveDate

	return engineParams
}
