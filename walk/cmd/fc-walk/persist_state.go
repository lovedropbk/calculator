package main

import (
	"encoding/json"
	"errors"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"sync"
	"time"

	"github.com/financial-calculator/engines/types"
	"github.com/lxn/walk"
)

// Schema versioning for persisted UI state
const CurrentSchemaVersion = 2

// StickyState holds user inputs we want to persist across sessions (versioned).
// Backward compatible: legacy fields retained; new fields use omitempty where appropriate.
type StickyState struct {
	// Version
	SchemaVersion int `json:"schemaVersion,omitempty"`

	// Core product/timing
	Product string `json:"product"`
	Timing  string `json:"timing"` // "arrears" | "advance"`

	// Price (legacy + v2)
	Price         float64  `json:"price"`                   // legacy
	PriceExTaxTHB *float64 `json:"priceExTaxTHB,omitempty"` // v2 preferred

	// Down payment (legacy + v2)
	DPUnit             string  `json:"dpUnit"`                       // "THB" | "%"
	DPValue            float64 `json:"dpValue"`                      // value in unit (legacy)
	DownpaymentUnit    string  `json:"downpaymentUnit,omitempty"`    // "PERCENT" | "THB"
	DownpaymentPercent float64 `json:"downpaymentPercent,omitempty"` // percent value
	DownpaymentTHB     float64 `json:"downpaymentTHB,omitempty"`     // THB value

	// Term (legacy + v2)
	Term       int `json:"term"`
	TermMonths int `json:"termMonths,omitempty"`

	// Balloon (legacy + v2)
	BalloonUnit    string  `json:"balloonUnit"`
	BalloonValue   float64 `json:"balloonValue"`
	BalloonPercent float64 `json:"balloonPercent,omitempty"`
	BalloonTHB     float64 `json:"balloonTHB,omitempty"`

	// Rate mode and rate values
	RateMode             string  `json:"rateMode"`                       // "fixed_rate" | "target_installment"
	CustomerRatePct      float64 `json:"customerRatePct"`                // % p.a. (legacy)
	CustomerRateAPR      float64 `json:"customerRateAPR,omitempty"`      // % p.a. (v2 alias)
	TargetInstallment    float64 `json:"targetInstallment"`              // THB (legacy)
	TargetInstallmentTHB float64 `json:"targetInstallmentTHB,omitempty"` // THB (v2 alias)

	// Budget / IDC
	SubsidyBudgetTHB float64 `json:"subsidyBudgetTHB"` // THB

	// IDC Commission persistence (UI/view-model only)
	IDCCommissionMode    string  `json:"idcCommissionMode,omitempty"`    // "AUTO" | "MANUAL"
	IDCCommissionPercent float64 `json:"idcCommissionPercent,omitempty"` // as percent (e.g., 3.00 means 3%)
	IDCCommissionTHB     float64 `json:"idcCommissionTHB,omitempty"`     // THB override

	IDCOtherTHB float64 `json:"idcOtherTHB"` // THB

	// Optional nicety
	SelectedCampaignIx    int  `json:"selectedCampaignIx"`              // legacy
	SelectedCampaignIndex *int `json:"selectedCampaignIndex,omitempty"` // v2
}

func stateFilePath() (string, error) {
	// Prefer %AppData%\FinancialCalculator\state.json on Windows
	if appData := os.Getenv("APPDATA"); strings.TrimSpace(appData) != "" {
		dir := filepath.Join(appData, "FinancialCalculator")
		if err := os.MkdirAll(dir, 0755); err == nil {
			return filepath.Join(dir, "state.json"), nil
		}
	}
	// Fallback to local walk/bin
	if err := os.MkdirAll("walk/bin", 0755); err != nil {
		return "", err
	}
	return "walk/bin/state.json", nil
}

func LoadStickyState() (StickyState, bool) {
	var s StickyState
	fp, err := stateFilePath()
	if err != nil {
		return s, false
	}
	data, err := os.ReadFile(fp)
	if err != nil {
		return s, false
	}
	if err := json.Unmarshal(data, &s); err != nil {
		return StickyState{}, false
	}
	return s, true
}

func SaveStickyState(s StickyState) error {
	fp, err := stateFilePath()
	if err != nil {
		return err
	}
	b, err := json.MarshalIndent(s, "", "  ")
	if err != nil {
		return err
	}
	tmp := fp + ".tmp"
	if err := os.WriteFile(tmp, b, 0644); err != nil {
		return err
	}
	return os.Rename(tmp, fp)
}

// Debounced save of StickyState to avoid frequent disk writes during typing.
var (
	stickySaveMu     sync.Mutex
	stickySaveTimer  *time.Timer
	stickySaveDelay  = 300 * time.Millisecond
	stickySaveLatest StickyState
)

// ScheduleStickySave queues a debounced save. Subsequent calls reset the timer.
func ScheduleStickySave(s StickyState) {
	stickySaveMu.Lock()
	defer stickySaveMu.Unlock()
	stickySaveLatest = s
	if stickySaveTimer != nil {
		_ = stickySaveTimer.Stop()
	}
	stickySaveTimer = time.AfterFunc(stickySaveDelay, func() {
		stickySaveMu.Lock()
		latest := stickySaveLatest
		stickySaveMu.Unlock()
		_ = SaveStickyState(latest)
	})
}

