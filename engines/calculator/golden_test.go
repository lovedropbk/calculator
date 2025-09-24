package calculator

import (
	"encoding/json"
	"errors"
	"flag"
	"os"
	"path/filepath"
	"testing"
	"time"

	"github.com/financial-calculator/engines/types"
	"github.com/shopspring/decimal"
	"github.com/stretchr/testify/require"
	"gopkg.in/yaml.v3"
)

var update = flag.Bool("update", false, "update golden files under testdata from current engine outputs")

func TestMain(m *testing.M) {
	// Ensure flags like -update are parsed
	flag.Parse()
	os.Exit(m.Run())
}

type yamlConfig struct {
	Version          string `yaml:"version"`
	CostOfFundsCurve []struct {
		TermMonths int     `yaml:"termMonths"`
		Rate       float64 `yaml:"rate"`
	} `yaml:"costOfFundsCurve"`
	MatchedFundedSpread float64 `yaml:"matchedFundedSpread"`
	EconomicCapital     struct {
		Ratio float64 `yaml:"ratio"`
	} `yaml:"economicCapital"`
	Opex struct {
		ByProductPct map[string]float64 `yaml:"byProductPct"`
	} `yaml:"opex"`
}

func discoverConfigPath() (string, error) {
	if p := os.Getenv("FC_CONFIG"); p != "" {
		if fileExists(p) {
			return p, nil
		}
	}
	// Search common relative locations (package dir => engines/calculator)
	candidates := []string{
		"config.yaml",       // if tests are invoked from repo root (rare)
		"../config.yaml",    // if working dir is engines/
		"../../config.yaml", // typical: package dir engines/calculator -> repo root
		filepath.Join("..", "..", "config.yaml"),
	}
	for _, c := range candidates {
		if fileExists(c) {
			return c, nil
		}
	}
	return "", errors.New("config.yaml not found (set FC_CONFIG or place config.yaml in repo root)")
}

func fileExists(p string) bool {
	st, err := os.Stat(p)
	return err == nil && !st.IsDir()
}

func loadEngineParamsFromYAML(path string) (types.ParameterSet, string, error) {
	raw, err := os.ReadFile(path)
	if err != nil {
		return types.ParameterSet{}, "", err
	}
	var yc yamlConfig
	if err := yaml.Unmarshal(raw, &yc); err != nil {
		return types.ParameterSet{}, "", err
	}

	ps := types.ParameterSet{
		ID:                 yc.Version,
		Version:            yc.Version,
		EffectiveDate:      time.Date(2025, 9, 1, 0, 0, 0, 0, time.UTC),
		DayCountConvention: "ACT/365",
		RoundingRules: types.RoundingRules{
			Currency:    "THB",
			MinorUnits:  0,
			Method:      "bank",
			DisplayRate: 4,
		},
	}

	// Cost of funds curve
	for _, pt := range yc.CostOfFundsCurve {
		ps.CostOfFundsCurve = append(ps.CostOfFundsCurve, types.RateCurvePoint{
			TermMonths: pt.TermMonths,
			Rate:       decimal.NewFromFloat(pt.Rate),
		})
	}

	// Matched funded spread
	ps.MatchedFundedSpread = decimal.NewFromFloat(yc.MatchedFundedSpread)

	// Economic capital ratio
	ps.EconomicCapitalParams = types.EconomicCapitalParams{
		BaseCapitalRatio:     decimal.NewFromFloat(yc.EconomicCapital.Ratio),
		CapitalAdvantage:     decimal.Zero,
		DTLAdvantage:         decimal.Zero,
		SecurityDepAdvantage: decimal.Zero,
		OtherAdvantage:       decimal.Zero,
	}

	// OPEX by product - map to engine keys: ProductCode + "_opex"
	ps.OPEXRates = make(map[string]decimal.Decimal)
	for k, v := range yc.Opex.ByProductPct {
		engineKey := mapProductKeyToEngineOpexKey(k)
		if engineKey != "" {
			ps.OPEXRates[engineKey] = decimal.NewFromFloat(v)
		}
	}

	return ps, yc.Version, nil
}

func mapProductKeyToEngineOpexKey(k string) string {
	switch k {
	case "HP", "HirePurchase", "Hire Purchase":
		return "HP_opex"
	case "mySTAR", "Balloon", "BalloonHP":
		return "mySTAR_opex"
	case "FinanceLease", "F-Lease", "Financing Lease", "Finance Lease":
		return "F-Lease_opex"
	case "OperatingLease", "Op-Lease", "Operating Lease":
		return "Op-Lease_opex"
	default:
		return ""
	}
}

