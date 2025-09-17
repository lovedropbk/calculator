package cashflow

import (
	"errors"
	"fmt"
	"math"
	"time"

	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
)

// Engine handles cashflow and rate calculations
type Engine struct {
	dayCountConvention string
	roundingRules      types.RoundingRules
}

// NewEngine creates a new cashflow engine
func NewEngine(params types.ParameterSet) *Engine {
	return &Engine{
		dayCountConvention: params.DayCountConvention,
		roundingRules:      params.RoundingRules,
	}
}

// ConstructT0Flows builds the initial time-zero cashflows
func (e *Engine) ConstructT0Flows(deal types.Deal, campaignFlows []types.Cashflow, idcItems []types.IDCItem) []types.Cashflow {
	flows := []types.Cashflow{}

	// Dealer disbursement (outflow)
	disbursement := types.Cashflow{
		Date:      deal.PayoutDate,
		Direction: "out",
		Type:      types.CashflowDisbursement,
		Amount:    deal.FinancedAmount,
		Memo:      "Dealer disbursement",
	}
	flows = append(flows, disbursement)

	// Note: Down payment is NOT included in lender cashflows
	// The down payment goes directly from customer to dealer, not through the lender

	// Campaign subsidies (inflows)
	flows = append(flows, campaignFlows...)

	// IDC items at T0
	for _, idc := range idcItems {
		if idc.Timing == types.IDCTimingUpfront {
			// Financed IDC items are recovered via periodic installments,
			// so they should NOT create a T0 cashflow.
			if idc.Financed {
				continue
			}

			direction := "out"
			if idc.IsRevenue {
				direction = "in"
			}

			idcFlow := types.Cashflow{
				Date:      deal.PayoutDate,
				Direction: direction,
				Type:      types.CashflowIDC,
				Amount:    idc.Amount,
				Memo:      fmt.Sprintf("IDC: %s", idc.Description),
			}
			flows = append(flows, idcFlow)
		}
	}

	return flows
}

// BuildPeriodicSchedule constructs the periodic payment schedule
func (e *Engine) BuildPeriodicSchedule(deal types.Deal, installment decimal.Decimal, rate decimal.Decimal) []types.Cashflow {
	schedule := []types.Cashflow{}
	balance := deal.FinancedAmount
	monthlyRate := rate.Div(decimal.NewFromInt(12))

	for month := 1; month <= deal.TermMonths; month++ {
		// Calculate payment date
		var paymentDate time.Time
		if deal.Timing == types.TimingArrears {
			paymentDate = types.AddMonths(deal.PayoutDate, month+deal.FirstPaymentOffset)
		} else {
			paymentDate = types.AddMonths(deal.PayoutDate, month-1+deal.FirstPaymentOffset)
		}

		// Calculate interest
		interest := balance.Mul(monthlyRate)
		interest = types.RoundTHB(interest)

		// Calculate principal
		principal := installment.Sub(interest)

		// Handle final payment
		if month == deal.TermMonths {
			if deal.BalloonAmount.GreaterThan(decimal.Zero) {
				// Final payment with balloon
				principal = balance.Sub(deal.BalloonAmount)
			} else {
				// Final payment clears balance
				principal = balance
			}
			// Adjust installment for final payment
			installment = principal.Add(interest)
		}

		principal = types.RoundTHB(principal)
		installment = types.RoundTHB(installment)

		// Create cashflow
		cf := types.Cashflow{
			Date:      paymentDate,
			Direction: "in",
			Type:      types.CashflowPrincipal,
			Amount:    installment,
			Principal: principal,
			Interest:  interest,
			Balance:   balance.Sub(principal),
			Memo:      fmt.Sprintf("Payment %d of %d", month, deal.TermMonths),
		}

		schedule = append(schedule, cf)
		balance = balance.Sub(principal)
	}

	return schedule
}

// AddBalloonPayment adds balloon payment at maturity
func (e *Engine) AddBalloonPayment(deal types.Deal, schedule []types.Cashflow) []types.Cashflow {
	if deal.BalloonAmount.LessThanOrEqual(decimal.Zero) {
		return schedule
	}

	balloonDate := types.AddMonths(deal.PayoutDate, deal.TermMonths+deal.FirstPaymentOffset)

	balloonFlow := types.Cashflow{
		Date:      balloonDate,
		Direction: "in",
		Type:      types.CashflowBalloon,
		Amount:    deal.BalloonAmount,
		Principal: deal.BalloonAmount,
		Interest:  decimal.Zero,
		Balance:   decimal.Zero,
		Memo:      "Balloon payment at maturity",
	}

	return append(schedule, balloonFlow)
}

