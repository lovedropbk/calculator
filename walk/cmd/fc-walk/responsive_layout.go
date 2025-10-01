//go:build windows

package main

import (
	"github.com/lxn/walk"
)

// LayoutMode represents different responsive layout modes
type LayoutMode int

const (
	// CompactLayout for narrow windows (< 900px effective width)
	CompactLayout LayoutMode = iota
	// NormalLayout for standard windows (900-1400px effective width)
	NormalLayout
	// WideLayout for large windows (> 1400px effective width)
	WideLayout
)

// LayoutBreakpoints defines the pixel thresholds for layout modes (at 96 DPI)
type LayoutBreakpoints struct {
	CompactThreshold int // Below this: compact mode
	WideThreshold    int // Above this: wide mode
}

var defaultBreakpoints = LayoutBreakpoints{
	CompactThreshold: 900,
	WideThreshold:    1400,
}

// DetermineLayoutMode calculates the appropriate layout mode based on window width
func DetermineLayoutMode(windowWidth int, dpiCtx *DPIContext) LayoutMode {
	if dpiCtx == nil {
		return NormalLayout
	}

	// Convert actual pixels to effective pixels (DPI-independent)
	effectiveWidth := dpiCtx.GetEffectivePixels(windowWidth)

	if effectiveWidth < defaultBreakpoints.CompactThreshold {
		return CompactLayout
	} else if effectiveWidth > defaultBreakpoints.WideThreshold {
		return WideLayout
	}
	return NormalLayout
}

// ColumnWidths holds the calculated widths for table columns
type ColumnWidths struct {
	Copy     int // Copy button column (only for default campaigns)
	Select   int // Selection checkbox column
	Name     int // Campaign name
	Monthly  int // Monthly installment
	DP       int // Down payment
	Subdown  int // Subdown subsidy
	Cash     int // Cash discount
	MBSP     int // MBSP
	Subsidy  int // Subsidy utilized
	Acq      int // Acquisition RoRAC
	Dealer   int // Dealer commission
	Notes    int // Notes (stretched)
}

// CalcCampaignTableWidthsResponsive calculates responsive column widths for the Default Campaigns table
func CalcCampaignTableWidthsResponsive(totalWidth int, dpiCtx *DPIContext, mode LayoutMode) ColumnWidths {
	if totalWidth <= 0 {
		totalWidth = dpiCtx.Scale(1000)
	}

	// Reserve space for copy button and scrollbar/margins
	const copyColBase = 60
	const marginsBase = 40

	copyCol := dpiCtx.Scale(copyColBase)
	margins := dpiCtx.Scale(marginsBase)
	remaining := totalWidth - copyCol - margins

	if remaining < dpiCtx.Scale(300) {
		remaining = dpiCtx.Scale(300)
	}

	var widths ColumnWidths
	widths.Copy = copyCol

	switch mode {
	case CompactLayout:
		widths = calcCompactCampaignWidths(remaining, dpiCtx)
	case WideLayout:
		widths = calcWideCampaignWidths(remaining, dpiCtx)
	default: // NormalLayout
		widths = calcNormalCampaignWidths(remaining, dpiCtx)
	}

	widths.Copy = copyCol
	return widths
}

func calcCompactCampaignWidths(remaining int, dpiCtx *DPIContext) ColumnWidths {
	// Compact mode: prioritize essential columns, hide or minimize others
	var widths ColumnWidths

	// Fixed widths for essential columns
	widths.Select = dpiCtx.Scale(60)
	remaining -= widths.Select

	if remaining < dpiCtx.Scale(300) {
		remaining = dpiCtx.Scale(300)
	}

	// Proportional distribution favoring name and monthly
	widths.Name = int(float64(remaining) * 0.28)
	widths.Monthly = int(float64(remaining) * 0.23)
	widths.DP = int(float64(remaining) * 0.11)
	widths.Subdown = int(float64(remaining) * 0.11)
	widths.Acq = int(float64(remaining) * 0.15)
	widths.Dealer = remaining - widths.Name - widths.Monthly - widths.DP - widths.Subdown - widths.Acq

	// Compact minimums (smaller than normal)
	minName := dpiCtx.Scale(120)
	minMonthly := dpiCtx.Scale(100)
	minDP := dpiCtx.Scale(80)
	minSubdown := dpiCtx.Scale(80)
	minAcq := dpiCtx.Scale(90)
	minDealer := dpiCtx.Scale(90)

	if widths.Name < minName {
		widths.Name = minName
	}
	if widths.Monthly < minMonthly {
		widths.Monthly = minMonthly
	}
	if widths.DP < minDP {
		widths.DP = minDP
	}
	if widths.Subdown < minSubdown {
		widths.Subdown = minSubdown
	}
	if widths.Acq < minAcq {
		widths.Acq = minAcq
	}
	if widths.Dealer < minDealer {
		widths.Dealer = minDealer
	}

	// Hide less critical columns in compact mode
	widths.Cash = 0
	widths.MBSP = 0
	widths.Subsidy = 0

	return widths
}

