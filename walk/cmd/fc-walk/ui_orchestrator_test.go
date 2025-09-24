//go:build windows

package main

import "testing"

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