type campaignSnap struct {
	ID          string `json:"id"`
	Type        string `json:"type"`
	Applied     bool   `json:"applied"`
	T0FlowTHB   string `json:"t0_flow_thb"`
	Description string `json:"desc"`
}

type snapshot struct {
	Name                   string         `json:"name"`
	Version                string         `json:"version"`
	Product                string         `json:"product"`
	TermMonths             int            `json:"term_months"`
	BalloonPercent         string         `json:"balloon_percent"`
	MonthlyInstallmentTHB  string         `json:"monthly_installment_thb"`
	CustomerRateNominal    string         `json:"customer_rate_nominal"`
	CustomerRateEffective  string         `json:"customer_rate_effective"`
	AcquisitionRoRAC       string         `json:"acquisition_rorac"`
	ScheduleLen            int            `json:"schedule_len"`
	FirstPaymentDate       string         `json:"first_payment_date"`
	Campaigns              []campaignSnap `json:"campaigns,omitempty"`
	ParameterSetVersion    string         `json:"parameter_set_version"`
	MatchedFundedSpreadBps string         `json:"matched_funded_spread"`
}

func toSnapshot(name string, deal types.Deal, ver string, quote types.Quote, mfSpread decimal.Decimal) snapshot {
	firstDate := ""
	if len(quote.Schedule) > 0 {
		firstDate = quote.Schedule[0].Date.Format("2006-01-02")
	}
	// helper formatters
	thb2 := func(d decimal.Decimal) string { return d.StringFixed(2) }
	rate4 := func(d decimal.Decimal) string { return d.StringFixed(4) }

	cs := make([]campaignSnap, 0, len(quote.CampaignAudit))
	for _, a := range quote.CampaignAudit {
		cs = append(cs, campaignSnap{
			ID:          a.CampaignID,
			Type:        string(a.CampaignType),
			Applied:     a.Applied,
			T0FlowTHB:   thb2(a.T0Flow),
			Description: a.Description,
		})
	}

	return snapshot{
		Name:                   name,
		Version:                ver,
		Product:                string(deal.Product),
		TermMonths:             deal.TermMonths,
		BalloonPercent:         rate4(deal.BalloonPercent),
		MonthlyInstallmentTHB:  thb2(quote.MonthlyInstallment),
		CustomerRateNominal:    rate4(quote.CustomerRateNominal),
		CustomerRateEffective:  rate4(quote.CustomerRateEffective),
		AcquisitionRoRAC:       rate4(quote.Profitability.AcquisitionRoRAC),
		ScheduleLen:            len(quote.Schedule),
		FirstPaymentDate:       firstDate,
		Campaigns:              cs,
		ParameterSetVersion:    ver,
		MatchedFundedSpreadBps: rate4(mfSpread),
	}
}

func writeOrCompareGolden(t *testing.T, path string, snap snapshot) {
	t.Helper()
	data, err := json.MarshalIndent(snap, "", "  ")
	require.NoError(t, err)

	if *update {
		require.NoError(t, os.MkdirAll(filepath.Dir(path), 0o755))
		require.NoError(t, os.WriteFile(path, data, 0o644))
		return
	}

	existing, err := os.ReadFile(path)
	if err != nil {
		t.Skipf("golden file %s missing; run `go test ./... -run Golden -args -update` to generate", path)
		return
	}
	require.Equal(t, string(existing), string(data), "golden mismatch for %s", path)
}

