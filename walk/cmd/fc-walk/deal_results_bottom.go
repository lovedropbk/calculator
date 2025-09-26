//go:build windows

package main

import (
	"fmt"
	"strconv"
	"strings"

	"github.com/lxn/walk"
)

// MARK: Build – Bottom Results

// ViewModel is a placeholder to satisfy the requested API. The current UI uses
// declarative construction in main.go; this type enables future extraction.
type ViewModel struct{}

// uiRefs holds pointers to UI controls for future imperative updates.
// Not used yet by main.go, but provided to satisfy the requested API.
type uiRefs struct{}

// BuildBottomResults currently delegates to declarative construction in main.go.
// This stub exists to satisfy the requested API and keep future extraction simple.
func BuildBottomResults(parent *walk.Composite, vm *ViewModel, refs *uiRefs) error {
	return nil
}

// UpdateCampaignDetails refreshes the left "Campaign Details" group values.
// Sources:
// - Campaign Name: from selected row
// - Term (months): current term input
// - Financed Amount: price - down payment (amount or percent)
// - Subsidy budget (THB): from NumberEdit subsidyBudgetEd
// - Subsidy utilized (THB): parsed from row.SubsidyRorac ("THB X / ...")
// - Subsidy remaining (THB): max(0, budget - utilized)
// - Dealer Commissions Paid (THB): row.IDCDealerTHB
// - IDCs - Others (THB): from NumberEdit idcOtherEd
// - IDC - Free Insurance / MBSP: 0 or "—" if not exposed
func UpdateCampaignDetails(
	row CampaignRow,
	selCampNameValLbl, selTermValLbl, selFinancedValLbl, selSubsidyUsedValLbl, selSubsidyBudgetValLbl, selSubsidyRemainValLbl, selIDCDealerValLbl, selIDCInsValLbl, selIDCMBSPValLbl, selIDCOtherValLbl *walk.Label,
	priceEdit *walk.LineEdit, dpUnitCmb *walk.ComboBox, dpValueEd, dpAmountEd *walk.NumberEdit, termEdit *walk.LineEdit,
	subsidyBudgetEd, idcOtherEd *walk.NumberEdit,
) {
	// Campaign name
	if selCampNameValLbl != nil {
		selCampNameValLbl.SetText(row.Name)
	}
	// Term (months)
	term := parseInt(termEdit)
	if selTermValLbl != nil {
		selTermValLbl.SetText(fmt.Sprintf("%d", term))
	}

	// Financed Amount
	price := parseFloat(priceEdit)
	dpAmt := 0.0
	if dpUnitCmb != nil && dpUnitCmb.Text() == "THB" && dpAmountEd != nil {
		dpAmt = dpAmountEd.Value()
	} else if dpValueEd != nil {
		dpAmt = RoundTo(price*(dpValueEd.Value()/100.0), 0)
	}
	financed := price - dpAmt
	if financed < 0 {
		financed = 0
	}
	if selFinancedValLbl != nil {
		selFinancedValLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(financed)))
	}

	// Subsidy utilized (THB): parse from "THB X / ..."
	subsidyUsed := 0.0
	s := strings.TrimSpace(row.SubsidyRorac)
	if strings.HasPrefix(s, "THB ") {
		rest := strings.TrimPrefix(s, "THB ")
		if i := strings.Index(rest, "/"); i >= 0 {
			rest = strings.TrimSpace(rest[:i])
		}
		rest = strings.ReplaceAll(rest, ",", "")
		if v, err := strconv.ParseFloat(rest, 64); err == nil {
			subsidyUsed = v
		}
	}
	if selSubsidyUsedValLbl != nil {
		selSubsidyUsedValLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(subsidyUsed)))
	}

	// Subsidy budget (THB)
	budget := 0.0
	if subsidyBudgetEd != nil {
		budget = subsidyBudgetEd.Value()
	}
	if selSubsidyBudgetValLbl != nil {
		selSubsidyBudgetValLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(budget)))
	}

	// Subsidy remaining (THB) = max(0, budget - utilized)
	remaining := budget - subsidyUsed
	if remaining < 0 {
		remaining = 0
	}
	if selSubsidyRemainValLbl != nil {
		selSubsidyRemainValLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(remaining)))
	}

	// Dealer commissions paid (THB)
	if selIDCDealerValLbl != nil {
		selIDCDealerValLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(row.IDCDealerTHB)))
	}

	// IDCs - Others (THB)
	other := 0.0
	if idcOtherEd != nil {
		other = idcOtherEd.Value()
	}
	if selIDCOtherValLbl != nil {
		selIDCOtherValLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(other)))
	}

	// IDC - Free Insurance / MBSP (not exposed -> 0 or "—")
	if selIDCInsValLbl != nil {
		selIDCInsValLbl.SetText("THB 0")
	}
	if selIDCMBSPValLbl != nil {
		selIDCMBSPValLbl.SetText("THB 0")
	}
}