func calcNormalCampaignWidths(remaining int, dpiCtx *DPIContext) ColumnWidths {
	// Normal mode: balanced column distribution
	var widths ColumnWidths

	widths.Select = dpiCtx.Scale(70)
	remaining -= widths.Select

	if remaining < dpiCtx.Scale(400) {
		remaining = dpiCtx.Scale(400)
	}

	// Proportional splits
	widths.Name = int(float64(remaining) * 0.22)
	widths.Monthly = int(float64(remaining) * 0.16)
	widths.DP = int(float64(remaining) * 0.10)
	widths.Subdown = int(float64(remaining) * 0.10)
	widths.Cash = int(float64(remaining) * 0.10)
	widths.MBSP = int(float64(remaining) * 0.09)
	widths.Subsidy = int(float64(remaining) * 0.10)
	widths.Acq = int(float64(remaining) * 0.07)
	widths.Dealer = remaining - widths.Name - widths.Monthly - widths.DP - widths.Subdown - widths.Cash - widths.MBSP - widths.Subsidy - widths.Acq

	// Normal minimums
	minName := dpiCtx.Scale(150)
	minMonthly := dpiCtx.Scale(130)
	minDP := dpiCtx.Scale(100)
	minSubdown := dpiCtx.Scale(90)
	minCash := dpiCtx.Scale(110)
	minMBSP := dpiCtx.Scale(100)
	minSubsidy := dpiCtx.Scale(120)
	minAcq := dpiCtx.Scale(110)
	minDealer := dpiCtx.Scale(120)

	if widths.Name < minName {
		widths.Name = minName
	}
	if widths.Monthly < minMonthly {
		widths.Monthly = minMonthly
	}
	if widths.DP < minDP {
		widths.DP = minDP
	}
	if widths.Subdown < minSubdown {
		widths.Subdown = minSubdown
	}
	if widths.Cash < minCash {
		widths.Cash = minCash
	}
	if widths.MBSP < minMBSP {
		widths.MBSP = minMBSP
	}
	if widths.Subsidy < minSubsidy {
		widths.Subsidy = minSubsidy
	}
	if widths.Acq < minAcq {
		widths.Acq = minAcq
	}
	if widths.Dealer < minDealer {
		widths.Dealer = minDealer
	}

	return widths
}

