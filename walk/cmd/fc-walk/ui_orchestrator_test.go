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

// --- Milestone 2: tiny pure tests for edit-mode helpers

func TestSelectMyCampaignSetsEditMode(t *testing.T) {
	st := EditorState{}
	SelectMyCampaign(&st, "id-123")
	if !st.IsEditMode {
		t.Fatalf("IsEditMode should be true after SelectMyCampaign")
	}
	if st.SelectedMyCampaignID != "id-123" {
		t.Fatalf("SelectedMyCampaignID = %q; want %q", st.SelectedMyCampaignID, "id-123")
	}
}

func TestExitEditModeClearsFlag(t *testing.T) {
	st := EditorState{IsEditMode: true, SelectedMyCampaignID: "x"}
	ExitEditMode(&st)
	if st.IsEditMode {
		t.Fatalf("IsEditMode should be false after ExitEditMode")
	}
}

// --- Milestone 4: pure reducers tests

func TestUpdateDraftInputs_SetsInputsAndUpdatedAt(t *testing.T) {
	base := CampaignDraft{
		ID: "id-1", Name: "Camp 1", Product: "HP",
		Inputs:      CampaignInputs{PriceExTaxTHB: 100000, TermMonths: 12, RateMode: "fixed_rate", CustomerRateAPR: 0.0399},
		Adjustments: CampaignAdjustments{CashDiscountTHB: 1000},
		Metadata:    CampaignMetadata{CreatedAt: "2021-01-01T00:00:00Z", UpdatedAt: "2021-01-02T00:00:00Z"},
	}
	in := CampaignInputs{
		PriceExTaxTHB: 200000, DownpaymentPercent: 15, DownpaymentTHB: 30000, TermMonths: 24,
		BalloonPercent: 10, RateMode: "target_installment", CustomerRateAPR: 0.0299, TargetInstallmentTHB: 5555,
	}
	now := "2025-09-29T00:00:00Z"

	got := UpdateDraftInputs(base, in, now)

	if got.Inputs != in {
		t.Fatalf("Inputs not updated: %#v", got.Inputs)
	}
	if got.Metadata.UpdatedAt != now {
		t.Fatalf("UpdatedAt = %q; want %q", got.Metadata.UpdatedAt, now)
	}
	if got.Metadata.CreatedAt != base.Metadata.CreatedAt {
		t.Fatalf("CreatedAt changed: %q -> %q", base.Metadata.CreatedAt, got.Metadata.CreatedAt)
	}
	if got.ID != base.ID || got.Name != base.Name || got.Product != base.Product {
		t.Fatalf("Identity fields changed")
	}
	if got.Adjustments != base.Adjustments {
		t.Fatalf("Adjustments should be unchanged")
	}
}

func TestUpdateDraftAdjustments_SetsAdjustmentsAndUpdatedAt(t *testing.T) {
	base := CampaignDraft{
		ID: "id-2", Name: "Camp 2", Product: "mySTAR",
		Inputs:      CampaignInputs{PriceExTaxTHB: 150000, TermMonths: 18},
		Adjustments: CampaignAdjustments{CashDiscountTHB: 500},
		Metadata:    CampaignMetadata{CreatedAt: "2022-02-02T00:00:00Z"},
	}
	adj := CampaignAdjustments{
		CashDiscountTHB: 5000, SubdownTHB: 2000, IDCFreeInsuranceTHB: 1200, IDCFreeMBSPTHB: 700, IDCOtherTHB: 99,
	}
	now := "2025-09-29T00:00:00Z"

	got := UpdateDraftAdjustments(base, adj, now)

	if got.Adjustments != adj {
		t.Fatalf("Adjustments not updated: %#v", got.Adjustments)
	}
	if got.Metadata.UpdatedAt != now {
		t.Fatalf("UpdatedAt = %q; want %q", got.Metadata.UpdatedAt, now)
	}
	if got.Metadata.CreatedAt != base.Metadata.CreatedAt {
		t.Fatalf("CreatedAt changed: %q -> %q", base.Metadata.CreatedAt, got.Metadata.CreatedAt)
	}
	if got.ID != base.ID || got.Name != base.Name || got.Product != base.Product {
		t.Fatalf("Identity fields changed")
	}
	if got.Inputs != base.Inputs {
		t.Fatalf("Inputs should be unchanged")
	}
}

func TestUpdateDraft_UpdatesBothAndUpdatedAt(t *testing.T) {
	base := CampaignDraft{
		ID: "id-3", Name: "Camp 3", Product: "HP",
		Inputs:      CampaignInputs{PriceExTaxTHB: 90000, TermMonths: 12},
		Adjustments: CampaignAdjustments{CashDiscountTHB: 0},
		Metadata:    CampaignMetadata{CreatedAt: "2020-03-03T00:00:00Z"},
	}
	in := CampaignInputs{PriceExTaxTHB: 250000, DownpaymentPercent: 20, DownpaymentTHB: 50000, TermMonths: 36, BalloonPercent: 0, RateMode: "fixed_rate", CustomerRateAPR: 0.035}
	adj := CampaignAdjustments{CashDiscountTHB: 10000, SubdownTHB: 0, IDCFreeInsuranceTHB: 0, IDCFreeMBSPTHB: 0, IDCOtherTHB: 0}
	now := "2025-09-29T00:00:00Z"

	got := UpdateDraft(base, in, adj, now)

	if got.Inputs != in || got.Adjustments != adj {
		t.Fatalf("Inputs/Adjustments not updated")
	}
	if got.Metadata.UpdatedAt != now {
		t.Fatalf("UpdatedAt = %q; want %q", got.Metadata.UpdatedAt, now)
	}
	if got.Metadata.CreatedAt != base.Metadata.CreatedAt {
		t.Fatalf("CreatedAt changed")
	}
	if got.ID != base.ID || got.Name != base.Name || got.Product != base.Product {
		t.Fatalf("Identity fields changed")
	}
}

func TestBuildCampaignInputs_ConstructsFields(t *testing.T) {
	price := 321000.0
	in := BuildCampaignInputs(price, 25.0, 80250.0, 48, 5.0, "target_installment", 0.0299, 7999.0)

	if in.PriceExTaxTHB != price ||
		in.DownpaymentPercent != 25.0 ||
		in.DownpaymentTHB != 80250.0 ||
		in.TermMonths != 48 ||
		in.BalloonPercent != 5.0 ||
		in.RateMode != "target_installment" ||
		in.CustomerRateAPR != 0.0299 ||
		in.TargetInstallmentTHB != 7999.0 {
		t.Fatalf("BuildCampaignInputs mismatch: %#v", in)
	}
}

func TestSanitizeMonthly_ForRow(t *testing.T) {
	cases := []struct {
		in, want string
	}{
		{"THB 12,345.67", "12,345.67"},
		{"—", ""},
		{"12,345.67", "12,345.67"},
		{" THB    9,999 ", "9,999"},
		{"-", ""},
		{"", ""},
	}
	for _, c := range cases {
		got := sanitizeMonthlyForRow(c.in)
		if got != c.want {
			t.Fatalf("sanitizeMonthlyForRow(%q) = %q; want %q", c.in, got, c.want)
		}
	}
}
