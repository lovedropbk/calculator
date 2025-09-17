//go:build windows

package main

import (
	"fmt"
	"log"
	"os"
	"runtime"
	"runtime/debug"
	"strconv"
	"strings"
	"time"
	"unsafe"

	"github.com/financial-calculator/engines/calculator"
	"github.com/financial-calculator/engines/types"
	"github.com/lxn/walk"
	. "github.com/lxn/walk/declarative"
	"github.com/lxn/win"
)

func main() {
	// Lock the GUI to the main OS thread
	runtime.LockOSThread()

	// Startup logging setup
	_ = os.MkdirAll("walk/bin", 0755)
	logPath := "walk/bin/startup.log"
	logFile, lfErr := os.OpenFile(logPath, os.O_CREATE|os.O_APPEND|os.O_WRONLY, 0644)
	var logger *log.Logger
	if lfErr != nil {
		logger = log.New(os.Stderr, "", log.LstdFlags|log.Lmicroseconds)
		logger.Printf("warn: failed to open %s: %v; logging to stderr", logPath, lfErr)
	} else {
		logger = log.New(logFile, "", log.LstdFlags|log.Lmicroseconds)
	}

	// Ensure log is flushed and file is closed on exit
	defer func() {
		if logFile != nil {
			_ = logFile.Sync()
			_ = logFile.Close()
		}
	}()

	// Top-level panic guard to ensure graceful exit on unexpected panics
	defer func() {
		if r := recover(); r != nil {
			logger.Printf("panic: %v\n%s", r, debug.Stack())
			if logFile != nil {
				_ = logFile.Sync()
				_ = logFile.Close()
			}
			os.Exit(1)
		}
	}()

	logger.Printf("startup: begin")
	logger.Printf("env: GOOS=%s GOARCH=%s GOVERSION=%s GOTRACEBACK=%s", runtime.GOOS, runtime.GOARCH, runtime.Version(), os.Getenv("GOTRACEBACK"))

	// Initialize Windows Common Controls before creating any widgets
	logger.Printf("step: InitCommonControlsEx begin")
	var initOK bool
	{
		// Attempt with broad, modern set first (prefers ComCtl32 v6 when available)
		icex := win.INITCOMMONCONTROLSEX{}
		icex.DwSize = uint32(unsafe.Sizeof(icex))
		icex.DwICC = win.ICC_WIN95_CLASSES |
			win.ICC_BAR_CLASSES |
			win.ICC_TAB_CLASSES |
			win.ICC_TREEVIEW_CLASSES |
			win.ICC_LISTVIEW_CLASSES |
			win.ICC_PROGRESS_CLASS |
			win.ICC_DATE_CLASSES |
			win.ICC_COOL_CLASSES |
			win.ICC_USEREX_CLASSES |
			win.ICC_LINK_CLASS
		if win.InitCommonControlsEx(&icex) {
			logger.Printf("step: InitCommonControlsEx success (broad set); dwICC=0x%08x", icex.DwICC)
			initOK = true
		} else {
			logger.Printf("warn: InitCommonControlsEx failed (broad set); dwICC=0x%08x", icex.DwICC)
		}
	}

	if !initOK {
		// Fallback 1: conservative set compatible with older ComCtl versions (no LINK/COOL/USEREX)
		icex := win.INITCOMMONCONTROLSEX{}
		icex.DwSize = uint32(unsafe.Sizeof(icex))
		icex.DwICC = win.ICC_WIN95_CLASSES |
			win.ICC_BAR_CLASSES |
			win.ICC_TAB_CLASSES |
			win.ICC_TREEVIEW_CLASSES |
			win.ICC_LISTVIEW_CLASSES |
			win.ICC_PROGRESS_CLASS |
			win.ICC_DATE_CLASSES
		if win.InitCommonControlsEx(&icex) {
			logger.Printf("step: InitCommonControlsEx success (conservative set); dwICC=0x%08x", icex.DwICC)
			initOK = true
		} else {
			logger.Printf("warn: InitCommonControlsEx failed (conservative set); dwICC=0x%08x", icex.DwICC)
		}
	}

	if !initOK {
		logger.Printf("error: InitCommonControlsEx failed for all configurations; cannot continue")
		os.Exit(1)
	}

	var mw *walk.MainWindow

	// Engines orchestrator with an explicit default ParameterSet
	ps := defaultParameterSet()
	calc := calculator.New(ps)

	// UI state
	product := "HP"          // engines/types constants expect "HP", "mySTAR", "F-Lease", "Op-Lease"
	timing := "arrears"      // "arrears" | "advance"
	dpLock := "percent"      // "percent" | "amount"
	rateMode := "fixed_rate" // "fixed_rate" | "target_installment"

	// Widgets
	var productCB *walk.ComboBox
	var timingCB *walk.ComboBox
	var priceEdit, dpPercentEdit, dpAmountEdit, termEdit, balloonEdit, nominalRateEdit, targetInstallmentEdit *walk.LineEdit
	var lockPercentRB, lockAmountRB *walk.RadioButton
	var fixedRateRB, targetInstallmentRB *walk.RadioButton

	var monthlyLbl, custNominalLbl, custEffLbl, roracLbl *walk.Label
	var financedLbl, metaVersionLbl, metaCalcTimeLbl *walk.Label

	var headerMonthlyLbl, headerRoRacLbl *walk.Label
	// Campaign toggles
	var subdown bool
	var subinterest bool
	var freeInsurance bool
	var freeMBSP bool
	var cashDiscount bool
	recalc := func() {
		price := parseFloat(priceEdit)
		dpPercent := parseFloat(dpPercentEdit)
		dpAmount := parseFloat(dpAmountEdit)
		term := parseInt(termEdit)
		balloon := parseFloat(balloonEdit)
		nominal := parseFloat(nominalRateEdit)
		targetInstall := parseFloat(targetInstallmentEdit)

		if price < 0 {
			price = 0
		}
		if term <= 0 {
			term = 36
		}

		// Two-way lock between % and amount
		if dpLock == "percent" {
			dpAmount = price * (dpPercent / 100.0)
			if dpAmountEdit != nil {
				_ = dpAmountEdit.SetText(fmt.Sprintf("%.0f", dpAmount))
			}
		} else {
			if price > 0 {
				dpPercent = (dpAmount / price) * 100.0
				if dpPercentEdit != nil {
					_ = dpPercentEdit.SetText(fmt.Sprintf("%.2f", dpPercent))
				}
			}
		}

		// Build engines/types.Deal
		deal := types.Deal{
			Market:              "TH",
			Currency:            "THB",
			Product:             types.Product(product),
			PriceExTax:          types.NewDecimal(price),
			DownPaymentAmount:   types.NewDecimal(dpAmount),
			DownPaymentPercent:  types.NewDecimal(dpPercent / 100.0), // fraction
			DownPaymentLocked:   dpLock,
			FinancedAmount:      types.NewDecimal(0),
			TermMonths:          term,
			BalloonPercent:      types.NewDecimal(balloon / 100.0), // fraction
			BalloonAmount:       types.NewDecimal(0),
			Timing:              types.PaymentTiming(timing),
			PayoutDate:          time.Now(),
			FirstPaymentOffset:  0,
			RateMode:            rateMode,
			CustomerNominalRate: types.NewDecimal(nominal / 100.0), // annual fraction
			TargetInstallment:   types.NewDecimal(targetInstall),
		}

		// Build campaigns from UI toggles
		buildCampaigns := func() []types.Campaign {
			var cams []types.Campaign
			id := 1
			if subdown {
				cams = append(cams, types.Campaign{
					ID:             fmt.Sprintf("CAMP-%d", id),
					Type:           types.CampaignSubdown,
					SubsidyPercent: types.NewDecimal(0.05),
					Funder:         "Dealer",
					Stacking:       1,
				})
				id++
			}
			if subinterest {
				cams = append(cams, types.Campaign{
					ID:         fmt.Sprintf("CAMP-%d", id),
					Type:       types.CampaignSubinterest,
					TargetRate: types.NewDecimal(0.0299),
					Funder:     "Manufacturer",
					Stacking:   2,
				})
				id++
			}
			if freeInsurance {
				cams = append(cams, types.Campaign{
					ID:            fmt.Sprintf("CAMP-%d", id),
					Type:          types.CampaignFreeInsurance,
					InsuranceCost: types.NewDecimal(15000),
					Funder:        "Insurance Partner",
					Stacking:      3,
				})
				id++
			}
			if freeMBSP {
				cams = append(cams, types.Campaign{
					ID:       fmt.Sprintf("CAMP-%d", id),
					Type:     types.CampaignFreeMBSP,
					MBSPCost: types.NewDecimal(5000),
					Funder:   "Manufacturer",
					Stacking: 4,
				})
				id++
			}
			if cashDiscount {
				cams = append(cams, types.Campaign{
					ID:              fmt.Sprintf("CAMP-%d", id),
					Type:            types.CampaignCashDiscount,
					DiscountPercent: types.NewDecimal(0.02),
					Funder:          "Dealer",
					Stacking:        5,
				})
				id++
			}
			return cams
		}
		campaigns := buildCampaigns()
		idcItems := []types.IDCItem{}

		// Run engines pipeline using explicit ParameterSet to match engine instances
		req := types.CalculationRequest{
			Deal:         deal,
			Campaigns:    campaigns,
			IDCItems:     idcItems,
			ParameterSet: ps,
		}
		result, err := calc.Calculate(req)
		if err != nil || result == nil || !result.Success {
			msg := "calculation failed"
			if err != nil {
				msg = err.Error()
			} else if result != nil && len(result.Errors) > 0 {
				msg = result.Errors[0]
			}
			walk.MsgBox(mw, "Calculation Error", msg, walk.MsgBoxIconError)
			return
		}

		q := result.Quote
		// Update UI labels
		if monthlyLbl != nil {
			monthlyLbl.SetText(fmt.Sprintf("THB %s", formatCurrency(q.MonthlyInstallment.InexactFloat64())))
		}
		if headerMonthlyLbl != nil {
			headerMonthlyLbl.SetText(fmt.Sprintf("THB %s", formatCurrency(q.MonthlyInstallment.InexactFloat64())))
		}
		if custNominalLbl != nil {
			custNominalLbl.SetText(fmt.Sprintf("%.2f%%", q.CustomerRateNominal.Mul(types.NewDecimal(100)).InexactFloat64()))
		}
		if custEffLbl != nil {
			custEffLbl.SetText(fmt.Sprintf("%.2f%%", q.CustomerRateEffective.Mul(types.NewDecimal(100)).InexactFloat64()))
		}
		if roracLbl != nil {
			roracLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.AcquisitionRoRAC.Mul(types.NewDecimal(100)).InexactFloat64()))
		}
		if headerRoRacLbl != nil {
			headerRoRacLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.AcquisitionRoRAC.Mul(types.NewDecimal(100)).InexactFloat64()))
		}
		if financedLbl != nil {
			financedVal := price - dpAmount
			if financedVal < 0 {
				financedVal = 0
			}
			financedLbl.SetText(fmt.Sprintf("THB %s", formatCurrency(financedVal)))
		}
		if metaVersionLbl != nil {
			if v, ok := result.Metadata["parameter_set_version"].(string); ok {
				metaVersionLbl.SetText(v)
			} else {
				metaVersionLbl.SetText(ps.Version)
			}
		}
		if metaCalcTimeLbl != nil {
			if ts, ok := result.Metadata["calculation_time_ms"]; ok {
				metaCalcTimeLbl.SetText(fmt.Sprintf("%v ms", ts))
			}
		}
	}

	createStart := time.Now()
	logger.Printf("step: MainWindow.Create begin")

	err := (MainWindow{
		AssignTo: &mw,
		Title:    "Financial Calculator (Walk UI)",
		MinSize:  Size{Width: 1100, Height: 700},
		Size:     Size{Width: 1280, Height: 860},
		Layout:   VBox{},
		Children: []Widget{
			Composite{
				Layout: HBox{Margins: Margins{Left: 12, Top: 8, Right: 12, Bottom: 0}, Spacing: 12},
				Children: []Widget{
					GroupBox{
						Title:  "Headline",
						Layout: Grid{Columns: 2, Spacing: 6},
						Children: []Widget{
							Label{Text: "Monthly Installment:"}, Label{AssignTo: &headerMonthlyLbl, Text: "-"},
							Label{Text: "Acquisition RoRAC:"}, Label{AssignTo: &headerRoRacLbl, Text: "-"},
						},
					},
					HSpacer{},
					GroupBox{
						Title:  "Version",
						Layout: Grid{Columns: 2, Spacing: 6},
						Children: []Widget{
							Label{Text: "Param Ver:"}, Label{AssignTo: &metaVersionLbl, Text: "-"},
							Label{Text: "Calc time:"}, Label{AssignTo: &metaCalcTimeLbl, Text: "-"},
						},
					},
				},
			},
			HSplitter{
				Children: []Widget{
					// Left: Inputs
					Composite{
						StretchFactor: 1,
						Layout:        VBox{Margins: Margins{Left: 12, Top: 12, Right: 6, Bottom: 12}, Spacing: 8},
						Children: []Widget{
							GroupBox{
								Title:  "Deal Inputs",
								Layout: Grid{Columns: 2, Spacing: 6},
								Children: []Widget{
									Label{Text: "Product:"},
									ComboBox{
										AssignTo:     &productCB,
										Model:        []string{"HP", "mySTAR", "F-Lease", "Op-Lease"},
										CurrentIndex: 0,
										OnCurrentIndexChanged: func() {
											product = productCB.Text()
										},
									},
									Label{Text: "Price ex tax (THB):"},
									LineEdit{
										AssignTo:          &priceEdit,
										Text:              "1000000",
										OnEditingFinished: recalc,
									},
									Label{Text: "Down payment %:"},
									LineEdit{
										AssignTo: &dpPercentEdit,
										Text:     "20",
										OnEditingFinished: func() {
											dpLock = "percent"
											if lockPercentRB != nil {
												lockPercentRB.SetChecked(true)
											}
											recalc()
										},
									},
									Label{Text: "Down payment amount (THB):"},
									LineEdit{
										AssignTo: &dpAmountEdit,
										Text:     "200000",
										OnEditingFinished: func() {
											dpLock = "amount"
											if lockAmountRB != nil {
												lockAmountRB.SetChecked(true)
											}
											recalc()
										},
									},
									Label{Text: "Lock mode:"},
									Composite{
										Layout: HBox{Spacing: 6},
										Children: []Widget{
											RadioButton{
												AssignTo: &lockPercentRB,
												Text:     "Percent",
												OnClicked: func() {
													dpLock = "percent"
													recalc()
												},
											},
											RadioButton{
												AssignTo: &lockAmountRB,
												Text:     "Amount",
												OnClicked: func() {
													dpLock = "amount"
													recalc()
												},
											},
										},
									},
									Label{Text: "Term (months):"},
									LineEdit{
										AssignTo:          &termEdit,
										Text:              "36",
										OnEditingFinished: recalc,
									},
									Label{Text: "Timing:"},
									ComboBox{
										AssignTo:     &timingCB,
										Model:        []string{"arrears", "advance"},
										CurrentIndex: 0,
										OnCurrentIndexChanged: func() {
											timing = timingCB.Text()
											recalc()
										},
									},
									Label{Text: "Balloon %:"},
									LineEdit{
										AssignTo:          &balloonEdit,
										Text:              "0",
										OnEditingFinished: recalc,
									},
								},
							},
							GroupBox{
								Title:  "Rate Mode",
								Layout: Grid{Columns: 2, Spacing: 6},
								Children: []Widget{
									RadioButton{
										AssignTo: &fixedRateRB,
										Text:     "Fixed Rate",
										OnClicked: func() {
											rateMode = "fixed_rate"
											if nominalRateEdit != nil {
												nominalRateEdit.SetEnabled(true)
											}
											if targetInstallmentEdit != nil {
												targetInstallmentEdit.SetEnabled(false)
											}
											recalc()
										},
									},
									RadioButton{
										AssignTo: &targetInstallmentRB,
										Text:     "Target Installment",
										OnClicked: func() {
											rateMode = "target_installment"
											if nominalRateEdit != nil {
												nominalRateEdit.SetEnabled(false)
											}
											if targetInstallmentEdit != nil {
												targetInstallmentEdit.SetEnabled(true)
											}
											recalc()
										},
									},
									Label{Text: "Customer rate (% p.a.):"},
									LineEdit{
										AssignTo:          &nominalRateEdit,
										Text:              "3.99",
										OnEditingFinished: recalc,
									},
									Label{Text: "Target installment (THB):"},
									LineEdit{
										AssignTo:          &targetInstallmentEdit,
										Text:              "0",
										OnEditingFinished: recalc,
									},
								},
							},
							GroupBox{
								Title:  "Campaigns",
								Layout: Grid{Columns: 2, Spacing: 6},
								Children: []Widget{
									CheckBox{
										Text: "Subdown",
										OnClicked: func() {
											subdown = !subdown
											recalc()
										},
									},
									CheckBox{
										Text: "Subinterest",
										OnClicked: func() {
											subinterest = !subinterest
											recalc()
										},
									},
									CheckBox{
										Text: "Free Insurance",
										OnClicked: func() {
											freeInsurance = !freeInsurance
											recalc()
										},
									},
									CheckBox{
										Text: "Free MBSP",
										OnClicked: func() {
											freeMBSP = !freeMBSP
											recalc()
										},
									},
									CheckBox{
										Text: "Cash Discount",
										OnClicked: func() {
											cashDiscount = !cashDiscount
											recalc()
										},
									},
								},
							},
							PushButton{
								Text:      "Calculate",
								OnClicked: recalc,
							},
						},
					},
					// Right: Results
					Composite{
						StretchFactor: 1,
						Layout:        VBox{Margins: Margins{Left: 6, Top: 12, Right: 12, Bottom: 12}, Spacing: 8},
						Children: []Widget{
							GroupBox{
								Title:  "Key Metrics",
								Layout: Grid{Columns: 2, Spacing: 6},
								Children: []Widget{
									Label{Text: "Monthly Installment:"}, Label{AssignTo: &monthlyLbl, Text: "-"},
									Label{Text: "Customer Rate Nominal:"}, Label{AssignTo: &custNominalLbl, Text: "-"},
									Label{Text: "Customer Rate Effective:"}, Label{AssignTo: &custEffLbl, Text: "-"},
									Label{Text: "Acquisition RoRAC:"}, Label{AssignTo: &roracLbl, Text: "-"},
									Label{Text: "Financed Amount:"}, Label{AssignTo: &financedLbl, Text: "-"},
								},
							},
							GroupBox{
								Title:  "Metadata",
								Layout: Grid{Columns: 2, Spacing: 6},
								Children: []Widget{
									Label{Text: "Parameter Version:"}, Label{AssignTo: &metaVersionLbl, Text: "-"},
									Label{Text: "Calc time:"}, Label{AssignTo: &metaCalcTimeLbl, Text: "-"},
								},
							},
							VSpacer{},
						},
					},
				},
			},
		},
	}.Create())

	logger.Printf("step: MainWindow.Create end; duration=%s", time.Since(createStart))

	if err != nil {
		logger.Printf("error: MainWindow.Create failed: %v", err)
		os.Exit(1)
	}

	// Post-create initialization on UI thread after controls are realized
	mw.Synchronize(func() {
		if lockPercentRB != nil {
			lockPercentRB.SetChecked(true)
		}
		if fixedRateRB != nil {
			fixedRateRB.SetChecked(true)
		}
		if nominalRateEdit != nil {
			nominalRateEdit.SetEnabled(true)
		}
		if targetInstallmentEdit != nil {
			targetInstallmentEdit.SetEnabled(false)
		}
		recalc()
		logger.Printf("post-create: defaults applied and initial recalc completed")
	})

	runStart := time.Now()
	logger.Printf("step: mw.Run begin")
	mw.Run()
	logger.Printf("step: mw.Run end; duration=%s", time.Since(runStart))
	logger.Printf("shutdown: normal exit")
}

