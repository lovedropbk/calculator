package enginesvc

import (
	"sort"

	"financial-calculator/parameters"
	"financial-calculator/internal/services/adapters"
	"github.com/shopspring/decimal"
	"github.com/financial-calculator/engines/campaigns"
	"github.com/financial-calculator/engines/calculator"
	enginetypes "github.com/financial-calculator/engines/types"
)

type EngineService struct {
	ps enginetypes.ParameterSet
	campaigns *campaigns.Engine
}

type commissionLookupAdapter struct{ a *adapters.Adapters }
func (c commissionLookupAdapter) CommissionPercentByProduct(product string) float64 {
	if c.a == nil || c.a.Params == nil { return 0 }
	return c.a.Params.CommissionPercentByProduct(product)
}

func New(a *adapters.Adapters) (*EngineService, error) {
	psvc := a.Params
	params, _ := psvc.LoadLatest()
	ps := convertParametersToEngine(params)
	eng := campaigns.NewEngine(ps)
	eng.SetCommissionLookup(commissionLookupAdapter{a: a})
	return &EngineService{ps: ps, campaigns: eng}, nil
}

// Calculate executes a calculation using the current engine parameter set.
func (e *EngineService) Calculate(req enginetypes.CalculationRequest) (*enginetypes.CalculationResult, error) {
	if req.ParameterSet.ID == "" { req.ParameterSet = e.ps }
	calc := calculator.New(req.ParameterSet)
	return calc.Calculate(req)
}

// Summaries generates campaign summaries for a given deal/state/options.
func (e *EngineService) Summaries(deal enginetypes.Deal, state enginetypes.DealState, camps []enginetypes.Campaign) []enginetypes.CampaignSummary {
	return e.campaigns.GenerateCampaignSummaries(deal, state, camps)
}

// Catalog exposes the campaign catalog mapped from parameters.
func (e *EngineService) Catalog(a *adapters.Adapters) []enginetypes.Campaign {
	params, _ := a.Params.LoadLatest()
	return mapCatalogToEngineCampaigns(params)
}

// convertParametersToEngine maps repo ParameterSet to engines/types.ParameterSet.
func convertParametersToEngine(ps *parameters.ParameterSet) enginetypes.ParameterSet {
	if ps == nil { return enginetypes.ParameterSet{} }
	var terms []int
	for term := range ps.CostOfFunds { terms = append(terms, term) }
	sort.Ints(terms)
	cof := make([]enginetypes.RateCurvePoint, 0, len(terms))
	for _, t := range terms {
		cof = append(cof, enginetypes.RateCurvePoint{ TermMonths: t, Rate: enginetypes.NewDecimal(ps.CostOfFunds[t]) })
	}
	opex := make(map[string]decimal.Decimal)
	for k, v := range ps.OPEXRates { opex[k+"_opex"] = enginetypes.NewDecimal(v) }
	pdlgd := make(map[string]enginetypes.PDLGDEntry, len(ps.PDLGDTables))
	for key, r := range ps.PDLGDTables {
		pdlgd[key] = enginetypes.PDLGDEntry{ Product: r.Product, Segment: r.Segment, PD: enginetypes.NewDecimal(r.PD), LGD: enginetypes.NewDecimal(r.LGD) }
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
			BaseCapitalRatio:     enginetypes.NewDecimal(0.088),
			CapitalAdvantage:     enginetypes.NewDecimal(ps.EconomicCapital.CapitalAdvantage),
			DTLAdvantage:         enginetypes.NewDecimal(ps.EconomicCapital.DTLAdvantage),
			SecurityDepAdvantage: enginetypes.NewDecimal(ps.EconomicCapital.SecurityDepAdvantage),
			OtherAdvantage:       enginetypes.NewDecimal(ps.EconomicCapital.OtherAdvantage),
		},
		CentralHQAddOn: enginetypes.NewDecimal(ps.CentralHQAddOn),
		RoundingRules: enginetypes.RoundingRules{ Currency: ps.RoundingRules.Currency, MinorUnits: ps.RoundingRules.MinorUnits, Method: ps.RoundingRules.Method, DisplayRate: ps.RoundingRules.DisplayRate },
	}
}

// mapCatalogToEngineCampaigns converts parameters campaign catalog to engine campaigns.
func mapCatalogToEngineCampaigns(ps *parameters.ParameterSet) []enginetypes.Campaign {
	if ps == nil { return nil }
	out := make([]enginetypes.Campaign, 0, len(ps.CampaignCatalog))
	for _, c := range ps.CampaignCatalog {
		out = append(out, enginetypes.Campaign{ ID: c.ID, Type: enginetypes.CampaignType(c.Type), Parameters: c.Parameters, Eligibility: c.Eligibility, Funder: c.Funder, Stacking: c.StackingOrder,
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

func asDecimal(v any) decimal.Decimal {
	switch t := v.(type) {
	case float64: return enginetypes.NewDecimal(t)
	case float32: return enginetypes.NewDecimal(float64(t))
	case int: return enginetypes.NewDecimal(float64(t))
	case int64: return enginetypes.NewDecimal(float64(t))
	case int32: return enginetypes.NewDecimal(float64(t))
	default: return enginetypes.NewDecimal(0)
	}
}
