package pricing

import (
	"errors"
	"fmt"
	"math"

	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
)

// Engine handles pricing calculations
type Engine struct {
	dayCountConvention string
	roundingRules      types.RoundingRules
}

// NewEngine creates a new pricing engine
func NewEngine(params types.ParameterSet) *Engine {
	return &Engine{
		dayCountConvention: params.DayCountConvention,
		roundingRules:      params.RoundingRules,
	}
}

// CalculateInstallment calculates the monthly installment for a loan
func (e *Engine) CalculateInstallment(principal, rate decimal.Decimal, termMonths int, balloonAmount decimal.Decimal) (decimal.Decimal, error) {
	if termMonths <= 0 {
		return decimal.Zero, errors.New("term must be positive")
	}

	// Convert annual rate to monthly rate
	monthlyRate := rate.Div(decimal.NewFromInt(12))

	// If rate is zero, simple division
	if monthlyRate.IsZero() {
		if balloonAmount.GreaterThan(principal) {
			return decimal.Zero, errors.New("balloon cannot exceed principal with zero rate")
		}
		amortizingPrincipal := principal.Sub(balloonAmount)
		return types.RoundTHB(amortizingPrincipal.Div(decimal.NewFromInt(int64(termMonths)))), nil
	}

	// Calculate present value of balloon
	balloonPV := balloonAmount.Div(decimal.NewFromFloat(math.Pow(1+monthlyRate.InexactFloat64(), float64(termMonths))))

	// Amortizing principal is total principal minus balloon PV
	amortizingPrincipal := principal.Sub(balloonPV)

	// Standard amortization formula: PMT = P * r / (1 - (1 + r)^-n)
	onePlusRate := decimal.NewFromFloat(1).Add(monthlyRate)
	denominator := decimal.NewFromFloat(1).Sub(
		decimal.NewFromFloat(math.Pow(onePlusRate.InexactFloat64(), float64(-termMonths))),
	)

	if denominator.IsZero() {
		return decimal.Zero, errors.New("invalid rate calculation")
	}

	installment := amortizingPrincipal.Mul(monthlyRate).Div(denominator)
	return types.RoundTHB(installment), nil
}

// SolveForRate finds the interest rate that produces the target installment
func (e *Engine) SolveForRate(principal, targetInstallment decimal.Decimal, termMonths int, balloonAmount decimal.Decimal) (decimal.Decimal, error) {
	if termMonths <= 0 {
		return decimal.Zero, errors.New("term must be positive")
	}

	// Newton-Raphson method for rate solving
	maxIterations := 100
	tolerance := decimal.NewFromFloat(0.0000001)

	// Initial guess - simple approximation
	rate := decimal.NewFromFloat(0.06) // Start with 6% annual

	for i := 0; i < maxIterations; i++ {
		installment, err := e.CalculateInstallment(principal, rate, termMonths, balloonAmount)
		if err != nil {
			return decimal.Zero, err
		}

		diff := installment.Sub(targetInstallment)
		if diff.Abs().LessThan(tolerance) {
			return types.RoundBasisPoints(rate), nil
		}

		// Adjust rate based on difference
		adjustment := diff.Div(principal).Mul(decimal.NewFromFloat(2))
		rate = rate.Sub(adjustment)

		// Keep rate positive
		if rate.LessThanOrEqual(decimal.Zero) {
			rate = decimal.NewFromFloat(0.001)
		}
	}

	return decimal.Zero, errors.New("could not solve for rate")
}

// BuildAmortizationSchedule creates the full payment schedule
func (e *Engine) BuildAmortizationSchedule(deal types.Deal, nominalRate decimal.Decimal) ([]types.Cashflow, error) {
	schedule := []types.Cashflow{}

	// Calculate installment
	installment, err := e.CalculateInstallment(
		deal.FinancedAmount,
		nominalRate,
		deal.TermMonths,
		deal.BalloonAmount,
	)
	if err != nil {
		return nil, err
	}

	// Initialize balance
	balance := deal.FinancedAmount
	monthlyRate := nominalRate.Div(decimal.NewFromInt(12))

	// Build schedule
	for month := 1; month <= deal.TermMonths; month++ {
		// Calculate payment date
		paymentDate := types.AddMonths(deal.PayoutDate, month+deal.FirstPaymentOffset)

		// Calculate interest for the period
		interest := balance.Mul(monthlyRate)
		interest = types.RoundTHB(interest)

		// Principal is installment minus interest
		principal := installment.Sub(interest)

		// Handle final payment with balloon
		if month == deal.TermMonths && deal.BalloonAmount.GreaterThan(decimal.Zero) {
			// Final payment includes balloon
			principal = balance.Sub(deal.BalloonAmount)
			// Ensure we don't have negative principal
			if principal.LessThan(decimal.Zero) {
				principal = decimal.Zero
			}
		} else if month == deal.TermMonths {
			// Final payment clears remaining balance
			principal = balance
		}

		principal = types.RoundTHB(principal)

		// Create cashflow entry
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

		// Update balance
		balance = balance.Sub(principal)
	}

	// Add balloon payment if applicable
	if deal.BalloonAmount.GreaterThan(decimal.Zero) {
		balloonDate := types.AddMonths(deal.PayoutDate, deal.TermMonths+deal.FirstPaymentOffset)
		balloonCF := types.Cashflow{
			Date:      balloonDate,
			Direction: "in",
			Type:      types.CashflowBalloon,
			Amount:    deal.BalloonAmount,
			Principal: deal.BalloonAmount,
			Interest:  decimal.Zero,
			Balance:   decimal.Zero,
			Memo:      "Balloon payment",
		}
		schedule = append(schedule, balloonCF)
	}

	return schedule, nil
}

