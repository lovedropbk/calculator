package calculator

import (
	"math"

	"github.com/financial-calculator/engines/types"
)

// CommissionLookup is the minimal contract required from a parameter service.
// Any type (e.g., *parameters.Service in the root module) that implements this
// method can be passed to these helpers without creating a module cycle.
type CommissionLookup interface {
	CommissionPercentByProduct(product string) float64
}

// defaultCommissionPercent returns fallback defaults by product when policy is missing or key not present.
// HP: 3%; FinanceLease/F-Lease: 7%; OperatingLease/Op-Lease: 7%; mySTAR/BalloonHP: 7%
func defaultCommissionPercent(product string) float64 {
	switch product {
	case "HP", "HirePurchase":
		return 0.03
	case "mySTAR", "BalloonHP", "Balloon":
		return 0.07
	case "F-Lease", "FinanceLease", "Finance Lease", "FLease", "F Lease":
		return 0.07
	case "Op-Lease", "OperatingLease", "Operating Lease", "OpLease", "Op Lease":
		return 0.07
	default:
		return 0
	}
}

// ResolveDealerCommissionAuto resolves dealer commission automatically from parameters for a given product and financed amount.
// - pct is read via params.CommissionPercentByProduct(product) (negative values already clamped to 0 by accessor).
// - amountTHB is rounded to nearest THB. Base is clamped at 0.
func ResolveDealerCommissionAuto(params CommissionLookup, product string, financedAmt float64) (pct float64, amountTHB float64) {
	// Defensive: if params nil, behave as unknown product (pct=0, amount=0)
	if params == nil {
		base := math.Max(financedAmt, 0)
		return 0, math.Round(base * 0.0)
	}

	pct = params.CommissionPercentByProduct(product) // accessor clamps negatives to 0
	if pct == 0 {
		// Fallback to defaults when policy missing or key not present
		if d := defaultCommissionPercent(product); d > 0 {
			pct = d
		}
	}
	base := math.Max(financedAmt, 0)
	amountTHB = math.Round(base * pct)
	if amountTHB < 0 {
		amountTHB = 0
	}
	return
}

// ResolveDealerCommissionResolved resolves dealer commission using override rules, otherwise falls back to auto.
// Precedence (per architecture):
//  1. Amount override (Amt) - rounded and clamped to 0.
//  2. Percent override (Pct) - clamped to 0, then computed on financed base with rounding.
//  3. Auto lookup (by product via parameters).
//
// Notes:
// - DealState currently does not expose product; when absent, auto lookup with empty product yields pct=0 per accessor.
func ResolveDealerCommissionResolved(params CommissionLookup, deal types.DealState, financedAmt float64) (pct float64, amountTHB float64) {
	mode := deal.DealerCommission.Mode
	if mode == types.DealerCommissionModeOverride {
		// 1) Amount override takes precedence when provided
		if deal.DealerCommission.Amt != nil {
			amt := math.Round(*deal.DealerCommission.Amt)
			if amt < 0 {
				amt = 0
			}
			return 0, amt
		}
		// 2) Percent override (fraction, e.g., 0.015)
		if deal.DealerCommission.Pct != nil {
			pct = *deal.DealerCommission.Pct
			if pct < 0 {
				pct = 0
			}
			base := math.Max(financedAmt, 0)
			amountTHB = math.Round(base * pct)
			if amountTHB < 0 {
				amountTHB = 0
			}
			return
		}
		// 3) Fall back to auto when neither Amt nor Pct is provided
	}

	// Auto mode (or fallback from override without values)
	// Product is not present on DealState; use empty string to trigger defined fallback (pct=0).
	var product string
	return ResolveDealerCommissionAuto(params, product, math.Max(financedAmt, 0))
}