// CalculateMonthlyIRR calculates the monthly internal rate of return
func (e *Engine) CalculateMonthlyIRR(cashflows []types.Cashflow) (decimal.Decimal, error) {
	if len(cashflows) == 0 {
		return decimal.Zero, errors.New("no cashflows provided")
	}

	// Newton-Raphson method for IRR
	maxIterations := 100
	tolerance := decimal.NewFromFloat(0.0000001)

	// Initial guess
	irr := decimal.NewFromFloat(0.005) // Start with 0.5% monthly

	for i := 0; i < maxIterations; i++ {
		npv, npvDerivative := e.calculateNPVAndDerivative(cashflows, irr)

		if npv.Abs().LessThan(tolerance) {
			return types.RoundBasisPoints(irr), nil
		}

		if npvDerivative.IsZero() {
			return decimal.Zero, errors.New("derivative is zero, cannot continue")
		}

		// Newton-Raphson update
		irrDelta := npv.Div(npvDerivative)
		irr = irr.Sub(irrDelta)

		// Bound the IRR to reasonable values
		if irr.LessThan(decimal.NewFromFloat(-0.99)) {
			irr = decimal.NewFromFloat(-0.99)
		} else if irr.GreaterThan(decimal.NewFromFloat(10)) {
			irr = decimal.NewFromFloat(10)
		}
	}

	return decimal.Zero, errors.New("IRR did not converge")
}

// calculateNPVAndDerivative calculates NPV and its derivative for Newton-Raphson
func (e *Engine) calculateNPVAndDerivative(cashflows []types.Cashflow, rate decimal.Decimal) (decimal.Decimal, decimal.Decimal) {
	npv := decimal.Zero
	npvDerivative := decimal.Zero

	if len(cashflows) == 0 {
		return npv, npvDerivative
	}

	// Get reference date (first cashflow date)
	refDate := cashflows[0].Date

	for _, cf := range cashflows {
		// Calculate time period in months
		months := types.MonthsBetween(refDate, cf.Date)

		// Calculate amount considering direction
		amount := cf.Amount
		if cf.Direction == "out" {
			amount = amount.Neg()
		}

		// Calculate discount factor
		if months > 0 {
			discountFactor := decimal.NewFromFloat(math.Pow(1+rate.InexactFloat64(), float64(-months)))
			npv = npv.Add(amount.Mul(discountFactor))

			// Calculate derivative
			derivative := decimal.NewFromFloat(-float64(months) * math.Pow(1+rate.InexactFloat64(), float64(-months-1)))
			npvDerivative = npvDerivative.Add(amount.Mul(derivative))
		} else {
			// No discounting for t=0
			npv = npv.Add(amount)
		}
	}

	return npv, npvDerivative
}

// CalculateEffectiveAnnualRate converts monthly IRR to effective annual rate
func (e *Engine) CalculateEffectiveAnnualRate(monthlyIRR decimal.Decimal) decimal.Decimal {
	// EAR = (1 + monthly_rate)^12 - 1
	onePlusRate := decimal.NewFromFloat(1).Add(monthlyIRR)
	ear := decimal.NewFromFloat(math.Pow(onePlusRate.InexactFloat64(), 12) - 1)
	return types.RoundBasisPoints(ear)
}

// CalculateNominalRate converts effective rate to nominal rate
func (e *Engine) CalculateNominalRate(effectiveRate decimal.Decimal, compoundingPeriods int) decimal.Decimal {
	if compoundingPeriods <= 0 {
		return effectiveRate
	}

	// Nominal = n * ((1 + effective)^(1/n) - 1)
	onePlusEffective := decimal.NewFromFloat(1).Add(effectiveRate)
	periodicRate := decimal.NewFromFloat(
		math.Pow(onePlusEffective.InexactFloat64(), 1.0/float64(compoundingPeriods)) - 1,
	)

	nominal := periodicRate.Mul(decimal.NewFromInt(int64(compoundingPeriods)))
	return types.RoundBasisPoints(nominal)
}

