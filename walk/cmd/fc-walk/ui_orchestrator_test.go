//go:build windows

package main

import (
	"testing"

	"github.com/financial-calculator/engines/calculator"
	"github.com/financial-calculator/engines/campaigns"
	"github.com/financial-calculator/engines/types"
)

func TestValidateInputs_DefaultState_ErrorPrice(t *testing.T) {
	// Empty/default state -> expect "price must be positive"
	s := UIState{
		Product:    "HP",
		PriceExTax: 0,
		TermMonths: 0,
		BalloonPct: 0,
	}
	err := validateInputs(s)
	if err == nil {
		t.Fatalf("expected error for default state, got nil")
	}
	if err.Error() != "price must be positive" {
		t.Fatalf("expected 'price must be positive', got %q", err.Error())
	}
	if shouldCompute(s) {
		t.Fatalf("shouldCompute must be false for invalid inputs")
	}
}

func TestValidateInputs_ValidPriceAndTerm_OK(t *testing.T) {
	// Price > 0 and Term > 0 -> expect nil
	s := UIState{
		Product:    "HP",
		PriceExTax: 100000,
		TermMonths: 12,
		BalloonPct: 0,
	}
	if err := validateInputs(s); err != nil {
		t.Fatalf("unexpected error for valid inputs: %v", err)
	}
	if !shouldCompute(s) {
		t.Fatalf("shouldCompute must be true for valid inputs")
	}
}

func TestValidateInputs_MyStarBalloonShortTerm_Error(t *testing.T) {
	// mySTAR with BalloonPct > 0 and Term = 3 -> expect error
	s := UIState{
		Product:    "mySTAR",
		PriceExTax: 100000,
		TermMonths: 3,
		BalloonPct: 10,
	}
	err := validateInputs(s)
	if err == nil {
		t.Fatalf("expected error for mySTAR with balloon > 0 and short term, got nil")
	}
	if shouldCompute(s) {
		t.Fatalf("shouldCompute must be false for invalid mySTAR balloon/term inputs")
	}
}

func TestFormatTHB_Integer(t *testing.T) {
	got := FormatTHB(12000)
	want := "12,000"
	if got != want {
		t.Fatalf("FormatTHB(12000) = %q; want %q", got, want)
	}
}

func TestFormatTHB_Decimal(t *testing.T) {
	got := FormatTHB(22198.61)
	want := "22,198.61"
	if got != want {
		t.Fatalf("FormatTHB(22198.61) = %q; want %q", got, want)
	}
}

func TestFormatRatePct(t *testing.T) {
	got := FormatRatePct(0.0558)
	want := "5.58 percent"
	if got != want {
		t.Fatalf("FormatRatePct(0.0558) = %q; want %q", got, want)
	}
}

func TestMapProductDisplayToEnum_FinanceLease(t *testing.T) {
	p, err := MapProductDisplayToEnum("Financing Lease")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if string(p) != "F-Lease" {
		t.Fatalf("expected product 'F-Lease', got %q", string(p))
	}
}

// Minimal orchestrator test: ensure Subinterest row populates Monthly strings (not "—")
func TestComputeCampaignRows_SubinterestBudget_PopulatesMonthly(t *testing.T) {
	// Parameter set and engines
	ps := defaultParameterSet()
	calc := calculator.New(ps)
	campEng := campaigns.NewEngine(ps)
	// Commission lookup for auto mode (empty map falls back to 0 in summaries for non-cash rows)
	campEng.SetCommissionLookup(staticCommissionLookup{by: map[string]float64{}})

	// Base deal from helpers (36m, 1,000,000 THB, 20% down, fixed rate 3.99%)
	deal := buildDealFromControls(
		"HP", "arrears", "percent", "fixed_rate",
		1_000_000, 20, 0,
		36, 0, 3.99, 0,
	)

	// Display campaigns includes a Subinterest option
	display := []types.Campaign{{
		ID:         "SUBINT-TEST",
		Type:       types.CampaignSubinterest,
		TargetRate: types.NewDecimal(0.0299),
		Funder:     "Manufacturer",
		Stacking:   2,
	}}

	state := types.DealState{
		DealerCommission: types.DealerCommission{Mode: types.DealerCommissionModeAuto},
		IDCOther:         types.IDCOther{Value: 0, UserEdited: false},
	}

	rows, idx := computeCampaignRows(ps, calc, campEng, deal, state, display, 15000 /*budget THB*/, 20 /*dp%*/, 0)
	if idx != 0 {
		t.Fatalf("expected selected index 0, got %d", idx)
	}
	if len(rows) == 0 {
		t.Fatalf("expected at least one campaign row")
	}
	s := rows[0].MonthlyInstallmentStr
	if s == "" {
		t.Fatalf("expected MonthlyInstallmentStr to be populated")
	}
	if rows[0].MonthlyInstallment <= 0 {
		t.Fatalf("expected positive MonthlyInstallment, got %f", rows[0].MonthlyInstallment)
	}
}
