package pricing

import (
	"testing"
	"time"

	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

func TestCalculateInstallment(t *testing.T) {
	engine := NewEngine(createTestParameterSet())

	tests := []struct {
		name          string
		principal     decimal.Decimal
		rate          decimal.Decimal
		termMonths    int
		balloonAmount decimal.Decimal
		expected      decimal.Decimal
		expectError   bool
	}{
		{
			name:          "Standard HP loan",
			principal:     decimal.NewFromFloat(1000000),
			rate:          decimal.NewFromFloat(0.0644),
			termMonths:    12,
			balloonAmount: decimal.Zero,
			expected:      decimal.NewFromFloat(86269),
			expectError:   false,
		},
		{
			name:          "mySTAR with balloon",
			principal:     decimal.NewFromFloat(1000000),
			rate:          decimal.NewFromFloat(0.0644),
			termMonths:    12,
			balloonAmount: decimal.NewFromFloat(300000),
			expected:      decimal.NewFromFloat(61998),
			expectError:   false,
		},
		{
			name:          "Zero rate loan",
			principal:     decimal.NewFromFloat(1000000),
			rate:          decimal.Zero,
			termMonths:    12,
			balloonAmount: decimal.Zero,
			expected:      decimal.NewFromFloat(83333),
			expectError:   false,
		},
		{
			name:          "Invalid term",
			principal:     decimal.NewFromFloat(1000000),
			rate:          decimal.NewFromFloat(0.0644),
			termMonths:    0,
			balloonAmount: decimal.Zero,
			expected:      decimal.Zero,
			expectError:   true,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result, err := engine.CalculateInstallment(tt.principal, tt.rate, tt.termMonths, tt.balloonAmount)

			if tt.expectError {
				assert.Error(t, err)
			} else {
				require.NoError(t, err)
				// Allow for small rounding differences
				assert.True(t, result.Sub(tt.expected).Abs().LessThan(decimal.NewFromFloat(1)),
					"Expected %s, got %s", tt.expected.String(), result.String())
			}
		})
	}
}

func TestSolveForRate(t *testing.T) {
	engine := NewEngine(createTestParameterSet())

	tests := []struct {
		name              string
		principal         decimal.Decimal
		targetInstallment decimal.Decimal
		termMonths        int
		balloonAmount     decimal.Decimal
		expectedRateMin   decimal.Decimal
		expectedRateMax   decimal.Decimal
		expectError       bool
	}{
		{
			name:              "Solve for standard HP rate",
			principal:         decimal.NewFromFloat(1000000),
			targetInstallment: decimal.NewFromFloat(86263),
			termMonths:        12,
			balloonAmount:     decimal.Zero,
			expectedRateMin:   decimal.NewFromFloat(0.0640),
			expectedRateMax:   decimal.NewFromFloat(0.0648),
			expectError:       false,
		},
		{
			name:              "Solve for rate with balloon",
			principal:         decimal.NewFromFloat(1000000),
			targetInstallment: decimal.NewFromFloat(61998),
			termMonths:        12,
			balloonAmount:     decimal.NewFromFloat(300000),
			expectedRateMin:   decimal.NewFromFloat(0.0640),
			expectedRateMax:   decimal.NewFromFloat(0.0648),
			expectError:       false,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result, err := engine.SolveForRate(tt.principal, tt.targetInstallment, tt.termMonths, tt.balloonAmount)

			if tt.expectError {
				assert.Error(t, err)
			} else {
				require.NoError(t, err)
				assert.True(t, result.GreaterThanOrEqual(tt.expectedRateMin),
					"Rate %s should be >= %s", result.String(), tt.expectedRateMin.String())
				assert.True(t, result.LessThanOrEqual(tt.expectedRateMax),
					"Rate %s should be <= %s", result.String(), tt.expectedRateMax.String())
			}
		})
	}
}