func calcWideCampaignWidths(remaining int, dpiCtx *DPIContext) ColumnWidths {
	// Wide mode: give extra space to important columns
	var widths ColumnWidths

	widths.Select = dpiCtx.Scale(70)
	remaining -= widths.Select

	if remaining < dpiCtx.Scale(500) {
		remaining = dpiCtx.Scale(500)
	}

	// More generous proportions
	widths.Name = int(float64(remaining) * 0.25)
	widths.Monthly = int(float64(remaining) * 0.16)
	widths.DP = int(float64(remaining) * 0.10)
	widths.Subdown = int(float64(remaining) * 0.10)
	widths.Cash = int(float64(remaining) * 0.10)
	widths.MBSP = int(float64(remaining) * 0.08)
	widths.Subsidy = int(float64(remaining) * 0.10)
	widths.Acq = int(float64(remaining) * 0.07)
	widths.Dealer = remaining - widths.Name - widths.Monthly - widths.DP - widths.Subdown - widths.Cash - widths.MBSP - widths.Subsidy - widths.Acq

	// Wide minimums (more comfortable)
	minName := dpiCtx.Scale(180)
	minMonthly := dpiCtx.Scale(150)
	minDP := dpiCtx.Scale(120)
	minSubdown := dpiCtx.Scale(110)
	minCash := dpiCtx.Scale(130)
	minMBSP := dpiCtx.Scale(120)
	minSubsidy := dpiCtx.Scale(140)
	minAcq := dpiCtx.Scale(130)
	minDealer := dpiCtx.Scale(140)

	if widths.Name < minName {
		widths.Name = minName
	}
	if widths.Monthly < minMonthly {
		widths.Monthly = minMonthly
	}
	if widths.DP < minDP {
		widths.DP = minDP
	}
	if widths.Subdown < minSubdown {
		widths.Subdown = minSubdown
	}
	if widths.Cash < minCash {
		widths.Cash = minCash
	}
	if widths.MBSP < minMBSP {
		widths.MBSP = minMBSP
	}
	if widths.Subsidy < minSubsidy {
		widths.Subsidy = minSubsidy
	}
	if widths.Acq < minAcq {
		widths.Acq = minAcq
	}
	if widths.Dealer < minDealer {
		widths.Dealer = minDealer
	}

	return widths
}

// CalcMyCampTableWidthsResponsive calculates responsive column widths for My Campaigns table
func CalcMyCampTableWidthsResponsive(totalWidth int, dpiCtx *DPIContext, mode LayoutMode) ColumnWidths {
	if totalWidth <= 0 {
		totalWidth = dpiCtx.Scale(1000)
	}

	const marginsBase = 40
	margins := dpiCtx.Scale(marginsBase)
	remaining := totalWidth - margins

	if remaining < dpiCtx.Scale(300) {
		remaining = dpiCtx.Scale(300)
	}

	var widths ColumnWidths

	switch mode {
	case CompactLayout:
		widths = calcCompactMyCampWidths(remaining, dpiCtx)
	case WideLayout:
		widths = calcWideMyCampWidths(remaining, dpiCtx)
	default: // NormalLayout
		widths = calcNormalMyCampWidths(remaining, dpiCtx)
	}

	return widths
}

func calcCompactMyCampWidths(remaining int, dpiCtx *DPIContext) ColumnWidths {
	var widths ColumnWidths

	widths.Select = dpiCtx.Scale(60)
	remaining -= widths.Select

	if remaining < dpiCtx.Scale(300) {
		remaining = dpiCtx.Scale(300)
	}

	// Prioritize name and monthly in compact mode
	widths.Name = int(float64(remaining) * 0.33)
	widths.Monthly = int(float64(remaining) * 0.23)
	widths.DP = int(float64(remaining) * 0.13)
	widths.Subdown = int(float64(remaining) * 0.11)
	widths.Acq = int(float64(remaining) * 0.20)

	// Minimums
	minName := dpiCtx.Scale(120)
	minMonthly := dpiCtx.Scale(100)
	minDP := dpiCtx.Scale(80)
	minSubdown := dpiCtx.Scale(70)
	minAcq := dpiCtx.Scale(90)

	if widths.Name < minName {
		widths.Name = minName
	}
	if widths.Monthly < minMonthly {
		widths.Monthly = minMonthly
	}
	if widths.DP < minDP {
		widths.DP = minDP
	}
	if widths.Subdown < minSubdown {
		widths.Subdown = minSubdown
	}
	if widths.Acq < minAcq {
		widths.Acq = minAcq
	}

	// Hide in compact mode
	widths.Cash = 0
	widths.MBSP = 0
	widths.Subsidy = 0
	widths.Dealer = 0

	return widths
}

