package main

import (
	"encoding/json"
	"errors"
	"os"
	"path/filepath"
	"strconv"
	"strings"

	"github.com/lxn/walk"
)

// StickyState holds user inputs we want to persist across sessions.
type StickyState struct {
	Product            string  `json:"product"`
	Price              float64 `json:"price"`
	DPUnit             string  `json:"dpUnit"`  // "THB" | "%"
	DPValue            float64 `json:"dpValue"` // value in unit
	Term               int     `json:"term"`
	Timing             string  `json:"timing"` // "arrears" | "advance"
	BalloonUnit        string  `json:"balloonUnit"`
	BalloonValue       float64 `json:"balloonValue"`
	RateMode           string  `json:"rateMode"`           // "fixed_rate" | "target_installment"
	CustomerRatePct    float64 `json:"customerRatePct"`    // % p.a.
	TargetInstallment  float64 `json:"targetInstallment"`  // THB
	SubsidyBudgetTHB   float64 `json:"subsidyBudgetTHB"`   // THB
	IDCOtherTHB        float64 `json:"idcOtherTHB"`        // THB
	SelectedCampaignIx int     `json:"selectedCampaignIx"` // TableView selection
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
	balloonUnit string, balloonUnitCmb *walk.ComboBox, balloonValueEd *walk.NumberEdit,
	rateMode string,
	nominalRateEdit *walk.LineEdit,
	targetInstallmentEdit *walk.LineEdit,
	subsidyBudgetEd, idcOtherEd *walk.NumberEdit,
	selectedCampaignIdx int,
) (StickyState, error) {
	if priceEdit == nil || termEdit == nil || dpUnitCmb == nil || dpValueEd == nil || balloonValueEd == nil {
		return StickyState{}, errors.New("controls not initialized")
	}
	dpUnit := "%"
	if dpUnitCmb != nil && dpUnitCmb.Text() != "" {
		dpUnit = dpUnitCmb.Text()
	}
	bu := balloonUnit
	if balloonUnitCmb != nil && balloonUnitCmb.Text() != "" {
		bu = balloonUnitCmb.Text()
	}
	s := StickyState{
		Product:           product,
		Price:             textFloat(priceEdit),
		DPUnit:            dpUnit,
		DPValue:           dpValueEd.Value(),
		Term:              textInt(termEdit),
		Timing:            timing,
		BalloonUnit:       bu,
		BalloonValue:      balloonValueEd.Value(),
		RateMode:          rateMode,
		CustomerRatePct:   textFloat(nominalRateEdit),
		TargetInstallment: textFloat(targetInstallmentEdit),
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
		SelectedCampaignIx: selectedCampaignIdx,
	}
	return s, nil
}
