package calculator

import (
	"fmt"
	"testing"

	"github.com/shopspring/decimal"
)

// MARK: Tolerance and formatting helpers shared across HQ tests
func assertWithinTolerance(t *testing.T, name string, exp *hqExpected, act actualMetrics) {
	t.Helper()

	relTol := decimal.NewFromFloat(0.02)     // 2%
	absBpMin := decimal.NewFromFloat(0.0002) // 2 bps

	var rows []string
	var failed bool

	// helper for percent comparisons
	checkPct := func(label string, expectedPtr *decimal.Decimal, actual decimal.Decimal) {
		if expectedPtr == nil {
			return
		}
		expected := *expectedPtr
		absDiff := actual.Sub(expected).Abs()
		relAllowed := expected.Abs().Mul(relTol)
		allowed := relAllowed
		if allowed.LessThan(absBpMin) {
			allowed = absBpMin
		}
		ok := absDiff.LessThanOrEqual(allowed)
		if !ok {
			failed = true
		}
		rows = append(rows, fmt.Sprintf("%-26s expected=%s actual=%s absΔ=%s relΔ=%.4f%%",
			label,
			formatRate(expected), formatRate(actual),
			formatRate(absDiff),
			relDeltaPercent(expected, actual),
		))
	}

	// strict relative-only percent comparison (no absolute minimum) — for CoR
	checkPctStrictRelative := func(label string, expectedPtr *decimal.Decimal, actual decimal.Decimal) {
		if expectedPtr == nil {
			return
		}
		expected := *expectedPtr
		absDiff := actual.Sub(expected).Abs()
		allowed := expected.Abs().Mul(relTol)
		ok := absDiff.LessThanOrEqual(allowed)
		if !ok {
			failed = true
		}
		rows = append(rows, fmt.Sprintf("%-26s expected=%s actual=%s absΔ=%s relΔ=%.4f%%",
			label,
			formatRate(expected), formatRate(actual),
			formatRate(absDiff),
			relDeltaPercent(expected, actual),
		))
	}

	// helper for THB comparisons
	checkTHB := func(label string, expectedPtr *decimal.Decimal, actual decimal.Decimal) {
		if expectedPtr == nil {
			return
		}
		expected := *expectedPtr
		absDiff := actual.Sub(expected).Abs()
		absFloor := decimal.NewFromFloat(0.01)
		relAllowed := expected.Abs().Mul(relTol)
		allowed := relAllowed
		if allowed.LessThan(absFloor) {
			allowed = absFloor
		}
		ok := absDiff.LessThanOrEqual(allowed)
		if !ok {
			failed = true
		}
		rows = append(rows, fmt.Sprintf("%-26s expected=THB %s actual=THB %s absΔ=THB %s relΔ=%.4f%%",
			label,
			expected.StringFixed(2), actual.StringFixed(2),
			absDiff.StringFixed(2),
			relDeltaPercent(expected, actual),
		))
	}

	// THB
	checkTHB("Customer Installment", exp.InstallmentTHB, act.InstallmentTHB)

	// Percent fields (fractions)
	checkPct("Customer Nominal", exp.CustomerNominal, act.CustomerNominal)
	checkPct("Customer Effective", exp.CustomerEffective, act.CustomerEffective)
	checkPct("Deal IRR Nominal", exp.DealIRRNominal, act.DealIRRNominal)
	checkPct("Deal IRR Effective", exp.DealIRREffective, act.DealIRREffective)
	checkPct("Cost of Debt", exp.CostOfDebt, act.CostOfDebt)
	checkPct("Matched Funded Spread", exp.MatchedFundedSpread, act.MatchedFundedSpread)
	checkPct("Capital Advantage", exp.CapitalAdvantage, act.CapitalAdvantage)
	checkPctStrictRelative("Cost of Credit Risk", exp.CreditRisk, act.CreditRisk)
	checkPct("OPEX", exp.OPEX, act.OPEX)
	checkPct("IDC Upfront Percent", exp.IDCUpfrontRate, act.IDCUpfrontRate)
	checkPct("IDC Periodic Percent", exp.IDCPeriodicRate, act.IDCPeriodicRate)
	checkPct("Net Interest Margin", exp.NetInterestMargin, act.NetInterestMargin)
	checkPct("Net EBIT Margin", exp.NetEBITMargin, act.NetEBITMargin)
	checkPct("Economic Capital", exp.EconomicCapital, act.EconomicCapital)
	checkPct("Acquisition RoRAC", exp.AcquisitionRoRAC, act.AcquisitionRoRAC)

	// Effective Maturity (years)
	if exp.EffectiveMaturityYear != nil {
		expYears := *exp.EffectiveMaturityYear
		diff := act.EffectiveMaturityYear - expYears
		if diff < -0.01 || diff > 0.01 {
			failed = true
		}
		rows = append(rows, fmt.Sprintf("%-26s expected=%.2f actual=%.2f absΔ=%.4f",
			"Effective Maturity (y)", expYears, act.EffectiveMaturityYear, absFloat(diff)))
	}

	if failed {
		msg := "HQ Golden tolerance check FAILED\n" +
			fmt.Sprintf("  Case: %s | ParamSet: %s\n", name, act.ParameterSetVersion) +
			"  Diagnostics:\n" +
			"  - Check nominal vs effective basis alignment and rounding policy (Thai THB)\n" +
			"  - Verify IDC modeling (upfront vs financed) and sign conventions\n" +
			"  - Confirm Cost of Funds curve and OPEX product mapping\n" +
			"  Table (asserted fields):\n    - " + joinRows(rows, "\n    - ")
		t.Errorf("%s", msg)
	}
}

func formatRate(d decimal.Decimal) string {
	return d.Mul(decimal.NewFromInt(100)).StringFixed(4) + "%"
}
func relDeltaPercent(expected, actual decimal.Decimal) float64 {
	if expected.IsZero() {
		if actual.IsZero() {
			return 0
		}
		return 100.0
	}
	rel := actual.Sub(expected).Div(expected).Mul(decimal.NewFromInt(100))
	return rel.InexactFloat64()
}
func absFloat(v float64) float64 {
	if v < 0 {
		return -v
	}
	return v
}
func joinRows(rows []string, sep string) string {
	out := ""
	for i, r := range rows {
		if i > 0 {
			out += sep
		}
		out += r
	}
	return out
}