func TestGoldenScenarios(t *testing.T) {
	configPath, err := discoverConfigPath()
	require.NoError(t, err, "configuration not found; set FC_CONFIG or place config.yaml in repo root")

	params, version, err := loadEngineParamsFromYAML(configPath)
	require.NoError(t, err)

	calc := New(params)

	// Scenario 1: HP baseline 12m, 20% DP, balloon 0, fixed nominal 6.44%
	{
		price := decimal.NewFromFloat(1665576) // THB
		dpPct := decimal.NewFromFloat(0.20)
		dpAmt := price.Mul(dpPct).Round(0)
		financed := price.Sub(dpAmt)

		deal := types.Deal{
			Market:              "TH",
			Currency:            "THB",
			Product:             types.ProductHirePurchase,
			PriceExTax:          price,
			DownPaymentAmount:   dpAmt,
			DownPaymentPercent:  dpPct,
			DownPaymentLocked:   "percent",
			FinancedAmount:      financed,
			TermMonths:          12,
			BalloonPercent:      decimal.Zero,
			BalloonAmount:       decimal.Zero,
			Timing:              types.TimingArrears,
			PayoutDate:          time.Date(2025, 8, 4, 0, 0, 0, 0, time.UTC),
			FirstPaymentOffset:  0,
			RateMode:            "fixed_rate",
			CustomerNominalRate: decimal.NewFromFloat(0.0644),
		}
		req := types.CalculationRequest{
			Deal:         deal,
			Campaigns:    []types.Campaign{},
			IDCItems:     []types.IDCItem{},
			ParameterSet: params,
		}
		res, err := calc.Calculate(req)
		require.NoError(t, err)
		require.True(t, res.Success)

		snap := toSnapshot(
			"s1_hp_12m_baseline",
			deal, version, res.Quote, params.MatchedFundedSpread,
		)
		writeOrCompareGolden(t, filepath.Join("testdata", "s1_hp_12m_baseline.golden.json"), snap)
	}

	// Scenario 2: mySTAR 36m, 20% DP, 30% balloon, target installment 20,000 THB
	{
		price := decimal.NewFromFloat(1665576)
		dpPct := decimal.NewFromFloat(0.20)
		dpAmt := price.Mul(dpPct).Round(0)
		financed := price.Sub(dpAmt)

		// Using fixed nominal rate for stability in golden tests; compute balloon amount only.
		balloonAmt := price.Mul(decimal.NewFromFloat(0.30)).Round(0)

		deal := types.Deal{
			Market:              "TH",
			Currency:            "THB",
			Product:             types.ProductMySTAR,
			PriceExTax:          price,
			DownPaymentAmount:   dpAmt,
			DownPaymentPercent:  dpPct,
			DownPaymentLocked:   "percent",
			FinancedAmount:      financed,
			TermMonths:          36,
			BalloonPercent:      decimal.NewFromFloat(0.30),
			BalloonAmount:       balloonAmt,
			Timing:              types.TimingArrears,
			PayoutDate:          time.Date(2025, 8, 4, 0, 0, 0, 0, time.UTC),
			FirstPaymentOffset:  0,
			RateMode:            "fixed_rate",
			CustomerNominalRate: decimal.NewFromFloat(0.0600),
		}
		req := types.CalculationRequest{
			Deal:         deal,
			Campaigns:    []types.Campaign{},
			IDCItems:     []types.IDCItem{},
			ParameterSet: params,
		}
		res, err := calc.Calculate(req)
		require.NoError(t, err)
		require.True(t, res.Success)

		snap := toSnapshot(
			"s2_mystar_36m_b30",
			deal, version, res.Quote, params.MatchedFundedSpread,
		)
		writeOrCompareGolden(t, filepath.Join("testdata", "s2_mystar_36m_b30.golden.json"), snap)
	}

	// Scenario 3: HP 60m with Subinterest to nominal 5.90%
	{
		price := decimal.NewFromFloat(1665576)
		dpPct := decimal.NewFromFloat(0.20)
		dpAmt := price.Mul(dpPct).Round(0)
		financed := price.Sub(dpAmt)

		deal := types.Deal{
			Market:              "TH",
			Currency:            "THB",
			Product:             types.ProductHirePurchase,
			PriceExTax:          price,
			DownPaymentAmount:   dpAmt,
			DownPaymentPercent:  dpPct,
			DownPaymentLocked:   "percent",
			FinancedAmount:      financed,
			TermMonths:          60,
			BalloonPercent:      decimal.Zero,
			BalloonAmount:       decimal.Zero,
			Timing:              types.TimingArrears,
			PayoutDate:          time.Date(2025, 8, 4, 0, 0, 0, 0, time.UTC),
			FirstPaymentOffset:  0,
			RateMode:            "fixed_rate",
			CustomerNominalRate: decimal.NewFromFloat(0.0640), // base higher than target to activate subinterest
		}
		camps := []types.Campaign{
			{
				ID:         "SUBINT-TO-5P90",
				Type:       types.CampaignSubinterest,
				TargetRate: decimal.NewFromFloat(0.0590),
				Funder:     "Dealer",
				Stacking:   2,
			},
		}
		req := types.CalculationRequest{
			Deal:         deal,
			Campaigns:    camps,
			IDCItems:     []types.IDCItem{},
			ParameterSet: params,
		}
		res, err := calc.Calculate(req)
		require.NoError(t, err)
		require.True(t, res.Success)

		snap := toSnapshot(
			"s3_hp_60m_subinterest_5p90",
			deal, version, res.Quote, params.MatchedFundedSpread,
		)
		writeOrCompareGolden(t, filepath.Join("testdata", "s3_hp_60m_subinterest_5p90.golden.json"), snap)
	}
}
