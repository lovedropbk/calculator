//go:build windows

package main

import (
	"fmt"
	"time"

	"github.com/financial-calculator/engines/calculator"
	"github.com/financial-calculator/engines/campaigns"
	"github.com/financial-calculator/engines/types"
)

// UIState captures only the input fields needed for validation/orchestration.
// Keep this independent of Walk runtime so it can be tested as pure functions.
type UIState struct {
	Product    string  // "HP" | "mySTAR" | "F-Lease" | "Op-Lease"
	PriceExTax float64 // THB
	TermMonths int     // months
	BalloonPct float64 // percent, e.g., 10.0 means 10%
}

// ValidationError is a typed error describing why inputs are invalid.
type ValidationError struct {
	Reason string
}

func (e *ValidationError) Error() string {
	return e.Reason
}

// validateInputs enforces minimal guards that must pass before any compute.
// Rules:
// - PriceExTax > 0
// - TermMonths > 0
// - For mySTAR: BalloonPct >= 0 and if BalloonPct > 0 then TermMonths >= 6
func validateInputs(s UIState) error {
	if s.PriceExTax <= 0 {
		return &ValidationError{Reason: "price must be positive"}
	}
	if s.TermMonths <= 0 {
		return &ValidationError{Reason: "term must be positive"}
	}
	if s.Product == "mySTAR" {
		if s.BalloonPct < 0 {
			return &ValidationError{Reason: "balloon percent must be ≥ 0"}
		}
		if s.BalloonPct > 0 && s.TermMonths < 6 {
			return &ValidationError{Reason: "term must be ≥ 6 when balloon > 0 for mySTAR"}
		}
	}
	return nil
}

// shouldCompute returns true only when validateInputs returns nil.
func shouldCompute(s UIState) bool {
	return validateInputs(s) == nil
}

// MARK: Edit mode helpers (pure)
//
// Keep Walk-agnostic: main.go can use these to manage "edit mode" and selection flags.
type EditorState struct {
	SelectedMyCampaignID string
	IsEditMode           bool
}

// SelectMyCampaign sets the selected campaign ID and enters edit mode.
func SelectMyCampaign(state *EditorState, id string) {
	if state == nil {
		return
	}
	state.SelectedMyCampaignID = id
	state.IsEditMode = true
}

// ExitEditMode clears the edit mode flag (selection can be kept or cleared by caller).
func ExitEditMode(state *EditorState) {
	if state == nil {
		return
	}
	state.IsEditMode = false
}

// MARK: Draft reducers (pure)
//
// Small, UI-agnostic helpers for editing CampaignDraft values. They do not
// mutate the input; they return an updated copy.

// UpdateDraftInputs returns a copy of d with Inputs replaced by in and
// Metadata.UpdatedAt set to now (CreatedAt unchanged).
func UpdateDraftInputs(d CampaignDraft, in CampaignInputs, now string) CampaignDraft {
	out := d
	out.Inputs = in
	out.Metadata.UpdatedAt = now
	return out
}

// UpdateDraftAdjustments returns a copy of d with Adjustments replaced by adj
// and Metadata.UpdatedAt set to now (CreatedAt unchanged).
func UpdateDraftAdjustments(d CampaignDraft, adj CampaignAdjustments, now string) CampaignDraft {
	out := d
	out.Adjustments = adj
	out.Metadata.UpdatedAt = now
	return out
}

// UpdateDraft returns a copy of d with both Inputs and Adjustments replaced,
// and Metadata.UpdatedAt set to now (CreatedAt unchanged).
func UpdateDraft(d CampaignDraft, in CampaignInputs, adj CampaignAdjustments, now string) CampaignDraft {
	out := d
	out.Inputs = in
	out.Adjustments = adj
	out.Metadata.UpdatedAt = now
	return out
}

// BuildCampaignInputs constructs CampaignInputs from high-level values.
// No validation here; validation remains in validateInputs and other callers.
func BuildCampaignInputs(
	price float64,
	dpPercent float64,
	dpTHB float64,
	term int,
	balloonPct float64,
	rateMode string,
	apr float64,
	targetTHB float64,
) CampaignInputs {
	return CampaignInputs{
		PriceExTaxTHB:        price,
		DownpaymentPercent:   dpPercent,
		DownpaymentTHB:       dpTHB,
		TermMonths:           term,
		BalloonPercent:       balloonPct,
		RateMode:             rateMode,
		CustomerRateAPR:      apr,
		TargetInstallmentTHB: targetTHB,
	}
}

// MARK: Campaign rows computation and binding (UI-agnostic helpers)
//
// These helpers compute per-campaign metrics for the Walk UI grid and selected summary.
// They are kept free of Walk types so they are unit testable.

// normalizeDealForCampaign ensures FinancedAmount and BalloonAmount are set for engines that don't
// auto-derive them from percent fields.
func normalizeDealForCampaign(d types.Deal) types.Deal {
	// Down payment normalization
	if d.DownPaymentLocked == "percent" {
		d.DownPaymentAmount = types.RoundTHB(d.PriceExTax.Mul(d.DownPaymentPercent))
	} else if d.DownPaymentLocked == "amount" {
		if d.PriceExTax.GreaterThan(types.NewDecimal(0)) {
			d.DownPaymentPercent = d.DownPaymentAmount.Div(d.PriceExTax)
		}
	}
	// Financed amount (exclude financed IDCs here; grid uses clean base)
	d.FinancedAmount = types.RoundTHB(d.PriceExTax.Sub(d.DownPaymentAmount))

	// Balloon amount from percent if needed
	if d.BalloonAmount.IsZero() && d.BalloonPercent.GreaterThan(types.NewDecimal(0)) {
		d.BalloonAmount = types.RoundTHB(d.PriceExTax.Mul(d.BalloonPercent))
	}
	return d
}