func calcNormalMyCampWidths(remaining int, dpiCtx *DPIContext) ColumnWidths {
	var widths ColumnWidths

	widths.Select = dpiCtx.Scale(70)
	remaining -= widths.Select

	if remaining < dpiCtx.Scale(400) {
		remaining = dpiCtx.Scale(400)
	}

	widths.Name = int(float64(remaining) * 0.25)
	widths.Monthly = int(float64(remaining) * 0.16)
	widths.DP = int(float64(remaining) * 0.10)
	widths.Subdown = int(float64(remaining) * 0.10)
	widths.Cash = int(float64(remaining) * 0.10)
	widths.MBSP = int(float64(remaining) * 0.07)
	widths.Subsidy = int(float64(remaining) * 0.09)
	widths.Acq = int(float64(remaining) * 0.06)
	widths.Dealer = remaining - widths.Name - widths.Monthly - widths.DP - widths.Subdown - widths.Cash - widths.MBSP - widths.Subsidy - widths.Acq

	// Minimums
	minName := dpiCtx.Scale(150)
	minMonthly := dpiCtx.Scale(130)
	minDP := dpiCtx.Scale(100)
	minSubdown := dpiCtx.Scale(90)
	minCash := dpiCtx.Scale(110)
	minMBSP := dpiCtx.Scale(90)
	minSubsidy := dpiCtx.Scale(120)
	minAcq := dpiCtx.Scale(100)
	minDealer := dpiCtx.Scale(110)

	if widths.Name < minName {
		widths.Name = minName
	}
	if widths.Monthly < minMonthly {
		widths.Monthly = minMonthly
	}
	if widths.DP < minDP {
		widths.DP = minDP
	}
	if widths.Subdown < minSubdown {
		widths.Subdown = minSubdown
	}
	if widths.Cash < minCash {
		widths.Cash = minCash
	}
	if widths.MBSP < minMBSP {
		widths.MBSP = minMBSP
	}
	if widths.Subsidy < minSubsidy {
		widths.Subsidy = minSubsidy
	}
	if widths.Acq < minAcq {
		widths.Acq = minAcq
	}
	if widths.Dealer < minDealer {
		widths.Dealer = minDealer
	}

	return widths
}

func calcWideMyCampWidths(remaining int, dpiCtx *DPIContext) ColumnWidths {
	var widths ColumnWidths

	widths.Select = dpiCtx.Scale(70)
	remaining -= widths.Select

	if remaining < dpiCtx.Scale(500) {
		remaining = dpiCtx.Scale(500)
	}

	widths.Name = int(float64(remaining) * 0.27)
	widths.Monthly = int(float64(remaining) * 0.16)
	widths.DP = int(float64(remaining) * 0.10)
	widths.Subdown = int(float64(remaining) * 0.10)
	widths.Cash = int(float64(remaining) * 0.10)
	widths.MBSP = int(float64(remaining) * 0.07)
	widths.Subsidy = int(float64(remaining) * 0.10)
	widths.Acq = int(float64(remaining) * 0.05)
	widths.Dealer = remaining - widths.Name - widths.Monthly - widths.DP - widths.Subdown - widths.Cash - widths.MBSP - widths.Subsidy - widths.Acq

	// Wide minimums
	minName := dpiCtx.Scale(180)
	minMonthly := dpiCtx.Scale(150)
	minDP := dpiCtx.Scale(120)
	minSubdown := dpiCtx.Scale(110)
	minCash := dpiCtx.Scale(130)
	minMBSP := dpiCtx.Scale(110)
	minSubsidy := dpiCtx.Scale(140)
	minAcq := dpiCtx.Scale(120)
	minDealer := dpiCtx.Scale(130)

	if widths.Name < minName {
		widths.Name = minName
	}
	if widths.Monthly < minMonthly {
		widths.Monthly = minMonthly
	}
	if widths.DP < minDP {
		widths.DP = minDP
	}
	if widths.Subdown < minSubdown {
		widths.Subdown = minSubdown
	}
	if widths.Cash < minCash {
		widths.Cash = minCash
	}
	if widths.MBSP < minMBSP {
		widths.MBSP = minMBSP
	}
	if widths.Subsidy < minSubsidy {
		widths.Subsidy = minSubsidy
	}
	if widths.Acq < minAcq {
		widths.Acq = minAcq
	}
	if widths.Dealer < minDealer {
		widths.Dealer = minDealer
	}

	return widths
}