// UpdateKeyMetrics refreshes the right "Key Metrics Summary" group values.
// It also updates the "Profitability Details" labels and Financed Amount and IDC totals.
func UpdateKeyMetrics(
	row CampaignRow,
	monthlyLbl, headerMonthlyLbl *walk.Label,
	custNominalLbl, custEffLbl *walk.Label,
	roracLbl, headerRoRacLbl *walk.Label,
	idcTotalLbl, idcDealerLbl, idcOtherLbl *walk.Label,
	financedLbl *walk.Label,
	priceEdit *walk.LineEdit, dpUnitCmb *walk.ComboBox, dpValueEd, dpAmountEd *walk.NumberEdit,
	wfCustRateEffLbl, wfCustRateNomLbl *walk.Label,
	wfDealIRREffLbl, wfDealIRRNomLbl, wfIDCUpLbl, wfSubUpLbl, wfCostDebtLbl, wfMFSpreadLbl, wfGIMEffLbl, wfGIMLbl, wfCapAdvLbl, wfNIMEffLbl, wfNIMLbl, wfRiskLbl, wfOpexLbl, wfNetEbitEffLbl, wfNetEbitLbl, wfEconCapLbl, wfAcqRoRacDetailLbl *walk.Label,
	idcOtherEd *walk.NumberEdit,
) {
	// Monthly installment
	if monthlyLbl != nil {
		if row.MonthlyInstallmentStr != "" {
			monthlyLbl.SetText("THB " + row.MonthlyInstallmentStr)
		} else {
			monthlyLbl.SetText("—")
		}
	}
	if headerMonthlyLbl != nil {
		if row.MonthlyInstallmentStr != "" {
			headerMonthlyLbl.SetText("THB " + row.MonthlyInstallmentStr)
		} else {
			headerMonthlyLbl.SetText("—")
		}
	}

	// Customer rates
	if custNominalLbl != nil {
		if row.NominalRate > 0 {
			custNominalLbl.SetText(fmt.Sprintf("%.2f%%", row.NominalRate*100.0))
		} else {
			custNominalLbl.SetText("—")
		}
	}
	if custEffLbl != nil {
		if row.EffectiveRate > 0 {
			custEffLbl.SetText(fmt.Sprintf("%.2f%%", row.EffectiveRate*100.0))
		} else {
			custEffLbl.SetText("—")
		}
	}

	// Acquisition RoRAC
	if roracLbl != nil {
		if row.AcqRoRacStr != "" {
			roracLbl.SetText(row.AcqRoRacStr)
		} else if row.AcqRoRac != 0 {
			roracLbl.SetText(fmt.Sprintf("%.2f%%", row.AcqRoRac*100.0))
		} else {
			roracLbl.SetText("—")
		}
	}
	if headerRoRacLbl != nil {
		if row.AcqRoRacStr != "" {
			headerRoRacLbl.SetText(row.AcqRoRacStr)
		} else if row.AcqRoRac != 0 {
			headerRoRacLbl.SetText(fmt.Sprintf("%.2f%%", row.AcqRoRac*100.0))
		} else {
			headerRoRacLbl.SetText("—")
		}
	}

	// Profitability Details from per-row snapshot
	p := row.Profit
	if wfCustRateEffLbl != nil {
		if row.EffectiveRate != 0 {
			wfCustRateEffLbl.SetText(fmt.Sprintf("%.2f%%", row.EffectiveRate*100.0))
		} else {
			wfCustRateEffLbl.SetText("—")
		}
	}
	if wfCustRateNomLbl != nil {
		if row.NominalRate != 0 {
			wfCustRateNomLbl.SetText(fmt.Sprintf("%.2f%%", row.NominalRate*100.0))
		} else {
			wfCustRateNomLbl.SetText("—")
		}
	}
	if wfSubUpLbl != nil {
		if p.SubsidyUpfrontPct != 0 {
			wfSubUpLbl.SetText(fmt.Sprintf("%.2f%%", p.SubsidyUpfrontPct*100.0))
		} else {
			wfSubUpLbl.SetText("—")
		}
	}
	if wfIDCUpLbl != nil {
		if p.IDCUpfrontCostPct != 0 {
			wfIDCUpLbl.SetText(fmt.Sprintf("%.2f%%", p.IDCUpfrontCostPct*100.0))
		} else {
			wfIDCUpLbl.SetText("—")
		}
	}
	if wfDealIRREffLbl != nil {
		if p.DealIRREffective != 0 {
			wfDealIRREffLbl.SetText(fmt.Sprintf("%.2f%%", p.DealIRREffective*100.0))
		} else {
			wfDealIRREffLbl.SetText("—")
		}
	}
	if wfDealIRRNomLbl != nil {
		if p.DealIRRNominal != 0 {
			wfDealIRRNomLbl.SetText(fmt.Sprintf("%.2f%%", p.DealIRRNominal*100.0))
		} else {
			wfDealIRRNomLbl.SetText("—")
		}
	}
	if wfCostDebtLbl != nil {
		if p.CostOfDebt != 0 {
			wfCostDebtLbl.SetText(fmt.Sprintf("%.2f%%", p.CostOfDebt*100.0))
		} else {
			wfCostDebtLbl.SetText("—")
		}
	}
	if wfMFSpreadLbl != nil {
		if p.MatchedFundedSpread != 0 {
			wfMFSpreadLbl.SetText(fmt.Sprintf("%.2f%%", p.MatchedFundedSpread*100.0))
		} else {
			wfMFSpreadLbl.SetText("—")
		}
	}
	if wfGIMEffLbl != nil {
		if p.GrossInterestMargin != 0 {
			wfGIMEffLbl.SetText(fmt.Sprintf("%.2f%%", p.GrossInterestMargin*100.0))
		} else {
			wfGIMEffLbl.SetText("—")
		}
	}
	if wfGIMLbl != nil {
		if p.GrossInterestMargin != 0 {
			wfGIMLbl.SetText(fmt.Sprintf("%.2f%%", p.GrossInterestMargin*100.0))
		} else {
			wfGIMLbl.SetText("—")
		}
	}
	if wfCapAdvLbl != nil {
		if p.CapitalAdvantage != 0 {
			wfCapAdvLbl.SetText(fmt.Sprintf("%.2f%%", p.CapitalAdvantage*100.0))
		} else {
			wfCapAdvLbl.SetText("—")
		}
	}
	if wfNIMEffLbl != nil {
		if p.NetInterestMargin != 0 {
			wfNIMEffLbl.SetText(fmt.Sprintf("%.2f%%", p.NetInterestMargin*100.0))
		} else {
			wfNIMEffLbl.SetText("—")
		}
	}
	if wfNIMLbl != nil {
		if p.NetInterestMargin != 0 {
			wfNIMLbl.SetText(fmt.Sprintf("%.2f%%", p.NetInterestMargin*100.0))
		} else {
			wfNIMLbl.SetText("—")
		}
	}
	if wfRiskLbl != nil {
		if p.CostOfCreditRisk != 0 {
			wfRiskLbl.SetText(fmt.Sprintf("%.2f%%", p.CostOfCreditRisk*100.0))
		} else {
			wfRiskLbl.SetText("—")
		}
	}
	if wfOpexLbl != nil {
		if p.OPEX != 0 {
			wfOpexLbl.SetText(fmt.Sprintf("%.2f%%", p.OPEX*100.0))
		} else {
			wfOpexLbl.SetText("—")
		}
	}
	if wfNetEbitEffLbl != nil {
		if p.NetEBITMargin != 0 {
			wfNetEbitEffLbl.SetText(fmt.Sprintf("%.2f%%", p.NetEBITMargin*100.0))
		} else {
			wfNetEbitEffLbl.SetText("—")
		}
	}
	if wfNetEbitLbl != nil {
		if p.NetEBITMargin != 0 {
			wfNetEbitLbl.SetText(fmt.Sprintf("%.2f%%", p.NetEBITMargin*100.0))
		} else {
			wfNetEbitLbl.SetText("—")
		}
	}
	if wfEconCapLbl != nil {
		if p.EconomicCapital != 0 {
			wfEconCapLbl.SetText(fmt.Sprintf("%.2f%%", p.EconomicCapital*100.0))
		} else {
			wfEconCapLbl.SetText("—")
		}
	}
	if wfAcqRoRacDetailLbl != nil {
		if row.AcqRoRac != 0 {
			wfAcqRoRacDetailLbl.SetText(fmt.Sprintf("%.2f%%", row.AcqRoRac*100.0))
		} else {
			wfAcqRoRacDetailLbl.SetText("—")
		}
	}

	// Financed Amount (same logic as Campaign Details)
	if financedLbl != nil {
		price := parseFloat(priceEdit)
		dpAmt := 0.0
		if dpUnitCmb != nil && dpUnitCmb.Text() == "THB" && dpAmountEd != nil {
			dpAmt = dpAmountEd.Value()
		} else if dpValueEd != nil {
			dpAmt = RoundTo(price*(dpValueEd.Value()/100.0), 0)
		}
		financed := price - dpAmt
		if financed < 0 {
			financed = 0
		}
		financedLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(financed)))
	}

	// IDC metrics
	dealer := row.IDCDealerTHB
	other := 0.0
	if idcOtherEd != nil {
		other = idcOtherEd.Value()
	}
	if idcDealerLbl != nil {
		idcDealerLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(dealer)))
	}
	if idcOtherLbl != nil {
		idcOtherLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(other)))
	}
	if idcTotalLbl != nil {
		idcTotalLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(dealer+other)))
	}
}
