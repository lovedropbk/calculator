package main

import (
	"sort"

	"financial-calculator/parameters"

	"github.com/shopspring/decimal"
	"github.com/financial-calculator/engines/campaigns"
	"github.com/financial-calculator/engines/calculator"
	enginetypes "github.com/financial-calculator/engines/types"
)

// convertParametersToEngine maps parameters.ParameterSet (repo root) to engines/types.ParameterSet.
func convertParametersToEngine(ps *parameters.ParameterSet) enginetypes.ParameterSet {
	if ps == nil {
		return enginetypes.ParameterSet{}
	}

	// Cost of funds curve: sort by term for stable order.
	var terms []int
	for term := range ps.CostOfFunds {
		terms = append(terms, term)
	}
	sort.Ints(terms)
	cof := make([]enginetypes.RateCurvePoint, 0, len(terms))
	for _, t := range terms {
		cof = append(cof, enginetypes.RateCurvePoint{
			TermMonths: t,
			Rate:       enginetypes.NewDecimal(ps.CostOfFunds[t]),
		})
	}

	// OPEX: engines expect keys like "HP_opex", "mySTAR_opex", "F-Lease_opex", "Op-Lease_opex"
	opex := make(map[string]decimal.Decimal)
	for k, v := range ps.OPEXRates {
		opex[k+"_opex"] = enginetypes.NewDecimal(v)
	}

	// PDLGD tables
	pdlgd := make(map[string]enginetypes.PDLGDEntry, len(ps.PDLGDTables))
	for key, r := range ps.PDLGDTables {
		pdlgd[key] = enginetypes.PDLGDEntry{
			Product: r.Product,
			Segment: r.Segment,
			PD:      enginetypes.NewDecimal(r.PD),
			LGD:     enginetypes.NewDecimal(r.LGD),
		}
	}

	return enginetypes.ParameterSet{
		ID:                 ps.ID,
		Version:            ps.ID,
		EffectiveDate:      ps.EffectiveDate,
		DayCountConvention: ps.DayCountConvention,

		CostOfFundsCurve:    cof,
		MatchedFundedSpread: enginetypes.NewDecimal(ps.MatchedSpread),
		PDLGD:               pdlgd,
		OPEXRates:           opex,
		EconomicCapitalParams: enginetypes.EconomicCapitalParams{
			// HQ directive: fix EC at 8.8% for display and RoRAC until policy changes.
			BaseCapitalRatio:     enginetypes.NewDecimal(0.088),
			CapitalAdvantage:     enginetypes.NewDecimal(ps.EconomicCapital.CapitalAdvantage),
			DTLAdvantage:         enginetypes.NewDecimal(ps.EconomicCapital.DTLAdvantage),
			SecurityDepAdvantage: enginetypes.NewDecimal(ps.EconomicCapital.SecurityDepAdvantage),
			OtherAdvantage:       enginetypes.NewDecimal(ps.EconomicCapital.OtherAdvantage),
		},
		CentralHQAddOn: enginetypes.NewDecimal(ps.CentralHQAddOn),
		RoundingRules: enginetypes.RoundingRules{
			Currency:    ps.RoundingRules.Currency,
			MinorUnits:  ps.RoundingRules.MinorUnits,
			Method:      ps.RoundingRules.Method,
			DisplayRate: ps.RoundingRules.DisplayRate,
		},
	}
}

// commissionLookupAdapter bridges parameters.Service to campaigns.Engine commission lookup.
type commissionLookupAdapter struct{ svc *parameters.Service }

func (a commissionLookupAdapter) CommissionPercentByProduct(product string) float64 {
	if a.svc == nil {
		return 0
	}
	return a.svc.CommissionPercentByProduct(product)
}

// mapCatalogToEngineCampaigns converts parameters campaign catalog to engine campaign definitions.
func mapCatalogToEngineCampaigns(ps *parameters.ParameterSet) []enginetypes.Campaign {
	if ps == nil {
		return nil
	}
	out := make([]enginetypes.Campaign, 0, len(ps.CampaignCatalog))
	for _, c := range ps.CampaignCatalog {
		out = append(out, enginetypes.Campaign{
			ID:       c.ID,
			Type:     enginetypes.CampaignType(c.Type),
			Parameters: c.Parameters,
			Eligibility: c.Eligibility,
			Funder:   c.Funder,
			Stacking: c.StackingOrder,
			// Best-effort mapping of type-specific fields from parameters.Parameters if present
			SubsidyPercent:  asDecimal(c.Parameters["subsidy_percent"]),
			SubsidyAmount:   asDecimal(c.Parameters["subsidy_amount"]),
			TargetRate:      asDecimal(c.Parameters["target_rate"]),
			DiscountPercent: asDecimal(c.Parameters["discount_percent"]),
			DiscountAmount:  asDecimal(c.Parameters["discount_amount"]),
			InsuranceCost:   asDecimal(c.Parameters["insurance_cost"]),
			MBSPCost:        asDecimal(c.Parameters["mbsp_cost"]),
		})
	}
	return out
}

// asDecimal tries to convert arbitrary numeric to engine decimal.
func asDecimal(v any) decimal.Decimal {
	switch t := v.(type) {
	case float64:
		return enginetypes.NewDecimal(t)
	case float32:
		return enginetypes.NewDecimal(float64(t))
	case int:
		return enginetypes.NewDecimal(float64(t))
	case int64:
		return enginetypes.NewDecimal(float64(t))
	case int32:
		return enginetypes.NewDecimal(float64(t))
	default:
		return enginetypes.NewDecimal(0)
	}
}

// CampaignSummariesRequest is the input shape for /campaigns/summaries
// It carries Deal + DealState for commission overrides and a list of candidate campaigns.
type CampaignSummariesRequest struct {
	Deal      enginetypes.Deal       `json:"deal"`
	State     enginetypes.DealState  `json:"state"`
	Campaigns []enginetypes.Campaign `json:"campaigns"`
}

func generateSummaries(ps enginetypes.ParameterSet, svc *parameters.Service, deal enginetypes.Deal, state enginetypes.DealState, camps []enginetypes.Campaign) ([]enginetypes.CampaignSummary, error) {
	eng := campaigns.NewEngine(ps)
	eng.SetCommissionLookup(commissionLookupAdapter{svc: svc})
	return eng.GenerateCampaignSummaries(deal, state, camps), nil
}

func calculate(ps enginetypes.ParameterSet, req enginetypes.CalculationRequest) (*enginetypes.CalculationResult, error) {
	calc := calculator.New(ps)
	return calc.Calculate(req)
}
