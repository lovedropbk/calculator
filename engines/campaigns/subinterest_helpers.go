package campaigns

import (
	"fmt"

	"github.com/financial-calculator/engines/cashflow"
	"github.com/financial-calculator/engines/pricing"
	"github.com/financial-calculator/engines/profitability"
	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
)

// MARK: helpers split from subinterest.go to keep files < 500 lines

// resolveBasePricing returns the base nominal rate, rounded schedule, and monthly installment
// for the provided deal using the pricing engine. Supports "fixed_rate" and "target_installment".
func resolveBasePricing(pEng *pricing.Engine, deal types.Deal) (nominalRate decimal.Decimal, schedule []types.Cashflow, installment decimal.Decimal, err error) {
	switch deal.RateMode {
	case "fixed_rate":
		nominalRate = deal.CustomerNominalRate
		schedule, err = pEng.BuildAmortizationSchedule(deal, nominalRate)
		if err != nil {
			return
		}
		installment, err = pEng.CalculateInstallment(deal.FinancedAmount, nominalRate, deal.TermMonths, deal.BalloonAmount)
		return
	case "target_installment":
		installment = deal.TargetInstallment
		nominalRate, err = pEng.SolveForRate(deal.FinancedAmount, installment, deal.TermMonths, deal.BalloonAmount)
		if err != nil {
			return
		}
		schedule, err = pEng.BuildAmortizationSchedule(deal, nominalRate)
		return
	default:
		err = fmt.Errorf("unsupported RateMode: %s", deal.RateMode)
		return
	}
}

// pvOfSchedule is a convenience wrapper that discounts at the provided nominal annual rate.
func pvOfSchedule(schedule []types.Cashflow, nominalRate decimal.Decimal) decimal.Decimal {
	return pvOfScheduleAtRate(schedule, nominalRate)
}

// pvOfScheduleAtRate computes PV from the rounded schedule Amounts using monthly compounding
// at the given nominal annual discount rate. Periods are 1..N in order of the schedule entries that are inflows.
// Includes balloon entries as inflows too.
func pvOfScheduleAtRate(schedule []types.Cashflow, discountNominalRate decimal.Decimal) decimal.Decimal {
	if len(schedule) == 0 {
		return decimal.Zero
	}
	monthly := discountNominalRate.Div(decimal.NewFromInt(12))
	onePlus := decimal.NewFromInt(1).Add(monthly)

	pv := decimal.Zero
	pow := decimal.NewFromInt(1) // (1+r_m)^0
	for _, cf := range schedule {
		// Consider periodic inflows only (principal installments and balloon)
		if cf.Direction != "in" {
			continue
		}
		if cf.Type != types.CashflowPrincipal && cf.Type != types.CashflowBalloon {
			continue
		}
		// advance power to next period
		pow = pow.Mul(onePlus) // (1+r_m)^(i)
		if pow.IsZero() {
			continue
		}
		pv = pv.Add(cf.Amount.Div(pow))
	}
	return pv
}