// ApplyCampaignTableWidths applies calculated widths to the Default Campaigns table
func ApplyCampaignTableWidths(tv *walk.TableView, widths ColumnWidths) {
	if tv == nil {
		return
	}

	cols := tv.Columns()
	if cols.Len() < 11 {
		return
	}

	// Column order: [0:Copy 60], [1:Select], [2:Campaign], [3:Monthly], [4:DP], [5:Subdown], [6:CashDisc], [7:MBSP], [8:Subsidy], [9:Acq], [10:Dealer], [11:Notes(stretched)]
	cols.At(0).SetWidth(widths.Copy)
	cols.At(1).SetWidth(widths.Select)
	cols.At(2).SetWidth(widths.Name)
	cols.At(3).SetWidth(widths.Monthly)
	cols.At(4).SetWidth(widths.DP)

	// Handle Subdown column
	if widths.Subdown > 0 {
		cols.At(5).SetWidth(widths.Subdown)
	} else {
		cols.At(5).SetWidth(0) // Hide column in compact mode
	}

	// Handle hidden columns in compact mode
	if widths.Cash > 0 {
		cols.At(6).SetWidth(widths.Cash)
	} else {
		cols.At(6).SetWidth(0) // Hide column
	}

	if widths.MBSP > 0 {
		cols.At(7).SetWidth(widths.MBSP)
	} else {
		cols.At(7).SetWidth(0)
	}

	if widths.Subsidy > 0 {
		cols.At(8).SetWidth(widths.Subsidy)
	} else {
		cols.At(8).SetWidth(0)
	}

	cols.At(9).SetWidth(widths.Acq)
	cols.At(10).SetWidth(widths.Dealer)
	// Column 11 (Notes) is stretched automatically
}

// ApplyMyCampTableWidths applies calculated widths to the My Campaigns table
func ApplyMyCampTableWidths(tv *walk.TableView, widths ColumnWidths) {
	if tv == nil {
		return
	}

	cols := tv.Columns()
	if cols.Len() < 10 {
		return
	}

	// Column order: [0:Select], [1:Campaign], [2:Monthly], [3:DP], [4:Subdown], [5:CashDisc], [6:MBSP], [7:Subsidy], [8:Acq], [9:Dealer], [10:Notes(stretched)]
	cols.At(0).SetWidth(widths.Select)
	cols.At(1).SetWidth(widths.Name)
	cols.At(2).SetWidth(widths.Monthly)
	cols.At(3).SetWidth(widths.DP)

	if widths.Subdown > 0 {
		cols.At(4).SetWidth(widths.Subdown)
	} else {
		cols.At(4).SetWidth(0)
	}

	if widths.Cash > 0 {
		cols.At(5).SetWidth(widths.Cash)
	} else {
		cols.At(5).SetWidth(0)
	}

	if widths.MBSP > 0 {
		cols.At(6).SetWidth(widths.MBSP)
	} else {
		cols.At(6).SetWidth(0)
	}

	if widths.Subsidy > 0 {
		cols.At(7).SetWidth(widths.Subsidy)
	} else {
		cols.At(7).SetWidth(0)
	}

	if widths.Acq > 0 {
		cols.At(8).SetWidth(widths.Acq)
	} else {
		cols.At(8).SetWidth(0)
	}

	if widths.Dealer > 0 {
		cols.At(9).SetWidth(widths.Dealer)
	} else {
		cols.At(9).SetWidth(0)
	}
	// Column 10 (Notes) is stretched automatically
}

// AdjustSplitterRatio adjusts the splitter ratio based on layout mode
func AdjustSplitterRatio(splitter *walk.Splitter, mode LayoutMode) {
	if splitter == nil {
		return
	}

	// The splitter ratio is controlled by StretchFactor in the widget definitions
	// In compact mode, we might want to give more space to the right panel
	// However, Walk's Splitter doesn't provide a direct API to adjust ratios dynamically
	// This would require re-creating the widgets or using SetFixed()
	
	// For now, we rely on the initial StretchFactor values (1:2 ratio)
	// Future enhancement: implement dynamic ratio adjustment if needed
}

// clamp restricts a value to a range
func clamp(value, min, max int) int {
	if value < min {
		return min
	}
	if value > max {
		return max
	}
	return value
}

// min returns the minimum of two integers
func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}
