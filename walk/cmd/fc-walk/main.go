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

	// My Campaigns state (Interactive Campaign Manager)
	var myCampaigns []CampaignDraft
	var campaignsDirty bool
	var campaignsFileVersion int
	var selectedMyCampaignID string
	// Edit mode and selection (UI-agnostic state)
	var editor EditorState
	// Temporary anchors to appease unused-var checks in some build paths
	_ = campaignsFileVersion
	_ = selectedMyCampaignID

	// Widgets
	var productCB *walk.ComboBox
	var timingCB *walk.ComboBox
	var priceEdit, termEdit, nominalRateEdit, targetInstallmentEdit *walk.LineEdit
	var dpValueEd, dpAmountEd, balloonValueEd, balloonAmountEd *walk.NumberEdit
	var dpUnitCmb, balloonUnitCmb *walk.ComboBox
	var fixedRateRB, targetInstallmentRB *walk.RadioButton

	var monthlyLbl, custNominalLbl, custEffLbl, roracLbl *walk.Label
	var financedLbl, metaVersionLbl *walk.Label
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
	// Progressive disclosure UI for Campaign edit mode
	var editModeUI *EditModeUI
	var dealInputsGB *walk.GroupBox
	// Selected Campaign Details labels (left bottom panel)
	var selCampNameValLbl, selTermValLbl, selFinancedValLbl, selSubsidyUsedValLbl, selSubsidyBudgetValLbl, selSubsidyRemainValLbl, selIDCDealerValLbl, selIDCOtherValLbl, selIDCInsValLbl, selIDCMBSPValLbl *walk.Label

	// Interactive Campaign Manager controls (MVP)
	var myCampTV *walk.TableView
	var myCampModel *MyCampaignsTableModel
	var btnNewBlankCampaign, btnSaveAllCampaigns, btnLoadCampaigns, btnClearCampaigns *walk.PushButton

	var headerMonthlyLbl, headerRoRacLbl *walk.Label
	// Campaign toggles
	// Profitability details controls (toggle and labels)
	var detailsTogglePB *walk.PushButton
	var wfPanel *walk.Composite
	// Matrix (Effective | Nominal) label pointers
	var wfCustRateEffLbl, wfCustRateNomLbl *walk.Label
	var wfDealIRREffLbl, wfDealIRRNomLbl *walk.Label
	var wfCostDebtLbl, wfMFSpreadLbl, wfGIMEffLbl, wfGIMLbl, wfCapAdvLbl, wfNIMEffLbl, wfNIMLbl *walk.Label
	var wfRiskLbl, wfOpexLbl, wfIDCUpLbl, wfSubUpLbl, wfNetEbitEffLbl, wfNetEbitLbl, wfEconCapLbl, wfAcqRoRacDetailLbl *walk.Label
	var subdown bool
	var subinterest bool
	var freeInsurance bool
	var freeMBSP bool
	var cashDiscount bool
	// Persistent selected row index for Campaign grid
	var selectedCampaignIdx int
	var creatingUI bool
	// Forward declaration so earlier closures can call recalc()
	var recalc func()

	// Minimal deps for My Campaigns handlers
	var myCampDeps MyCampaignsDeps
	myCampDeps.Save = SaveCampaigns
	myCampDeps.Load = LoadCampaigns
	myCampDeps.Clear = ClearCampaigns
	// Lifecycle callbacks to keep canonical slice and file version in sync
	myCampDeps.OnSaved = func(drafts []CampaignDraft) {
		myCampaigns = append([]CampaignDraft(nil), drafts...)
	}
	myCampDeps.OnLoaded = func(drafts []CampaignDraft, ver int) {
		myCampaigns = append([]CampaignDraft(nil), drafts...)
		campaignsFileVersion = ver
	}
	myCampDeps.OnCleared = func() {
		myCampaigns = nil
	}
	myCampDeps.SetDirty = func(b bool) { campaignsDirty = b }
	myCampDeps.SelectedID = func() string { return selectedMyCampaignID }
	myCampDeps.SeedBlank = func() CampaignDraft { return AddNewBlankDraft("") }
	myCampDeps.SeedCopy = func() (CampaignDraft, error) {
		// Determine base name from selected default campaign row
		baseName := "Custom Campaign"
		if campaignModel != nil && selectedCampaignIdx >= 0 && selectedCampaignIdx < len(campaignModel.rows) {
			baseName = campaignModel.rows[selectedCampaignIdx].Name
		}

		// Read current Deal Inputs high-level fields (identical logic)
		price := parseFloat(priceEdit)
		dpUnit := "%"
		if dpUnitCmb != nil && dpUnitCmb.Text() != "" {
			dpUnit = dpUnitCmb.Text()
		}
		dpPercent := 0.0
		dpTHB := 0.0
		if dpUnit == "THB" {
			if dpAmountEd != nil {
				dpTHB = dpAmountEd.Value()
			}
			if price > 0 {
				dpPercent = (dpTHB / price) * 100.0
			}
		} else {
			if dpValueEd != nil {
				dpPercent = dpValueEd.Value()
			}
			dpTHB = price * (dpPercent / 100.0)
		}
		term := parseInt(termEdit)
		bu := "%"
		if balloonUnitCmb != nil && balloonUnitCmb.Text() != "" {
			bu = balloonUnitCmb.Text()
		}
		balloonPct := 0.0
		if bu == "%" {
			if balloonValueEd != nil {
				balloonPct = balloonValueEd.Value()
			}
		} else {
			if balloonAmountEd != nil && price > 0 {
				balloonPct = (balloonAmountEd.Value() / price) * 100.0
			}
		}

		inputs := CampaignInputs{
			PriceExTaxTHB:        price,
			DownpaymentPercent:   dpPercent,
			DownpaymentTHB:       dpTHB,
			TermMonths:           term,
			BalloonPercent:       balloonPct,
			RateMode:             rateMode,
			CustomerRateAPR:      parseFloat(nominalRateEdit),
			TargetInstallmentTHB: parseFloat(targetInstallmentEdit),
		}
		name := "Copied: " + baseName
		return SeedCopyDraft(name, product, inputs), nil
	}
	myCampDeps.SelectMyCampaign = func(id string) {
		selectedMyCampaignID = id
		SelectMyCampaign(&editor, id)
		var selName string
		if myCampModel != nil {
			idx := myCampModel.IndexByID(id)
			if idx >= 0 {
				myCampModel.SetSelectedIndex(idx)
				if myCampTV != nil {
					_ = myCampTV.SetCurrentIndex(idx)
				}
				rows := myCampModel.Rows()
				if idx >= 0 && idx < len(rows) {
					selName = rows[idx].Name
				}
			}
		}
		ShowCampaignEditState(editModeUI, selName)
		// Recompute to reflect draft values and refresh selected row
		recalc()
	}
	myCampDeps.ExitEditMode = func() {
		ExitEditMode(&editor)
		ShowHighLevelState(editModeUI)
		selectedMyCampaignID = ""
		if myCampTV != nil {
			_ = myCampTV.SetCurrentIndex(-1)
		}
		if myCampModel != nil {
			rows := myCampModel.Rows()
			for i := range rows {
				rows[i].Selected = false
			}
			myCampModel.ReplaceRows(rows)
		}
	}

	computeMode := "implicit"

	// MARK: Helpers — live sync for selected My Campaign draft and row refresh
	findDraftIndexByID := func(id string) int {
		for i := range myCampaigns {
			if myCampaigns[i].ID == id {
				return i
			}
		}
		return -1
	}
	ensureDraftInSlice := func(id string) int {
		if id == "" {
			return -1
		}
		idx := findDraftIndexByID(id)
		if idx >= 0 {
			return idx
		}
		// Build a minimal stub from the table row if available
		name := ""
		if myCampModel != nil {
			if mi := myCampModel.IndexByID(id); mi >= 0 {
				rows := myCampModel.Rows()
				if mi >= 0 && mi < len(rows) {
					name = rows[mi].Name
				}
			}
		}
		now := nowRFC3339()
		stub := CampaignDraft{
			ID:      id,
			Name:    name,
			Product: product,
			Inputs:  CampaignInputs{RateMode: rateMode},
			Adjustments: CampaignAdjustments{
				CashDiscountTHB:     0,
				SubdownTHB:          0,
				IDCFreeInsuranceTHB: 0,
				IDCFreeMBSPTHB:      0,
			},
			Metadata: CampaignMetadata{
				CreatedAt: now,
				UpdatedAt: now,
				Version:   1,
			},
		}
		myCampaigns = append(myCampaigns, stub)
		return len(myCampaigns) - 1
	}
	updateSelectedDraftFromUI := func() {
		if !editor.IsEditMode || selectedMyCampaignID == "" {
			return
		}
		// Collect Deal Inputs from UI (same logic as myCampDeps.SeedCopy)
		price := parseFloat(priceEdit)
		dpUnit := "%"
		if dpUnitCmb != nil && dpUnitCmb.Text() != "" {
			dpUnit = dpUnitCmb.Text()
		}
		dpPercent := 0.0
		dpTHB := 0.0
		if dpUnit == "THB" {
			if dpAmountEd != nil {
				dpTHB = dpAmountEd.Value()
			}
			if price > 0 {
				dpPercent = (dpTHB / price) * 100.0
			}
		} else {
			if dpValueEd != nil {
				dpPercent = dpValueEd.Value()
			}
			dpTHB = price * (dpPercent / 100.0)
		}
		term := parseInt(termEdit)
		bu := "%"
		if balloonUnitCmb != nil && balloonUnitCmb.Text() != "" {
			bu = balloonUnitCmb.Text()
		}
		balloonPct := 0.0
		if bu == "%" {
			if balloonValueEd != nil {
				balloonPct = balloonValueEd.Value()
			}
		} else if price > 0 {
			if balloonAmountEd != nil {
				balloonPct = (balloonAmountEd.Value() / price) * 100.0
			}
		}
		apr := parseFloat(nominalRateEdit)
		target := parseFloat(targetInstallmentEdit)

		inputs := BuildCampaignInputs(price, dpPercent, dpTHB, term, balloonPct, rateMode, apr, target)
		idx := ensureDraftInSlice(selectedMyCampaignID)
		if idx >= 0 {
			now := nowRFC3339()
			myCampaigns[idx] = UpdateDraftInputs(myCampaigns[idx], inputs, now)
			campaignsDirty = true
		}
	}
	updateSelectedDraftAdjustmentsFromUI := func() {
		if !editor.IsEditMode || selectedMyCampaignID == "" || editModeUI == nil {
			return
		}
		adj := CampaignAdjustments{}
		if editModeUI.CashDiscountNE != nil {
			adj.CashDiscountTHB = editModeUI.CashDiscountNE.Value()
		}
		if editModeUI.SubdownNE != nil {
			adj.SubdownTHB = editModeUI.SubdownNE.Value()
		}
		if editModeUI.IDCInsuranceNE != nil {
			adj.IDCFreeInsuranceTHB = editModeUI.IDCInsuranceNE.Value()
		}
		if editModeUI.IDCMBSPNE != nil {
			adj.IDCFreeMBSPTHB = editModeUI.IDCMBSPNE.Value()
		}
		idx := ensureDraftInSlice(selectedMyCampaignID)
		if idx >= 0 {
			now := nowRFC3339()
			myCampaigns[idx] = UpdateDraftAdjustments(myCampaigns[idx], adj, now)
			campaignsDirty = true
		}
	}
	updateRowMonthlyFromLabels := func() {
		if !editor.IsEditMode || selectedMyCampaignID == "" || myCampModel == nil {
			return
		}
		val := ""
		if headerMonthlyLbl != nil && headerMonthlyLbl.Text() != "" {
			val = headerMonthlyLbl.Text()
		} else if monthlyLbl != nil {
			val = monthlyLbl.Text()
		}
		s := sanitizeMonthlyForRow(val)
		_ = myCampModel.SetMonthlyInstallmentByID(selectedMyCampaignID, s)
	}

	recalc = func() {
		if creatingUI {
			return
		}
		// Sync the selected draft (inputs + adjustments) from current UI when in edit mode
		if editor.IsEditMode && selectedMyCampaignID != "" {
			updateSelectedDraftFromUI()
			updateSelectedDraftAdjustmentsFromUI()
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
			// Ensure Default Campaigns grid shows placeholders (no pre-population)
			if campaignTV != nil {
				display := defaultCampaignsForUI()
				rows := placeholderCampaignRows(display)
				if campaignModel == nil {
					campaignModel = &CampaignTableModel{rows: rows}
					_ = campaignTV.SetModel(campaignModel)
				} else {
					campaignModel.ReplaceRows(rows)
				}
			}
			// Ensure selected row shows unknown monthly when params are not ready
			if editor.IsEditMode && selectedMyCampaignID != "" && myCampModel != nil {
				updateRowMonthlyFromLabels()
			}
			return
		}
		price := parseFloat(priceEdit)

		// Derive DP percent/amount from dual editors (static layout; only enabled editor is active)
		dpUnit := "%"
		if dpUnitCmb != nil && dpUnitCmb.Text() != "" {
			dpUnit = dpUnitCmb.Text()
		}
		var dpPercent, dpAmount float64
		if dpUnit == "%" {
			val := 0.0
			if dpValueEd != nil {
				val = dpValueEd.Value()
			}
			dpPercent = val
			dpAmount = price * (dpPercent / 100.0)
			dpLock = "percent"
		} else {
			val := 0.0
			if dpAmountEd != nil {
				val = dpAmountEd.Value()
			}
			dpAmount = val
			if price > 0 {
				dpPercent = (dpAmount / price) * 100.0
			}
			dpLock = "amount"
		}

		term := parseInt(termEdit)

		// Balloon percent from dual editors
		balloonSel := "%"
		if balloonUnitCmb != nil && balloonUnitCmb.Text() != "" {
			balloonSel = balloonUnitCmb.Text()
		}
		balloonPct := 0.0
		if balloonSel == "%" {
			if balloonValueEd != nil {
				balloonPct = balloonValueEd.Value()
			}
		} else if price > 0 {
			val := 0.0
			if balloonAmountEd != nil {
				val = balloonAmountEd.Value()
			}
			balloonPct = (val / price) * 100.0
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
			// Ensure Default Campaigns grid shows placeholders (no pre-population)
			if campaignTV != nil {
				display := defaultCampaignsForUI()
				rows := placeholderCampaignRows(display)
				if campaignModel == nil {
					campaignModel = &CampaignTableModel{rows: rows}
					_ = campaignTV.SetModel(campaignModel)
				} else {
					campaignModel.ReplaceRows(rows)
				}
			}
			// Reflect unknown monthly to the selected My Campaign row
			if editor.IsEditMode && selectedMyCampaignID != "" && myCampModel != nil {
				updateRowMonthlyFromLabels()
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
		// Live row refresh for selected My Campaign
		if editor.IsEditMode && selectedMyCampaignID != "" {
			updateRowMonthlyFromLabels()
		}

		// Success log for compute
		logger.Printf("compute ok: version=%v installment=THB %s rorac=%.2f%%",
			result.Metadata["parameter_set_version"],
			FormatTHB(q.MonthlyInstallment.InexactFloat64()),
			q.Profitability.AcquisitionRoRAC.Mul(types.NewDecimal(100)).InexactFloat64(),
		)

		// Profitability Details panel is driven by the selected CampaignRow snapshot via updateSummaryFromRow.

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
					dealerCommissionPill.SetText(fmt.Sprintf("override (THB %s)", FormatTHB(dealerAmt)))
				} else {
					dealerCommissionPill.SetText(fmt.Sprintf("override %.2f%% (THB %s)", dealerPct*100, FormatTHB(dealerAmt)))
				}
			} else {
				dealerCommissionPill.SetText(fmt.Sprintf("auto %.2f%% (THB %s)", dealerPct*100, FormatTHB(dealerAmt)))
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

			// Update Key Metrics Summary and Campaign Details from selected row
			if selectedCampaignIdx >= 0 && selectedCampaignIdx < len(rows) {
				sel := rows[selectedCampaignIdx]
				UpdateKeyMetrics(
					sel,
					monthlyLbl, headerMonthlyLbl,
					custNominalLbl, custEffLbl,
					roracLbl, headerRoRacLbl,
					idcTotalLbl, idcDealerLbl, idcOtherLbl,
					financedLbl,
					priceEdit, dpUnitCmb, dpValueEd, dpAmountEd,
					wfCustRateEffLbl, wfCustRateNomLbl,
					wfDealIRREffLbl, wfDealIRRNomLbl, wfIDCUpLbl, wfSubUpLbl, wfCostDebtLbl, wfMFSpreadLbl, wfGIMEffLbl, wfGIMLbl, wfCapAdvLbl, wfNIMEffLbl, wfNIMLbl, wfRiskLbl, wfOpexLbl, wfNetEbitEffLbl, wfNetEbitLbl, wfEconCapLbl, wfAcqRoRacDetailLbl,
					idcOtherEd,
				)
				UpdateCampaignDetails(
					sel,
					selCampNameValLbl, selTermValLbl, selFinancedValLbl, selSubsidyUsedValLbl, selSubsidyBudgetValLbl, selSubsidyRemainValLbl, selIDCDealerValLbl, selIDCInsValLbl, selIDCMBSPValLbl, selIDCOtherValLbl,
					priceEdit, dpUnitCmb, dpValueEd, dpAmountEd, termEdit,
					subsidyBudgetEd, idcOtherEd,
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
		// Persist sticky state after compute (debounced; ignore error)
		if s, err := CollectStickyFromUI(
			product,
			priceEdit,
			dpUnitCmb, func() *walk.NumberEdit {
				if dpUnitCmb != nil && dpUnitCmb.Text() == "THB" {
					return dpAmountEd
				}
				return dpValueEd
			}(),
			termEdit,
			timing,
			balloonUnit, balloonUnitCmb, func() *walk.NumberEdit {
				if balloonUnitCmb != nil && balloonUnitCmb.Text() == "THB" {
					return balloonAmountEd
				}
				return balloonValueEd
			}(),
			rateMode,
			nominalRateEdit,
			targetInstallmentEdit,
			subsidyBudgetEd, idcOtherEd,
			selectedCampaignIdx,
			dealState,
		); err == nil {
			ScheduleStickySave(s)
		}
	}

	// Debounced persistence helper: collect current UI and schedule a save
	queueSave := func() {
		if creatingUI {
			return
		}
		if s, err := CollectStickyFromUI(
			product,
			priceEdit,
			dpUnitCmb, func() *walk.NumberEdit {
				if dpUnitCmb != nil && dpUnitCmb.Text() == "THB" {
					return dpAmountEd
				}
				return dpValueEd
			}(),
			termEdit,
			timing,
			balloonUnit, balloonUnitCmb, func() *walk.NumberEdit {
				if balloonUnitCmb != nil && balloonUnitCmb.Text() == "THB" {
					return balloonAmountEd
				}
				return balloonValueEd
			}(),
			rateMode,
			nominalRateEdit,
			targetInstallmentEdit,
			subsidyBudgetEd, idcOtherEd,
			selectedCampaignIdx,
			dealState,
		); err == nil {
			ScheduleStickySave(s)
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
								AssignTo: &dealInputsGB,
								Title:    "Deal Inputs",
								Layout:   Grid{Columns: 2, Spacing: 6},
								Children: []Widget{
									// Basic inputs
									Label{Text: "Product:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
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
											if balloonValueEd != nil && balloonAmountEd != nil && balloonUnitCmb != nil {
												if product != "mySTAR" {
													_ = balloonValueEd.SetValue(0)
													_ = balloonAmountEd.SetValue(0)
													balloonValueEd.SetEnabled(false)
													balloonAmountEd.SetEnabled(false)
													balloonUnitCmb.SetEnabled(false)
													balloonUnit = "%"
												} else {
													// Enable only active editor by unit; keep both resident
													if balloonUnitCmb.Text() == "THB" {
														balloonValueEd.SetEnabled(false)
														balloonAmountEd.SetEnabled(true)
													} else {
														balloonValueEd.SetEnabled(true)
														balloonAmountEd.SetEnabled(false)
													}
													balloonUnitCmb.SetEnabled(true)
												}
											}
											recalc()
											queueSave()
										},
									},
									Label{Text: "Price ex tax (THB):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
									LineEdit{
										AssignTo: &priceEdit,
										Text:     "1000000",
										MinSize:  Size{Width: 360},
										OnEditingFinished: func() {
											// Reformat with thousand separators on commit
											v := parseFloat(priceEdit)
											_ = priceEdit.SetText(FormatWithThousandSep(v, 0))
											recalc()
											queueSave()
										},
									},
									Label{Text: "Down payment:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
									Composite{
										// Alignment strategy: fixed label column, fixed input min-width; keep both editors resident and toggle Enabled
										MinSize: Size{Width: 360},
										Layout:  Grid{Columns: 4, Spacing: 6, Margins: Margins{Left: 0, Top: 0, Right: 0, Bottom: 0}},
										Children: []Widget{
											NumberEdit{
												AssignTo: &dpValueEd, // percent editor (primary - first input box)
												Decimals: 2,
												MinValue: 0,
												Value:    20,
												MinSize:  Size{Width: 120},
												OnValueChanged: func() {
													// Keep suffix and THB editor synced from percent
													price := parseFloat(priceEdit)
													pct := dpValueEd.Value()
													if dpShadowLbl != nil {
														dpShadowLbl.SetText(fmt.Sprintf("(%.2f%% DP)", pct))
													}
													if dpAmountEd != nil && price >= 0 {
														thb := RoundTo(price*(pct/100.0), 0)
														if math.Abs(dpAmountEd.Value()-thb) > 0.5 {
															_ = dpAmountEd.SetValue(thb)
														}
													}
													queueSave()
												},
											},
											NumberEdit{
												AssignTo: &dpAmountEd, // THB editor (secondary)
												Decimals: 0,
												MinValue: 0,
												Value:    0,
												MinSize:  Size{Width: 120},
												Enabled:  false, // default unit is percent
												OnValueChanged: func() {
													// Keep suffix and percent editor synced from THB
													price := parseFloat(priceEdit)
													thb := dpAmountEd.Value()
													pct := 0.0
													if price > 0 {
														pct = RoundTo((thb/price)*100.0, 2)
													}
													if dpShadowLbl != nil {
														dpShadowLbl.SetText(fmt.Sprintf("(%.2f%% DP)", pct))
													}
													if dpValueEd != nil && math.Abs(dpValueEd.Value()-pct) > 1e-6 {
														_ = dpValueEd.SetValue(pct)
													}
													queueSave()
												},
											},
											ComboBox{
												AssignTo:     &dpUnitCmb,
												Model:        []string{"THB", "%"},
												CurrentIndex: 1,
												MaxSize:      Size{Width: 64},
												OnCurrentIndexChanged: func() {
													price := parseFloat(priceEdit)
													if dpValueEd == nil || dpAmountEd == nil || dpUnitCmb == nil {
														return
													}
													newUnit := dpUnitCmb.Text()
													if newUnit == "%" && dpLock != "percent" {
														// THB -> % (compute and switch enable)
														thb := dpAmountEd.Value()
														pct := 0.0
														if price > 0 {
															pct = RoundTo((thb/price)*100.0, 2)
														}
														_ = dpValueEd.SetValue(pct)
														dpValueEd.SetEnabled(true)
														dpAmountEd.SetEnabled(false)
														dpLock = "percent"
													} else if newUnit == "THB" && dpLock != "amount" {
														// % -> THB
														pct := dpValueEd.Value()
														thb := RoundTo(price*(pct/100.0), 0)
														_ = dpAmountEd.SetValue(thb)
														dpValueEd.SetEnabled(false)
														dpAmountEd.SetEnabled(true)
														dpLock = "amount"
													}
													// Refresh suffix text
													if dpShadowLbl != nil {
														pval := dpValueEd.Value() // always reflect percent
														dpShadowLbl.SetText(fmt.Sprintf("(%.2f%% DP)", pval))
													}
													recalc()
													queueSave()
												},
											},
											Label{
												AssignTo: &dpShadowLbl,
												Text:     "(0.00% DP)",
												MaxSize:  Size{Width: 120},
											},
										},
									},
									// removed legacy alignment placeholders
									// removed legacy alignment placeholders
									// removed legacy alignment placeholders
									Label{Text: "Term (months):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
									LineEdit{
										AssignTo: &termEdit,
										Text:     "36",
										MinSize:  Size{Width: 360},
										OnEditingFinished: func() {
											recalc()
											queueSave()
										},
									},
									Label{Text: "Timing:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
									ComboBox{
										AssignTo:     &timingCB,
										Model:        []string{"arrears", "advance"},
										CurrentIndex: 0,
										MinSize:      Size{Width: 360},
										OnCurrentIndexChanged: func() {
											timing = timingCB.Text()
											recalc()
											queueSave()
										},
									},
									Label{Text: "Balloon:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
									Composite{
										MinSize: Size{Width: 360},
										Layout:  Grid{Columns: 4, Spacing: 6, Margins: Margins{Left: 0, Top: 0, Right: 0, Bottom: 0}},
										Children: []Widget{
											NumberEdit{
												AssignTo: &balloonValueEd, // percent editor (primary - first input box)
												Decimals: 2,
												MinValue: 0,
												Value:    0,
												MinSize:  Size{Width: 120},
												OnValueChanged: func() {
													// Update suffix and sync THB
													price := parseFloat(priceEdit)
													pct := balloonValueEd.Value()
													if balloonShadowLbl != nil {
														balloonShadowLbl.SetText(fmt.Sprintf("(%.2f%% Balloon)", pct))
													}
													if balloonAmountEd != nil && price >= 0 {
														thb := RoundTo(price*(pct/100.0), 0)
														if math.Abs(balloonAmountEd.Value()-thb) > 0.5 {
															_ = balloonAmountEd.SetValue(thb)
														}
													}
													queueSave()
												},
											},
											NumberEdit{
												AssignTo: &balloonAmountEd, // THB editor
												Decimals: 0,
												MinValue: 0,
												Value:    0,
												MinSize:  Size{Width: 120},
												Enabled:  false,
												OnValueChanged: func() {
													price := parseFloat(priceEdit)
													thb := balloonAmountEd.Value()
													pct := 0.0
													if price > 0 {
														pct = RoundTo((thb/price)*100.0, 2)
													}
													if balloonShadowLbl != nil {
														balloonShadowLbl.SetText(fmt.Sprintf("(%.2f%% Balloon)", pct))
													}
													if balloonValueEd != nil && math.Abs(balloonValueEd.Value()-pct) > 1e-6 {
														_ = balloonValueEd.SetValue(pct)
													}
													queueSave()
												},
											},
											ComboBox{
												AssignTo:     &balloonUnitCmb,
												Model:        []string{"THB", "%"},
												CurrentIndex: 1,
												MaxSize:      Size{Width: 64},
												OnCurrentIndexChanged: func() {
													price := parseFloat(priceEdit)
													if balloonValueEd == nil || balloonAmountEd == nil || balloonUnitCmb == nil {
														return
													}
													newUnit := balloonUnitCmb.Text()
													if newUnit == "%" && balloonUnit != "%" {
														// THB -> %
														thb := balloonAmountEd.Value()
														pct := 0.0
														if price > 0 {
															pct = RoundTo((thb/price)*100.0, 2)
														}
														_ = balloonValueEd.SetValue(pct)
														balloonValueEd.SetEnabled(true)
														balloonAmountEd.SetEnabled(false)
														balloonUnit = "%"
													} else if newUnit == "THB" && balloonUnit != "THB" {
														// % -> THB
														pct := balloonValueEd.Value()
														thb := RoundTo(price*(pct/100.0), 0)
														_ = balloonAmountEd.SetValue(thb)
														balloonValueEd.SetEnabled(false)
														balloonAmountEd.SetEnabled(true)
														balloonUnit = "THB"
													}
													// Refresh suffix (percent)
													if balloonShadowLbl != nil {
														p := balloonValueEd.Value()
														balloonShadowLbl.SetText(fmt.Sprintf("(%.2f%% Balloon)", p))
													}
													recalc()
													queueSave()
												},
											},
											Label{
												AssignTo: &balloonShadowLbl,
												Text:     "(0.00% Balloon)",
												MaxSize:  Size{Width: 120},
											},
										},
									},

									// Integrated Rate Mode controls
									Label{Text: "Rate mode:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
									Composite{
										MinSize: Size{Width: 360},
										Layout:  HBox{Spacing: 6, Margins: Margins{Left: 0, Top: 0, Right: 0, Bottom: 0}},
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
													queueSave()
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
													queueSave()
												},
											},
										},
									},
									Label{Text: "Customer rate (% p.a.):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
									LineEdit{
										AssignTo: &nominalRateEdit,
										Text:     "3.99",
										MinSize:  Size{Width: 360},
										OnEditingFinished: func() {
											recalc()
											queueSave()
										},
									},
									Label{Text: "Target installment (THB):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
									LineEdit{
										AssignTo: &targetInstallmentEdit,
										Text:     "0",
										MinSize:  Size{Width: 360},
										OnEditingFinished: func() {
											v := parseFloat(targetInstallmentEdit)
											_ = targetInstallmentEdit.SetText(FormatWithThousandSep(v, 0))
											recalc()
											queueSave()
										},
									},

									// Product Subsidy (moved under Deal Inputs)
									Label{Text: "Subsidy budget (THB):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
									NumberEdit{
										AssignTo: &subsidyBudgetEd,
										Decimals: 0,
										MinValue: 0,
										Value:    0,
										MinSize:  Size{Width: 360},

										ToolTipText: "Budget available for subsidies (placeholder)",
										OnValueChanged: func() {
											// Recompute grid metrics when subsidy budget changes
											recalc()
											queueSave()
										},
									},
									// Commission input presentation (label+value control consistent with others)
									Label{Text: "IDCs — Commissions:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
									PushButton{
										AssignTo:    &dealerCommissionPill,
										Text:        "auto",
										Enabled:     true,
										MinSize:     Size{Width: 360},
										ToolTipText: "Auto-calculated from product policy; click to override or reset",
										OnClicked: func() {
											// Open editor; if accepted, trigger recompute
											if editDealerCommission(mw, &dealState) {
												recalc()
												queueSave()
											}
										},
									},
									Label{Text: "IDCs - Other (THB):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}},
									NumberEdit{
										AssignTo: &idcOtherEd,
										Decimals: 0,
										MinValue: 0,
										Value:    0,
										MinSize:  Size{Width: 360},

										OnValueChanged: func() {
											// Mark user-edited and recalc to refresh IDC totals and grid
											dealState.IDCOther.Value = idcOtherEd.Value()
											dealState.IDCOther.UserEdited = true
											recalc()
											queueSave()
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
									queueSave()
									computeMode = "implicit"
								},
							},
						},
					},
					// Right: Results (Tabs)
					TabWidget{
						AssignTo:      &mainTabs,
						StretchFactor: 2,
						Pages: []TabPage{
							{
								Title:  "Calculator",
								Layout: VBox{Spacing: 8},
								Children: []Widget{
									// Campaign Options (grid)
									Composite{
										Layout: HBox{Spacing: 6},
										Children: []Widget{
											PushButton{
												Text: "Copy Selected to My Campaigns",
												OnClicked: func() {
													if err := HandleMyCampaignCopySelected(myCampDeps); err != nil {
														walk.MsgBox(mw, "Copy Selected to My Campaigns", fmt.Sprintf("Copy failed: %v", err), walk.MsgBoxIconError)
													} else {
														// Keep canonical slice in sync
														if myCampModel != nil {
															myCampaigns = myCampModel.ToDrafts()
														}
													}
												},
											},
										},
									},
									TableView{
										AssignTo:       &campaignTV,
										StretchFactor:  1,
										MultiSelection: false,
										Columns: []TableViewColumn{
											{Title: "Select", Width: 70},
											{Title: "Campaign", Width: 220},
											{Title: "Monthly Installment", Width: 180},
											{Title: "Downpayment", Width: 120},
											{Title: "Cash Discount", Width: 140},
											{Title: "Free MBSP THB", Width: 140},
											{Title: "Subsidy / Acq.RoRAC", Width: 180},
											{Title: "Dealer Comm.", Width: 160},
											{Title: "Notes", Width: 220},
										},
										OnCurrentIndexChanged: func() {
											if campaignTV != nil {
												selectedCampaignIdx = campaignTV.CurrentIndex()
											}
											// Leaving Default Campaigns selection disables edit mode for My Campaigns
											ExitEditMode(&editor)
											// Clear any selected My Campaign while browsing Default Campaigns
											selectedMyCampaignID = ""
											ShowHighLevelState(editModeUI)
										},
										ContextMenuItems: []MenuItem{
											Action{
												Text: "Copy Selected to My Campaigns",
												OnTriggered: func() {
													if err := HandleMyCampaignCopySelected(myCampDeps); err != nil {
														walk.MsgBox(mw, "Copy Selected to My Campaigns", fmt.Sprintf("Copy failed: %v", err), walk.MsgBoxIconError)
													} else {
														// Keep canonical slice in sync
														if myCampModel != nil {
															myCampaigns = myCampModel.ToDrafts()
														}
													}
												},
											},
										},
									},
									// My Campaigns (Editable)
									GroupBox{
										Title:  "My Campaigns (Editable)",
										Layout: VBox{Spacing: 6},
										Children: []Widget{
											Composite{
												Layout: HBox{Spacing: 6},
												Children: []Widget{
													PushButton{
														AssignTo: &btnNewBlankCampaign,
														Text:     "+ New Blank Campaign",
														OnClicked: func() {
															if err := HandleMyCampaignNewBlank(myCampDeps); err != nil {
																walk.MsgBox(mw, "New Blank Campaign", fmt.Sprintf("Create failed: %v", err), walk.MsgBoxIconError)
															} else {
																// Keep canonical slice in sync
																if myCampModel != nil {
																	myCampaigns = myCampModel.ToDrafts()
																}
															}
														},
													},
													PushButton{
														AssignTo: &btnLoadCampaigns,
														Text:     "Load Campaigns",
														OnClicked: func() {
															// Dirty guard: confirm discarding unsaved changes
															if campaignsDirty {
																choice := walk.MsgBox(mw, "Load Campaigns", "You have unsaved changes. Load will discard them. Continue?", walk.MsgBoxYesNoCancel|walk.MsgBoxIconWarning)
																if choice != walk.DlgCmdYes {
																	return
																}
															}
															if err := HandleMyCampaignLoad(myCampDeps); err != nil {
																walk.MsgBox(mw, "Load Campaigns", fmt.Sprintf("Load failed: %v", err), walk.MsgBoxIconError)
																return
															}
															// Optional toast
															walk.MsgBox(mw, "Load Campaigns", "Loaded My Campaigns.", walk.MsgBoxOK|walk.MsgBoxIconInformation)
														},
													},
													PushButton{
														AssignTo: &btnSaveAllCampaigns,
														Text:     "Save All Changes",
														OnClicked: func() {
															if err := HandleMyCampaignSaveAll(myCampDeps); err != nil {
																walk.MsgBox(mw, "Save Campaigns", fmt.Sprintf("Save failed: %v", err), walk.MsgBoxIconError)
																return
															}
															// Success toast and dirty reset already handled by handler
															walk.MsgBox(mw, "Save Campaigns", "Saved My Campaigns.", walk.MsgBoxOK|walk.MsgBoxIconInformation)
														},
													},
													PushButton{
														AssignTo: &btnClearCampaigns,
														Text:     "Clear Campaigns",
														OnClicked: func() {
															// Dirty-aware confirmation per spec
															if campaignsDirty {
																choice := walk.MsgBox(mw, "Clear Campaigns", "Clear all My Campaigns and discard unsaved changes?", walk.MsgBoxYesNo|walk.MsgBoxIconWarning)
																if choice != walk.DlgCmdYes {
																	return
																}
															}
															if err := HandleMyCampaignClear(myCampDeps); err != nil {
																walk.MsgBox(mw, "Clear Campaigns", fmt.Sprintf("Clear failed: %v", err), walk.MsgBoxIconError)
																return
															}
															// Canonical slice cleared via callback; ensure nil
															myCampaigns = nil
														},
													},
												},
											},
											TableView{
												AssignTo:       &myCampTV,
												StretchFactor:  1,
												MultiSelection: false,
												Columns: []TableViewColumn{
													{Title: "Sel", Width: 70},
													{Title: "Campaign", Width: 220},
													{Title: "Monthly Installment", Width: 180},
													{Title: "Notes", Width: 220},
												},
												OnCurrentIndexChanged: func() {
													if myCampModel == nil || myCampTV == nil {
														return
													}
													idx := myCampTV.CurrentIndex()
													if idx >= 0 && idx < myCampModel.RowCount() {
														myCampModel.SetSelectedIndex(idx)
														id := ""
														if idx < len(myCampModel.rows) {
															id = myCampModel.rows[idx].ID
														}
														selectedMyCampaignID = id
														SelectMyCampaign(&editor, id)
														// Show edit section with campaign name
														name := ""
														rowsCopy := myCampModel.Rows()
														if idx >= 0 && idx < len(rowsCopy) {
															name = rowsCopy[idx].Name
														}
														ShowCampaignEditState(editModeUI, name)
													} else {
														// Clear bullets and exit edit mode
														rows := myCampModel.rows
														for i := range rows {
															rows[i].Selected = false
														}
														myCampModel.ReplaceRows(rows)
														selectedMyCampaignID = ""
														ExitEditMode(&editor)
														ShowHighLevelState(editModeUI)
													}
												},
												ContextMenuItems: []MenuItem{
													Action{
														Text: "Delete",
														OnTriggered: func() {
															if err := HandleMyCampaignDelete(myCampDeps); err != nil {
																walk.MsgBox(mw, "Delete Campaign", fmt.Sprintf("Delete failed: %v", err), walk.MsgBoxIconError)
															} else {
																// Keep canonical slice in sync
																if myCampModel != nil {
																	myCampaigns = myCampModel.ToDrafts()
																}
															}
														},
													},
												},
											},
										},
									},
									// Summary
									Composite{
										Layout: Grid{Columns: 2, Spacing: 8, MarginsZero: true},
										Children: []Widget{
											// Left half: Campaign Details (4-column grid)
											GroupBox{
												Title: "Campaign Details",
												Row:   0, Column: 0,
												Layout: Grid{Columns: 4, Spacing: 6},
												Children: []Widget{
													// Row 1
													Label{Text: "Campaign Name:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &selCampNameValLbl, Text: "-"},
													Label{Text: "Term (months):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &selTermValLbl, Text: "-"},
													// Row 2
													Label{Text: "Financed Amount:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &selFinancedValLbl, Text: "-"},
													Label{Text: "Subsidy budget (THB):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &selSubsidyBudgetValLbl, Text: "-"},
													// Row 3
													Label{Text: "Subsidy utilized (THB):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &selSubsidyUsedValLbl, Text: "-"},
													Label{Text: "Subsidy remaining (THB):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &selSubsidyRemainValLbl, Text: "-"},
													// Row 4
													Label{Text: "Dealer Commissions Paid (THB):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &selIDCDealerValLbl, Text: "-"},
													Label{Text: "IDCs - Others (THB):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &selIDCOtherValLbl, Text: "-"},
													// Row 5
													Label{Text: "IDC - Free Insurance (THB):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &selIDCInsValLbl, Text: "THB 0"},
													Label{Text: "IDC - Free MBSP (THB):", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &selIDCMBSPValLbl, Text: "THB 0"},
												},
											},

											// Right half: Key Metrics Summary in 4-column grid
											GroupBox{
												Title: "Key Metrics Summary",
												Row:   0, Column: 1,
												Layout: Grid{Columns: 4, Spacing: 6},
												Children: []Widget{
													// Row 1
													Label{Text: "Monthly Installment:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &monthlyLbl, Text: "-"},
													Label{Text: "Nominal Customer Rate:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &custNominalLbl, Text: "-"},
													// Row 2
													Label{Text: "Effective Rate:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &custEffLbl, Text: "-"},
													Label{Text: "Financed Amount:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &financedLbl, Text: "-"},
													// Row 3
													Label{Text: "Acquisition RoRAC:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &roracLbl, Text: "-"},
													Label{Text: "IDC Total:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &idcTotalLbl, Text: "-"},
													// Row 4
													Label{Text: "IDC - Dealer Comm.:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &idcDealerLbl, Text: "-"},
													Label{Text: "IDC - Other:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &idcOtherLbl, Text: "-"},
													// Profitability Details (toggle) spanning all 4 columns
													GroupBox{
														Title:      "Profitability Details",
														ColumnSpan: 4,
														Layout:     Grid{Columns: 2, Spacing: 6},
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
																Layout:     Grid{Columns: 3, Spacing: 6},
																Children: []Widget{
																	// Header row
																	Label{Text: ""}, Label{Text: "Effective"}, Label{Text: "Nominal"},
																	// Customer Rate
																	Label{Text: "Customer Rate in %:"},
																	Label{AssignTo: &wfCustRateEffLbl, Text: "—"},
																	Label{AssignTo: &wfCustRateNomLbl, Text: "—"},
																	// Subsidy Upfront (income) %
																	Label{Text: "+ Subsidy Upfront (income) %:"},
																	Label{Text: "—"},
																	Label{AssignTo: &wfSubUpLbl, Text: "—"},
																	// IDC Upfront (cost) %
																	Label{Text: "− IDC Upfront (cost) %:"},
																	Label{Text: "—"},
																	Label{AssignTo: &wfIDCUpLbl, Text: "—"},
																	// Deal IRR
																	Label{Text: "= Deal Rate (IRR):"},
																	Label{AssignTo: &wfDealIRREffLbl, Text: "—"},
																	Label{AssignTo: &wfDealIRRNomLbl, Text: "—"},
																	// Cost of Debt
																	Label{Text: "− Cost of Debt (matched):"},
																	Label{Text: "—"},
																	Label{AssignTo: &wfCostDebtLbl, Text: "—"},
																	// Gross Interest Margin
																	Label{Text: "= Gross Interest Margin:"},
																	Label{AssignTo: &wfGIMEffLbl, Text: "—"},
																	Label{AssignTo: &wfGIMLbl, Text: "—"},
																	// Capital Advantage
																	Label{Text: "+ Capital Advantage:"},
																	Label{Text: "—"},
																	Label{AssignTo: &wfCapAdvLbl, Text: "—"},
																	// Net Interest Margin
																	Label{Text: "= Net Interest Margin:"},
																	Label{AssignTo: &wfNIMEffLbl, Text: "—"},
																	Label{AssignTo: &wfNIMLbl, Text: "—"},
																	// Cost of Credit Risk
																	Label{Text: "− Cost of Credit Risk:"},
																	Label{Text: "—"},
																	Label{AssignTo: &wfRiskLbl, Text: "—"},
																	// OPEX
																	Label{Text: "− OPEX:"},
																	Label{Text: "—"},
																	Label{AssignTo: &wfOpexLbl, Text: "—"},
																	// Net EBIT Margin (remove periodic IDC line per HQ UI spec)
																	Label{Text: "= Net EBIT Margin:"},
																	Label{AssignTo: &wfNetEbitEffLbl, Text: "—"},
																	Label{AssignTo: &wfNetEbitLbl, Text: "—"},
																	// Economic Capital
																	Label{Text: "/ Economic Capital:"},
																	Label{Text: "—"},
																	Label{AssignTo: &wfEconCapLbl, Text: "—"},
																	// Acquisition RoRAC
																	Label{Text: "= Acquisition RoRAC:"},
																	Label{Text: "—"},
																	Label{AssignTo: &wfAcqRoRacDetailLbl, Text: "—"},
																},
															},
														},
													},
													// Row 5: Parameter Version at the very bottom (last row)
													Label{Text: "Parameter Version:", MinSize: Size{Width: 160}, MaxSize: Size{Width: 160}}, Label{AssignTo: &metaVersionLbl, Text: "-"},
													Label{Text: ""}, Label{Text: ""},
												},
											},

											// Export XLSX full-width under both boxes
											PushButton{Row: 1, Column: 0,
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
													if dpUnitCmb != nil {
														unit := dpUnitCmb.Text()
														val := 0.0
														if unit == "THB" && dpAmountEd != nil {
															val = dpAmountEd.Value()
															summary["Down payment"] = "THB " + FormatWithThousandSep(val, 0)
														} else if dpValueEd != nil {
															val = dpValueEd.Value()
															summary["Down payment"] = FormatWithThousandSep(val, 2) + " percent"
														}
													}
													summary["Term (months)"] = fmt.Sprintf("%d", parseInt(termEdit))
													if timingCB != nil {
														summary["Timing"] = timingCB.Text()
													} else {
														summary["Timing"] = timing
													}
													if balloonUnitCmb != nil {
														bu := balloonUnitCmb.Text()
														if bu == "THB" && balloonAmountEd != nil {
															bv := balloonAmountEd.Value()
															summary["Balloon"] = "THB " + FormatWithThousandSep(bv, 0)
														} else if balloonValueEd != nil {
															bv := balloonValueEd.Value()
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
											{Title: "Period", Width: 70},
											{Title: "Date", Width: 110},
											{Title: "Principal Outflow", Width: 140},
											{Title: "Downpayment Inflow", Width: 150},
											{Title: "Balloon Inflow", Width: 130},
											{Title: "Principal Amortization", Width: 170},
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
		// Load My Campaigns from AppData (Interactive Campaign Manager)
		if list, ver, err := LoadCampaigns(); err != nil {
			logger.Printf("warn: LoadCampaigns failed: %v", err)
			myCampaigns = []CampaignDraft{}
			campaignsFileVersion = CampaignsFileVersion
		} else {
			myCampaigns = list
			campaignsFileVersion = ver
		}
		campaignsDirty = false

		// Bind My Campaigns model to TableView
		if myCampTV != nil {
			myCampModel = NewMyCampaignsTableModel()
			if err := myCampTV.SetModel(myCampModel); err != nil {
				logger.Printf("warn: my campaigns table model set failed: %v", err)
			}
			myCampModel.ReplaceFromDrafts(myCampaigns)
			myCampDeps.Model = myCampModel
		}
		// Build progressive disclosure UI under Deal Inputs
		if dealInputsGB != nil {
			if ui, err := NewEditModeUI(dealInputsGB); err != nil {
				logger.Printf("warn: NewEditModeUI failed: %v", err)
			} else {
				editModeUI = ui
				ShowHighLevelState(editModeUI)
				// Hook adjustments NumberEdits to live-sync draft and recalc when editing
				attachAdj := func(ne *walk.NumberEdit) {
					if ne != nil {
						ne.ValueChanged().Attach(func() {
							if editor.IsEditMode && selectedMyCampaignID != "" {
								updateSelectedDraftAdjustmentsFromUI()
								recalc()
							}
						})
					}
				}
				attachAdj(editModeUI.CashDiscountNE)
				attachAdj(editModeUI.SubdownNE)
				attachAdj(editModeUI.IDCInsuranceNE)
				attachAdj(editModeUI.IDCMBSPNE)
			}
		}
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
				// Apply product gating early before other fields to keep Balloon UI consistent
				if balloonValueEd != nil && balloonAmountEd != nil && balloonUnitCmb != nil {
					if product != "mySTAR" {
						balloonValueEd.SetEnabled(false)
						balloonAmountEd.SetEnabled(false)
						balloonUnitCmb.SetEnabled(false)
					} else {
						if balloonUnitCmb.Text() == "THB" {
							balloonValueEd.SetEnabled(false)
							balloonAmountEd.SetEnabled(true)
						} else {
							balloonValueEd.SetEnabled(true)
							balloonAmountEd.SetEnabled(false)
						}
						balloonUnitCmb.SetEnabled(true)
					}
				}
			}
			// Price
			if priceEdit != nil {
				_ = priceEdit.SetText(FormatWithThousandSep(s.Price, 0))
			}
			// DP unit + value (both editors resident; toggle Enabled)
			if dpUnitCmb != nil {
				_ = dpUnitCmb.SetText(s.DPUnit)
			}
			if dpValueEd != nil && dpAmountEd != nil {
				price := parseFloat(priceEdit)
				if s.DPUnit == "THB" {
					_ = dpAmountEd.SetDecimals(0)
					_ = dpAmountEd.SetValue(s.DPValue)
					// derive %
					pct := 0.0
					if price > 0 {
						pct = RoundTo((s.DPValue/price)*100.0, 2)
					}
					_ = dpValueEd.SetDecimals(2)
					_ = dpValueEd.SetValue(pct)
					dpValueEd.SetEnabled(false)
					dpAmountEd.SetEnabled(true)
					dpLock = "amount"
				} else {
					_ = dpValueEd.SetDecimals(2)
					_ = dpValueEd.SetValue(s.DPValue)
					// derive THB
					thb := RoundTo(price*(s.DPValue/100.0), 0)
					_ = dpAmountEd.SetDecimals(0)
					_ = dpAmountEd.SetValue(thb)
					dpValueEd.SetEnabled(true)
					dpAmountEd.SetEnabled(false)
					dpLock = "percent"
				}
			}
			// Refresh DP suffix label to mirror current percent
			if dpShadowLbl != nil && dpValueEd != nil {
				dpShadowLbl.SetText(fmt.Sprintf("(%.2f%% DP)", dpValueEd.Value()))
			}
			// Term
			if termEdit != nil {
				_ = termEdit.SetText(fmt.Sprintf("%d", s.Term))
			}
			// Timing (default to arrears)
			timing = s.Timing
			if strings.TrimSpace(timing) == "" {
				timing = "arrears"
			}
			if timingCB != nil {
				_ = timingCB.SetText(timing)
			}
			// Balloon unit + value and enabling based on product
			balloonUnit = s.BalloonUnit
			if strings.TrimSpace(balloonUnit) == "" {
				balloonUnit = "%"
			}
			if balloonUnitCmb != nil {
				_ = balloonUnitCmb.SetText(balloonUnit)
			}
			if balloonValueEd != nil && balloonAmountEd != nil {
				price := parseFloat(priceEdit)
				if s.BalloonUnit == "THB" {
					_ = balloonAmountEd.SetDecimals(0)
					_ = balloonAmountEd.SetValue(s.BalloonValue)
					// derive %
					pct := 0.0
					if price > 0 {
						pct = RoundTo((s.BalloonValue/price)*100.0, 2)
					}
					_ = balloonValueEd.SetDecimals(2)
					_ = balloonValueEd.SetValue(pct)
				} else {
					_ = balloonValueEd.SetDecimals(2)
					_ = balloonValueEd.SetValue(s.BalloonValue)
					// derive THB
					thb := RoundTo(price*(s.BalloonValue/100.0), 0)
					_ = balloonAmountEd.SetDecimals(0)
					_ = balloonAmountEd.SetValue(thb)
				}
				if product != "mySTAR" {
					balloonValueEd.SetEnabled(false)
					balloonAmountEd.SetEnabled(false)
					if balloonUnitCmb != nil {
						balloonUnitCmb.SetEnabled(false)
					}
				} else {
					if s.BalloonUnit == "THB" {
						balloonValueEd.SetEnabled(false)
						balloonAmountEd.SetEnabled(true)
					} else {
						balloonValueEd.SetEnabled(true)
						balloonAmountEd.SetEnabled(false)
					}
					if balloonUnitCmb != nil {
						balloonUnitCmb.SetEnabled(true)
					}
				}
			}
			// Refresh Balloon suffix label to mirror current percent
			if balloonShadowLbl != nil && balloonValueEd != nil {
				balloonShadowLbl.SetText(fmt.Sprintf("(%.2f%% Balloon)", balloonValueEd.Value()))
			}
			// Rate mode + values (default to fixed_rate)
			rateMode = s.RateMode
			if strings.TrimSpace(rateMode) == "" {
				rateMode = "fixed_rate"
			}
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
			// Restore Dealer Commission (UI) from persisted IDCCommission* fields
			if s.IDCCommissionMode != "" {
				if strings.ToUpper(s.IDCCommissionMode) == "MANUAL" {
					dealState.DealerCommission.Mode = types.DealerCommissionModeOverride
					dealState.DealerCommission.Amt = nil
					dealState.DealerCommission.Pct = nil
					if s.IDCCommissionTHB > 0 {
						v := s.IDCCommissionTHB
						dealState.DealerCommission.Amt = &v
					} else if s.IDCCommissionPercent > 0 {
						p := s.IDCCommissionPercent / 100.0
						dealState.DealerCommission.Pct = &p
					}
				} else {
					dealState.DealerCommission.Mode = types.DealerCommissionModeAuto
					dealState.DealerCommission.Amt = nil
					dealState.DealerCommission.Pct = nil
				}
			}
			// Selected campaign index (prefer v2 pointer if provided)
			selectedCampaignIdx = s.SelectedCampaignIx
			if s.SelectedCampaignIndex != nil {
				selectedCampaignIdx = *s.SelectedCampaignIndex
			}
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
			// Optionally restore previously selected campaign row if valid
			if selectedCampaignIdx >= 0 && selectedCampaignIdx < campaignModel.RowCount() {
				_ = campaignTV.SetCurrentIndex(selectedCampaignIdx)
			}

			// Selection behavior: update summary/details and cashflow from selected row; do not mutate inputs
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
				queueSave()

				// Reflect "radio dot" selection
				for i := range campaignModel.rows {
					campaignModel.rows[i].Selected = (i == idx)
				}
				campaignModel.PublishRowsReset()

				// Update summary and Profitability Details from the row's snapshot
				row := campaignModel.rows[idx]
				UpdateKeyMetrics(
					row,
					monthlyLbl, headerMonthlyLbl,
					custNominalLbl, custEffLbl,
					roracLbl, headerRoRacLbl,
					idcTotalLbl, idcDealerLbl, idcOtherLbl,
					financedLbl,
					priceEdit, dpUnitCmb, dpValueEd, dpAmountEd,
					wfCustRateEffLbl, wfCustRateNomLbl,
					wfDealIRREffLbl, wfDealIRRNomLbl, wfIDCUpLbl, wfSubUpLbl, wfCostDebtLbl, wfMFSpreadLbl, wfGIMEffLbl, wfGIMLbl, wfCapAdvLbl, wfNIMEffLbl, wfNIMLbl, wfRiskLbl, wfOpexLbl /* removed wfIDCPeLbl */, wfNetEbitEffLbl, wfNetEbitLbl, wfEconCapLbl, wfAcqRoRacDetailLbl,
					idcOtherEd,
				)

				// Update Campaign Details
				UpdateCampaignDetails(
					row,
					selCampNameValLbl, selTermValLbl, selFinancedValLbl, selSubsidyUsedValLbl, selSubsidyBudgetValLbl, selSubsidyRemainValLbl, selIDCDealerValLbl, selIDCInsValLbl, selIDCMBSPValLbl, selIDCOtherValLbl,
					priceEdit, dpUnitCmb, dpValueEd, dpAmountEd, termEdit,
					subsidyBudgetEd, idcOtherEd,
				)

				// If Cashflow tab is active, refresh from the selected row
				if mainTabs != nil && mainTabs.CurrentIndex() == 1 && cashflowTV != nil {
					refreshCashflowTable(cashflowTV, row.Cashflows)
				}
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

	// App-exit guard + persist sticky state on window close
	mw.Closing().Attach(func(canceled *bool, reason walk.CloseReason) {
		// Guard unsaved My Campaign changes
		if campaignsDirty {
			choice := walk.MsgBox(mw, "Unsaved My Campaigns", "You have unsaved changes. Save before closing?", walk.MsgBoxYesNoCancel|walk.MsgBoxIconWarning)
			switch choice {
			case walk.DlgCmdYes:
				// Attempt to save; on error keep app open
				if err := HandleMyCampaignSaveAll(myCampDeps); err != nil {
					walk.MsgBox(mw, "Save Campaigns", fmt.Sprintf("Save failed: %v", err), walk.MsgBoxIconError)
					*canceled = true
					return
				}
			case walk.DlgCmdNo:
				// Discard
				campaignsDirty = false
			default:
				// Cancel app close
				*canceled = true
				return
			}
		}

		if s, err := CollectStickyFromUI(
			product,
			priceEdit,
			dpUnitCmb, func() *walk.NumberEdit {
				if dpUnitCmb != nil && dpUnitCmb.Text() == "THB" {
					return dpAmountEd
				}
				return dpValueEd
			}(),
			termEdit,
			timing,
			balloonUnit, balloonUnitCmb, func() *walk.NumberEdit {
				if balloonUnitCmb != nil && balloonUnitCmb.Text() == "THB" {
					return balloonAmountEd
				}
				return balloonValueEd
			}(),
			rateMode,
			nominalRateEdit,
			targetInstallmentEdit,
			subsidyBudgetEd, idcOtherEd,
			selectedCampaignIdx,
			dealState,
		); err == nil {
			FlushStickySave(s)
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
			BaseCapitalRatio:     types.NewDecimal(0.088),
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

// ProfitabilitySnapshot caches computed waterfall lines per row to avoid recompute on selection clicks.
type ProfitabilitySnapshot struct {
	DealIRREffective    float64
	DealIRRNominal      float64
	IDCUpfrontCostPct   float64
	SubsidyUpfrontPct   float64
	CostOfDebt          float64
	MatchedFundedSpread float64
	GrossInterestMargin float64
	CapitalAdvantage    float64
	NetInterestMargin   float64
	CostOfCreditRisk    float64
	OPEX                float64
	IDCPeriodicPct      float64
	SubsidyPeriodicPct  float64
	NetEBITMargin       float64
	EconomicCapital     float64
	AcquisitionRoRAC    float64
}

type CampaignRow struct {
	Selected bool

	// Display fields (stringified for grid bindings)
	Name                  string // campaign display name
	MonthlyInstallmentStr string // e.g., "22,198.61"
	DownpaymentStr        string // e.g., "20%"
	CashDiscountStr       string // e.g., "THB 50,000" for cash discount row; "—" otherwise
	MBSPTHBStr            string // e.g., "THB 5,000" only for Free MBSP rows; "—" otherwise
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
	DealerCommAmt   float64
	DealerCommPct   float64
	SubsidyValue    float64
	CashDiscountTHB float64

	// Profit snapshot for details panel (per-row)
	Profit ProfitabilitySnapshot

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
				MBSPTHBStr:            "",
				SubsidyRorac:          "- / -",
				Notes:                 "Baseline (placeholder)",
			},
			{
				Selected:              false,
				Name:                  "Subinterest 2.99%",
				MonthlyInstallmentStr: "",
				DownpaymentStr:        "20%",
				MBSPTHBStr:            "",
				SubsidyRorac:          "THB 0 / 8.5%",
				Notes:                 "Static row (Phase 1)",
			},
			{
				Selected:              false,
				Name:                  "Subdown 5%",
				MonthlyInstallmentStr: "",
				DownpaymentStr:        "15%",
				MBSPTHBStr:            "",
				SubsidyRorac:          "THB 50,000 / 6.8%",
				Notes:                 "Static row (Phase 1)",
			},
			{
				Selected:              false,
				Name:                  "Free Insurance",
				MonthlyInstallmentStr: "",
				DownpaymentStr:        "20%",
				MBSPTHBStr:            "",
				SubsidyRorac:          "THB 15,000 / 7.2%",
				Notes:                 "Static row (Phase 1)",
			},
		},
	}
}

// Build placeholder rows with dashes until inputs are valid.
func placeholderCampaignRows(camps []types.Campaign) []CampaignRow {
	rows := make([]CampaignRow, 0, len(camps))
	for i, c := range camps {
		rows = append(rows, CampaignRow{
			Selected:              i == 0,
			Name:                  campaignTypeDisplayName(c.Type),
			MonthlyInstallmentStr: "",
			DownpaymentStr:        "",
			CashDiscountStr:       "",
			MBSPTHBStr:            "",
			NominalRateStr:        "",
			EffectiveRateStr:      "",
			AcqRoRacStr:           "",
			SubsidyRorac:          "—",
			DealerComm:            "",
			Notes:                 "",
		})
	}
	return rows
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
		if r.CashDiscountStr == "" {
			return "—"
		}
		return r.CashDiscountStr
	case 5: // Free MBSP THB
		if r.MBSPTHBStr == "" {
			return "—"
		}
		return r.MBSPTHBStr
	case 6:
		return r.SubsidyRorac
	case 7:
		return r.DealerComm
	case 8:
		return r.Notes
	default:
		return ""
	}
}

// Map engine campaign type to display name for the grid.
func campaignTypeDisplayName(t types.CampaignType) string {
	switch t {
	case types.CampaignBaseNoSubsidy:
		return "Base (no subsidy)"
	case types.CampaignBaseSubsidy:
		return "Base (subsidy included)"
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
			ID:       "BASE-NO-SUB",
			Type:     types.CampaignBaseNoSubsidy,
			Funder:   "",
			Stacking: 0,
		},
		{
			ID:       "BASE-WITH-SUB",
			Type:     types.CampaignBaseSubsidy,
			Funder:   "",
			Stacking: 1,
		},
		{
			ID:             "SUBDOWN-5",
			Type:           types.CampaignSubdown,
			SubsidyPercent: types.NewDecimal(0.05),
			Funder:         "Dealer",
			Stacking:       2,
		},
		{
			ID:         "SUBINT-299",
			Type:       types.CampaignSubinterest,
			TargetRate: types.NewDecimal(0.0299),
			Funder:     "Manufacturer",
			Stacking:   3,
		},
		{
			ID:            "FREE-INS",
			Type:          types.CampaignFreeInsurance,
			InsuranceCost: types.NewDecimal(15000),
			Funder:        "Insurance Partner",
			Stacking:      4,
		},
		{
			ID:       "FREE-MBSP",
			Type:     types.CampaignFreeMBSP,
			MBSPCost: types.NewDecimal(5000),
			Funder:   "Manufacturer",
			Stacking: 5,
		},
		{
			ID:              "CASH-DISC-2",
			Type:            types.CampaignCashDiscount,
			DiscountPercent: types.NewDecimal(0.02),
			Funder:          "Dealer",
			Stacking:        6,
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

// updateSelectedDetailsFromRow sets the left-panel details based on selected row and current inputs.
func updateSelectedDetailsFromRow(
	row CampaignRow,
	selCampNameValLbl, selTermValLbl, selFinancedValLbl, selSubsidyValLbl, selIDCDealerValLbl, selIDCInsValLbl, selIDCMBSPValLbl *walk.Label,
	priceEdit *walk.LineEdit, dpUnitCmb *walk.ComboBox, dpValueEd, dpAmountEd *walk.NumberEdit, termEdit *walk.LineEdit,
) {
	// Campaign name
	if selCampNameValLbl != nil {
		selCampNameValLbl.SetText(row.Name)
	}
	// Term (months)
	term := parseInt(termEdit)
	if selTermValLbl != nil {
		selTermValLbl.SetText(fmt.Sprintf("%d", term))
	}
	// Financed Amount (same logic as Key Metrics)
	price := parseFloat(priceEdit)
	dpAmt := 0.0
	if dpUnitCmb != nil && dpUnitCmb.Text() == "THB" && dpAmountEd != nil {
		dpAmt = dpAmountEd.Value()
	} else if dpValueEd != nil {
		dpAmt = RoundTo(price*(dpValueEd.Value()/100.0), 0)
	}
	financed := price - dpAmt
	if financed < 0 {
		financed = 0
	}
	if selFinancedValLbl != nil {
		selFinancedValLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(financed)))
	}
	// Subsidy utilized (THB): parse from "THB X / ..." or default 0
	subsidyUsed := 0.0
	s := strings.TrimSpace(row.SubsidyRorac)
	if strings.HasPrefix(s, "THB ") {
		rest := strings.TrimPrefix(s, "THB ")
		// cut at slash if present
		if i := strings.Index(rest, "/"); i >= 0 {
			rest = strings.TrimSpace(rest[:i])
		}
		rest = strings.ReplaceAll(rest, ",", "")
		if v, err := strconv.ParseFloat(rest, 64); err == nil {
			subsidyUsed = v
		}
	}
	if selSubsidyValLbl != nil {
		selSubsidyValLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(subsidyUsed)))
	}
	// Dealer commissions paid (THB)
	if selIDCDealerValLbl != nil {
		selIDCDealerValLbl.SetText(fmt.Sprintf("THB %s", FormatTHB(row.IDCDealerTHB)))
	}
	// Other IDC breakdown currently unavailable -> 0
	if selIDCInsValLbl != nil {
		selIDCInsValLbl.SetText("THB 0")
	}
	if selIDCMBSPValLbl != nil {
		selIDCMBSPValLbl.SetText("THB 0")
	}
}

func updateSummaryFromRow(
	row CampaignRow,
	monthlyLbl, headerMonthlyLbl *walk.Label,
	custNominalLbl, custEffLbl *walk.Label,
	roracLbl, headerRoRacLbl *walk.Label,
	wfCustRateEffLbl, wfCustRateNomLbl *walk.Label,
	wfDealIRREffLbl, wfDealIRRNomLbl, wfIDCUpLbl, wfSubUpLbl, wfCostDebtLbl, wfMFSpreadLbl, wfGIMEffLbl, wfGIMLbl, wfCapAdvLbl, wfNIMEffLbl, wfNIMLbl, wfRiskLbl, wfOpexLbl /*wfIDCPeLbl,*/, wfNetEbitEffLbl, wfNetEbitLbl, wfEconCapLbl, wfAcqRoRacDetailLbl *walk.Label,
) {
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
		if row.AcqRoRacStr != "" {
			roracLbl.SetText(row.AcqRoRacStr)
		} else if row.AcqRoRac != 0 {
			roracLbl.SetText(fmt.Sprintf("%.2f%%", row.AcqRoRac*100.0))
		} else {
			roracLbl.SetText("—")
		}
	}
	if headerRoRacLbl != nil {
		if row.AcqRoRacStr != "" {
			headerRoRacLbl.SetText(row.AcqRoRacStr)
		} else if row.AcqRoRac != 0 {
			headerRoRacLbl.SetText(fmt.Sprintf("%.2f%%", row.AcqRoRac*100.0))
		} else {
			headerRoRacLbl.SetText("—")
		}
	}

	// Profitability Details from per-row snapshot (two-column matrix)
	p := row.Profit
	// Customer Rate
	if wfCustRateEffLbl != nil {
		if row.EffectiveRate != 0 {
			wfCustRateEffLbl.SetText(fmt.Sprintf("%.2f%%", row.EffectiveRate*100.0))
		} else {
			wfCustRateEffLbl.SetText("—")
		}
	}
	if wfCustRateNomLbl != nil {
		if row.NominalRate != 0 {
			wfCustRateNomLbl.SetText(fmt.Sprintf("%.2f%%", row.NominalRate*100.0))
		} else {
			wfCustRateNomLbl.SetText("—")
		}
	}
	// Upfront components (nominal column): show income positive, costs negative
	if wfSubUpLbl != nil {
		wfSubUpLbl.SetText(fmt.Sprintf("%.2f%%", p.SubsidyUpfrontPct*100.0))
	}
	if wfIDCUpLbl != nil {
		wfIDCUpLbl.SetText(fmt.Sprintf("-%.2f%%", p.IDCUpfrontCostPct*100.0))
	}
	// Deal IRR
	if wfDealIRREffLbl != nil {
		wfDealIRREffLbl.SetText(fmt.Sprintf("%.2f%%", p.DealIRREffective*100.0))
	}
	if wfDealIRRNomLbl != nil {
		wfDealIRRNomLbl.SetText(fmt.Sprintf("%.2f%%", p.DealIRRNominal*100.0))
	}
	// Remaining nominal lines: costs as negative numbers
	if wfCostDebtLbl != nil {
		wfCostDebtLbl.SetText(fmt.Sprintf("-%.2f%%", p.CostOfDebt*100.0))
	}
	// Derived Effective side: GIM, NIM, Net EBIT (use Deal IRR effective; deduct nominal CoD + MF spread).
	effGIM := p.DealIRREffective - p.CostOfDebt - p.MatchedFundedSpread
	if wfGIMEffLbl != nil {
		wfGIMEffLbl.SetText(fmt.Sprintf("%.2f%%", effGIM*100.0))
	}
	if wfGIMLbl != nil {
		wfGIMLbl.SetText(fmt.Sprintf("%.2f%%", p.GrossInterestMargin*100.0))
	}
	if wfCapAdvLbl != nil {
		wfCapAdvLbl.SetText(fmt.Sprintf("%.2f%%", p.CapitalAdvantage*100.0))
	}
	effNIM := effGIM + p.CapitalAdvantage
	if wfNIMEffLbl != nil {
		wfNIMEffLbl.SetText(fmt.Sprintf("%.2f%%", effNIM*100.0))
	}
	if wfNIMLbl != nil {
		wfNIMLbl.SetText(fmt.Sprintf("%.2f%%", p.NetInterestMargin*100.0))
	}
	if wfRiskLbl != nil {
		wfRiskLbl.SetText(fmt.Sprintf("-%.2f%%", p.CostOfCreditRisk*100.0))
	}
	if wfOpexLbl != nil {
		wfOpexLbl.SetText(fmt.Sprintf("-%.2f%%", p.OPEX*100.0))
	}
	// Remove periodic IDC/Subsidies line from UI; keep EBITDA as currently computed by engine.
	effNetEBIT := effNIM - p.CostOfCreditRisk - p.OPEX
	if wfNetEbitEffLbl != nil {
		wfNetEbitEffLbl.SetText(fmt.Sprintf("%.2f%%", effNetEBIT*100.0))
	}
	if wfNetEbitLbl != nil {
		wfNetEbitLbl.SetText(fmt.Sprintf("%.2f%%", p.NetEBITMargin*100.0))
	}
	if wfEconCapLbl != nil {
		wfEconCapLbl.SetText(fmt.Sprintf("%.2f%%", p.EconomicCapital*100.0))
	}
	if wfAcqRoRacDetailLbl != nil {
		wfAcqRoRacDetailLbl.SetText(fmt.Sprintf("%.2f%%", p.AcquisitionRoRAC*100.0))
	}
}

// Initialize Campaign Table column widths so all columns are visible without horizontal scrolling.
// totalWidth should be the client width of the TableView.
func initCampaignTableColumns(tv *walk.TableView, totalWidth int) {
	if tv == nil || totalWidth <= 0 {
		return
	}

	// Base target widths for ~1200–1400px total client width.
	// Columns: Select, Campaign, Monthly, Downpayment, Cash Discount, Subsidy/RoRAC, Dealer Comm., Notes
	base := []int{70, 200, 180, 120, 140, 180, 160, 220}
	mins := []int{60, 160, 140, 100, 110, 140, 120, 140}

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
