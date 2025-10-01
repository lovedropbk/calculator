//go:build windows

package main

import (
	"github.com/lxn/walk"
)

// Legacy functions retained for backward compatibility during transition
// These now delegate to the new DPI-aware responsive layout system

// applyCampaignTableWidths applies responsive column widths to the Default Campaigns table
// This function now uses the DPI-aware layout system
func applyCampaignTableWidths(tv *walk.TableView, dpiCtx *DPIContext, mode LayoutMode) {
	if tv == nil {
		return
	}

	w := tv.ClientBoundsPixels().Width
	widths := CalcCampaignTableWidthsResponsive(w, dpiCtx, mode)
	ApplyCampaignTableWidths(tv, widths)
}

// applyMyCampTableWidths applies responsive column widths to the My Campaigns table
// This function now uses the DPI-aware layout system
func applyMyCampTableWidths(tv *walk.TableView, dpiCtx *DPIContext, mode LayoutMode) {
	if tv == nil {
		return
	}

	w := tv.ClientBoundsPixels().Width
	widths := CalcMyCampTableWidthsResponsive(w, dpiCtx, mode)
	ApplyMyCampTableWidths(tv, widths)
}

// Helper function for backwards compatibility with old signature (no DPI/mode)
// Uses defaults if DPI context is not available
func applyCampaignTableWidthsLegacy(tv *walk.TableView) {
	if tv == nil {
		return
	}
	
	// Create a minimal DPI context at 96 DPI (no scaling)
	dpiCtx := &DPIContext{
		BaseDPI:     96,
		CurrentDPI:  96,
		ScaleFactor: 1.0,
	}
	
	w := tv.ClientBoundsPixels().Width
	mode := DetermineLayoutMode(w, dpiCtx)
	widths := CalcCampaignTableWidthsResponsive(w, dpiCtx, mode)
	ApplyCampaignTableWidths(tv, widths)
}

// Helper function for backwards compatibility with old signature (no DPI/mode)
func applyMyCampTableWidthsLegacy(tv *walk.TableView) {
	if tv == nil {
		return
	}
	
	// Create a minimal DPI context at 96 DPI (no scaling)
	dpiCtx := &DPIContext{
		BaseDPI:     96,
		CurrentDPI:  96,
		ScaleFactor: 1.0,
	}
	
	w := tv.ClientBoundsPixels().Width
	mode := DetermineLayoutMode(w, dpiCtx)
	widths := CalcMyCampTableWidthsResponsive(w, dpiCtx, mode)
	ApplyMyCampTableWidths(tv, widths)
}
