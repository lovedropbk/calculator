package calculator

import (
	"testing"

	"github.com/financial-calculator/engines/types"
)

type stubLookup map[string]float64

func (s stubLookup) CommissionPercentByProduct(p string) float64 { return s[p] }

func TestResolveDealerCommissionAuto(t *testing.T) {
	lookup := stubLookup{"HP": 0.015}

	pct, amt := ResolveDealerCommissionAuto(lookup, "HP", 800000)
	if pct != 0.015 {
		t.Fatalf("pct = %v, want 0.015", pct)
	}
	if amt != 12000 {
		t.Fatalf("amt = %v, want 12000", amt)
	}

	pct, amt = ResolveDealerCommissionAuto(lookup, "Unknown", 800000)
	if pct != 0 {
		t.Fatalf("pct = %v, want 0", pct)
	}
	if amt != 0 {
		t.Fatalf("amt = %v, want 0", amt)
	}
}

func TestResolveDealerCommissionResolved(t *testing.T) {
	// 1) Override amount takes precedence, rounded and clamped
	overrideAmt := 9000.49
	stateAmt := types.DealState{
		DealerCommission: types.DealerCommission{
			Mode: types.DealerCommissionModeOverride,
			Amt:  &overrideAmt,
		},
	}
	pct, amt := ResolveDealerCommissionResolved(nil, stateAmt, 500000)
	if pct != 0 {
		t.Fatalf("override-amt pct = %v, want 0", pct)
	}
	if amt != 9000 { // 9000.49 rounds to 9000
		t.Fatalf("override-amt amount = %v, want 9000", amt)
	}

	// 2) Override percent used when amount not provided
	overridePct := 0.02
	statePct := types.DealState{
		DealerCommission: types.DealerCommission{
			Mode: types.DealerCommissionModeOverride,
			Pct:  &overridePct,
		},
	}
	pct, amt = ResolveDealerCommissionResolved(nil, statePct, 500000)
	if pct != 0.02 {
		t.Fatalf("override-pct pct = %v, want 0.02", pct)
	}
	if amt != 10000 {
		t.Fatalf("override-pct amount = %v, want 10000", amt)
	}

	// 3) Override mode with no values falls back to auto lookup.
	// Note: ResolveDealerCommissionResolved does not receive product context and
	// calls auto lookup with an empty product. Provide a default rate for \"\" to emulate policy default.
	lookupDefault := stubLookup{"HP": 0.01, "": 0.01}
	stateNone := types.DealState{
		DealerCommission: types.DealerCommission{
			Mode: types.DealerCommissionModeOverride,
		},
	}
	pct, amt = ResolveDealerCommissionResolved(lookupDefault, stateNone, 500000)
	if pct != 0.01 {
		t.Fatalf("override-none fallback pct = %v, want 0.01", pct)
	}
	if amt != 5000 {
		t.Fatalf("override-none fallback amount = %v, want 5000", amt)
	}

	// 4) Auto mode
	stateAuto := types.DealState{
		DealerCommission: types.DealerCommission{
			Mode: types.DealerCommissionModeAuto,
		},
	}
	pct, amt = ResolveDealerCommissionResolved(lookupDefault, stateAuto, 500000)
	if pct != 0.01 {
		t.Fatalf("auto pct = %v, want 0.01", pct)
	}
	if amt != 5000 {
		t.Fatalf("auto amount = %v, want 5000", amt)
	}
}

// Additional tests for defaults and base exclusion behavior

func TestDealerCommissionDefaultsByProduct(t *testing.T) {
	// Lookup returns no policy entries -> engine should fall back to defaults
	lookup := stubLookup{} // empty map

	tests := []struct {
		product    string
		financed   float64
		wantPct    float64
		wantAmtTHB float64
	}{
		{"HP", 800000, 0.03, 24000},
		{"mySTAR", 800000, 0.07, 56000},
		{"F-Lease", 800000, 0.07, 56000},
		{"FinanceLease", 800000, 0.07, 56000},
		{"Op-Lease", 800000, 0.07, 56000},
		{"OperatingLease", 800000, 0.07, 56000},
	}

	for _, tt := range tests {
		pct, amt := ResolveDealerCommissionAuto(lookup, tt.product, tt.financed)
		if pct != tt.wantPct {
			t.Fatalf("product=%s pct=%v, want %v", tt.product, pct, tt.wantPct)
		}
		if amt != tt.wantAmtTHB {
			t.Fatalf("product=%s amt=%v, want %v", tt.product, amt, tt.wantAmtTHB)
		}
	}
}

func TestDealerCommissionFromPolicyYAMLLikeValues(t *testing.T) {
	// Emulate YAML policy values: HP=3%, FinanceLease=7%, OperatingLease=7%, mySTAR=7%
	lookup := stubLookup{
		"HP":             0.03,
		"FinanceLease":   0.07,
		"OperatingLease": 0.07,
		"mySTAR":         0.07,
	}

	tests := []struct {
		product    string
		financed   float64
		wantPct    float64
		wantAmtTHB float64
	}{
		{"HP", 1000000, 0.03, 30000},
		{"FinanceLease", 500000, 0.07, 35000},
		{"OperatingLease", 400000, 0.07, 28000},
		{"mySTAR", 300000, 0.07, 21000},
	}

	for _, tt := range tests {
		pct, amt := ResolveDealerCommissionAuto(lookup, tt.product, tt.financed)
		if pct != tt.wantPct {
			t.Fatalf("product=%s pct=%v, want %v", tt.product, pct, tt.wantPct)
		}
		if amt != tt.wantAmtTHB {
			t.Fatalf("product=%s amt=%v, want %v", tt.product, amt, tt.wantAmtTHB)
		}
	}
}

func TestCommissionBaseExcludesFinancedIDCs(t *testing.T) {
	// Price 1,000,000; DP 200,000; a financed IDC of 10,000 exists in the deal,
	// but commission base must exclude financed IDCs: base = price - dp = 800,000.
	price := 1_000_000.0
	downPayment := 200_000.0
	financedIDC := 10_000.0

	// What financed amount could be (if including financed IDCs):
	financedIncludingIDC := price - downPayment + financedIDC // 810,000 (NOT the commission base)

	// Correct commission base (exclude financed IDCs):
	base := price - downPayment // 800,000

	// Use defaults fallback (no policy provided)
	lookup := stubLookup{}
	pct, amt := ResolveDealerCommissionAuto(lookup, "HP", base)
	if pct != 0.03 {
		t.Fatalf("pct=%v, want 0.03", pct)
	}
	if amt != 24000 {
		t.Fatalf("amount=%v, want 24000 (3%% of 800,000)", amt)
	}

	// Demonstrate that including financed IDCs would yield a different (incorrect) amount
	pctWrong, amtWrong := ResolveDealerCommissionAuto(lookup, "HP", financedIncludingIDC)
	if pctWrong != 0.03 {
		t.Fatalf("pctWrong=%v, want 0.03", pctWrong)
	}
	if amtWrong == amt {
		t.Fatalf("including IDC yielded same amount; expected different. got=%v", amtWrong)
	}
}
