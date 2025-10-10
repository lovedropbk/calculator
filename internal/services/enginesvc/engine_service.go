package enginesvc

import (
	"fmt"
	"sort"
	"sync"

	"financial-calculator/internal/services/adapters"
	"financial-calculator/parameters"
	"github.com/financial-calculator/engines/calculator"
	"github.com/financial-calculator/engines/campaigns"
	enginetypes "github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
)

type EngineService struct {
	ps        enginetypes.ParameterSet
	campaigns *campaigns.Engine
}

// EngineParameterSet returns the current engine parameter set in use.
func (e *EngineService) EngineParameterSet() enginetypes.ParameterSet { return e.ps }

type commissionLookupAdapter struct{ a *adapters.Adapters }

func (c commissionLookupAdapter) CommissionPercentByProduct(product string) float64 {
	if c.a == nil || c.a.Params == nil {
		return 0
	}
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
	if req.ParameterSet.ID == "" {
		req.ParameterSet = e.ps
	}
	calc := calculator.New(req.ParameterSet)
	return calc.Calculate(req)
}

// Summaries generates campaign summaries and enriches them with KPIs and subsidy splits.
func (e *EngineService) Summaries(deal enginetypes.Deal, state enginetypes.DealState, camps []enginetypes.Campaign) []enginetypes.CampaignSummary {
	base := e.campaigns.GenerateCampaignSummaries(deal, state, camps)
	if len(base) == 0 || len(camps) == 0 {
		return base
	}

	// Index campaigns by ID for quick lookup
	campByID := make(map[string]enginetypes.Campaign, len(camps))
	for _, c := range camps {
		campByID[c.ID] = c
	}

	out := make([]enginetypes.CampaignSummary, len(base))
	var wg sync.WaitGroup
	wg.Add(len(base))

	for i := range base {
		i := i
		go func() {
			defer wg.Done()
			b := base[i]
			c, ok := campByID[b.CampaignID]
			if !ok {
				out[i] = b
				return
			}

			req := enginetypes.CalculationRequest{
				Deal:         deal,
				Campaigns:    []enginetypes.Campaign{c},
				IDCItems:     nil,
				ParameterSet: e.ps, // pin exact PS for determinism
				Options:      map[string]any{"derive_idc_from_cf": true},
			}

			res, err := e.Calculate(req)
			if err != nil || res == nil || !res.Success {
				// On failure, keep base values
				out[i] = b
				return
			}

			// Extract KPIs
			monthly := res.Quote.MonthlyInstallment.InexactFloat64()
			nom := res.Quote.CustomerRateNominal.InexactFloat64()
			eff := res.Quote.CustomerRateEffective.InexactFloat64()
			rorac := res.Quote.Profitability.AcquisitionRoRAC.InexactFloat64()

			// Extract subsidy components from audit
			var subdown, freeIns, mbsp, cash float64
			for _, eae := range res.Quote.CampaignAudit {
				if !eae.Applied {
					continue
				}
				amt := enginetypes.RoundTHB(eae.Impact).InexactFloat64()
				switch eae.CampaignType {
				case enginetypes.CampaignSubdown:
					subdown += amt
				case enginetypes.CampaignFreeInsurance:
					freeIns += amt
				case enginetypes.CampaignFreeMBSP:
					mbsp += amt
				case enginetypes.CampaignCashDiscount:
					cash += amt
				}
			}
			subsidyUsed := subdown + freeIns + mbsp + cash

			// Viability check against budget (optional)
			viable := true
			var reason string
			if state.BudgetTHB > 0 && subsidyUsed > state.BudgetTHB {
				viable = false
				over := enginetypes.NewDecimal(subsidyUsed).Sub(enginetypes.NewDecimal(state.BudgetTHB))
				reason = fmt.Sprintf("exceeds budget by THB %s", over.Round(0).StringFixed(0))
			}

			// Fill enriched fields (raw numbers; UI formats display)
			b.MonthlyInstallment = monthly
			b.CustomerRateNominal = nom
			b.CustomerRateEffective = eff
			b.AcquisitionRoRAC = rorac

			b.FSSubDownTHB = subdown
			b.FreeInsuranceTHB = freeIns
			b.FreeMBSPTHB = mbsp
			b.CashDiscountTHB = cash
			b.SubsidyUsedTHB = subsidyUsed

			b.Viable = viable
			b.ViabilityReason = reason

			out[i] = b
		}()
	}

	wg.Wait()
	return out
}

// Catalog exposes the campaign catalog mapped from parameters.
func (e *EngineService) Catalog(a *adapters.Adapters) []enginetypes.Campaign {
	params, _ := a.Params.LoadLatest()
	return mapCatalogToEngineCampaigns(params)
}

// convertParametersToEngine maps repo ParameterSet to engines/types.ParameterSet.
func convertParametersToEngine(ps *parameters.ParameterSet) enginetypes.ParameterSet {
	if ps == nil {
		return enginetypes.ParameterSet{}
	}
	var terms []int
	for term := range ps.CostOfFunds {
		terms = append(terms, term)
	}
	sort.Ints(terms)
	cof := make([]enginetypes.RateCurvePoint, 0, len(terms))
	for _, t := range terms {
		cof = append(cof, enginetypes.RateCurvePoint{TermMonths: t, Rate: enginetypes.NewDecimal(ps.CostOfFunds[t])})
	}
	opex := make(map[string]decimal.Decimal)
	for k, v := range ps.OPEXRates {
		opex[k+"_opex"] = enginetypes.NewDecimal(v)
	}
	pdlgd := make(map[string]enginetypes.PDLGDEntry, len(ps.PDLGDTables))
	for key, r := range ps.PDLGDTables {
		pdlgd[key] = enginetypes.PDLGDEntry{Product: r.Product, Segment: r.Segment, PD: enginetypes.NewDecimal(r.PD), LGD: enginetypes.NewDecimal(r.LGD)}
	}
	return enginetypes.ParameterSet{
		ID:                  ps.ID,
		Version:             ps.ID,
		EffectiveDate:       ps.EffectiveDate,
		DayCountConvention:  ps.DayCountConvention,
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
		RoundingRules:  enginetypes.RoundingRules{Currency: ps.RoundingRules.Currency, MinorUnits: ps.RoundingRules.MinorUnits, Method: ps.RoundingRules.Method, DisplayRate: ps.RoundingRules.DisplayRate},
	}
}

// mapCatalogToEngineCampaigns converts parameters campaign catalog to engine campaigns.
func mapCatalogToEngineCampaigns(ps *parameters.ParameterSet) []enginetypes.Campaign {
	if ps == nil {
		return nil
	}
	out := make([]enginetypes.Campaign, 0, len(ps.CampaignCatalog))
	for _, c := range ps.CampaignCatalog {
		out = append(out, enginetypes.Campaign{ID: c.ID, Type: enginetypes.CampaignType(c.Type), Parameters: c.Parameters, Eligibility: c.Eligibility, Funder: c.Funder, Stacking: c.StackingOrder,
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