// SolveForNominalRate finds the nominal rate that produces the target installment
func (e *Engine) SolveForNominalRate(principal, targetInstallment decimal.Decimal, termMonths int, balloonAmount decimal.Decimal) (decimal.Decimal, error) {
	if termMonths <= 0 {
		return decimal.Zero, errors.New("term must be positive")
	}

	// Newton-Raphson method
	maxIterations := 100
	tolerance := decimal.NewFromFloat(0.0000001)

	// Initial guess
	rate := decimal.NewFromFloat(0.06) // Start with 6% annual

	for i := 0; i < maxIterations; i++ {
		// Calculate installment at current rate
		installment := e.calculateInstallmentForRate(principal, rate, termMonths, balloonAmount)

		// Check convergence
		diff := installment.Sub(targetInstallment)
		if diff.Abs().LessThan(tolerance) {
			return types.RoundBasisPoints(rate), nil
		}

		// Calculate derivative (numerical)
		deltaRate := decimal.NewFromFloat(0.00001)
		installmentPlus := e.calculateInstallmentForRate(principal, rate.Add(deltaRate), termMonths, balloonAmount)
		derivative := installmentPlus.Sub(installment).Div(deltaRate)

		if derivative.IsZero() {
			return decimal.Zero, errors.New("derivative is zero")
		}

		// Update rate
		rate = rate.Sub(diff.Div(derivative))

		// Keep rate positive
		if rate.LessThanOrEqual(decimal.Zero) {
			rate = decimal.NewFromFloat(0.001)
		}
	}

	return decimal.Zero, errors.New("could not solve for rate")
}

// calculateInstallmentForRate helper function for rate solving
func (e *Engine) calculateInstallmentForRate(principal, annualRate decimal.Decimal, termMonths int, balloonAmount decimal.Decimal) decimal.Decimal {
	monthlyRate := annualRate.Div(decimal.NewFromInt(12))

	if monthlyRate.IsZero() {
		amortizingPrincipal := principal.Sub(balloonAmount)
		return types.RoundTHB(amortizingPrincipal.Div(decimal.NewFromInt(int64(termMonths))))
	}

	// Calculate PV of balloon
	balloonPV := balloonAmount.Div(
		decimal.NewFromFloat(math.Pow(1+monthlyRate.InexactFloat64(), float64(termMonths))),
	)

	// Amortizing principal
	amortizingPrincipal := principal.Sub(balloonPV)

	// Standard formula: PMT = P * r / (1 - (1 + r)^-n)
	onePlusRate := decimal.NewFromFloat(1).Add(monthlyRate)
	denominator := decimal.NewFromFloat(1).Sub(
		decimal.NewFromFloat(math.Pow(onePlusRate.InexactFloat64(), float64(-termMonths))),
	)

	if denominator.IsZero() {
		return decimal.Zero
	}

	installment := amortizingPrincipal.Mul(monthlyRate).Div(denominator)
	return types.RoundTHB(installment)
}

// MergeCashflows combines multiple cashflow streams
func (e *Engine) MergeCashflows(streams ...[]types.Cashflow) []types.Cashflow {
	merged := []types.Cashflow{}

	for _, stream := range streams {
		merged = append(merged, stream...)
	}

	// Sort by date
	// Note: In production, would implement proper sorting

	return merged
}

// CalculateDealIRR calculates the deal-level IRR on full cashflows (customer cashflows perspective)
// - T0: include disbursement outflow and any non-financed IDC/campaign flows
// - Periodic: include full installment Amount as inflows
// - Balloon/terminal flows are already represented in schedule where applicable
func (e *Engine) CalculateDealIRR(t0Flows []types.Cashflow, schedule []types.Cashflow, idcPeriodic []types.Cashflow) (decimal.Decimal, error) {
	// Merge all cashflows
	allFlows := e.MergeCashflows(t0Flows, schedule, idcPeriodic)

	if len(allFlows) == 0 {
		return decimal.Zero, errors.New("no cashflows for IRR calculation")
	}

	// Calculate monthly IRR
	monthlyIRR, err := e.CalculateMonthlyIRR(allFlows)
	if err != nil {
		return decimal.Zero, err
	}

	// Convert to effective annual rate
	return e.CalculateEffectiveAnnualRate(monthlyIRR), nil
}