// computeCampaignRows produces the rows for the Campaign Options grid and returns the clamped selected index.
// For each campaign type, it computes:
// - Subinterest: via SubinterestByBudget using subsidyBudgetTHB
// - Cash Discount: baseline metrics (no campaign) with Dealer Commission forced to 0% via summaries
// - Subdown: use subsidyBudgetTHB to increase Down Payment (reduce financed amount). No T0 subsidy cashflow created.
// - Free Insurance / Free MBSP: baseline pricing + treat subsidyBudgetTHB as IDC Other T0 inflow (revenue)
// Rows carry numeric metrics for the Key Metrics Summary binding.
func computeCampaignRows(
	ps types.ParameterSet,
	calc *calculator.Calculator,
	campEng *campaigns.Engine,
	baseDeal types.Deal,
	state types.DealState,
	displayCampaigns []types.Campaign,
	subsidyBudgetTHB float64,
	dpPercent float64,
	selectedIdx int,
) ([]CampaignRow, int) {
	deal := normalizeDealForCampaign(baseDeal)

	// Dealer commission summaries per option (forces 0% for Cash Discount)
	var summaries []types.CampaignSummary
	if campEng != nil {
		summaries = campEng.GenerateCampaignSummaries(deal, state, displayCampaigns)
	}

	// Helper: compute quote with provided IDC items (commission/subsidy), no campaign transforms
	baselineQuote := func(idcs []types.IDCItem, d types.Deal) (types.Quote, bool) {
		if calc == nil {
			return types.Quote{}, false
		}
		req := types.CalculationRequest{
			Deal:         d,
			Campaigns:    []types.Campaign{},
			IDCItems:     idcs,
			ParameterSet: ps,
			Options:      map[string]interface{}{"derive_idc_from_cf": true},
		}
		res, err := calc.Calculate(req)
		if err != nil || res == nil || !res.Success {
			return types.Quote{}, false
		}
		return res.Quote, true
	}

	// Local helper: map engine quote -> ProfitabilitySnapshot
	snapshotFromQuote := func(q types.Quote) ProfitabilitySnapshot {
		p := q.Profitability
		return ProfitabilitySnapshot{
			DealIRREffective:    p.DealIRREffective.InexactFloat64(),
			DealIRRNominal:      p.DealIRRNominal.InexactFloat64(),
			IDCUpfrontCostPct:   p.IDCUpfrontCostPct.InexactFloat64(),
			SubsidyUpfrontPct:   p.SubsidyUpfrontPct.InexactFloat64(),
			CostOfDebt:          p.CostOfDebtMatched.InexactFloat64(),
			MatchedFundedSpread: p.MatchedFundedSpread.InexactFloat64(),
			GrossInterestMargin: p.GrossInterestMargin.InexactFloat64(),
			CapitalAdvantage:    p.CapitalAdvantage.InexactFloat64(),
			NetInterestMargin:   p.NetInterestMargin.InexactFloat64(),
			CostOfCreditRisk:    p.CostOfCreditRisk.InexactFloat64(),
			OPEX:                p.OPEX.InexactFloat64(),
			// Show net periodic impact actually used in NetEBIT (IDCSubsidiesFeesPeriodic), not the separated placeholders.
			IDCPeriodicPct:     p.IDCSubsidiesFeesPeriodic.InexactFloat64(),
			SubsidyPeriodicPct: p.SubsidyPeriodicPct.InexactFloat64(),
			NetEBITMargin:      p.NetEBITMargin.InexactFloat64(),
			EconomicCapital:    p.EconomicCapital.InexactFloat64(),
			AcquisitionRoRAC:   p.AcquisitionRoRAC.InexactFloat64(),
		}
	}

	// Local helper: map limited metrics -> ProfitabilitySnapshot (fallback when full quote not available)
	snapshotFromMetrics := func(m types.CampaignMetrics) ProfitabilitySnapshot {
		return ProfitabilitySnapshot{
			NetEBITMargin:    m.NetEBITMargin.InexactFloat64(),
			EconomicCapital:  m.EconomicCapital.InexactFloat64(),
			AcquisitionRoRAC: m.AcquisitionRoRAC.InexactFloat64(),
		}
	}

	// Default selected index
	if selectedIdx < 0 || selectedIdx >= len(displayCampaigns) {
		selectedIdx = 0
	}

	rows := make([]CampaignRow, 0, len(displayCampaigns))

	// Local: format downpayment as "THB X (Y% DP)"
	dpString := func(dpAmtF, priceF float64) string {
		pct := 0.0
		if priceF > 0 {
			pct = (dpAmtF / priceF) * 100.0
		}
		return fmt.Sprintf("THB %s (%.0f%% DP)", FormatTHB(dpAmtF), pct)
	}

	for i, c := range displayCampaigns {
		row := CampaignRow{
			Selected:        i == selectedIdx,
			Name:            campaignTypeDisplayName(c.Type),
			DownpaymentStr:  "",
			CashDiscountStr: "",
			Notes:           "",
			SubsidyValue:    subsidyBudgetTHB,
		}

		// Dealer commission from summaries (if available)
		if i < len(summaries) {
			row.DealerCommAmt = summaries[i].DealerCommissionAmt
			row.DealerCommPct = summaries[i].DealerCommissionPct
			row.DealerComm = FormatDealerCommission(row.DealerCommAmt, row.DealerCommPct)
		} else {
			row.DealerCommAmt = 0
			row.DealerCommPct = 0
			row.DealerComm = FormatDealerCommission(0, 0)
		}

		dpForCF := deal.DownPaymentAmount
		// Default DP string for this row (may be overridden in specific cases below)
		row.DownpaymentStr = dpString(dpForCF.InexactFloat64(), deal.PriceExTax.InexactFloat64())

		// Common IDC - Other from state (applied as upfront T0 cost in all financed scenarios)
		otherIDC := state.IDCOther.Value

		switch c.Type {
		case types.CampaignBaseNoSubsidy:
			// Baseline with dealer commission + IDC Other; no subsidy injected.
			idcs := []types.IDCItem{}
			if row.DealerCommAmt > 0 {
				idcs = append(idcs, types.IDCItem{
					Category:    types.IDCBrokerCommission,
					Amount:      types.NewDecimal(row.DealerCommAmt),
					Payer:       "Dealer",
					Financed:    false,
					Withheld:    false,
					Timing:      types.IDCTimingUpfront,
					TaxFlags:    nil,
					IsRevenue:   false,
					IsCost:      true,
					Description: "Dealer commission",
				})
			}
			if otherIDC > 0 {
				idcs = append(idcs, types.IDCItem{
					Category:    types.IDCAdminFee,
					Amount:      types.NewDecimal(otherIDC),
					Payer:       "Dealer/Provider",
					Financed:    false,
					Withheld:    false,
					Timing:      types.IDCTimingUpfront,
					TaxFlags:    nil,
					IsRevenue:   false,
					IsCost:      true,
					Description: "IDC - Other",
				})
			}
			if q, ok := baselineQuote(idcs, deal); ok {
				mi := q.MonthlyInstallment.InexactFloat64()
				row.MonthlyInstallment = mi
				row.MonthlyInstallmentStr = FormatTHB(mi)
				row.NominalRate = q.CustomerRateNominal.InexactFloat64()
				row.EffectiveRate = q.CustomerRateEffective.InexactFloat64()
				row.AcqRoRac = q.Profitability.AcquisitionRoRAC.InexactFloat64()
				row.NominalRateStr = FormatRatePct(row.NominalRate)
				row.EffectiveRateStr = FormatRatePct(row.EffectiveRate)
				row.AcqRoRacStr = FormatRatePct(row.AcqRoRac)
				row.IDCDealerTHB = row.DealerCommAmt
				row.IDCOtherTHB = otherIDC
				row.SubsidyUsedTHBStr = FormatTHB(0)
				row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(0), row.AcqRoRac*100.0)
				row.Profit = snapshotFromQuote(q)
				row.Cashflows = q.Cashflows
			} else {
				row.MonthlyInstallmentStr = ""
				row.SubsidyUsedTHBStr = ""
				row.SubsidyRorac = fmt.Sprintf("THB %s / -", FormatTHB(0))
			}

		case types.CampaignBaseSubsidy:
			// Baseline with dealer commission + IDC Other; inject full subsidy upfront at T0 (income).
			idcs := []types.IDCItem{}
			if row.DealerCommAmt > 0 {
				idcs = append(idcs, types.IDCItem{
					Category:    types.IDCBrokerCommission,
					Amount:      types.NewDecimal(row.DealerCommAmt),
					Payer:       "Dealer",
					Financed:    false,
					Withheld:    false,
					Timing:      types.IDCTimingUpfront,
					TaxFlags:    nil,
					IsRevenue:   false,
					IsCost:      true,
					Description: "Dealer commission",
				})
			}
			if otherIDC > 0 {
				idcs = append(idcs, types.IDCItem{
					Category:    types.IDCAdminFee,
					Amount:      types.NewDecimal(otherIDC),
					Payer:       "Dealer/Provider",
					Financed:    false,
					Withheld:    false,
					Timing:      types.IDCTimingUpfront,
					TaxFlags:    nil,
					IsRevenue:   false,
					IsCost:      true,
					Description: "IDC - Other",
				})
			}
			req := types.CalculationRequest{
				Deal: deal,
				Campaigns: []types.Campaign{
					{ID: "BASE-SUBSIDY", Type: types.CampaignBaseSubsidy, SubsidyAmount: types.NewDecimal(subsidyBudgetTHB)},
				},
				IDCItems:     idcs,
				ParameterSet: ps,
				Options:      map[string]interface{}{"derive_idc_from_cf": true},
			}
			if res, err := calc.Calculate(req); err == nil && res != nil && res.Success {
				q := res.Quote
				mi := q.MonthlyInstallment.InexactFloat64()
				row.MonthlyInstallment = mi
				row.MonthlyInstallmentStr = FormatTHB(mi)
				row.NominalRate = q.CustomerRateNominal.InexactFloat64()
				row.EffectiveRate = q.CustomerRateEffective.InexactFloat64()
				row.AcqRoRac = q.Profitability.AcquisitionRoRAC.InexactFloat64()
				row.NominalRateStr = FormatRatePct(row.NominalRate)
				row.EffectiveRateStr = FormatRatePct(row.EffectiveRate)
				row.AcqRoRacStr = FormatRatePct(row.AcqRoRac)
				row.IDCDealerTHB = row.DealerCommAmt
				row.IDCOtherTHB = otherIDC
				row.SubsidyUsedTHBStr = FormatTHB(subsidyBudgetTHB)
				row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(subsidyBudgetTHB), row.AcqRoRac*100.0)
				row.Profit = snapshotFromQuote(q)
				row.Cashflows = q.Cashflows
			} else {
				row.MonthlyInstallmentStr = ""
				row.SubsidyUsedTHBStr = ""
				row.SubsidyRorac = fmt.Sprintf("THB %s / -", FormatTHB(subsidyBudgetTHB))
			}

		case types.CampaignSubinterest:
			// Budget-constrained nominal rate reduction
			input := types.CampaignBudgetInput{
				Deal:         deal,
				ParameterSet: ps,
				BudgetTHB:    types.NewDecimal(subsidyBudgetTHB),
				RateCaps:     nil,
			}
			out, err := campaigns.SubinterestByBudget(input)
			if err == nil && out.Error == nil {
				// Prepare IDC items: commission cost + IDC Other (subsidy modeled as periodic income via Options)
				idcItems := []types.IDCItem{}
				if row.DealerCommAmt > 0 {
					idcItems = append(idcItems, types.IDCItem{
						Category:    types.IDCBrokerCommission,
						Amount:      types.NewDecimal(row.DealerCommAmt),
						Payer:       "Dealer",
						Financed:    false,
						Withheld:    false,
						Timing:      types.IDCTimingUpfront,
						TaxFlags:    nil,
						IsRevenue:   false,
						IsCost:      true,
						Description: "Dealer commission",
					})
				}
				if otherIDC > 0 {
					idcItems = append(idcItems, types.IDCItem{
						Category:    types.IDCAdminFee,
						Amount:      types.NewDecimal(otherIDC),
						Payer:       "Dealer/Provider",
						Financed:    false,
						Withheld:    false,
						Timing:      types.IDCTimingUpfront,
						TaxFlags:    nil,
						IsRevenue:   false,
						IsCost:      true,
						Description: "IDC - Other",
					})
				}

				// Recompute quote at the solved nominal rate so profitability includes IDC impacts and periodic subsidy income.
				deal2 := deal
				deal2.RateMode = "fixed_rate"
				deal2.CustomerNominalRate = out.Metrics.CustomerNominalRate

				req := types.CalculationRequest{
					Deal:         deal2,
					Campaigns:    []types.Campaign{},
					IDCItems:     idcItems,
					ParameterSet: ps,
					Options:      map[string]interface{}{"derive_idc_from_cf": true, "add_subsidy_upfront_thb": out.Metrics.SubsidyUsedTHB.InexactFloat64()},
				}
				if res2, err2 := calc.Calculate(req); err2 == nil && res2 != nil && res2.Success {
					q := res2.Quote
					mi := q.MonthlyInstallment.InexactFloat64()
					row.MonthlyInstallment = mi
					row.MonthlyInstallmentStr = FormatTHB(mi)

					row.NominalRate = q.CustomerRateNominal.InexactFloat64()
					row.EffectiveRate = q.CustomerRateEffective.InexactFloat64()
					row.AcqRoRac = q.Profitability.AcquisitionRoRAC.InexactFloat64()

					row.NominalRateStr = FormatRatePct(row.NominalRate)
					row.EffectiveRateStr = FormatRatePct(row.EffectiveRate)
					row.AcqRoRacStr = FormatRatePct(row.AcqRoRac)

					row.IDCDealerTHB = row.DealerCommAmt
					row.IDCOtherTHB = out.Metrics.SubsidyUsedTHB.InexactFloat64()

					row.SubsidyUsedTHBStr = FormatTHB(row.IDCOtherTHB)
					row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(row.IDCOtherTHB), row.AcqRoRac*100.0)
					row.Profit = snapshotFromQuote(q)
					row.Cashflows = q.Cashflows
				} else {
					// Best-effort fallback: use solver metrics
					mi := out.Metrics.MonthlyInstallment.InexactFloat64()
					row.MonthlyInstallment = mi
					row.MonthlyInstallmentStr = FormatTHB(mi)

					row.NominalRate = out.Metrics.CustomerNominalRate.InexactFloat64()
					row.EffectiveRate = out.Metrics.CustomerEffectiveRate.InexactFloat64()
					row.AcqRoRac = out.Metrics.AcquisitionRoRAC.InexactFloat64()

					row.NominalRateStr = FormatRatePct(row.NominalRate)
					row.EffectiveRateStr = FormatRatePct(row.EffectiveRate)
					row.AcqRoRacStr = FormatRatePct(row.AcqRoRac)

					row.IDCDealerTHB = row.DealerCommAmt
					row.IDCOtherTHB = out.Metrics.SubsidyUsedTHB.InexactFloat64()

					row.SubsidyUsedTHBStr = FormatTHB(row.IDCOtherTHB)
					row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(row.IDCOtherTHB), row.AcqRoRac*100.0)
					row.Profit = snapshotFromMetrics(out.Metrics)
					row.Cashflows = out.Cashflows
				}
			} else {
				// Fallback to baseline with commission + IDC Other
				idcs := []types.IDCItem{}
				if row.DealerCommAmt > 0 {
					idcs = append(idcs, types.IDCItem{
						Category:    types.IDCBrokerCommission,
						Amount:      types.NewDecimal(row.DealerCommAmt),
						Payer:       "Dealer",
						Financed:    false,
						Withheld:    false,
						Timing:      types.IDCTimingUpfront,
						TaxFlags:    nil,
						IsRevenue:   false,
						IsCost:      true,
						Description: "Dealer commission",
					})
				}
				if otherIDC > 0 {
					idcs = append(idcs, types.IDCItem{
						Category:    types.IDCAdminFee,
						Amount:      types.NewDecimal(otherIDC),
						Payer:       "Dealer/Provider",
						Financed:    false,
						Withheld:    false,
						Timing:      types.IDCTimingUpfront,
						TaxFlags:    nil,
						IsRevenue:   false,
						IsCost:      true,
						Description: "IDC - Other",
					})
				}
				if q, ok := baselineQuote(idcs, deal); ok {
					mi := q.MonthlyInstallment.InexactFloat64()
					row.MonthlyInstallment = mi
					row.MonthlyInstallmentStr = FormatTHB(mi)

					row.NominalRate = q.CustomerRateNominal.InexactFloat64()
					row.EffectiveRate = q.CustomerRateEffective.InexactFloat64()
					row.AcqRoRac = q.Profitability.AcquisitionRoRAC.InexactFloat64()

					row.NominalRateStr = FormatRatePct(row.NominalRate)
					row.EffectiveRateStr = FormatRatePct(row.EffectiveRate)
					row.AcqRoRacStr = FormatRatePct(row.AcqRoRac)

					row.IDCDealerTHB = row.DealerCommAmt
					row.IDCOtherTHB = otherIDC

					row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(0), row.AcqRoRac*100.0)
					row.Profit = snapshotFromQuote(q)
					row.Cashflows = q.Cashflows
				} else {
					row.MonthlyInstallmentStr = ""
					row.SubsidyUsedTHBStr = ""
					row.SubsidyRorac = fmt.Sprintf("THB %s / -", FormatTHB(subsidyBudgetTHB))
				}
			}

		case types.CampaignCashDiscount:
			// Non-financing reference row:
			// - Cash Discount column shows subsidy budget
			// - Downpayment column shows effective cash price (Vehicle Price - Subsidy Budget)
			// - All financing metrics blank; Notes updated.
			row.CashDiscountStr = "THB " + FormatTHB(subsidyBudgetTHB)
			effectiveCash := deal.PriceExTax.Sub(types.NewDecimal(subsidyBudgetTHB)).InexactFloat64()
			if effectiveCash < 0 {
				effectiveCash = 0
			}
			row.DownpaymentStr = "THB " + FormatTHB(effectiveCash)
			row.MonthlyInstallmentStr = ""
			row.NominalRateStr = ""
			row.EffectiveRateStr = ""
			row.AcqRoRacStr = ""
			row.SubsidyRorac = "—"
			row.DealerComm = "THB 0 (0%)"
			row.Notes = "No financing (reference only)"
			row.Cashflows = nil

		case types.CampaignSubdown:
			// Subdown modeling: use subsidy to increase down payment (reduce financed amount).
			// No T0 subsidy cashflow is created; commission + IDC Other are modeled as T0 IDC outflows.
			usedSubsidyTHB := subsidyBudgetTHB
			financedBase := deal.PriceExTax.Sub(deal.DownPaymentAmount)
			// Clamp to ensure financed amount stays positive (engine requires > 0)
			if financedBase.LessThanOrEqual(types.NewDecimal(1)) {
				usedSubsidyTHB = 0
			} else {
				maxUse := financedBase.Sub(types.NewDecimal(1)).InexactFloat64()
				if usedSubsidyTHB > maxUse {
					usedSubsidyTHB = maxUse
				}
				if usedSubsidyTHB < 0 {
					usedSubsidyTHB = 0
				}
			}

			// Populate Subdown field
			row.SubdownTHBStr = "THB " + FormatTHB(usedSubsidyTHB)

			// Build adjusted deal with higher DP
			deal2 := deal
			deal2.DownPaymentAmount = types.RoundTHB(deal.DownPaymentAmount.Add(types.NewDecimal(usedSubsidyTHB)))
			if deal2.PriceExTax.GreaterThan(types.NewDecimal(0)) {
				deal2.DownPaymentPercent = deal2.DownPaymentAmount.Div(deal2.PriceExTax)
			}
			deal2.DownPaymentLocked = "amount"
			deal2.FinancedAmount = types.RoundTHB(deal2.PriceExTax.Sub(deal2.DownPaymentAmount))

			// IDC items: commission + IDC Other
			idcItems := []types.IDCItem{}
			if row.DealerCommAmt > 0 {
				idcItems = append(idcItems, types.IDCItem{
					Category:    types.IDCBrokerCommission,
					Amount:      types.NewDecimal(row.DealerCommAmt),
					Payer:       "Dealer",
					Financed:    false,
					Withheld:    false,
					Timing:      types.IDCTimingUpfront,
					TaxFlags:    nil,
					IsRevenue:   false,
					IsCost:      true,
					Description: "Dealer commission",
				})
			}
			if otherIDC > 0 {
				idcItems = append(idcItems, types.IDCItem{
					Category:    types.IDCAdminFee,
					Amount:      types.NewDecimal(otherIDC),
					Payer:       "Dealer/Provider",
					Financed:    false,
					Withheld:    false,
					Timing:      types.IDCTimingUpfront,
					TaxFlags:    nil,
					IsRevenue:   false,
					IsCost:      true,
					Description: "IDC - Other",
				})
			}

			if q, ok := baselineQuote(idcItems, deal2); ok {
				mi := q.MonthlyInstallment.InexactFloat64()
				row.MonthlyInstallment = mi
				row.MonthlyInstallmentStr = FormatTHB(mi)

				row.NominalRate = q.CustomerRateNominal.InexactFloat64()
				row.EffectiveRate = q.CustomerRateEffective.InexactFloat64()
				row.AcqRoRac = q.Profitability.AcquisitionRoRAC.InexactFloat64()

				row.NominalRateStr = FormatRatePct(row.NominalRate)
				row.EffectiveRateStr = FormatRatePct(row.EffectiveRate)
				row.AcqRoRacStr = FormatRatePct(row.AcqRoRac)

				// Downpayment column shows "THB X (Y% DP)" consistently
				row.DownpaymentStr = dpString(deal2.DownPaymentAmount.InexactFloat64(), deal2.PriceExTax.InexactFloat64())

				row.IDCDealerTHB = row.DealerCommAmt
				row.IDCOtherTHB = 0 // subsidy is not modeled as IDC Other for subdown

				// Summary column “Subsidy / Acq.RoRAC”: display used subsidy and RoRAC
				row.SubsidyUsedTHBStr = FormatTHB(usedSubsidyTHB)
				row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(usedSubsidyTHB), row.AcqRoRac*100.0)

				row.Profit = snapshotFromQuote(q)
				row.Cashflows = q.Cashflows

				// Cashflow tab: append DP using adjusted DP
				dpForCF = deal2.DownPaymentAmount
			} else {
				// Fallback when quote fails
				row.MonthlyInstallmentStr = ""
				row.DownpaymentStr = dpString(deal2.DownPaymentAmount.InexactFloat64(), deal2.PriceExTax.InexactFloat64())
				row.SubsidyUsedTHBStr = ""
				row.SubsidyRorac = fmt.Sprintf("THB %s / -", FormatTHB(usedSubsidyTHB))
				dpForCF = deal2.DownPaymentAmount
			}

		case types.CampaignFreeInsurance, types.CampaignFreeMBSP:
			// Treat subsidy as periodic income (not T0). Add placeholder IDC expense + Dealer Commission + IDC Other.
			placeholderTHB := 50000.0
			placeholderCat := types.IDCAdminFee
			if c.Type == types.CampaignFreeMBSP {
				placeholderTHB = 150000.0
				placeholderCat = types.IDCMaintenanceFee
			}

			idcItems := []types.IDCItem{
				{
					Category:    placeholderCat,
					Amount:      types.NewDecimal(placeholderTHB),
					Payer:       "Dealer/Provider",
					Financed:    false,
					Withheld:    false,
					Timing:      types.IDCTimingUpfront,
					TaxFlags:    nil,
					IsRevenue:   false,
					IsCost:      true,
					Description: fmt.Sprintf("%s placeholder IDC", campaignTypeDisplayName(c.Type)),
				},
			}
			// Inject dealer commission as upfront cost (applies to finance options)
			if row.DealerCommAmt > 0 {
				idcItems = append(idcItems, types.IDCItem{
					Category:    types.IDCBrokerCommission,
					Amount:      types.NewDecimal(row.DealerCommAmt),
					Payer:       "Dealer",
					Financed:    false,
					Withheld:    false,
					Timing:      types.IDCTimingUpfront,
					TaxFlags:    nil,
					IsRevenue:   false,
					IsCost:      true,
					Description: "Dealer commission",
				})
			}
			if otherIDC > 0 {
				idcItems = append(idcItems, types.IDCItem{
					Category:    types.IDCAdminFee,
					Amount:      types.NewDecimal(otherIDC),
					Payer:       "Dealer/Provider",
					Financed:    false,
					Withheld:    false,
					Timing:      types.IDCTimingUpfront,
					TaxFlags:    nil,
					IsRevenue:   false,
					IsCost:      true,
					Description: "IDC - Other",
				})
			}
			req := types.CalculationRequest{
				Deal:         deal,
				Campaigns:    []types.Campaign{}, // terms unchanged
				IDCItems:     idcItems,
				ParameterSet: ps,
				Options:      map[string]interface{}{"derive_idc_from_cf": true, "add_subsidy_periodic_thb": subsidyBudgetTHB},
			}
			res, err := calc.Calculate(req)
			if err == nil && res != nil && res.Success {
				q := res.Quote
				mi := q.MonthlyInstallment.InexactFloat64()
				row.MonthlyInstallment = mi
				row.MonthlyInstallmentStr = FormatTHB(mi)

				row.NominalRate = q.CustomerRateNominal.InexactFloat64()
				row.EffectiveRate = q.CustomerRateEffective.InexactFloat64()
				row.AcqRoRac = q.Profitability.AcquisitionRoRAC.InexactFloat64()

				row.NominalRateStr = FormatRatePct(row.NominalRate)
				row.EffectiveRateStr = FormatRatePct(row.EffectiveRate)
				row.AcqRoRacStr = FormatRatePct(row.AcqRoRac)

				row.IDCDealerTHB = row.DealerCommAmt
				row.IDCOtherTHB = subsidyBudgetTHB
				// Free MBSP benefit display: show MBSP cost if available; fallback to budget
				if c.Type == types.CampaignFreeMBSP {
					if c.MBSPCost.GreaterThan(types.NewDecimal(0)) {
						row.MBSPTHBStr = "THB " + FormatTHB(c.MBSPCost.InexactFloat64())
					} else if subsidyBudgetTHB > 0 {
						row.MBSPTHBStr = "THB " + FormatTHB(subsidyBudgetTHB)
					}
				}

				row.SubsidyUsedTHBStr = FormatTHB(subsidyBudgetTHB)
				row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(subsidyBudgetTHB), row.AcqRoRac*100.0)
				row.Profit = snapshotFromQuote(q)
				row.Cashflows = res.Quote.Cashflows
			} else {
				// Fallback baseline without periodic subsidy effect
				if q, ok := baselineQuote(idcItems, deal); ok {
					mi := q.MonthlyInstallment.InexactFloat64()
					row.MonthlyInstallment = mi
					row.MonthlyInstallmentStr = FormatTHB(mi)

					row.NominalRate = q.CustomerRateNominal.InexactFloat64()
					row.EffectiveRate = q.CustomerRateEffective.InexactFloat64()
					row.AcqRoRac = q.Profitability.AcquisitionRoRAC.InexactFloat64()

					row.NominalRateStr = FormatRatePct(row.NominalRate)
					row.EffectiveRateStr = FormatRatePct(row.EffectiveRate)
					row.AcqRoRacStr = FormatRatePct(row.AcqRoRac)

					row.IDCDealerTHB = row.DealerCommAmt
					row.IDCOtherTHB = subsidyBudgetTHB
					// Free MBSP benefit display: show MBSP cost if available; fallback to budget
					if c.Type == types.CampaignFreeMBSP {
						if c.MBSPCost.GreaterThan(types.NewDecimal(0)) {
							row.MBSPTHBStr = "THB " + FormatTHB(c.MBSPCost.InexactFloat64())
						} else if subsidyBudgetTHB > 0 {
							row.MBSPTHBStr = "THB " + FormatTHB(subsidyBudgetTHB)
						}
					}

					row.SubsidyUsedTHBStr = FormatTHB(subsidyBudgetTHB)
					row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(subsidyBudgetTHB), row.AcqRoRac*100.0)
					row.Profit = snapshotFromQuote(q)
					row.Cashflows = q.Cashflows
				} else {
					row.MonthlyInstallmentStr = ""
					row.SubsidyUsedTHBStr = ""
					row.SubsidyRorac = fmt.Sprintf("THB %s / -", FormatTHB(subsidyBudgetTHB))
				}
			}

		default:
			// Unknown types fall back to baseline with IDC injection (commission as upfront outflow)
			idcs := []types.IDCItem{}
			if row.DealerCommAmt > 0 {
				idcs = append(idcs, types.IDCItem{
					Category:    types.IDCBrokerCommission,
					Amount:      types.NewDecimal(row.DealerCommAmt),
					Payer:       "Dealer",
					Financed:    false,
					Withheld:    false,
					Timing:      types.IDCTimingUpfront,
					TaxFlags:    nil,
					IsRevenue:   false,
					IsCost:      true,
					Description: "Dealer commission",
				})
			}
			if q, ok := baselineQuote(idcs, deal); ok {
				mi := q.MonthlyInstallment.InexactFloat64()
				row.MonthlyInstallment = mi
				row.MonthlyInstallmentStr = FormatTHB(mi)

				row.NominalRate = q.CustomerRateNominal.InexactFloat64()
				row.EffectiveRate = q.CustomerRateEffective.InexactFloat64()
				row.AcqRoRac = q.Profitability.AcquisitionRoRAC.InexactFloat64()

				row.NominalRateStr = FormatRatePct(row.NominalRate)
				row.EffectiveRateStr = FormatRatePct(row.EffectiveRate)
				row.AcqRoRacStr = FormatRatePct(row.AcqRoRac)

				row.IDCDealerTHB = row.DealerCommAmt
				row.IDCOtherTHB = 0

				row.SubsidyUsedTHBStr = FormatTHB(0)
				row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(0), row.AcqRoRac*100.0)
				row.Profit = snapshotFromQuote(q)
				row.Cashflows = q.Cashflows
			} else {
				row.MonthlyInstallmentStr = ""
				row.SubsidyUsedTHBStr = ""
				row.SubsidyRorac = "- / -"
			}
		}

		// Append synthetic downpayment inflow at T0 for Cashflow tab (UI-only)
		if dpForCF.GreaterThan(types.NewDecimal(0)) {
			dpFlow := types.Cashflow{
				Date:      deal.PayoutDate,
				Direction: "in",
				Type:      types.CashflowDownPayment,
				Amount:    dpForCF,
				Memo:      "Customer downpayment",
			}
			if len(row.Cashflows) > 0 {
				row.Cashflows = append([]types.Cashflow{dpFlow}, row.Cashflows...)
			} else {
				row.Cashflows = []types.Cashflow{dpFlow}
			}
		}

		rows = append(rows, row)
	}

	return rows, selectedIdx
}