// CalculateEffectiveRate converts nominal rate to effective annual rate
func (e *Engine) CalculateEffectiveRate(nominalRate decimal.Decimal, compoundingPeriods int) decimal.Decimal {
	if compoundingPeriods <= 0 {
		return nominalRate
	}

	// Effective rate = (1 + nominal/n)^n - 1 computed without float pow
	periodicRate := nominalRate.Div(decimal.NewFromInt(int64(compoundingPeriods)))
	onePlus := decimal.NewFromInt(1).Add(periodicRate)

	pow := decimal.NewFromInt(1)
	for i := 0; i < compoundingPeriods; i++ {
		pow = pow.Mul(onePlus)
	}
	effective := pow.Sub(decimal.NewFromInt(1))

	return types.RoundBasisPoints(effective)
}

// CalculateNominalRate converts effective rate to nominal rate
func (e *Engine) CalculateNominalRate(effectiveRate decimal.Decimal, compoundingPeriods int) decimal.Decimal {
	if compoundingPeriods <= 0 {
		return effectiveRate
	}

	// Nominal = n * ((1 + effective)^(1/n) - 1)
	onePlusEffective := decimal.NewFromFloat(1).Add(effectiveRate)

	// Use float for root calculation
	periodicRate := decimal.NewFromFloat(
		math.Pow(onePlusEffective.InexactFloat64(), 1.0/float64(compoundingPeriods)) - 1,
	)

	nominal := periodicRate.Mul(decimal.NewFromInt(int64(compoundingPeriods)))
	return types.RoundBasisPoints(nominal)
}

// ValidateDeal checks if a deal is valid for pricing
func (e *Engine) ValidateDeal(deal types.Deal) []error {
	var errors []error

	if deal.PriceExTax.LessThanOrEqual(decimal.Zero) {
		errors = append(errors, fmt.Errorf("price must be positive"))
	}

	if deal.TermMonths <= 0 {
		errors = append(errors, fmt.Errorf("term must be positive"))
	}

	if deal.DownPaymentPercent.LessThan(decimal.Zero) || deal.DownPaymentPercent.GreaterThan(decimal.NewFromFloat(0.8)) {
		errors = append(errors, fmt.Errorf("down payment must be between 0%% and 80%%"))
	}

	if deal.BalloonPercent.LessThan(decimal.Zero) || deal.BalloonPercent.GreaterThanOrEqual(decimal.NewFromFloat(1)) {
		errors = append(errors, fmt.Errorf("balloon must be between 0%% and 100%%"))
	}

	if deal.FinancedAmount.LessThanOrEqual(decimal.Zero) {
		errors = append(errors, fmt.Errorf("financed amount must be positive"))
	}

	return errors
}

// ProcessDeal handles the complete pricing calculation for a deal
func (e *Engine) ProcessDeal(deal types.Deal) (*PricingResult, error) {
	// Validate deal
	if errs := e.ValidateDeal(deal); len(errs) > 0 {
		return nil, errs[0]
	}

	var nominalRate decimal.Decimal
	var installment decimal.Decimal
	var err error

	// Handle rate mode
	if deal.RateMode == "fixed_rate" {
		nominalRate = deal.CustomerNominalRate
		installment, err = e.CalculateInstallment(
			deal.FinancedAmount,
			nominalRate,
			deal.TermMonths,
			deal.BalloonAmount,
		)
		if err != nil {
			return nil, err
		}
	} else if deal.RateMode == "target_installment" {
		installment = deal.TargetInstallment
		nominalRate, err = e.SolveForRate(
			deal.FinancedAmount,
			installment,
			deal.TermMonths,
			deal.BalloonAmount,
		)
		if err != nil {
			return nil, err
		}
	} else {
		return nil, errors.New("invalid rate mode")
	}

	// Calculate effective rate
	effectiveRate := e.CalculateEffectiveRate(nominalRate, 12)

	// Build amortization schedule
	schedule, err := e.BuildAmortizationSchedule(deal, nominalRate)
	if err != nil {
		return nil, err
	}

	return &PricingResult{
		MonthlyInstallment:    installment,
		CustomerRateNominal:   nominalRate,
		CustomerRateEffective: effectiveRate,
		Schedule:              schedule,
		FinancedAmount:        deal.FinancedAmount,
		TotalInterest:         calculateTotalInterest(schedule),
		TotalPayments:         calculateTotalPayments(schedule),
	}, nil
}

// PricingResult contains the results of pricing calculations
type PricingResult struct {
	MonthlyInstallment    decimal.Decimal
	CustomerRateNominal   decimal.Decimal
	CustomerRateEffective decimal.Decimal
	Schedule              []types.Cashflow
	FinancedAmount        decimal.Decimal
	TotalInterest         decimal.Decimal
	TotalPayments         decimal.Decimal
}

// Helper functions

func calculateTotalInterest(schedule []types.Cashflow) decimal.Decimal {
	total := decimal.Zero
	for _, cf := range schedule {
		if cf.Type != types.CashflowBalloon {
			total = total.Add(cf.Interest)
		}
	}
	return total
}

func calculateTotalPayments(schedule []types.Cashflow) decimal.Decimal {
	total := decimal.Zero
	for _, cf := range schedule {
		total = total.Add(cf.Amount)
	}
	return total
}