func TestBuildAmortizationSchedule(t *testing.T) {
	engine := NewEngine(createTestParameterSet())

	deal := types.Deal{
		Product:            types.ProductHirePurchase,
		PriceExTax:         decimal.NewFromFloat(1665576),
		DownPaymentAmount:  decimal.NewFromFloat(333115),
		FinancedAmount:     decimal.NewFromFloat(1332461),
		TermMonths:         12,
		BalloonAmount:      decimal.Zero,
		Timing:             types.TimingArrears,
		PayoutDate:         time.Date(2025, 8, 4, 0, 0, 0, 0, time.UTC),
		FirstPaymentOffset: 1,
	}

	nominalRate := decimal.NewFromFloat(0.0644)

	schedule, err := engine.BuildAmortizationSchedule(deal, nominalRate)
	require.NoError(t, err)

	// Verify schedule properties
	assert.Equal(t, 12, len(schedule))

	// Check first payment
	firstPayment := schedule[0]
	assert.Equal(t, time.Date(2025, 10, 4, 0, 0, 0, 0, time.UTC), firstPayment.Date)
	assert.Equal(t, "in", firstPayment.Direction)
	assert.Equal(t, types.CashflowPrincipal, firstPayment.Type)

	// Check that balance decreases
	for i := 1; i < len(schedule); i++ {
		assert.True(t, schedule[i].Balance.LessThan(schedule[i-1].Balance))
	}

	// Last payment should clear balance
	lastPayment := schedule[len(schedule)-1]
	assert.True(t, lastPayment.Balance.LessThanOrEqual(decimal.NewFromFloat(1)),
		"Final balance should be near zero, got %s", lastPayment.Balance.String())
}

func TestCalculateEffectiveRate(t *testing.T) {
	engine := NewEngine(createTestParameterSet())

	tests := []struct {
		name               string
		nominalRate        decimal.Decimal
		compoundingPeriods int
		expected           decimal.Decimal
	}{
		{
			name:               "Monthly compounding",
			nominalRate:        decimal.NewFromFloat(0.0644),
			compoundingPeriods: 12,
			expected:           decimal.NewFromFloat(0.0663),
		},
		{
			name:               "Quarterly compounding",
			nominalRate:        decimal.NewFromFloat(0.0644),
			compoundingPeriods: 4,
			expected:           decimal.NewFromFloat(0.0660),
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			result := engine.CalculateEffectiveRate(tt.nominalRate, tt.compoundingPeriods)
			assert.True(t, result.Sub(tt.expected).Abs().LessThan(decimal.NewFromFloat(0.0001)),
				"Expected %s, got %s", tt.expected.String(), result.String())
		})
	}
}

func TestValidateDeal(t *testing.T) {
	engine := NewEngine(createTestParameterSet())

	validDeal := types.Deal{
		Product:            types.ProductHirePurchase,
		PriceExTax:         decimal.NewFromFloat(1665576),
		DownPaymentAmount:  decimal.NewFromFloat(333115),
		DownPaymentPercent: decimal.NewFromFloat(0.2),
		FinancedAmount:     decimal.NewFromFloat(1332461),
		TermMonths:         12,
		BalloonPercent:     decimal.Zero,
	}

	tests := []struct {
		name        string
		modifyDeal  func(*types.Deal)
		expectError bool
	}{
		{
			name:        "Valid deal",
			modifyDeal:  func(d *types.Deal) {},
			expectError: false,
		},
		{
			name: "Negative price",
			modifyDeal: func(d *types.Deal) {
				d.PriceExTax = decimal.NewFromFloat(-1000)
			},
			expectError: true,
		},
		{
			name: "Invalid down payment",
			modifyDeal: func(d *types.Deal) {
				d.DownPaymentPercent = decimal.NewFromFloat(0.85)
			},
			expectError: true,
		},
		{
			name: "Invalid balloon",
			modifyDeal: func(d *types.Deal) {
				d.BalloonPercent = decimal.NewFromFloat(1.5)
			},
			expectError: true,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			deal := validDeal
			tt.modifyDeal(&deal)
			errors := engine.ValidateDeal(deal)

			if tt.expectError {
				assert.NotEmpty(t, errors)
			} else {
				assert.Empty(t, errors)
			}
		})
	}
}

func createTestParameterSet() types.ParameterSet {
	return types.ParameterSet{
		ID:                 "TEST-001",
		Version:            "test",
		DayCountConvention: "ACT/365",
		RoundingRules: types.RoundingRules{
			Currency:    "THB",
			MinorUnits:  0,
			Method:      "bank",
			DisplayRate: 4,
		},
	}
}