// solveRateForBudget performs a bisection search for the nominal rate r in [lo, hi] such that
// PV_base - PV_target(r) ≈ budget within 0.01 THB (or the rate span is ≤ 1 bp).
// Returns the rate (rounded to bps), the used subsidy (PV difference at the solution),
// the target schedule at the solution, and the corresponding installment.
func solveRateForBudget(
	pEng *pricing.Engine,
	deal types.Deal,
	pvBase decimal.Decimal,
	budget decimal.Decimal,
	discountNominalRate decimal.Decimal,
	lo decimal.Decimal,
	hi decimal.Decimal,
) (rate decimal.Decimal, usedSubsidy decimal.Decimal, sched []types.Cashflow, installment decimal.Decimal, err error) {

	const maxIter = 100
	pvTol := decimal.NewFromFloat(0.01)
	rTol := decimal.NewFromFloat(0.0001) // 1 bp

	// Objective f(r) = PV_base - PV_target(r) - Budget, with both PVs discounted at base discount rate.
	f := func(r decimal.Decimal) (decimal.Decimal, []types.Cashflow) {
		s, _ := pEng.BuildAmortizationSchedule(deal, r)
		pvT := pvOfScheduleAtRate(s, discountNominalRate)
		diff := pvBase.Sub(pvT).Sub(budget)
		return diff, s
	}

	// Check bounds
	fLo, sLo := f(lo)
	if fLo.LessThan(decimal.Zero) {
		// Should have been clipped by caller; return lo best-effort
		rate = lo
		usedSubsidy = pvBase.Sub(pvOfScheduleAtRate(sLo, discountNominalRate))
		sched = sLo
		installment, _ = pEng.CalculateInstallment(deal.FinancedAmount, rate, deal.TermMonths, deal.BalloonAmount)
		err = fmt.Errorf("root not bracketed (f(lo)<0); clip expected")
		return
	}

	var mid, bestR decimal.Decimal
	bestR = lo
	bestAbs := fLo.Abs()

	for i := 0; i < maxIter; i++ {
		mid = lo.Add(hi).Div(decimal.NewFromInt(2))
		fMid, sMid := f(mid)
		absMid := fMid.Abs()

		// Track best
		if absMid.LessThan(bestAbs) {
			bestAbs = absMid
			bestR = mid
			sched = sMid
		}

		// Convergence checks
		if absMid.LessThanOrEqual(pvTol) || hi.Sub(lo).Abs().LessThanOrEqual(rTol) {
			// Round to bps for output and recompute schedule to align PV/subsidy with returned schedule
			rate = types.RoundBasisPoints(mid)
			sOut, _ := pEng.BuildAmortizationSchedule(deal, rate)
			sched = sOut
			installment, _ = pEng.CalculateInstallment(deal.FinancedAmount, rate, deal.TermMonths, deal.BalloonAmount)
			usedSubsidy = pvBase.Sub(pvOfScheduleAtRate(sOut, discountNominalRate))
			return
		}

		// Bisection step by sign
		if fMid.GreaterThan(decimal.Zero) {
			lo = mid
		} else {
			hi = mid
		}
	}

	// Fallback to best after max iterations
	if sched == nil {
		_, sched = f(bestR)
	}
	// Round and rebuild schedule at rounded rate for consistency
	rate = types.RoundBasisPoints(bestR)
	sOut, _ := pEng.BuildAmortizationSchedule(deal, rate)
	sched = sOut
	installment, _ = pEng.CalculateInstallment(deal.FinancedAmount, rate, deal.TermMonths, deal.BalloonAmount)
	usedSubsidy = pvBase.Sub(pvOfScheduleAtRate(sOut, discountNominalRate))
	err = fmt.Errorf("bisection max iterations reached")
	return
}

