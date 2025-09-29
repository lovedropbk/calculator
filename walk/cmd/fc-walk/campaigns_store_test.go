package main

import (
	"encoding/json"
	"errors"
	"os"
	"path/filepath"
	"reflect"
	"testing"
	"time"
)

// useTempResolver overrides campaignsPathResolver to point into t.TempDir().
// It returns the resolved campaigns.json path for convenience.
func useTempResolver(t *testing.T) string {
	t.Helper()
	dir := t.TempDir()
	path := filepath.Join(dir, "campaigns.json")
	prev := campaignsPathResolver
	campaignsPathResolver = func() (string, error) { return path, nil }
	t.Cleanup(func() { campaignsPathResolver = prev })
	return path
}

func TestLoadMissingFileReturnsEmpty(t *testing.T) {
	_ = useTempResolver(t)

	list, ver, err := LoadCampaigns()
	if err != nil {
		t.Fatalf("LoadCampaigns unexpected error: %v", err)
	}
	if ver != CampaignsFileVersion {
		t.Fatalf("version: got %d want %d", ver, CampaignsFileVersion)
	}
	if len(list) != 0 {
		t.Fatalf("expected empty list on missing file; got %d", len(list))
	}
}

func TestSaveThenLoadRoundTrip(t *testing.T) {
	_ = useTempResolver(t)

	now := time.Now().UTC().Format(time.RFC3339)
	in := []CampaignDraft{
		{
			ID:      "11111111-1111-1111-1111-111111111111",
			Name:    "Campaign A",
			Product: "HP",
			Inputs: CampaignInputs{
				PriceExTaxTHB:        1_000_000,
				DownpaymentPercent:   20,
				DownpaymentTHB:       200_000,
				TermMonths:           36,
				BalloonPercent:       0,
				RateMode:             "fixed_rate",
				CustomerRateAPR:      0.0399,
				TargetInstallmentTHB: 0,
			},
			Adjustments: CampaignAdjustments{
				CashDiscountTHB:     0,
				SubdownTHB:          50_000,
				IDCFreeInsuranceTHB: 0,
				IDCFreeMBSPTHB:      0,
			},
			Metadata: CampaignMetadata{
				CreatedAt: now,
				UpdatedAt: now,
				Version:   1,
			},
		},
		{
			ID:      "22222222-2222-2222-2222-222222222222",
			Name:    "Campaign B",
			Product: "HP",
			Inputs: CampaignInputs{
				PriceExTaxTHB:        750_000,
				DownpaymentPercent:   15,
				DownpaymentTHB:       112_500,
				TermMonths:           48,
				BalloonPercent:       10,
				RateMode:             "target_installment",
				CustomerRateAPR:      0.0,
				TargetInstallmentTHB: 21_000,
			},
			Adjustments: CampaignAdjustments{
				CashDiscountTHB:     5_000,
				SubdownTHB:          0,
				IDCFreeInsuranceTHB: 0,
				IDCFreeMBSPTHB:      0,
			},
			Metadata: CampaignMetadata{
				CreatedAt: now,
				UpdatedAt: now,
				Version:   1,
			},
		},
	}

	if err := SaveCampaigns(in); err != nil {
		t.Fatalf("SaveCampaigns error: %v", err)
	}

	out, ver, err := LoadCampaigns()
	if err != nil {
		t.Fatalf("LoadCampaigns error: %v", err)
	}
	if ver != CampaignsFileVersion {
		t.Fatalf("version: got %d want %d", ver, CampaignsFileVersion)
	}
	if !reflect.DeepEqual(out, in) {
		t.Fatalf("round-trip mismatch:\n got: %+v\nwant: %+v", out, in)
	}
}

func TestCorruptedJSONReturnsError(t *testing.T) {
	p := useTempResolver(t)

	// Write garbage over the expected file path.
	if err := os.WriteFile(p, []byte("not a json"), 0o644); err != nil {
		t.Fatalf("write corrupted file: %v", err)
	}

	list, ver, err := LoadCampaigns()
	if err == nil {
		t.Fatalf("expected error on corrupted JSON, got nil")
	}
	if list != nil {
		t.Fatalf("expected nil slice on corrupted JSON, got: %+v", list)
	}
	if ver != 0 {
		t.Fatalf("expected version=0 on corrupted JSON, got %d", ver)
	}
}

func TestFutureVersionTolerance(t *testing.T) {
	p := useTempResolver(t)

	payload := campaignsFile{
		FileVersion: 999,
		Campaigns: []CampaignDraft{{
			ID:      "33333333-3333-3333-3333-333333333333",
			Name:    "Future Version",
			Product: "HP",
			Inputs: CampaignInputs{
				PriceExTaxTHB: 500_000,
				TermMonths:    12,
				RateMode:      "fixed_rate",
			},
			Metadata: CampaignMetadata{
				CreatedAt: time.Now().UTC().Format(time.RFC3339),
				UpdatedAt: time.Now().UTC().Format(time.RFC3339),
				Version:   1,
			},
		}},
	}
	b, err := json.MarshalIndent(payload, "", "  ")
	if err != nil {
		t.Fatalf("marshal payload: %v", err)
	}
	if err := os.WriteFile(p, b, 0o644); err != nil {
		t.Fatalf("write file: %v", err)
	}

	out, ver, err := LoadCampaigns()
	if !errors.Is(err, ErrFutureVersion) {
		t.Fatalf("expected ErrFutureVersion, got: %v", err)
	}
	if ver != payload.FileVersion {
		t.Fatalf("version passthrough mismatch: got %d want %d", ver, payload.FileVersion)
	}
	if len(out) != 1 || out[0].Name != "Future Version" {
		t.Fatalf("unexpected load result: ver=%d out=%+v", ver, out)
	}
}

func TestAtomicWriteLeavesNoTmp(t *testing.T) {
	p := useTempResolver(t)

	in := []CampaignDraft{{
		ID:      "44444444-4444-4444-4444-444444444444",
		Name:    "Atomic Test",
		Product: "HP",
		Inputs: CampaignInputs{
			RateMode: "fixed_rate",
		},
		Metadata: CampaignMetadata{
			CreatedAt: time.Now().UTC().Format(time.RFC3339),
			UpdatedAt: time.Now().UTC().Format(time.RFC3339),
			Version:   1,
		},
	}}

	if err := SaveCampaigns(in); err != nil {
		t.Fatalf("SaveCampaigns error: %v", err)
	}

	// Final file must exist.
	if _, err := os.Stat(p); err != nil {
		t.Fatalf("final file missing: %v", err)
	}
	// Temp file must not remain.
	if _, err := os.Stat(p + ".tmp"); err == nil {
		t.Fatalf("temp file still exists after atomic write")
	} else if !os.IsNotExist(err) {
		t.Fatalf("unexpected error checking tmp existence: %v", err)
	}
}
