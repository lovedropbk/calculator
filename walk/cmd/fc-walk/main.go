//go:build windows

package main

import (
	"fmt"
	"log"
	"math"
	"os"
	"runtime"
	"runtime/debug"
	"strconv"
	"strings"
	"time"
	"unsafe"

	"financial-calculator/parameters"

	"github.com/financial-calculator/engines/calculator"
	"github.com/financial-calculator/engines/campaigns"
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
			logger.Printf("step: InitCommonControlsEx fallback: conservative")
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
	var mainSplit *walk.Splitter
	var lastTVWidth int

	// Engines orchestrator with YAML-loaded ParameterSet (discover at startup)
	var (
		enginePS      types.ParameterSet
		calc          *calculator.Calculator
		campEng       *campaigns.Engine
		paramsReady   bool
		paramsVersion string
		paramsErr     string
		paramsSource  string
		autoLookup    staticCommissionLookup
	)

	// Discover and load YAML ParameterSet at startup
	if psYaml, src, err := parameters.DiscoverAndLoadParameterSet(); err == nil && psYaml != nil {
		enginePS = convertParametersToEngine(psYaml)
		calc = calculator.New(enginePS)
		campEng = campaigns.NewEngine(enginePS)
		// Commission lookup wired to YAML policy with defaults fallback
		autoLookup = staticCommissionLookup{by: psYaml.CommissionPolicy.ByProductPct}
		campEng.SetCommissionLookup(autoLookup)
		paramsReady = true
		paramsVersion = psYaml.ID
		paramsSource = src
		logger.Printf("params loaded: version=%s source=%s", paramsVersion, paramsSource)
	} else {
		// Minimal fallback: use built-in defaults so UI computes even without external YAML
		if err != nil {
			paramsErr = err.Error()
		} else {
			paramsErr = "unknown error"
		}
		enginePS = defaultParameterSet()
		calc = calculator.New(enginePS)
		campEng = campaigns.NewEngine(enginePS)
		// No YAML policy available; rely on static defaults inside lookup
		autoLookup = staticCommissionLookup{by: map[string]float64{}}
		campEng.SetCommissionLookup(autoLookup)
		paramsReady = true
		paramsVersion = enginePS.Version
		paramsSource = "defaults"
		logger.Printf("params load failed: %v; using built-in defaults version=%s", err, paramsVersion)
	}

	// UI state
	product := "HP"          // engines/types constants expect "HP", "mySTAR", "F-Lease", "Op-Lease"
	timing := "arrears"      // "arrears" | "advance"
	dpLock := "percent"      // "percent" | "amount" (mirrors dp unit)
	rateMode := "fixed_rate" // "fixed_rate" | "target_installment"
	balloonUnit := "%"       // "%" | "THB"

	// DealState for UI wiring (Phase 2 - auto by default)
	dealState := types.DealState{
		DealerCommission: types.DealerCommission{Mode: types.DealerCommissionModeAuto},
		IDCOther:         types.IDCOther{Value: 0, UserEdited: false},
	}

	// Widgets
	var productCB *walk.ComboBox
	var timingCB *walk.ComboBox
	var priceEdit, termEdit, nominalRateEdit, targetInstallmentEdit *walk.LineEdit
	var dpValueEd, balloonValueEd *walk.NumberEdit
	var dpUnitCmb, balloonUnitCmb *walk.ComboBox
	var fixedRateRB, targetInstallmentRB *walk.RadioButton

	var monthlyLbl, custNominalLbl, custEffLbl, roracLbl *walk.Label
	var financedLbl, metaVersionLbl, metaCalcTimeLbl *walk.Label
	var validationLbl *walk.Label

	// New UI controls (Phase 1 placeholders)
	var subsidyBudgetEd, idcOtherEd *walk.NumberEdit
	var dealerCommissionPill *walk.PushButton
	var campaignTV, cashflowTV *walk.TableView
	var mainTabs *walk.TabWidget
	var idcTotalLbl, idcDealerLbl, idcOtherLbl *walk.Label
	var tip *walk.ToolTip
	var campaignModel *CampaignTableModel
	var dpShadowLbl, balloonShadowLbl *walk.Label

	var headerMonthlyLbl, headerRoRacLbl *walk.Label
	// Campaign toggles
	// Profitability details controls (toggle and labels)
	var detailsTogglePB *walk.PushButton
	var wfPanel *walk.Composite
	var wfDealIRREffLbl, wfDealIRRNomLbl *walk.Label
	var wfCostDebtLbl, wfMFSpreadLbl, wfGIMLbl, wfCapAdvLbl, wfNIMLbl *walk.Label
	var wfRiskLbl, wfOpexLbl, wfIDCUpLbl, wfIDCPeLbl, wfNetEbitLbl, wfEconCapLbl, wfAcqRoRacDetailLbl *walk.Label
	var subdown bool
	var subinterest bool
	var freeInsurance bool
	var freeMBSP bool
	var cashDiscount bool
	// Persistent selected row index for Campaign grid
	var selectedCampaignIdx int
	var creatingUI bool
	computeMode := "implicit"
	recalc := func() {
		if creatingUI {
			return
		}
		// Gate on parameters load/validation
		if !paramsReady {
			if validationLbl != nil {
				_ = validationLbl.SetText(fmt.Sprintf("Parameters not loaded: %s", paramsErr))
			}
			// Placeholders for results
			if headerMonthlyLbl != nil {
				headerMonthlyLbl.SetText("—")
			}
			if headerRoRacLbl != nil {
				headerRoRacLbl.SetText("—")
			}
			if monthlyLbl != nil {
				monthlyLbl.SetText("—")
			}
			if custNominalLbl != nil {
				custNominalLbl.SetText("—")
			}
			if custEffLbl != nil {
				custEffLbl.SetText("—")
			}
			if roracLbl != nil {
				roracLbl.SetText("—")
			}
			if financedLbl != nil {
				financedLbl.SetText("—")
			}
			if idcTotalLbl != nil {
				idcTotalLbl.SetText("—")
			}
			if idcDealerLbl != nil {
				idcDealerLbl.SetText("—")
			}
			if idcOtherLbl != nil {
				idcOtherLbl.SetText("—")
			}
			logger.Printf("compute skipped: parameters not loaded: %s", paramsErr)
			return
		}
		price := parseFloat(priceEdit)

		// Derive DP percent/amount from compact unit switch
		dpVal := 0.0
		if dpValueEd != nil {
			dpVal = dpValueEd.Value()
		}
		dpUnit := "%"
		if dpUnitCmb != nil && dpUnitCmb.Text() != "" {
			dpUnit = dpUnitCmb.Text()
		}
		var dpPercent, dpAmount float64
		if dpUnit == "%" {
			dpPercent = dpVal
			dpAmount = price * (dpPercent / 100.0)
			dpLock = "percent"
		} else {
			dpAmount = dpVal
			if price > 0 {
				dpPercent = (dpAmount / price) * 100.0
			}
			dpLock = "amount"
		}

		term := parseInt(termEdit)

		// Balloon percent from compact unit switch
		balloonVal := 0.0
		if balloonValueEd != nil {
			balloonVal = balloonValueEd.Value()
		}
		balloonSel := "%"
		if balloonUnitCmb != nil && balloonUnitCmb.Text() != "" {
			balloonSel = balloonUnitCmb.Text()
		}
		balloonPct := 0.0
		if balloonSel == "%" {
			balloonPct = balloonVal
		} else if price > 0 {
			balloonPct = (balloonVal / price) * 100.0
		}

		nominal := parseFloat(nominalRateEdit)
		targetInstall := parseFloat(targetInstallmentEdit)

		// Validate inputs via orchestrator helper before any compute
		uiState := UIState{
			Product:    product,
			PriceExTax: price,
			TermMonths: term,
			BalloonPct: balloonPct,
		}
		if err := validateInputs(uiState); err != nil {
			// Inline indicator, placeholders, and logging; modal only on explicit action
			if validationLbl != nil {
				_ = validationLbl.SetText(fmt.Sprintf("Invalid inputs: %s", err.Error()))
			}
			// Placeholders for results
			if headerMonthlyLbl != nil {
				headerMonthlyLbl.SetText("—")
			}
			if headerRoRacLbl != nil {
				headerRoRacLbl.SetText("—")
			}
			if monthlyLbl != nil {
				monthlyLbl.SetText("—")
			}
			if custNominalLbl != nil {
				custNominalLbl.SetText("—")
			}
			if custEffLbl != nil {
				custEffLbl.SetText("—")
			}
			if roracLbl != nil {
				roracLbl.SetText("—")
			}
			if financedLbl != nil {
				financedLbl.SetText("—")
			}
			if idcTotalLbl != nil {
				idcTotalLbl.SetText("—")
			}
			if idcDealerLbl != nil {
				idcDealerLbl.SetText("—")
			}
			if idcOtherLbl != nil {
				idcOtherLbl.SetText("—")
			}
			// Profitability details placeholders if panel exists
			if wfPanel != nil {
				if wfDealIRREffLbl != nil {
					wfDealIRREffLbl.SetText("—")
				}
				if wfDealIRRNomLbl != nil {
					wfDealIRRNomLbl.SetText("—")
				}
				if wfCostDebtLbl != nil {
					wfCostDebtLbl.SetText("—")
				}
				if wfMFSpreadLbl != nil {
					wfMFSpreadLbl.SetText("—")
				}
				if wfGIMLbl != nil {
					wfGIMLbl.SetText("—")
				}
				if wfCapAdvLbl != nil {
					wfCapAdvLbl.SetText("—")
				}
				if wfNIMLbl != nil {
					wfNIMLbl.SetText("—")
				}
				if wfRiskLbl != nil {
					wfRiskLbl.SetText("—")
				}
				if wfOpexLbl != nil {
					wfOpexLbl.SetText("—")
				}
				if wfIDCUpLbl != nil {
					wfIDCUpLbl.SetText("—")
				}
				if wfIDCPeLbl != nil {
					wfIDCPeLbl.SetText("—")
				}
				if wfNetEbitLbl != nil {
					wfNetEbitLbl.SetText("—")
				}
				if wfEconCapLbl != nil {
					wfEconCapLbl.SetText("—")
				}
				if wfAcqRoRacDetailLbl != nil {
					wfAcqRoRacDetailLbl.SetText("—")
				}
			}
			logger.Printf("compute skipped: invalid inputs: %s", err.Error())
			if computeMode == "explicit" {
				walk.MsgBox(mw, "Invalid Inputs", err.Error(), walk.MsgBoxIconWarning)
			}
			return
		} else {
			if validationLbl != nil {
				_ = validationLbl.SetText("")
			}
		}

		if price < 0 {
			price = 0
		}
		if term <= 0 {
			term = 36
		}

		// Compact DP control: value is already consistent with unit; nothing to sync here

		// Build engines/types.Deal via pure helper
		deal := buildDealFromControls(
			product, timing, dpLock, rateMode,
			price, dpPercent, dpAmount,
			term, balloonPct, nominal, targetInstall,
		)

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
		// For display only: populate the grid with default options when none are selected.
		// Do NOT apply these defaults to the computation path.
		displayCampaigns := campaigns
		if len(displayCampaigns) == 0 {
			displayCampaigns = defaultCampaignsForUI()
		}
		idcItems := []types.IDCItem{}

		// Run engines pipeline (centralized)
		result, err := computeQuote(calc, enginePS, deal, campaigns, idcItems)
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

		// Unified results update for key metrics
		updateResultsUI(
			q,
			monthlyLbl, headerMonthlyLbl,
			custNominalLbl, custEffLbl,
			roracLbl, headerRoRacLbl,
		)

		// Success log for compute
		logger.Printf("compute ok: version=%v installment=THB %s rorac=%.2f%%",
			result.Metadata["parameter_set_version"],
			FormatTHB(q.MonthlyInstallment.InexactFloat64()),
			q.Profitability.AcquisitionRoRAC.Mul(types.NewDecimal(100)).InexactFloat64(),
		)

		// Populate Profitability Details panel if present
		if wfPanel != nil {
			if wfDealIRREffLbl != nil {
				wfDealIRREffLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.DealIRREffective.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfDealIRRNomLbl != nil {
				wfDealIRRNomLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.DealIRRNominal.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfCostDebtLbl != nil {
				wfCostDebtLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.CostOfDebtMatched.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfMFSpreadLbl != nil {
				wfMFSpreadLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.MatchedFundedSpread.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfGIMLbl != nil {
				wfGIMLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.GrossInterestMargin.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfCapAdvLbl != nil {
				wfCapAdvLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.CapitalAdvantage.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfNIMLbl != nil {
				wfNIMLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.NetInterestMargin.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfRiskLbl != nil {
				wfRiskLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.CostOfCreditRisk.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfOpexLbl != nil {
				wfOpexLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.OPEX.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfIDCUpLbl != nil {
				wfIDCUpLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.IDCSubsidiesFeesUpfront.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfIDCPeLbl != nil {
				wfIDCPeLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.IDCSubsidiesFeesPeriodic.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfNetEbitLbl != nil {
				wfNetEbitLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.NetEBITMargin.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfEconCapLbl != nil {
				wfEconCapLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.EconomicCapital.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
			if wfAcqRoRacDetailLbl != nil {
				wfAcqRoRacDetailLbl.SetText(fmt.Sprintf("%.2f%%", q.Profitability.AcquisitionRoRAC.Mul(types.NewDecimal(100)).InexactFloat64()))
			}
		}

		if financedLbl != nil {
			financedVal := price - dpAmount
			if financedVal < 0 {
				financedVal = 0
			}
			financedLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(financedVal)))
		}

		// Compute Dealer Commission (resolved) for pill and IDC labels
		var dealerPct float64
		var dealerAmt float64
		financedBase := price - dpAmount
		if financedBase < 0 {
			financedBase = 0
		}
		if dealState.DealerCommission.Mode == types.DealerCommissionModeOverride {
			if dealState.DealerCommission.Amt != nil {
				a := *dealState.DealerCommission.Amt
				if a < 0 {
					a = 0
				}
				dealerAmt = math.Round(a)
				dealerPct = 0
			} else if dealState.DealerCommission.Pct != nil {
				p := *dealState.DealerCommission.Pct
				if p < 0 {
					p = 0
				}
				dealerPct = p
				dealerAmt = math.Round(financedBase * dealerPct)
				if dealerAmt < 0 {
					dealerAmt = 0
				}
			} else {
				// override mode without values -> auto
				dealerPct = autoLookup.CommissionPercentByProduct(product)
				if dealerPct < 0 {
					dealerPct = 0
				}
				dealerAmt = math.Round(financedBase * dealerPct)
			}
		} else {
			// auto mode
			dealerPct = autoLookup.CommissionPercentByProduct(product)
			if dealerPct < 0 {
				dealerPct = 0
			}
			dealerAmt = math.Round(financedBase * dealerPct)
			if dealerAmt < 0 {
				dealerAmt = 0
			}
		}

		if dealerCommissionPill != nil {
			if dealState.DealerCommission.Mode == types.DealerCommissionModeOverride {
				if dealState.DealerCommission.Amt != nil {
					dealerCommissionPill.SetText(fmt.Sprintf("IDCs - Dealer Commissions: override (THB %s)", FormatTHB(dealerAmt)))
				} else {
					dealerCommissionPill.SetText(fmt.Sprintf("IDCs - Dealer Commissions: override %.2f%% (THB %s)", dealerPct*100, FormatTHB(dealerAmt)))
				}
			} else {
				dealerCommissionPill.SetText(fmt.Sprintf("IDCs - Dealer Commissions: auto %.2f%% (THB %s)", dealerPct*100, FormatTHB(dealerAmt)))
			}
		}

		// Update IDC labels
		var otherIDC float64
		if idcOtherEd != nil {
			otherIDC = idcOtherEd.Value()
		} else {
			otherIDC = dealState.IDCOther.Value
		}
		if idcDealerLbl != nil {
			idcDealerLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(dealerAmt)))
		}
		if idcOtherLbl != nil {
			idcOtherLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(otherIDC)))
		}
		if idcTotalLbl != nil {
			idcTotalLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(dealerAmt+otherIDC)))
		}

		// Compute and bind Campaign Options grid with per-row metrics
		if campaignTV != nil && campEng != nil {
			// Subsidy budget baseline for each row
			var subsidyBudget float64
			if subsidyBudgetEd != nil {
				subsidyBudget = subsidyBudgetEd.Value()
			}
			rows, idx := computeCampaignRows(
				enginePS,
				calc,
				campEng,
				deal,
				dealState,
				displayCampaigns,
				subsidyBudget,
				dpPercent,
				selectedCampaignIdx,
			)
			selectedCampaignIdx = idx

			if campaignModel == nil {
				campaignModel = &CampaignTableModel{rows: rows}
				_ = campaignTV.SetModel(campaignModel)
			} else {
				campaignModel.ReplaceRows(rows)
			}

			// Update Key Metrics Summary and header from selected row
			if selectedCampaignIdx >= 0 && selectedCampaignIdx < len(rows) {
				sel := rows[selectedCampaignIdx]
				updateSummaryFromRow(
					sel,
					monthlyLbl, headerMonthlyLbl,
					custNominalLbl, custEffLbl,
					roracLbl, headerRoRacLbl,
				)
				if cashflowTV != nil {
					refreshCashflowTable(cashflowTV, sel.Cashflows)
				}
			}
		}

		if metaVersionLbl != nil {
			if v, ok := result.Metadata["parameter_set_version"].(string); ok {
				metaVersionLbl.SetText(v)
			} else {
				metaVersionLbl.SetText(enginePS.Version)
			}
		}
		if metaCalcTimeLbl != nil {
			if ts, ok := result.Metadata["calculation_time_ms"]; ok {
				metaCalcTimeLbl.SetText(fmt.Sprintf("%v ms", ts))
			}
		}
		// Persist sticky state after compute (non-blocking; ignore error)
		if s, err := CollectStickyFromUI(
			product,
			priceEdit,
			dpUnitCmb, dpValueEd,
			termEdit,
			timing,
			balloonUnit, balloonUnitCmb, balloonValueEd,
			rateMode,
			nominalRateEdit,
			targetInstallmentEdit,
			subsidyBudgetEd, idcOtherEd,
			selectedCampaignIdx,
		); err == nil {
			go func(ss StickyState) { _ = SaveStickyState(ss) }(s)
		}
	}

	creatingUI = true

	createStart := time.Now()
	logger.Printf("step: MainWindow.Create begin")

	err := (MainWindow{
		AssignTo: &mw,
		Title:    "Financial Calculator (Walk UI)",
		MinSize:  Size{Width: 1100, Height: 700},
		Size:     Size{Width: 1280, Height: 860},
		Layout:   VBox{},
		Children: []Widget{
			HSplitter{
				AssignTo: &mainSplit,
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
									// Basic inputs
									Label{Text: "Product:"},
									ComboBox{
										AssignTo:     &productCB,
										Model:        []string{"HP", "mySTAR", "Financing Lease", "Operating Lease"},
										CurrentIndex: 0,
										OnCurrentIndexChanged: func() {
											if p, err := MapProductDisplayToEnum(productCB.Text()); err == nil {
												product = string(p)
											} else {
												product = "HP"
											}
											// Enable Balloon controls only for mySTAR; otherwise reset to 0 and disable
											if balloonValueEd != nil && balloonUnitCmb != nil {
												if product != "mySTAR" {
													_ = balloonValueEd.SetValue(0)
													balloonValueEd.SetEnabled(false)
													balloonUnitCmb.SetEnabled(false)
													balloonUnit = "%"
												} else {
													balloonValueEd.SetEnabled(true)
													balloonUnitCmb.SetEnabled(true)
												}
											}
											recalc()
										},
									},
									Label{Text: "Price ex tax (THB):"},
									LineEdit{
										AssignTo: &priceEdit,
										Text:     "1000000",
										OnEditingFinished: func() {
											// Reformat with thousand separators on commit
											v := parseFloat(priceEdit)
											_ = priceEdit.SetText(FormatWithThousandSep(v, 0))
											recalc()
										},
									},
									Label{Text: "Down payment:"},
									Composite{
										Layout: HBox{Spacing: 6},
										Children: []Widget{
											NumberEdit{
												AssignTo: &dpValueEd,
												Decimals: 2,
												MinValue: 0,
												Value:    20,

												OnValueChanged: func() {
													// Update pretty label when in THB
													if dpShadowLbl != nil && dpUnitCmb != nil && dpUnitCmb.Text() == "THB" {
														dpShadowLbl.SetText("(" + FormatWithThousandSep(dpValueEd.Value(), 0) + ")")
													}
												},
											},
											ComboBox{
												AssignTo:     &dpUnitCmb,
												Model:        []string{"THB", "%"},
												CurrentIndex: 1, // default to %
												MaxSize:      Size{Width: 70},
												OnCurrentIndexChanged: func() {
													price := parseFloat(priceEdit)
													if dpValueEd == nil || dpUnitCmb == nil {
														return
													}
													newUnit := dpUnitCmb.Text()
													// Convert existing value across units when toggled
													if newUnit == "%" && dpLock != "percent" {
														// THB -> %
														thb := dpValueEd.Value()
														pct := 0.0
														if price > 0 {
															pct = RoundTo((thb/price)*100.0, 2)
														}
														_ = dpValueEd.SetDecimals(2)
														_ = dpValueEd.SetValue(pct)
														dpLock = "percent"
														if dpShadowLbl != nil {
															dpShadowLbl.SetText("")
														}
													} else if newUnit == "THB" && dpLock != "amount" {
														// % -> THB
														pct := dpValueEd.Value()
														thb := RoundTo(price*(pct/100.0), 0)
														_ = dpValueEd.SetDecimals(0)
														_ = dpValueEd.SetValue(thb)
														dpLock = "amount"
														if dpShadowLbl != nil {
															dpShadowLbl.SetText("(" + FormatWithThousandSep(thb, 0) + ")")
														}
													}
													recalc()
												},
											},
											Label{
												AssignTo: &dpShadowLbl,
												Text:     "",
											},
										},
									},
									// placeholders to keep grid alignment where old fields were removed
									Label{Text: ""}, Label{Text: ""},
									Label{Text: ""}, Label{Text: ""},
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
									Label{Text: "Balloon:"},
									Composite{
										Layout: HBox{Spacing: 6},
										Children: []Widget{
											NumberEdit{
												AssignTo: &balloonValueEd,
												Decimals: 2,
												MinValue: 0,
												Value:    0,

												OnValueChanged: func() {
													// Update pretty label when in THB
													if balloonShadowLbl != nil && balloonUnitCmb != nil && balloonUnitCmb.Text() == "THB" {
														balloonShadowLbl.SetText("(" + FormatWithThousandSep(balloonValueEd.Value(), 0) + ")")
													}
												},
											},
											ComboBox{
												AssignTo:     &balloonUnitCmb,
												Model:        []string{"THB", "%"},
												CurrentIndex: 1,
												MaxSize:      Size{Width: 70},
												OnCurrentIndexChanged: func() {
													price := parseFloat(priceEdit)
													if balloonValueEd == nil || balloonUnitCmb == nil {
														return
													}
													newUnit := balloonUnitCmb.Text()
													if newUnit == "%" && balloonUnit != "%" {
														// THB -> %
														thb := balloonValueEd.Value()
														pct := 0.0
														if price > 0 {
															pct = RoundTo((thb/price)*100.0, 2)
														}
														_ = balloonValueEd.SetDecimals(2)
														_ = balloonValueEd.SetValue(pct)
														balloonUnit = "%"
														if balloonShadowLbl != nil {
															balloonShadowLbl.SetText("")
														}
													} else if newUnit == "THB" && balloonUnit != "THB" {
														// % -> THB
														pct := balloonValueEd.Value()
														thb := RoundTo(price*(pct/100.0), 0)
														_ = balloonValueEd.SetDecimals(0)
														_ = balloonValueEd.SetValue(thb)
														balloonUnit = "THB"
														if balloonShadowLbl != nil {
															balloonShadowLbl.SetText("(" + FormatWithThousandSep(thb, 0) + ")")
														}
													}
													recalc()
												},
											},
											Label{
												AssignTo: &balloonShadowLbl,
												Text:     "",
											},
										},
									},

									// Integrated Rate Mode controls
									Label{Text: "Rate mode:"},
									Composite{
										Layout: HBox{Spacing: 6},
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
										AssignTo: &targetInstallmentEdit,
										Text:     "0",
										OnEditingFinished: func() {
											v := parseFloat(targetInstallmentEdit)
											_ = targetInstallmentEdit.SetText(FormatWithThousandSep(v, 0))
											recalc()
										},
									},

									// Product Subsidy (moved under Deal Inputs)
									Label{Text: "Subsidy budget (THB):"},
									NumberEdit{
										AssignTo: &subsidyBudgetEd,
										Decimals: 0,
										MinValue: 0,
										Value:    0,

										ToolTipText: "Budget available for subsidies (placeholder)",
										OnValueChanged: func() {
											// Recompute grid metrics when subsidy budget changes
											recalc()
										},
									},
									// Dealer commission pill (opens override dialog)
									Label{Text: ""},
									PushButton{
										AssignTo:    &dealerCommissionPill,
										Text:        "IDCs - Dealer Commissions: auto",
										Enabled:     true,
										ToolTipText: "Auto-calculated from product policy; click to override or reset",
										OnClicked: func() {
											// Open editor; if accepted, trigger recompute
											if editDealerCommission(mw, &dealState) {
												recalc()
											}
										},
									},
									Label{Text: "IDCs - Other (THB):"},
									NumberEdit{
										AssignTo: &idcOtherEd,
										Decimals: 0,
										MinValue: 0,
										Value:    0,

										OnValueChanged: func() {
											// Mark user-edited and recalc to refresh IDC totals and grid
											dealState.IDCOther.Value = idcOtherEd.Value()
											dealState.IDCOther.UserEdited = true
											recalc()
										},
									},

									// Removed redundant Key Metrics group (migrated to summary on right)
									Label{Text: ""}, Label{Text: ""},
								},
							},
							// Campaign checkboxes removed in Phase 1; replaced by Campaign Options grid in right pane.
							PushButton{
								Text: "Calculate",
								OnClicked: func() {
									computeMode = "explicit"
									recalc()
									computeMode = "implicit"
								},
							},
						},
					},
					// Right: Results (Tabs)
					TabWidget{
						AssignTo: &mainTabs,
						Pages: []TabPage{
							{
								Title:  "Calculator",
								Layout: VBox{Spacing: 8},
								Children: []Widget{
									// Campaign Options (grid)
									TableView{
										AssignTo:       &campaignTV,
										StretchFactor:  1,
										MultiSelection: false,
										Columns: []TableViewColumn{
											{Title: "Select", Width: 70},
											{Title: "Campaign", Width: 220},
											{Title: "Monthly Installment", Width: 180},
											{Title: "Downpayment", Width: 120},
											{Title: "Subsidy / Acq.RoRAC", Width: 180},
											{Title: "Dealer Comm.", Width: 160},
											{Title: "Notes", Width: 220},
										},
									},
									// Summary
									GroupBox{
										Title:  "Key Metrics & Summary",
										Layout: Grid{Columns: 2, Spacing: 6},
										Children: []Widget{
											Label{Text: "Monthly Installment:"}, Label{AssignTo: &monthlyLbl, Text: "-"},
											Label{Text: "Nominal Customer Rate:"}, Label{AssignTo: &custNominalLbl, Text: "-"},
											Label{Text: "Effective Rate:"}, Label{AssignTo: &custEffLbl, Text: "-"},
											Label{Text: "Financed Amount:"}, Label{AssignTo: &financedLbl, Text: "-"},
											Label{Text: "Acquisition RoRAC:"}, Label{AssignTo: &roracLbl, Text: "-"},
											Label{Text: "IDC Total:"}, Label{AssignTo: &idcTotalLbl, Text: "-"},
											Label{Text: "IDC - Dealer Comm.:"}, Label{AssignTo: &idcDealerLbl, Text: "-"},
											Label{Text: "IDC - Other:"}, Label{AssignTo: &idcOtherLbl, Text: "-"},
											// Profitability Details (toggle)
											GroupBox{
												Title:  "Profitability Details",
												Layout: Grid{Columns: 2, Spacing: 6},
												Children: []Widget{
													PushButton{
														AssignTo:   &detailsTogglePB,
														Text:       "Details ▼",
														ColumnSpan: 2,
														OnClicked: func() {
															if wfPanel != nil {
																vis := wfPanel.Visible()
																wfPanel.SetVisible(!vis)
																if detailsTogglePB != nil {
																	if vis {
																		detailsTogglePB.SetText("Details ▼")
																	} else {
																		detailsTogglePB.SetText("Details ▲")
																	}
																}
															}
														},
													},
													Composite{
														AssignTo:   &wfPanel,
														Visible:    false,
														ColumnSpan: 2,
														Layout:     Grid{Columns: 2, Spacing: 6},
														Children: []Widget{
															Label{Text: "Deal IRR Effective:"}, Label{AssignTo: &wfDealIRREffLbl, Text: "—"},
															Label{Text: "Deal IRR Nominal:"}, Label{AssignTo: &wfDealIRRNomLbl, Text: "—"},
															Label{Text: "Cost of Debt Matched:"}, Label{AssignTo: &wfCostDebtLbl, Text: "—"},
															Label{Text: "Matched Funded Spread:"}, Label{AssignTo: &wfMFSpreadLbl, Text: "—"},
															Label{Text: "Gross Interest Margin:"}, Label{AssignTo: &wfGIMLbl, Text: "—"},
															Label{Text: "Capital Advantage:"}, Label{AssignTo: &wfCapAdvLbl, Text: "—"},
															Label{Text: "Net Interest Margin:"}, Label{AssignTo: &wfNIMLbl, Text: "—"},
															Label{Text: "Cost of Credit Risk:"}, Label{AssignTo: &wfRiskLbl, Text: "—"},
															Label{Text: "OPEX:"}, Label{AssignTo: &wfOpexLbl, Text: "—"},
															Label{Text: "IDC Subsidies/Fees Upfront:"}, Label{AssignTo: &wfIDCUpLbl, Text: "—"},
															Label{Text: "IDC Subsidies/Fees Periodic:"}, Label{AssignTo: &wfIDCPeLbl, Text: "—"},
															Label{Text: "Net EBIT Margin:"}, Label{AssignTo: &wfNetEbitLbl, Text: "—"},
															Label{Text: "Economic Capital:"}, Label{AssignTo: &wfEconCapLbl, Text: "—"},
															Label{Text: "Acquisition RoRAC:"}, Label{AssignTo: &wfAcqRoRacDetailLbl, Text: "—"},
														},
													},
												},
											},
											Label{Text: "Parameter Version:"}, Label{AssignTo: &metaVersionLbl, Text: "-"},
											Label{Text: "Calc Time:"}, Label{AssignTo: &metaCalcTimeLbl, Text: "-"},
											PushButton{
												ColumnSpan: 2,
												Text:       "Export XLSX",
												OnClicked: func() {
													// Build summary map and cashflow rows based on current selection
													summary := map[string]string{}
													if productCB != nil {
														summary["Product"] = productCB.Text()
													} else {
														summary["Product"] = product
													}
													summary["Price ex tax (THB)"] = FormatWithThousandSep(parseFloat(priceEdit), 0)
													if dpUnitCmb != nil && dpValueEd != nil {
														unit := dpUnitCmb.Text()
														val := dpValueEd.Value()
														if unit == "THB" {
															summary["Down payment"] = "THB " + FormatWithThousandSep(val, 0)
														} else {
															summary["Down payment"] = FormatWithThousandSep(val, 2) + " percent"
														}
													}
													summary["Term (months)"] = fmt.Sprintf("%d", parseInt(termEdit))
													if timingCB != nil {
														summary["Timing"] = timingCB.Text()
													} else {
														summary["Timing"] = timing
													}
													if balloonUnitCmb != nil && balloonValueEd != nil {
														bu := balloonUnitCmb.Text()
														bv := balloonValueEd.Value()
														if bu == "THB" {
															summary["Balloon"] = "THB " + FormatWithThousandSep(bv, 0)
														} else {
															summary["Balloon"] = FormatWithThousandSep(bv, 2) + " percent"
														}
													}
													summary["Rate mode"] = rateMode
													summary["Customer rate (% p.a.)"] = fmt.Sprintf("%.2f", parseFloat(nominalRateEdit))
													summary["Target installment (THB)"] = FormatWithThousandSep(parseFloat(targetInstallmentEdit), 0)
													if subsidyBudgetEd != nil {
														summary["Subsidy budget (THB)"] = FormatWithThousandSep(subsidyBudgetEd.Value(), 0)
													}
													if idcOtherEd != nil {
														summary["IDCs - Other (THB)"] = FormatWithThousandSep(idcOtherEd.Value(), 0)
													}
													var cfr []CashflowRow
													if campaignModel != nil && selectedCampaignIdx >= 0 && selectedCampaignIdx < len(campaignModel.rows) {
														row := campaignModel.rows[selectedCampaignIdx]
														summary["Selected Campaign"] = row.Name
														summary["Monthly Installment (THB)"] = row.MonthlyInstallmentStr
														summary["Nominal Rate"] = row.NominalRateStr
														summary["Effective Rate"] = row.EffectiveRateStr
														summary["Acq RoRAC"] = row.AcqRoRacStr
														summary["Dealer Commission"] = row.DealerComm
														cfr = buildCashflowRows(row.Cashflows)
													}
													if err := doExportXLSX(mw, summary, cfr); err != nil {
														walk.MsgBox(mw, "Export XLSX", fmt.Sprintf("Export failed: %v", err), walk.MsgBoxIconError)
													}
												},
											},
										},
									},
								},
							},
							{
								Title:  "Cashflow",
								Layout: VBox{Spacing: 8},
								Children: []Widget{
									Composite{
										Layout: HBox{Spacing: 6},
										Children: []Widget{
											PushButton{
												Text: "Refresh",
												OnClicked: func() {
													if campaignModel != nil && cashflowTV != nil {
														idx := selectedCampaignIdx
														if idx >= 0 && idx < len(campaignModel.rows) {
															refreshCashflowTable(cashflowTV, campaignModel.rows[idx].Cashflows)
														}
													}
												},
											},
										},
									},
									TableView{
										AssignTo:       &cashflowTV,
										StretchFactor:  1,
										MultiSelection: false,
										Columns: []TableViewColumn{
											{Title: "Period", Width: 80},
											{Title: "Date", Width: 120},
											{Title: "Principal", Width: 120},
											{Title: "Interest", Width: 120},
											{Title: "IDCs", Width: 120},
											{Title: "Subsidy", Width: 120},
											{Title: "Installment", Width: 140},
										},
									},
								},
							},
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
		// Initial 1/3 : 2/3 visual ratio (via StretchFactor) and initial table column widths
		if campaignTV != nil {
			cw := mw.ClientBounds().Width
			if cw <= 0 {
				cw = 1200
			}
			rw := int(float64(cw) * 2.0 / 3.0) // approximate right pane width
			tw := campaignTV.ClientBounds().Width
			if tw <= 0 {
				tw = rw - 32 // leave a little padding for borders/scrollbar
			}
			initCampaignTableColumns(campaignTV, tw)
			lastTVWidth = tw
		}

		// Keep campaign columns visible on resize
		mw.SizeChanged().Attach(func() {
			if campaignTV != nil {
				tw := campaignTV.ClientBounds().Width
				if tw > 0 && tw != lastTVWidth {
					initCampaignTableColumns(campaignTV, tw)
					lastTVWidth = tw
				}
			}
		})
		if fixedRateRB != nil {
			fixedRateRB.SetChecked(true)
		}
		if nominalRateEdit != nil {
			nominalRateEdit.SetEnabled(true)
		}
		if targetInstallmentEdit != nil {
			targetInstallmentEdit.SetEnabled(false)
		}

		// Load sticky state before first recalc and apply to controls
		if s, ok := LoadStickyState(); ok {
			// Product
			product = s.Product
			if productCB != nil {
				display := s.Product
				switch s.Product {
				case "HP":
					display = "HP"
				case "mySTAR":
					display = "mySTAR"
				case "F-Lease":
					display = "Financing Lease"
				case "Op-Lease":
					display = "Operating Lease"
				}
				_ = productCB.SetText(display)
			}
			// Price
			if priceEdit != nil {
				_ = priceEdit.SetText(FormatWithThousandSep(s.Price, 0))
			}
			// DP unit + value
			if dpUnitCmb != nil {
				_ = dpUnitCmb.SetText(s.DPUnit)
			}
			if dpValueEd != nil {
				if s.DPUnit == "THB" {
					_ = dpValueEd.SetDecimals(0)
					dpLock = "amount"
				} else {
					_ = dpValueEd.SetDecimals(2)
					dpLock = "percent"
				}
				dpValueEd.SetValue(s.DPValue)
			}
			// Term
			if termEdit != nil {
				_ = termEdit.SetText(fmt.Sprintf("%d", s.Term))
			}
			// Timing
			timing = s.Timing
			if timingCB != nil {
				_ = timingCB.SetText(s.Timing)
			}
			// Balloon unit + value and enabling based on product
			balloonUnit = s.BalloonUnit
			if balloonUnitCmb != nil {
				_ = balloonUnitCmb.SetText(s.BalloonUnit)
			}
			if balloonValueEd != nil {
				if s.BalloonUnit == "THB" {
					_ = balloonValueEd.SetDecimals(0)
				} else {
					_ = balloonValueEd.SetDecimals(2)
				}
				balloonValueEd.SetValue(s.BalloonValue)
				if product != "mySTAR" {
					balloonValueEd.SetEnabled(false)
					if balloonUnitCmb != nil {
						balloonUnitCmb.SetEnabled(false)
					}
				} else {
					balloonValueEd.SetEnabled(true)
					if balloonUnitCmb != nil {
						balloonUnitCmb.SetEnabled(true)
					}
				}
			}
			// Rate mode + values
			rateMode = s.RateMode
			if fixedRateRB != nil && targetInstallmentRB != nil {
				if s.RateMode == "fixed_rate" {
					fixedRateRB.SetChecked(true)
					if nominalRateEdit != nil {
						nominalRateEdit.SetEnabled(true)
						_ = nominalRateEdit.SetText(fmt.Sprintf("%.2f", s.CustomerRatePct))
					}
					if targetInstallmentEdit != nil {
						targetInstallmentEdit.SetEnabled(false)
						_ = targetInstallmentEdit.SetText(FormatWithThousandSep(0, 0))
					}
				} else {
					targetInstallmentRB.SetChecked(true)
					if nominalRateEdit != nil {
						nominalRateEdit.SetEnabled(false)
						_ = nominalRateEdit.SetText(fmt.Sprintf("%.2f", s.CustomerRatePct))
					}
					if targetInstallmentEdit != nil {
						targetInstallmentEdit.SetEnabled(true)
						_ = targetInstallmentEdit.SetText(FormatWithThousandSep(s.TargetInstallment, 0))
					}
				}
			}
			// Subsidy budget / IDC Other
			if subsidyBudgetEd != nil {
				subsidyBudgetEd.SetValue(s.SubsidyBudgetTHB)
			}
			if idcOtherEd != nil {
				idcOtherEd.SetValue(s.IDCOtherTHB)
				dealState.IDCOther.Value = s.IDCOtherTHB
				dealState.IDCOther.UserEdited = s.IDCOtherTHB > 0
			}
			// Selected campaign index
			selectedCampaignIdx = s.SelectedCampaignIx
		}

		// Tab change: auto refresh cashflow from selection
		if mainTabs != nil {
			mainTabs.CurrentIndexChanged().Attach(func() {
				if mainTabs.CurrentIndex() == 1 && cashflowTV != nil {
					if row, ok := SelectedCampaignRow(campaignModel, selectedCampaignIdx); ok {
						refreshCashflowTable(cashflowTV, row.Cashflows)
					} else {
						refreshCashflowTable(cashflowTV, []types.Cashflow{})
					}
				}
			})
		}

		// Initialize Campaign Options model (static placeholder rows)
		if campaignTV != nil {
			campaignModel = NewCampaignTableModel()
			if err := campaignTV.SetModel(campaignModel); err != nil {
				logger.Printf("warn: campaign table model set failed: %v", err)
			}
			campaignTV.SetMultiSelection(false)

			// Selection behavior: sync IDCs - Other from selected campaign's Subsidy
			campaignTV.CurrentIndexChanged().Attach(func() {
				if campaignTV == nil || campaignModel == nil {
					return
				}
				idx := campaignTV.CurrentIndex()
				if idx < 0 || idx >= campaignModel.RowCount() {
					return
				}

				// Update persistent selected index
				selectedCampaignIdx = idx

				// Reflect "radio dot" selection
				for i := range campaignModel.rows {
					campaignModel.rows[i].Selected = (i == idx)
				}
				campaignModel.PublishRowsReset()

				// Compute new Subsidy from selected row (fallback to budget)
				newSubsidy := 0.0
				if idx >= 0 && idx < len(campaignModel.rows) {
					newSubsidy = campaignModel.rows[idx].SubsidyValue
				}
				if newSubsidy <= 0 && subsidyBudgetEd != nil {
					newSubsidy = subsidyBudgetEd.Value()
				}

				// Respect user-edited flag and prompt to replace
				if dealState.IDCOther.UserEdited {
					ret := walk.MsgBox(mw, "Replace IDCs - Other?", fmt.Sprintf("Replace IDCs - Other with THB %s from selected campaign?", FormatTHB(newSubsidy)), walk.MsgBoxYesNo|walk.MsgBoxIconQuestion)
					if ret == walk.DlgCmdYes {
						if idcOtherEd != nil {
							idcOtherEd.SetValue(newSubsidy)
						}
						dealState.IDCOther.Value = newSubsidy
						dealState.IDCOther.UserEdited = false
					}
				} else {
					if idcOtherEd != nil {
						idcOtherEd.SetValue(newSubsidy)
					}
					dealState.IDCOther.Value = newSubsidy
				}

				// Update summary immediately from selection, then refresh IDC labels and headline
				if idx >= 0 && idx < len(campaignModel.rows) {
					updateSummaryFromRow(
						campaignModel.rows[idx],
						monthlyLbl, headerMonthlyLbl,
						custNominalLbl, custEffLbl,
						roracLbl, headerRoRacLbl,
					)
				}
				// If Cashflow tab is active, refresh from the selected row
				if mainTabs != nil && mainTabs.CurrentIndex() == 1 && cashflowTV != nil {
					if row, ok := SelectedCampaignRow(campaignModel, selectedCampaignIdx); ok {
						refreshCashflowTable(cashflowTV, row.Cashflows)
					} else {
						refreshCashflowTable(cashflowTV, []types.Cashflow{})
					}
				}
				recalc()
			})
		}

		// Create ToolTips after window initialization
		creatingUI = false
		if t, err := walk.NewToolTip(); err != nil {
			logger.Printf("warn: ToolTip Create failed: %v", err)
		} else {
			tip = t
			if subsidyBudgetEd != nil {
				_ = subsidyBudgetEd.SetToolTipText("Budget available for subsidies (placeholder)")
				if err := tip.AddTool(subsidyBudgetEd); err != nil {
					logger.Printf("warn: ToolTip AddTool failed for subsidyBudgetEd: %v", err)
				}
			}
			if dealerCommissionPill != nil {
				_ = dealerCommissionPill.SetToolTipText("Auto-calculated from product policy; click to override or reset")
				if err := tip.AddTool(dealerCommissionPill); err != nil {
					logger.Printf("warn: ToolTip AddTool failed for dealerCommissionPill: %v", err)
				}
			}
		}
		// Update version/status on startup
		if metaVersionLbl != nil {
			if paramsReady {
				metaVersionLbl.SetText(paramsVersion)
			} else {
				metaVersionLbl.SetText("—")
			}
		}
		if !paramsReady && validationLbl != nil {
			_ = validationLbl.SetText(fmt.Sprintf("Parameters not loaded: %s", paramsErr))
		}
		// Initial compute to populate campaign grid and summary
		recalc()
		logger.Printf("post-create: window initialized; awaiting user input")
	})

	// Persist sticky state on window close
	mw.Closing().Attach(func(canceled *bool, reason walk.CloseReason) {
		if s, err := CollectStickyFromUI(
			product,
			priceEdit,
			dpUnitCmb, dpValueEd,
			termEdit,
			timing,
			balloonUnit, balloonUnitCmb, balloonValueEd,
			rateMode,
			nominalRateEdit,
			targetInstallmentEdit,
			subsidyBudgetEd, idcOtherEd,
			selectedCampaignIdx,
		); err == nil {
			_ = SaveStickyState(s)
		}
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

// --- Phase 1 UI placeholders: Campaign Options table model (unwired) ---

type CampaignRow struct {
	Selected bool

	// Display fields (stringified for grid bindings)
	Name                  string // campaign display name
	MonthlyInstallmentStr string // e.g., "22,198.61"
	DownpaymentStr        string // e.g., "20%"
	NominalRateStr        string // e.g., "3.99 percent"
	EffectiveRateStr      string // e.g., "4.05 percent"
	AcqRoRacStr           string // e.g., "8.50 percent"
	SubsidyRorac          string // combined: "THB X / Y%"
	DealerComm            string
	Notes                 string

	// Numeric metrics to drive the summary panel
	MonthlyInstallment float64 // THB
	NominalRate        float64 // fractional, e.g., 0.0399
	EffectiveRate      float64 // fractional
	AcqRoRac           float64 // fractional
	IDCDealerTHB       float64
	IDCOtherTHB        float64

	// Additional values used elsewhere
	DealerCommAmt float64
	DealerCommPct float64
	SubsidyValue  float64

	// Detailed outputs for Cashflow tab/export
	Cashflows []types.Cashflow
}

type CampaignTableModel struct {
	walk.TableModelBase
	rows []CampaignRow
}

func NewCampaignTableModel() *CampaignTableModel {
	return &CampaignTableModel{
		rows: []CampaignRow{
			{
				Selected:              true,
				Name:                  "Standard (No Campaign)",
				MonthlyInstallmentStr: "", // will render as "Monthly —"
				DownpaymentStr:        "20% / THB 200,000",
				SubsidyRorac:          "- / -",
				Notes:                 "Baseline (placeholder)",
			},
			{
				Selected:              false,
				Name:                  "Subinterest 2.99%",
				MonthlyInstallmentStr: "",
				DownpaymentStr:        "20%",
				SubsidyRorac:          "THB 0 / 8.5%",
				Notes:                 "Static row (Phase 1)",
			},
			{
				Selected:              false,
				Name:                  "Subdown 5%",
				MonthlyInstallmentStr: "",
				DownpaymentStr:        "15%",
				SubsidyRorac:          "THB 50,000 / 6.8%",
				Notes:                 "Static row (Phase 1)",
			},
			{
				Selected:              false,
				Name:                  "Free Insurance",
				MonthlyInstallmentStr: "",
				DownpaymentStr:        "20%",
				SubsidyRorac:          "THB 15,000 / 7.2%",
				Notes:                 "Static row (Phase 1)",
			},
		},
	}
}

// ReplaceRows replaces the table model rows and refreshes the view.
func (m *CampaignTableModel) ReplaceRows(rows []CampaignRow) {
	m.rows = rows
	m.PublishRowsReset()
}

func (m *CampaignTableModel) RowCount() int {
	return len(m.rows)
}

func (m *CampaignTableModel) Value(row, col int) interface{} {
	if row < 0 || row >= len(m.rows) {
		return ""
	}
	r := m.rows[row]
	switch col {
	case 0:
		if r.Selected {
			return "●" // filled to simulate radio selected
		}
		return "○" // empty to simulate not selected
	case 1:
		return r.Name
	case 2:
		if r.MonthlyInstallmentStr == "" {
			return "Monthly —"
		}
		return "THB " + r.MonthlyInstallmentStr
	case 3:
		if r.DownpaymentStr == "" {
			return "—"
		}
		return r.DownpaymentStr
	case 4:
		return r.SubsidyRorac
	case 5:
		return r.DealerComm
	case 6:
		return r.Notes
	default:
		return ""
	}
}

// Map engine campaign type to display name for the grid.
func campaignTypeDisplayName(t types.CampaignType) string {
	switch t {
	case types.CampaignSubdown:
		return "Subdown"
	case types.CampaignSubinterest:
		return "Subinterest"
	case types.CampaignFreeInsurance:
		return "Free Insurance"
	case types.CampaignFreeMBSP:
		return "Free MBSP"
	case types.CampaignCashDiscount:
		return "Cash Discount"
	default:
		return string(t)
	}
}

// editDealerCommission opens a small dialog to set auto/override values.
// Returns true if state was updated.
func editDealerCommission(mw *walk.MainWindow, state *types.DealState) bool {
	if state == nil || mw == nil {
		return false
	}
	var dlg *walk.Dialog
	var modeCB *walk.ComboBox
	var amtEd, pctEd *walk.LineEdit
	accepted := false

	// Initialize fields from current state
	modeIndex := 0 // auto
	if state.DealerCommission.Mode == types.DealerCommissionModeOverride {
		if state.DealerCommission.Amt != nil {
			modeIndex = 1 // override amount
		} else if state.DealerCommission.Pct != nil {
			modeIndex = 2 // override percent
		} else {
			modeIndex = 0
		}
	}
	amtText := ""
	if state.DealerCommission.Amt != nil {
		amtText = fmt.Sprintf("%.0f", *state.DealerCommission.Amt)
	}
	pctText := ""
	if state.DealerCommission.Pct != nil {
		pctText = fmt.Sprintf("%.2f", *state.DealerCommission.Pct*100)
	}

	_, _ = (Dialog{
		AssignTo: &dlg,
		Title:    "Dealer Commission",
		MinSize:  Size{Width: 420, Height: 220},
		Layout:   Grid{Columns: 2, Spacing: 6},
		Children: []Widget{
			Label{Text: "Mode:"},
			ComboBox{
				AssignTo:     &modeCB,
				Model:        []string{"auto", "override: amount", "override: percent"},
				CurrentIndex: modeIndex,
			},
			Label{Text: "Amount (THB):"},
			LineEdit{AssignTo: &amtEd, Text: amtText},
			Label{Text: "Percent (%):"},
			LineEdit{AssignTo: &pctEd, Text: pctText},
			Composite{
				ColumnSpan: 2,
				Layout:     HBox{Spacing: 6},
				Children: []Widget{
					PushButton{
						Text: "Reset to auto",
						OnClicked: func() {
							state.DealerCommission.Mode = types.DealerCommissionModeAuto
							state.DealerCommission.Amt = nil
							state.DealerCommission.Pct = nil
							accepted = true
							dlg.Accept()
						},
					},
					HSpacer{},
					PushButton{
						Text:      "Cancel",
						OnClicked: func() { dlg.Cancel() },
					},
					PushButton{
						Text: "OK",
						OnClicked: func() {
							switch modeCB.CurrentIndex() {
							case 0: // auto
								state.DealerCommission.Mode = types.DealerCommissionModeAuto
								state.DealerCommission.Amt = nil
								state.DealerCommission.Pct = nil
							case 1: // override amount
								state.DealerCommission.Mode = types.DealerCommissionModeOverride
								// parse amount
								if v, err := strconv.ParseFloat(strings.ReplaceAll(strings.TrimSpace(amtEd.Text()), ",", ""), 64); err == nil {
									if v < 0 {
										v = 0
									}
									state.DealerCommission.Amt = &v
									state.DealerCommission.Pct = nil
								}
							case 2: // override percent
								state.DealerCommission.Mode = types.DealerCommissionModeOverride
								if v, err := strconv.ParseFloat(strings.ReplaceAll(strings.TrimSpace(pctEd.Text()), ",", ""), 64); err == nil {
									// convert percent -> fraction
									v = v / 100.0
									if v < 0 {
										v = 0
									}
									state.DealerCommission.Pct = &v
									state.DealerCommission.Amt = nil
								}
							}
							accepted = true
							dlg.Accept()
						},
					},
				},
			},
		},
	}).Run(mw)

	return accepted
}

// defaultCampaignsForUI returns a deterministic set of campaign options for the grid
// when no explicit selections are provided. These align with the docs' illustrative defaults.
func defaultCampaignsForUI() []types.Campaign {
	return []types.Campaign{
		{
			ID:             "SUBDOWN-5",
			Type:           types.CampaignSubdown,
			SubsidyPercent: types.NewDecimal(0.05),
			Funder:         "Dealer",
			Stacking:       1,
		},
		{
			ID:         "SUBINT-299",
			Type:       types.CampaignSubinterest,
			TargetRate: types.NewDecimal(0.0299),
			Funder:     "Manufacturer",
			Stacking:   2,
		},
		{
			ID:            "FREE-INS",
			Type:          types.CampaignFreeInsurance,
			InsuranceCost: types.NewDecimal(15000),
			Funder:        "Insurance Partner",
			Stacking:      3,
		},
		{
			ID:       "FREE-MBSP",
			Type:     types.CampaignFreeMBSP,
			MBSPCost: types.NewDecimal(5000),
			Funder:   "Manufacturer",
			Stacking: 4,
		},
		{
			ID:              "CASH-DISC-2",
			Type:            types.CampaignCashDiscount,
			DiscountPercent: types.NewDecimal(0.02),
			Funder:          "Dealer",
			Stacking:        5,
		},
	}
}

// buildDealFromControls constructs types.Deal from primitive control values (pure helper).
func buildDealFromControls(
	product, timing, dpLock, rateMode string,
	price, dpPercent, dpAmount float64,
	term int,
	balloonPct, nominalRatePct, targetInstallment float64,
) types.Deal {
	return types.Deal{
		Market:              "TH",
		Currency:            "THB",
		Product:             types.Product(product),
		PriceExTax:          types.NewDecimal(price),
		DownPaymentAmount:   types.NewDecimal(dpAmount),
		DownPaymentPercent:  types.NewDecimal(dpPercent / 100.0), // fraction
		DownPaymentLocked:   dpLock,
		FinancedAmount:      types.NewDecimal(0),
		TermMonths:          term,
		BalloonPercent:      types.NewDecimal(balloonPct / 100.0), // fraction
		BalloonAmount:       types.NewDecimal(0),
		Timing:              types.PaymentTiming(timing),
		PayoutDate:          time.Now(),
		FirstPaymentOffset:  0,
		RateMode:            rateMode,
		CustomerNominalRate: types.NewDecimal(nominalRatePct / 100.0), // annual fraction
		TargetInstallment:   types.NewDecimal(targetInstallment),
	}
}

// computeQuote centralizes the call to the calculator entrypoint.
func computeQuote(
	calc *calculator.Calculator,
	ps types.ParameterSet,
	deal types.Deal,
	campaigns []types.Campaign,
	idcItems []types.IDCItem,
) (*types.CalculationResult, error) {
	req := types.CalculationRequest{
		Deal:         deal,
		Campaigns:    campaigns,
		IDCItems:     idcItems,
		ParameterSet: ps,
	}
	return calc.Calculate(req)
}

// updateResultsUI updates the headline result labels in one cohesive call.
func updateResultsUI(
	q types.Quote,
	monthlyLbl, headerMonthlyLbl *walk.Label,
	custNominalLbl, custEffLbl *walk.Label,
	roracLbl, headerRoRacLbl *walk.Label,
) {
	if monthlyLbl != nil {
		monthlyLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(q.MonthlyInstallment.InexactFloat64())))
	}
	if headerMonthlyLbl != nil {
		headerMonthlyLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(q.MonthlyInstallment.InexactFloat64())))
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
}

func updateSummaryFromRow(row CampaignRow, monthlyLbl, headerMonthlyLbl *walk.Label, custNominalLbl, custEffLbl *walk.Label, roracLbl, headerRoRacLbl *walk.Label) {
	// Monthly installment
	if monthlyLbl != nil {
		if row.MonthlyInstallmentStr != "" {
			monthlyLbl.SetText("THB " + row.MonthlyInstallmentStr)
		} else {
			monthlyLbl.SetText("—")
		}
	}
	if headerMonthlyLbl != nil {
		if row.MonthlyInstallmentStr != "" {
			headerMonthlyLbl.SetText("THB " + row.MonthlyInstallmentStr)
		} else {
			headerMonthlyLbl.SetText("—")
		}
	}

	// Nominal rate
	if custNominalLbl != nil {
		if row.NominalRate > 0 {
			custNominalLbl.SetText(fmt.Sprintf("%.2f%%", row.NominalRate*100.0))
		} else {
			custNominalLbl.SetText("—")
		}
	}

	// Effective rate
	if custEffLbl != nil {
		if row.EffectiveRate > 0 {
			custEffLbl.SetText(fmt.Sprintf("%.2f%%", row.EffectiveRate*100.0))
		} else {
			custEffLbl.SetText("—")
		}
	}

	// Acquisition RoRAC
	if roracLbl != nil {
		if row.AcqRoRac > 0 {
			roracLbl.SetText(fmt.Sprintf("%.2f%%", row.AcqRoRac*100.0))
		} else {
			roracLbl.SetText("—")
		}
	}
	if headerRoRacLbl != nil {
		if row.AcqRoRac > 0 {
			headerRoRacLbl.SetText(fmt.Sprintf("%.2f%%", row.AcqRoRac*100.0))
		} else {
			headerRoRacLbl.SetText("—")
		}
	}
}

// Initialize Campaign Table column widths so all columns are visible without horizontal scrolling.
// totalWidth should be the client width of the TableView.
func initCampaignTableColumns(tv *walk.TableView, totalWidth int) {
	if tv == nil || totalWidth <= 0 {
		return
	}

	// Base target widths for ~1200–1400px total client width.
	// Columns: Select, Campaign, Monthly, Downpayment, Subsidy/RoRAC, Dealer Comm., Notes
	base := []int{70, 200, 180, 120, 180, 160, 220}
	mins := []int{60, 160, 140, 100, 140, 120, 140}

	// Fudge padding to account for borders/scrollbar
	pad := 24
	w := totalWidth - pad
	if w < 400 {
		w = totalWidth
	}

	baseSum := 0
	for _, bw := range base {
		baseSum += bw
	}
	scale := float64(w) / float64(baseSum)
	if scale <= 0 {
		scale = 1
	}

	widths := make([]int, len(base))
	for i := range base {
		widths[i] = int(math.Round(float64(base[i]) * scale))
		if widths[i] < mins[i] {
			widths[i] = mins[i]
		}
	}

	// Make the last column ("Notes") absorb any remaining space.
	sum := 0
	for i := 0; i < len(widths)-1; i++ {
		sum += widths[i]
	}
	rem := w - sum
	if rem < mins[len(widths)-1] {
		rem = mins[len(widths)-1]
	}
	widths[len(widths)-1] = rem

	cols := tv.Columns()
	n := cols.Len()
	if n > len(widths) {
		n = len(widths)
	}
	for i := 0; i < n; i++ {
		if c := cols.At(i); c != nil {
			_ = c.SetWidth(widths[i])
		}
	}
}