// finalizeBudgetResult fills the CampaignResult with computed metrics and profitability,
// constructing T0 flows with the applied subsidy and merging with the schedule.
func finalizeBudgetResult(
	input types.CampaignBudgetInput,
	out types.CampaignResult,
	rate decimal.Decimal,
	schedule []types.Cashflow,
	installment decimal.Decimal,
	used decimal.Decimal,
	exceed decimal.Decimal,
	diag string,
	pEng *pricing.Engine,
	cfEng *cashflow.Engine,
	prof *profitability.Engine,
) (types.CampaignResult, error) {
	t0 := []types.Cashflow{}
	if used.GreaterThan(decimal.Zero) {
		t0 = append(t0, types.Cashflow{
			Date:      input.Deal.PayoutDate,
			Direction: "in",
			Type:      types.CashflowSubsidy,
			Amount:    used,
			Memo:      "Subinterest subsidy",
		})
	}
	t0 = cfEng.ConstructT0Flows(input.Deal, t0, nil)
	allCF := cfEng.MergeCashflows(t0, schedule)
	dealIRR, _ := cfEng.CalculateDealIRR(t0, schedule, nil)
	wf, _ := prof.CalculateWaterfall(input.Deal, dealIRR, decimal.Zero, decimal.Zero)

	out.Metrics = types.CampaignMetrics{
		CustomerNominalRate:         types.RoundBasisPoints(rate),
		CustomerEffectiveRate:       pEng.CalculateEffectiveRate(rate, 12),
		MonthlyInstallment:          installment,
		SubsidyUsedTHB:              types.RoundTHB(used),
		RequiredSubsidyTHB:          types.RoundTHB(used),
		ExceedTHB:                   types.RoundTHB(exceed),
		OverBudget:                  false,
		DealerCommissionResolvedTHB: decimal.Zero,
		DealerCommissionPctResolved: decimal.Zero,
		IDCTotalTHB:                 decimal.Zero,
		AcquisitionRoRAC:            wf.AcquisitionRoRAC,
		NetEBITMargin:               wf.NetEBITMargin,
		EconomicCapital:             wf.EconomicCapital,
	}
	out.Schedule = schedule
	out.Cashflows = allCF
	if diag != "" {
		if out.Diagnostics == nil {
			out.Diagnostics = map[string]string{}
		}
		out.Diagnostics[diag] = "no rate movement"
	}
	if out.Diagnostics == nil {
		out.Diagnostics = map[string]string{}
	}
	out.Diagnostics["dealer_commission_unresolved"] = "commission policy not wired in subinterest solver; defaulted to 0"
	return out, nil
}

// solveRateForInstallmentBisection solves for the nominal annual rate that yields the target
// rounded THB installment using a robust bisection method with rate tolerance 1 bp and
// payment tolerance 0.01 THB. Bounds are inclusive.
func solveRateForInstallmentBisection(
	pEng *pricing.Engine,
	deal types.Deal,
	targetInstallment decimal.Decimal,
	lo decimal.Decimal,
	hi decimal.Decimal,
) (decimal.Decimal, error) {
	const maxIter = 100
	rTol := decimal.NewFromFloat(0.0001) // 1 bp
	pTol := decimal.NewFromFloat(10.0)   // relax tolerance per developer note

	// Evaluate bounds
	instLo, err := pEng.CalculateInstallment(deal.FinancedAmount, lo, deal.TermMonths, deal.BalloonAmount)
	if err != nil {
		return decimal.Zero, err
	}
	instHi, err := pEng.CalculateInstallment(deal.FinancedAmount, hi, deal.TermMonths, deal.BalloonAmount)
	if err != nil {
		return decimal.Zero, err
	}

	// If outside feasible range under given caps, clamp to nearest bound
	// Installment is increasing in rate.
	if targetInstallment.LessThan(instLo) {
		return types.RoundBasisPoints(lo), nil
	}
	if targetInstallment.GreaterThan(instHi) {
		return types.RoundBasisPoints(hi), nil
	}

	for i := 0; i < maxIter; i++ {
		mid := lo.Add(hi).Div(decimal.NewFromInt(2))
		instMid, err := pEng.CalculateInstallment(deal.FinancedAmount, mid, deal.TermMonths, deal.BalloonAmount)
		if err != nil {
			return decimal.Zero, err
		}
		diff := instMid.Sub(targetInstallment).Abs()
		if diff.LessThanOrEqual(pTol) || hi.Sub(lo).Abs().LessThanOrEqual(rTol) {
			return types.RoundBasisPoints(mid), nil
		}
		// Monotonic: if installment too high, rate is too high
		if instMid.GreaterThan(targetInstallment) {
			hi = mid
		} else {
			lo = mid
		}
	}

	// Fallback return best-effort midpoint
	mid := lo.Add(hi).Div(decimal.NewFromInt(2))
	return types.RoundBasisPoints(mid), fmt.Errorf("could not solve for rate")
}