// SelectedCampaignRow returns the CampaignRow at idx from the current model, with bounds checks.
// If idx is out of range but rows exist, it falls back to the first row.
func SelectedCampaignRow(m *CampaignTableModel, idx int) (CampaignRow, bool) {
	if m == nil || len(m.rows) == 0 {
		return CampaignRow{}, false
	}
	if idx < 0 || idx >= len(m.rows) {
		return m.rows[0], true
	}
	return m.rows[idx], true
}

// computeMyCampaignRow computes full metrics for a single My Campaign draft.
// This mirrors computeCampaignRows logic but works with campaign-specific adjustments.
func computeMyCampaignRow(
	ps types.ParameterSet,
	calc *calculator.Calculator,
	campEng *campaigns.Engine,
	draft CampaignDraft,
	state types.DealState,
) MyCampaignRow {
	row := MyCampaignRow{
		ID:   draft.ID,
		Name: draft.Name,
	}

	// Build deal from draft inputs
	deal := types.Deal{
		Product:             types.Product(draft.Product),
		PriceExTax:          types.NewDecimal(draft.Inputs.PriceExTaxTHB),
		DownPaymentPercent:  types.NewDecimal(draft.Inputs.DownpaymentPercent / 100.0),
		DownPaymentAmount:   types.NewDecimal(draft.Inputs.DownpaymentTHB),
		DownPaymentLocked:   "percent",
		TermMonths:          draft.Inputs.TermMonths,
		BalloonPercent:      types.NewDecimal(draft.Inputs.BalloonPercent / 100.0),
		Timing:              types.TimingArrears,
		RateMode:            draft.Inputs.RateMode,
		CustomerNominalRate: types.NewDecimal(draft.Inputs.CustomerRateAPR / 100.0),
		TargetInstallment:   types.NewDecimal(draft.Inputs.TargetInstallmentTHB),
	}

	// Set payout date to today
	deal.PayoutDate = time.Date(2024, 1, 1, 0, 0, 0, 0, time.UTC)

	// Normalize deal
	deal = normalizeDealForCampaign(deal)

	// Compute dealer commission
	financedBase := deal.PriceExTax.Sub(deal.DownPaymentAmount)
	if financedBase.LessThan(types.NewDecimal(0)) {
		financedBase = types.NewDecimal(0)
	}

	dealerPct := 0.025 // Default 2.5%
	if state.DealerCommission.Mode == types.DealerCommissionModeOverride {
		if state.DealerCommission.Pct != nil {
			dealerPct = *state.DealerCommission.Pct
		}
	}
	dealerAmt := financedBase.Mul(types.NewDecimal(dealerPct)).InexactFloat64()
	row.DealerCommAmt = dealerAmt
	row.DealerCommPct = dealerPct
	row.DealerComm = FormatDealerCommission(dealerAmt, dealerPct)

	// Determine campaign type from adjustments
	adj := draft.Adjustments
	otherIDC := state.IDCOther.Value

	// Helper: format downpayment as "THB X (Y% DP)"
	dpString := func(dpAmtF, priceF float64) string {
		pct := 0.0
		if priceF > 0 {
			pct = (dpAmtF / priceF) * 100.0
		}
		return fmt.Sprintf("THB %s (%.0f%% DP)", FormatTHB(dpAmtF), pct)
	}

	row.DownpaymentStr = dpString(deal.DownPaymentAmount.InexactFloat64(), deal.PriceExTax.InexactFloat64())

	// Helper: compute quote with IDC items
	baselineQuote := func(idcs []types.IDCItem, d types.Deal) (types.Quote, bool) {
		if calc == nil {
			return types.Quote{}, false
		}
		req := types.CalculationRequest{
			Deal:         d,
			Campaigns:    []types.Campaign{},
			IDCItems:     idcs,
			ParameterSet: ps,
			Options:      map[string]interface{}{"derive_idc_from_cf": true},
		}
		res, err := calc.Calculate(req)
		if err != nil || res == nil || !res.Success {
			return types.Quote{}, false
		}
		return res.Quote, true
	}

	// Helper: snapshot from quote
	snapshotFromQuote := func(q types.Quote) ProfitabilitySnapshot {
		p := q.Profitability
		return ProfitabilitySnapshot{
			DealIRREffective:    p.DealIRREffective.InexactFloat64(),
			DealIRRNominal:      p.DealIRRNominal.InexactFloat64(),
			IDCUpfrontCostPct:   p.IDCUpfrontCostPct.InexactFloat64(),
			SubsidyUpfrontPct:   p.SubsidyUpfrontPct.InexactFloat64(),
			CostOfDebt:          p.CostOfDebtMatched.InexactFloat64(),
			MatchedFundedSpread: p.MatchedFundedSpread.InexactFloat64(),
			GrossInterestMargin: p.GrossInterestMargin.InexactFloat64(),
			CapitalAdvantage:    p.CapitalAdvantage.InexactFloat64(),
			NetInterestMargin:   p.NetInterestMargin.InexactFloat64(),
			CostOfCreditRisk:    p.CostOfCreditRisk.InexactFloat64(),
			OPEX:                p.OPEX.InexactFloat64(),
			IDCPeriodicPct:      p.IDCSubsidiesFeesPeriodic.InexactFloat64(),
			SubsidyPeriodicPct:  p.SubsidyPeriodicPct.InexactFloat64(),
			NetEBITMargin:       p.NetEBITMargin.InexactFloat64(),
			EconomicCapital:     p.EconomicCapital.InexactFloat64(),
			AcquisitionRoRAC:    p.AcquisitionRoRAC.InexactFloat64(),
		}
	}

	// Calculate total subsidy from all components
	totalSubsidyRequested := adj.IDCFreeInsuranceTHB + adj.IDCFreeMBSPTHB + adj.SubdownTHB
	
	// Cash Discount is treated separately (non-financing reference)
	if adj.CashDiscountTHB > 0 {
		// Cash Discount campaign (non-financing reference)
		row.CashDiscountStr = "THB " + FormatTHB(adj.CashDiscountTHB)
		effectiveCash := deal.PriceExTax.Sub(types.NewDecimal(adj.CashDiscountTHB)).InexactFloat64()
		if effectiveCash < 0 {
			effectiveCash = 0
		}
		row.DownpaymentStr = "THB " + FormatTHB(effectiveCash)
		row.MonthlyInstallmentStr = ""
		row.AcqRoRacStr = ""
		row.SubsidyRorac = "—"
		row.DealerComm = "THB 0 (0%)"
		row.Notes = "No financing (reference only)"
		row.Cashflows = nil
		return row
	}
	
	// For financing campaigns, apply all components simultaneously
	// Note: Since we don't have a subsidy budget field in state for My Campaigns,
	// we use the total requested as the available budget (can be extended later)
	subsidyBudgetAvailable := totalSubsidyRequested
	
	// Cap total subsidy at budget available
	totalSubsidyUsed := totalSubsidyRequested
	if totalSubsidyUsed > subsidyBudgetAvailable {
		totalSubsidyUsed = subsidyBudgetAvailable
	}
	
	// Calculate scaling factor if we need to reduce subsidies
	scalingFactor := 1.0
	if totalSubsidyRequested > 0 && totalSubsidyUsed < totalSubsidyRequested {
		scalingFactor = totalSubsidyUsed / totalSubsidyRequested
	}
	
	// Apply scaling to each component
	usedSubdownTHB := adj.SubdownTHB * scalingFactor
	usedFreeInsuranceTHB := adj.IDCFreeInsuranceTHB * scalingFactor
	usedFreeMBSPTHB := adj.IDCFreeMBSPTHB * scalingFactor
	
	// Start with original deal, will be modified if Subdown is active
	deal2 := deal
	
	// Clamp Subdown to ensure financed amount stays positive
	if usedSubdownTHB > 0 {
		financedBase := deal.PriceExTax.Sub(deal.DownPaymentAmount)
		if financedBase.LessThanOrEqual(types.NewDecimal(1)) {
			usedSubdownTHB = 0
		} else {
			maxUse := financedBase.Sub(types.NewDecimal(1)).InexactFloat64()
			if usedSubdownTHB > maxUse {
				usedSubdownTHB = maxUse
			}
		}
		
		// Populate Subdown field
		row.SubdownTHBStr = "THB " + FormatTHB(usedSubdownTHB)
		
		// Adjust deal with increased downpayment
		deal2.DownPaymentAmount = types.RoundTHB(deal.DownPaymentAmount.Add(types.NewDecimal(usedSubdownTHB)))
		if deal2.PriceExTax.GreaterThan(types.NewDecimal(0)) {
			deal2.DownPaymentPercent = deal2.DownPaymentAmount.Div(deal2.PriceExTax)
		}
		deal2.DownPaymentLocked = "amount"
		deal2.FinancedAmount = types.RoundTHB(deal2.PriceExTax.Sub(deal2.DownPaymentAmount))
		
		// Update downpayment display
		row.DownpaymentStr = dpString(deal2.DownPaymentAmount.InexactFloat64(), deal2.PriceExTax.InexactFloat64())
	}
	
	// Build IDC items: dealer commission + other IDC + placeholder costs for Free Insurance/MBSP
	idcItems := []types.IDCItem{}
	
	// Dealer commission (always included for financed deals)
	if dealerAmt > 0 {
		idcItems = append(idcItems, types.IDCItem{
			Category:    types.IDCBrokerCommission,
			Amount:      types.NewDecimal(dealerAmt),
			Payer:       "Dealer",
			Financed:    false,
			Timing:      types.IDCTimingUpfront,
			IsRevenue:   false,
			IsCost:      true,
			Description: "Dealer commission",
		})
	}
	
	// Other IDC from state
	if otherIDC > 0 {
		idcItems = append(idcItems, types.IDCItem{
			Category:    types.IDCAdminFee,
			Amount:      types.NewDecimal(otherIDC),
			Payer:       "Dealer/Provider",
			Financed:    false,
			Timing:      types.IDCTimingUpfront,
			IsRevenue:   false,
			IsCost:      true,
			Description: "IDC - Other",
		})
	}
	
	// Add placeholder costs for Free Insurance and Free MBSP (upfront costs)
	if usedFreeInsuranceTHB > 0 {
		idcItems = append(idcItems, types.IDCItem{
			Category:    types.IDCAdminFee,
			Amount:      types.NewDecimal(50000.0), // Placeholder insurance cost
			Payer:       "Dealer/Provider",
			Financed:    false,
			Timing:      types.IDCTimingUpfront,
			IsRevenue:   false,
			IsCost:      true,
			Description: "Placeholder Insurance IDC",
		})
	}
	
	if usedFreeMBSPTHB > 0 {
		idcItems = append(idcItems, types.IDCItem{
			Category:    types.IDCMaintenanceFee,
			Amount:      types.NewDecimal(150000.0), // Placeholder MBSP cost
			Payer:       "Dealer/Provider",
			Financed:    false,
			Timing:      types.IDCTimingUpfront,
			IsRevenue:   false,
			IsCost:      true,
			Description: "Placeholder MBSP IDC",
		})
		row.MBSPTHBStr = "THB " + FormatTHB(usedFreeMBSPTHB)
	}
	
	// Calculate total periodic subsidy (Free Insurance + Free MBSP)
	totalPeriodicSubsidy := usedFreeInsuranceTHB + usedFreeMBSPTHB
	
	// Run calculation with all components
	req := types.CalculationRequest{
		Deal:         deal2,
		Campaigns:    []types.Campaign{},
		IDCItems:     idcItems,
		ParameterSet: ps,
		Options:      map[string]interface{}{"derive_idc_from_cf": true, "add_subsidy_periodic_thb": totalPeriodicSubsidy},
	}
	
	if res, err := calc.Calculate(req); err == nil && res != nil && res.Success {
		q := res.Quote
		mi := q.MonthlyInstallment.InexactFloat64()
		row.MonthlyInstallment = mi
		row.MonthlyInstallmentStr = FormatTHB(mi)
		row.NominalRate = q.CustomerRateNominal.InexactFloat64()
		row.EffectiveRate = q.CustomerRateEffective.InexactFloat64()
		row.AcqRoRac = q.Profitability.AcquisitionRoRAC.InexactFloat64()
		row.AcqRoRacStr = FormatRatePct(row.AcqRoRac)
		row.IDCDealerTHB = dealerAmt
		row.IDCOtherTHB = totalPeriodicSubsidy
		row.SubsidyValue = totalSubsidyUsed
		row.SubsidyUsedTHBStr = FormatTHB(totalSubsidyUsed)
		row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(totalSubsidyUsed), row.AcqRoRac*100.0)
		row.Profit = snapshotFromQuote(q)
		row.Cashflows = q.Cashflows
	} else {
		// Fallback: baseline quote without subsidies
		if q, ok := baselineQuote(idcItems, deal2); ok {
			mi := q.MonthlyInstallment.InexactFloat64()
			row.MonthlyInstallment = mi
			row.MonthlyInstallmentStr = FormatTHB(mi)
			row.NominalRate = q.CustomerRateNominal.InexactFloat64()
			row.EffectiveRate = q.CustomerRateEffective.InexactFloat64()
			row.AcqRoRac = q.Profitability.AcquisitionRoRAC.InexactFloat64()
			row.AcqRoRacStr = FormatRatePct(row.AcqRoRac)
			row.IDCDealerTHB = dealerAmt
			row.IDCOtherTHB = otherIDC
			row.SubsidyValue = totalSubsidyUsed
			row.SubsidyUsedTHBStr = FormatTHB(totalSubsidyUsed)
			row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(totalSubsidyUsed), row.AcqRoRac*100.0)
			row.Profit = snapshotFromQuote(q)
			row.Cashflows = q.Cashflows
		}
	}

	// Append downpayment cashflow for consistency with campaign rows
	if deal.DownPaymentAmount.GreaterThan(types.NewDecimal(0)) {
		dpFlow := types.Cashflow{
			Date:      deal.PayoutDate,
			Direction: "in",
			Type:      types.CashflowDownPayment,
			Amount:    deal.DownPaymentAmount,
			Memo:      "Customer downpayment",
		}
		if len(row.Cashflows) > 0 {
			row.Cashflows = append([]types.Cashflow{dpFlow}, row.Cashflows...)
		} else {
			row.Cashflows = []types.Cashflow{dpFlow}
		}
	}

	return row
}
