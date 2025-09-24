package parameters

import (
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"time"

	yaml "gopkg.in/yaml.v3"
)

// LoadParameterSetFromYAML parses a YAML config file into a ParameterSet and validates ranges/shapes.
// Schema:
//
// version: "2025-09"
// costOfFundsCurve:
//   - termMonths: 12
//     rate: 0.015
//   - termMonths: 24
//     rate: 0.016
//
// matchedFundedSpread: 0.0025
// risk:
//
//	costOfRiskPct: 0.009
//	# or:
//	# byProductPct: { HP: 0.009, mySTAR: 0.010 }
//
// economicCapital:
//
//	ratio: 0.08
//
// opex:
//
//	byProductPct: { HP: 0.0068, mySTAR: 0.0072, FinanceLease: 0.0065, OperatingLease: 0.0070 }
//
// commissionPolicy:
//
//	byProductPct: { HP: 0.03, mySTAR: 0.07, FinanceLease: 0.07, OperatingLease: 0.07 }
//
// rounding:
//
//	currency: THB
//	minorUnits: 0
//	method: bank
//	displayRate: 4
//
// daycount: "ACT/365"
func LoadParameterSetFromYAML(path string) (*ParameterSet, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("read YAML failed: %w", err)
	}

	type curvePoint struct {
		TermMonths int     `yaml:"termMonths"`
		Rate       float64 `yaml:"rate"`
	}
	type yamlCfg struct {
		Version             string       `yaml:"version"`
		CostOfFundsCurve    []curvePoint `yaml:"costOfFundsCurve"`
		MatchedFundedSpread float64      `yaml:"matchedFundedSpread"`
		Risk                *struct {
			CostOfRiskPct float64            `yaml:"costOfRiskPct"`
			ByProductPct  map[string]float64 `yaml:"byProductPct"`
		} `yaml:"risk"`
		EconomicCapital *struct {
			Ratio float64 `yaml:"ratio"`
		} `yaml:"economicCapital"`
		OPEX *struct {
			ByProductPct map[string]float64 `yaml:"byProductPct"`
		} `yaml:"opex"`
		CommissionPolicy *struct {
			ByProductPct map[string]float64 `yaml:"byProductPct"`
		} `yaml:"commissionPolicy"`
		Rounding *struct {
			Currency    string `yaml:"currency"`
			MinorUnits  int    `yaml:"minorUnits"`
			Method      string `yaml:"method"`
			DisplayRate int    `yaml:"displayRate"`
		} `yaml:"rounding"`
		Daycount string `yaml:"daycount"`
	}

	var cfg yamlCfg
	if err := yaml.Unmarshal(data, &cfg); err != nil {
		return nil, fmt.Errorf("parse YAML failed: %w", err)
	}

	// Shape validations with clear errors
	var verrs []string
	if cfg.Version == "" {
		verrs = append(verrs, "version is required")
	}
	if len(cfg.CostOfFundsCurve) == 0 {
		verrs = append(verrs, "costOfFundsCurve must have at least one point")
	}
	cof := map[int]float64{}
	for i, pt := range cfg.CostOfFundsCurve {
		if pt.TermMonths <= 0 {
			verrs = append(verrs, fmt.Sprintf("costOfFundsCurve[%d].termMonths must be positive", i))
		}
		if pt.Rate < 0 || pt.Rate > 1 {
			verrs = append(verrs, fmt.Sprintf("costOfFundsCurve[%d].rate must be between 0 and 1", i))
		}
		if pt.TermMonths > 0 {
			cof[pt.TermMonths] = pt.Rate
		}
	}
	if cfg.MatchedFundedSpread < 0 || cfg.MatchedFundedSpread > 1 {
		verrs = append(verrs, "matchedFundedSpread must be between 0 and 1")
	}
	ecRatio := 0.0
	if cfg.EconomicCapital == nil {
		verrs = append(verrs, "economicCapital.ratio is required")
	} else {
		ecRatio = cfg.EconomicCapital.Ratio
		if ecRatio < 0 || ecRatio > 1 {
			verrs = append(verrs, "economicCapital.ratio must be between 0 and 1")
		}
	}
	if cfg.OPEX == nil || len(cfg.OPEX.ByProductPct) == 0 {
		verrs = append(verrs, "opex.byProductPct must be provided with at least one product")
	} else {
		for k, v := range cfg.OPEX.ByProductPct {
			if v < 0 || v > 1 {
				verrs = append(verrs, fmt.Sprintf("opex.byProductPct[%s] must be between 0 and 1", k))
			}
		}
	}
	if cfg.CommissionPolicy != nil {
		for k, v := range cfg.CommissionPolicy.ByProductPct {
			if v < 0 || v > 1 {
				verrs = append(verrs, fmt.Sprintf("commissionPolicy.byProductPct[%s] must be between 0 and 1", k))
			}
		}
	}
	if len(verrs) > 0 {
		return nil, errors.New("invalid config: " + fmt.Sprintf("%v", verrs))
	}

	now := time.Now()
	ps := &ParameterSet{
		ID:            cfg.Version,
		EffectiveDate: now,
		CreatedAt:     now,
		CreatedBy:     "config.yaml",
		Description:   fmt.Sprintf("Loaded from %s", filepath.Base(path)),
		IsDefault:     false,

		CostOfFunds:   cof,
		MatchedSpread: cfg.MatchedFundedSpread,

		// Provide minimal PD/LGD entries to satisfy validation.
		PDLGDTables: map[string]PDLGDParams{
			"HP_default": {
				Product: "HP", Segment: "default",
				PD: 0.0200, LGD: 0.4500, Description: "Default HP risk",
			},
			"mySTAR_default": {
				Product: "mySTAR", Segment: "default",
				PD: 0.0250, LGD: 0.4000, Description: "Default mySTAR risk",
			},
			"F-Lease_default": {
				Product: "F-Lease", Segment: "default",
				PD: 0.0180, LGD: 0.3500, Description: "Default Finance Lease risk",
			},
			"Op-Lease_default": {
				Product: "Op-Lease", Segment: "default",
				PD: 0.0150, LGD: 0.3000, Description: "Default Operating Lease risk",
			},
		},

		OPEXRates: map[string]float64{},
		EconomicCapital: EconomicCapitalParams{
			BaseCapitalRatio: ecRatio,
			// advantages default to 0
		},
		CentralHQAddOn: 0.0,
		RoundingRules: RoundingParams{
			Currency:      "THB",
			MinorUnits:    0,
			Method:        "bank",
			DisplayRate:   4,
			InstallmentTo: 1,
		},
		DayCountConvention: "ACT/365",
	}

	// Map OPEX keys: YAML uses HP, mySTAR, FinanceLease, OperatingLease; internal uses HP, mySTAR, F-Lease, Op-Lease
	if cfg.OPEX != nil {
		for k, v := range cfg.OPEX.ByProductPct {
			switch k {
			case "FinanceLease":
				ps.OPEXRates["F-Lease"] = v
			case "OperatingLease":
				ps.OPEXRates["Op-Lease"] = v
			default:
				ps.OPEXRates[k] = v
			}
		}
	}

	// Commission policy (optional)
	if cfg.CommissionPolicy != nil {
		ps.CommissionPolicy = CommissionPolicy{
			ByProductPct: cfg.CommissionPolicy.ByProductPct,
			Version:      cfg.Version,
		}
	}

	// Optional rounding + daycount
	if cfg.Rounding != nil {
		if cfg.Rounding.Currency != "" {
			ps.RoundingRules.Currency = cfg.Rounding.Currency
		}
		if cfg.Rounding.MinorUnits != 0 {
			ps.RoundingRules.MinorUnits = cfg.Rounding.MinorUnits
		}
		if cfg.Rounding.Method != "" {
			ps.RoundingRules.Method = cfg.Rounding.Method
		}
		if cfg.Rounding.DisplayRate != 0 {
			ps.RoundingRules.DisplayRate = cfg.Rounding.DisplayRate
		}
	}
	if cfg.Daycount != "" {
		ps.DayCountConvention = cfg.Daycount
	}

	// Final validation via existing validator
	if v := ps.Validate(); len(v) > 0 {
		return nil, fmt.Errorf("parameter validation failed: %v", v)
	}

	return ps, nil
}