// FlushStickySave cancels any pending debounce and writes immediately.
func FlushStickySave(s StickyState) {
	stickySaveMu.Lock()
	if stickySaveTimer != nil {
		_ = stickySaveTimer.Stop()
		stickySaveTimer = nil
	}
	stickySaveMu.Unlock()
	_ = SaveStickyState(s)
}

// Helpers to safely read current UI values

func textFloat(le *walk.LineEdit) float64 {
	if le == nil {
		return 0
	}
	s := strings.ReplaceAll(strings.TrimSpace(le.Text()), ",", "")
	f, _ := strconv.ParseFloat(s, 64)
	return f
}

func textInt(le *walk.LineEdit) int {
	if le == nil {
		return 0
	}
	s := strings.ReplaceAll(strings.TrimSpace(le.Text()), ",", "")
	i, _ := strconv.Atoi(s)
	return i
}

// CollectStickyFromUI builds StickyState from current controls.
func CollectStickyFromUI(
	product string,
	priceEdit *walk.LineEdit,
	dpUnitCmb *walk.ComboBox, dpValueEd *walk.NumberEdit,
	termEdit *walk.LineEdit,
	timing string,
	balloonUnit string, balloonUnitCmb *walk.ComboBox, balloonSelectedEd *walk.NumberEdit,
	rateMode string,
	nominalRateEdit *walk.LineEdit,
	targetInstallmentEdit *walk.LineEdit,
	subsidyBudgetEd, idcOtherEd *walk.NumberEdit,
	selectedCampaignIdx int,
	dealState types.DealState,
) (StickyState, error) {
	if priceEdit == nil || termEdit == nil || dpUnitCmb == nil || dpValueEd == nil || balloonSelectedEd == nil {
		return StickyState{}, errors.New("controls not initialized")
	}

	// Price
	price := textFloat(priceEdit)

	// Downpayment: determine unit and compute both percent and THB
	dpUnit := "%"
	if dpUnitCmb != nil && dpUnitCmb.Text() != "" {
		dpUnit = dpUnitCmb.Text()
	}
	dpVal := 0.0
	if dpValueEd != nil {
		dpVal = dpValueEd.Value()
	}
	dpPercent := 0.0
	dpTHB := 0.0
	if dpUnit == "%" {
		dpPercent = dpVal
		dpTHB = price * (dpPercent / 100.0)
	} else {
		dpTHB = dpVal
		if price > 0 {
			dpPercent = (dpTHB / price) * 100.0
		}
	}

	// Balloon: determine unit and compute both percent and THB
	bu := balloonUnit
	if balloonUnitCmb != nil && balloonUnitCmb.Text() != "" {
		bu = balloonUnitCmb.Text()
	}
	balloonVal := 0.0
	if balloonSelectedEd != nil {
		balloonVal = balloonSelectedEd.Value()
	}
	balloonPct := 0.0
	balloonTHB := 0.0
	if bu == "%" {
		balloonPct = balloonVal
		balloonTHB = RoundTo(price*(balloonPct/100.0), 0)
	} else {
		balloonTHB = balloonVal
		if price > 0 {
			balloonPct = RoundTo((balloonTHB/price)*100.0, 2)
		}
	}

	// Customer rate and target installment
	custAPR := textFloat(nominalRateEdit)
	targetInstall := textFloat(targetInstallmentEdit)

	// Persisted state
	s := StickyState{
		SchemaVersion: CurrentSchemaVersion,

		Product: product,
		Timing:  timing,

		Price:         price,
		PriceExTaxTHB: func(v float64) *float64 { return &v }(price),

		DPUnit:  dpUnit,
		DPValue: dpVal,
		DownpaymentUnit: func() string {
			if dpUnit == "%" {
				return "PERCENT"
			} else {
				return "THB"
			}
		}(),
		DownpaymentPercent: dpPercent,
		DownpaymentTHB:     dpTHB,

		Term:       textInt(termEdit),
		TermMonths: textInt(termEdit),

		BalloonUnit:    bu,
		BalloonValue:   balloonVal,
		BalloonPercent: balloonPct,
		BalloonTHB:     balloonTHB,

		RateMode:             rateMode,
		CustomerRatePct:      custAPR,
		CustomerRateAPR:      custAPR,
		TargetInstallment:    targetInstall,
		TargetInstallmentTHB: targetInstall,

		SubsidyBudgetTHB: func() float64 {
			if subsidyBudgetEd != nil {
				return subsidyBudgetEd.Value()
			}
			return 0
		}(),
		IDCOtherTHB: func() float64 {
			if idcOtherEd != nil {
				return idcOtherEd.Value()
			}
			return 0
		}(),
		SelectedCampaignIx:    selectedCampaignIdx,
		SelectedCampaignIndex: func(ix int) *int { return &ix }(selectedCampaignIdx),
	}

	// IDC Commission persistence from DealState (UI)
	if dealState.DealerCommission.Mode == types.DealerCommissionModeOverride {
		s.IDCCommissionMode = "MANUAL"
		if dealState.DealerCommission.Amt != nil {
			s.IDCCommissionTHB = *dealState.DealerCommission.Amt
			s.IDCCommissionPercent = 0
		} else if dealState.DealerCommission.Pct != nil {
			s.IDCCommissionPercent = *dealState.DealerCommission.Pct * 100.0 // store as percent
			s.IDCCommissionTHB = 0
		}
	} else {
		s.IDCCommissionMode = "AUTO"
		s.IDCCommissionTHB = 0
		s.IDCCommissionPercent = 0
	}

	return s, nil
}
