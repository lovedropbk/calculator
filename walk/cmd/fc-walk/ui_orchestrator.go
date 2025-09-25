//go:build windows

package main

import (
	"fmt"

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
// - Subdown/Free Insurance/Free MBSP: baseline pricing + treat subsidyBudgetTHB as IDC Other T0 inflow (revenue)
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

	// Helper: baseline quote (no campaigns, no IDC)
	baselineQuote := func() (types.Quote, bool) {
		if calc == nil {
			return types.Quote{}, false
		}
		req := types.CalculationRequest{
			Deal:         deal,
			Campaigns:    []types.Campaign{},
			IDCItems:     []types.IDCItem{},
			ParameterSet: ps,
		}
		res, err := calc.Calculate(req)
		if err != nil || res == nil || !res.Success {
			return types.Quote{}, false
		}
		return res.Quote, true
	}

	// Default selected index
	if selectedIdx < 0 || selectedIdx >= len(displayCampaigns) {
		selectedIdx = 0
	}

	rows := make([]CampaignRow, 0, len(displayCampaigns))

	for i, c := range displayCampaigns {
		row := CampaignRow{
			Selected:       i == selectedIdx,
			Name:           campaignTypeDisplayName(c.Type),
			DownpaymentStr: fmt.Sprintf("%.0f%%", dpPercent),
			Notes:          "",
			SubsidyValue:   subsidyBudgetTHB,
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

		switch c.Type {
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

				row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(row.IDCOtherTHB), row.AcqRoRac*100.0)
			} else {
				// Fallback to baseline if solver fails
				if q, ok := baselineQuote(); ok {
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

					row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(0), row.AcqRoRac*100.0)
				} else {
					row.MonthlyInstallmentStr = ""
					row.SubsidyRorac = fmt.Sprintf("THB %s / -", FormatTHB(subsidyBudgetTHB))
				}
			}

		case types.CampaignCashDiscount:
			// Metrics same as baseline; commission already forced to 0 via summaries
			if q, ok := baselineQuote(); ok {
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

				row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(0), row.AcqRoRac*100.0)
			} else {
				row.MonthlyInstallmentStr = ""
				row.SubsidyRorac = fmt.Sprintf("THB %s / -", FormatTHB(0))
			}

		case types.CampaignSubdown, types.CampaignFreeInsurance, types.CampaignFreeMBSP:
			// Treat budget as IDC Other T0 inflow (revenue) to yield real RoRAC; pricing terms unchanged
			idcItems := []types.IDCItem{
				{
					Category:    types.IDCAdminFee,
					Amount:      types.NewDecimal(subsidyBudgetTHB),
					Payer:       "Campaign Funder",
					Financed:    false,
					Withheld:    false,
					Timing:      types.IDCTimingUpfront,
					TaxFlags:    nil,
					IsRevenue:   true,
					IsCost:      false,
					Description: fmt.Sprintf("%s subsidy", campaignTypeDisplayName(c.Type)),
				},
			}
			req := types.CalculationRequest{
				Deal:         deal,
				Campaigns:    []types.Campaign{}, // terms unchanged for MVP
				IDCItems:     idcItems,
				ParameterSet: ps,
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

				row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(subsidyBudgetTHB), row.AcqRoRac*100.0)
			} else {
				// Fallback baseline
				if q, ok := baselineQuote(); ok {
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

					row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(subsidyBudgetTHB), row.AcqRoRac*100.0)
				} else {
					row.MonthlyInstallmentStr = ""
					row.SubsidyRorac = fmt.Sprintf("THB %s / -", FormatTHB(subsidyBudgetTHB))
				}
			}

		default:
			// Unknown types fall back to baseline
			if q, ok := baselineQuote(); ok {
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

				row.SubsidyRorac = fmt.Sprintf("THB %s / %.2f%%", FormatTHB(0), row.AcqRoRac*100.0)
			} else {
				row.MonthlyInstallmentStr = ""
				row.SubsidyRorac = "- / -"
			}
		}

		rows = append(rows, row)
	}

	return rows, selectedIdx
}
