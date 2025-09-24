package parameters

import (
	"os"
	"path/filepath"
	"testing"
)

func TestCommissionPercentByProduct_KnownAndUnknown(t *testing.T) {
	s := &Service{
		cache: map[string]*ParameterSet{
			"test": {
				CommissionPolicy: CommissionPolicy{
					ByProductPct: map[string]float64{"HP": 0.015, "F-Lease": -0.01},
					Version:      "2025.09",
				},
			},
		},
		currentID: "test",
	}

	if got := s.CommissionPercentByProduct("HP"); got != 0.015 {
		t.Fatalf("CommissionPercentByProduct(HP) = %v, want 0.015", got)
	}
	if got := s.CommissionPercentByProduct("Unknown"); got != 0.0 {
		t.Fatalf("CommissionPercentByProduct(Unknown) = %v, want 0.0", got)
	}
	if got := s.CommissionPercentByProduct("F-Lease"); got != 0.0 {
		t.Fatalf("CommissionPercentByProduct(F-Lease) = %v, want 0.0 (negative clamped)", got)
	}
}

func TestCommissionPolicyVersion(t *testing.T) {
	s := &Service{
		cache: map[string]*ParameterSet{
			"test": {
				CommissionPolicy: CommissionPolicy{
					ByProductPct: map[string]float64{"HP": 0.015},
					Version:      "2025.09",
				},
			},
		},
		currentID: "test",
	}
	if got := s.CommissionPolicyVersion(); got != "2025.09" {
		t.Fatalf("CommissionPolicyVersion() = %q, want %q", got, "2025.09")
	}

	var empty Service
	if got := empty.CommissionPolicyVersion(); got != "" {
		t.Fatalf("empty Service CommissionPolicyVersion() = %q, want empty string", got)
	}
}

func TestLoadParameterSetFromYAML_Sample(t *testing.T) {
	// Load central repo config.yaml to avoid duplication
	p := filepath.Clean(filepath.Join("..", "config.yaml"))
	ps, err := LoadParameterSetFromYAML(p)
	if err != nil {
		t.Fatalf("LoadParameterSetFromYAML(%s) error: %v", p, err)
	}
	if ps == nil {
		t.Fatalf("LoadParameterSetFromYAML returned nil ParameterSet")
	}
	if ps.ID != "2025-09-sample" {
		t.Fatalf("ps.ID = %q, want %q", ps.ID, "2025-09-sample")
	}
	// CoF
	wantTerms := []int{12, 24, 36, 48, 60}
	for _, term := range wantTerms {
		if _, ok := ps.CostOfFunds[term]; !ok {
			t.Fatalf("CostOfFunds missing term %d", term)
		}
	}
	// EC ratio
	if ps.EconomicCapital.BaseCapitalRatio != 0.08 {
		t.Fatalf("EconomicCapital.BaseCapitalRatio = %v, want 0.08", ps.EconomicCapital.BaseCapitalRatio)
	}
	// OPEX mapping (FinanceLease -> F-Lease, OperatingLease -> Op-Lease)
	if ps.OPEXRates["HP"] != 0.0068 ||
		ps.OPEXRates["mySTAR"] != 0.0072 ||
		ps.OPEXRates["F-Lease"] != 0.0065 ||
		ps.OPEXRates["Op-Lease"] != 0.0070 {
		t.Fatalf("OPEXRates mapping mismatch: %v", ps.OPEXRates)
	}
	// Commission policy
	if ps.CommissionPolicy.ByProductPct["HP"] != 0.03 ||
		ps.CommissionPolicy.ByProductPct["mySTAR"] != 0.07 ||
		ps.CommissionPolicy.ByProductPct["FinanceLease"] != 0.07 ||
		ps.CommissionPolicy.ByProductPct["OperatingLease"] != 0.07 {
		t.Fatalf("Commission policy mapping mismatch: %v", ps.CommissionPolicy.ByProductPct)
	}
}

func TestLoadParameterSetFromYAML_Invalid(t *testing.T) {
	// Missing version should yield a clear error
	tmp := t.TempDir()
	badPath := filepath.Join(tmp, "bad.yaml")
	data := []byte(`
costOfFundsCurve:
  - termMonths: -12
    rate: 0.015
matchedFundedSpread: 0.002
economicCapital:
  ratio: 0.10
opex:
  byProductPct: { HP: 0.006 }
`)
	if err := os.WriteFile(badPath, data, 0644); err != nil {
		t.Fatalf("write temp bad yaml: %v", err)
	}
	if _, err := LoadParameterSetFromYAML(badPath); err == nil {
		t.Fatalf("expected error for invalid YAML (missing version, bad termMonths), got nil")
	}
}