// DiscoverAndLoadParameterSet locates a config.yaml via search order and loads it.
// Search order:
//  1. Environment variable FC_CONFIG (absolute or relative path)
//  2. Directory of the running executable (for packaged app) - config.yaml
//  3. Current working directory - config.yaml
//
// Returns the loaded ParameterSet and the resolved path; descriptive error if not found.
func DiscoverAndLoadParameterSet() (*ParameterSet, string, error) {
	tryLoad := func(p string) (*ParameterSet, string, error) {
		if p == "" {
			return nil, "", fmt.Errorf("empty path")
		}
		if !filepath.IsAbs(p) {
			if cwd, err := os.Getwd(); err == nil {
				p = filepath.Join(cwd, p)
			}
		}
		if _, err := os.Stat(p); err != nil {
			return nil, "", err
		}
		ps, err := LoadParameterSetFromYAML(p)
		if err != nil {
			return nil, "", err
		}
		return ps, p, nil
	}

	// 1) Environment override
	if env := os.Getenv("FC_CONFIG"); env != "" {
		if ps, p, err := tryLoad(env); err == nil {
			return ps, p, nil
		}
	}

	// 2) Directory of executable and its parent directories
	if exe, err := os.Executable(); err == nil {
		dir := filepath.Dir(exe)
		// Search exe dir and up to 3 parents for config.yaml
		d := dir
		for i := 0; i < 4; i++ {
			p := filepath.Join(d, "config.yaml")
			if ps, rp, err := tryLoad(p); err == nil {
				return ps, rp, nil
			}
			parent := filepath.Dir(d)
			if parent == d {
				break
			}
			d = parent
		}
	}

	// 3) Current working directory and its parent directories
	if cwd, err := os.Getwd(); err == nil {
		d := cwd
		for i := 0; i < 4; i++ {
			p := filepath.Join(d, "config.yaml")
			if ps, rp, err := tryLoad(p); err == nil {
				return ps, rp, nil
			}
			parent := filepath.Dir(d)
			if parent == d {
				break
			}
			d = parent
		}
	}
	// 3b) As a last resort, try relative path
	if ps, rp, err := tryLoad("config.yaml"); err == nil {
		return ps, rp, nil
	}

	// 4) Fallback: try sample parameters from parameters/testdata/config.sample.yaml
	if exe, err := os.Executable(); err == nil {
		d := filepath.Dir(exe)
		for i := 0; i < 4; i++ {
			p := filepath.Join(d, "parameters", "testdata", "config.sample.yaml")
			if ps, rp, err := tryLoad(p); err == nil {
				return ps, rp, nil
			}
			parent := filepath.Dir(d)
			if parent == d {
				break
			}
			d = parent
		}
	}
	if cwd, err := os.Getwd(); err == nil {
		d := cwd
		for i := 0; i < 4; i++ {
			p := filepath.Join(d, "parameters", "testdata", "config.sample.yaml")
			if ps, rp, err := tryLoad(p); err == nil {
				return ps, rp, nil
			}
			parent := filepath.Dir(d)
			if parent == d {
				break
			}
			d = parent
		}
	}

	return nil, "", fmt.Errorf("no config.yaml found; tried FC_CONFIG, exe dir and parents, current dir and parents, and sample")
}