func parseFloat(le *walk.LineEdit) float64 {
	if le == nil {
		return 0
	}
	s := strings.TrimSpace(le.Text())
	s = strings.ReplaceAll(s, ",", "")
	f, _ := strconv.ParseFloat(s, 64)
	return f
}

func parseInt(le *walk.LineEdit) int {
	if le == nil {
		return 0
	}
	s := strings.TrimSpace(le.Text())
	s = strings.ReplaceAll(s, ",", "")
	i, _ := strconv.Atoi(s)
	return i
}

func formatCurrency(v float64) string {
	s := fmt.Sprintf("%.2f", v)
	n := s
	dec := ""
	if idx := strings.LastIndex(s, "."); idx != -1 {
		n = s[:idx]
		dec = s[idx:]
	}
	var b []byte
	count := 0
	for i := len(n) - 1; i >= 0; i-- {
		b = append([]byte{n[i]}, b...)
		count++
		if count%3 == 0 && i != 0 {
			b = append([]byte{','}, b...)
		}
	}
	return string(b) + dec
}

func defaultParameterSet() types.ParameterSet {
	return types.ParameterSet{
		ID:                 "DEFAULT-001",
		Version:            "2025.08",
		EffectiveDate:      time.Now(),
		DayCountConvention: "ACT/365",
		CostOfFundsCurve: []types.RateCurvePoint{
			{TermMonths: 6, Rate: types.NewDecimal(0.0120)},
			{TermMonths: 12, Rate: types.NewDecimal(0.0148)},
			{TermMonths: 24, Rate: types.NewDecimal(0.0165)},
			{TermMonths: 36, Rate: types.NewDecimal(0.0175)},
			{TermMonths: 48, Rate: types.NewDecimal(0.0185)},
			{TermMonths: 60, Rate: types.NewDecimal(0.0195)},
		},
		MatchedFundedSpread: types.NewDecimal(0.0025),
		PDLGD: map[string]types.PDLGDEntry{
			"HP_default": {
				Product: "HP",
				Segment: "default",
				PD:      types.NewDecimal(0.02),
				LGD:     types.NewDecimal(0.45),
			},
			"mySTAR_default": {
				Product: "mySTAR",
				Segment: "default",
				PD:      types.NewDecimal(0.025),
				LGD:     types.NewDecimal(0.40),
			},
		},
		EconomicCapitalParams: types.EconomicCapitalParams{
			BaseCapitalRatio:     types.NewDecimal(0.12),
			CapitalAdvantage:     types.NewDecimal(0.0008),
			DTLAdvantage:         types.NewDecimal(0.0003),
			SecurityDepAdvantage: types.NewDecimal(0.0002),
			OtherAdvantage:       types.NewDecimal(0.0001),
		},
		CentralHQAddOn: types.NewDecimal(0.0015),
		RoundingRules: types.RoundingRules{
			Currency:    "THB",
			MinorUnits:  0,
			Method:      "bank",
			DisplayRate: 4,
		},
	}
}
