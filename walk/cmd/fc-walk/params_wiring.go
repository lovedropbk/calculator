package main

import (
	"sort"

	"financial-calculator/parameters"

	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
)

// convertParametersToEngine maps parameters.ParameterSet (repo root) to engines/types.ParameterSet.
func convertParametersToEngine(ps *parameters.ParameterSet) types.ParameterSet {
	if ps == nil {
		return types.ParameterSet{}
	}

	// Cost of funds curve: sort by term for stable order.
	var terms []int
	for term := range ps.CostOfFunds {
		terms = append(terms, term)
	}
	sort.Ints(terms)
	cof := make([]types.RateCurvePoint, 0, len(terms))
	for _, t := range terms {
		cof = append(cof, types.RateCurvePoint{
			TermMonths: t,
			Rate:       types.NewDecimal(ps.CostOfFunds[t]),
		})
	}

	// OPEX: engines expect keys like "HP_opex", "mySTAR_opex", "F-Lease_opex", "Op-Lease_opex"
	opex := make(map[string]decimal.Decimal)
	for k, v := range ps.OPEXRates {
		opex[k+"_opex"] = types.NewDecimal(v)
	}

	// PDLGD tables
	pdlgd := make(map[string]types.PDLGDEntry, len(ps.PDLGDTables))
	for key, r := range ps.PDLGDTables {
		pdlgd[key] = types.PDLGDEntry{
			Product: r.Product,
			Segment: r.Segment,
			PD:      types.NewDecimal(r.PD),
			LGD:     types.NewDecimal(r.LGD),
		}
	}

	return types.ParameterSet{
		ID:                 ps.ID,
		Version:            ps.ID,
		EffectiveDate:      ps.EffectiveDate,
		DayCountConvention: ps.DayCountConvention,

		CostOfFundsCurve:    cof,
		MatchedFundedSpread: types.NewDecimal(ps.MatchedSpread),
		PDLGD:               pdlgd,
		OPEXRates:           opex,
		EconomicCapitalParams: types.EconomicCapitalParams{
			BaseCapitalRatio:     types.NewDecimal(ps.EconomicCapital.BaseCapitalRatio),
			CapitalAdvantage:     types.NewDecimal(ps.EconomicCapital.CapitalAdvantage),
			DTLAdvantage:         types.NewDecimal(ps.EconomicCapital.DTLAdvantage),
			SecurityDepAdvantage: types.NewDecimal(ps.EconomicCapital.SecurityDepAdvantage),
			OtherAdvantage:       types.NewDecimal(ps.EconomicCapital.OtherAdvantage),
		},
		CentralHQAddOn: types.NewDecimal(ps.CentralHQAddOn),
		RoundingRules: types.RoundingRules{
			Currency:    ps.RoundingRules.Currency,
			MinorUnits:  ps.RoundingRules.MinorUnits,
			Method:      ps.RoundingRules.Method,
			DisplayRate: ps.RoundingRules.DisplayRate,
		},
	}
}

// staticCommissionLookup implements the calculator.CommissionLookup-compatible method
// used by UI when parameters are loaded from YAML directly.
type staticCommissionLookup struct {
	by map[string]float64
}

func (s staticCommissionLookup) CommissionPercentByProduct(product string) float64 {
	if product == "" {
		return 0
	}
	// Try YAML policy map with key synonyms
	if s.by != nil {
		for _, key := range commissionKeysLocal(product) {
			if v, ok := s.by[key]; ok {
				if v < 0 {
					return 0
				}
				return v
			}
		}
	}
	// Fallback defaults
	return defaultCommissionPercentLocal(product)
}

func defaultCommissionPercentLocal(product string) float64 {
	switch product {
	case "HP", "HirePurchase", "Hire Purchase":
		return 0.03
	case "mySTAR", "BalloonHP", "Balloon":
		return 0.07
	case "F-Lease", "FinanceLease", "Finance Lease", "FLease", "F Lease", "Financing Lease":
		return 0.07
	case "Op-Lease", "OperatingLease", "Operating Lease", "OpLease", "Op Lease":
		return 0.07
	default:
		return 0
	}
}

func commissionKeysLocal(product string) []string {
	switch product {
	case "HP", "HirePurchase", "Hire Purchase", "hp", "Hp":
		return []string{"HP", "HirePurchase", "Hire Purchase"}
	case "mySTAR", "mystar", "MySTAR", "BalloonHP", "Balloon":
		return []string{"mySTAR", "BalloonHP", "Balloon"}
	case "F-Lease", "FinanceLease", "Finance Lease", "FLease", "F Lease", "Financing Lease":
		return []string{"F-Lease", "FinanceLease", "Finance Lease", "FLease", "F Lease", "Financing Lease"}
	case "Op-Lease", "OperatingLease", "Operating Lease", "OpLease", "Op Lease":
		return []string{"Op-Lease", "OperatingLease", "Operating Lease", "OpLease", "Op Lease"}
	default:
		return []string{product}
	}
}
