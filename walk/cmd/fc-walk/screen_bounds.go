//go:build windows

package main

import (
	"github.com/lxn/walk"
	"github.com/lxn/walk/declarative"
	"github.com/lxn/win"
	"unsafe"
)

// getScreenWorkArea returns the work area (usable screen space) of the primary monitor
// The work area is the screen size minus the taskbar and other reserved areas
func getScreenWorkArea() walk.Rectangle {
	// Get primary monitor dimensions using GetSystemMetrics
	screenWidth := win.GetSystemMetrics(win.SM_CXSCREEN)
	screenHeight := win.GetSystemMetrics(win.SM_CYSCREEN)
	
	// Default to full screen if we can't get monitor info
	workArea := walk.Rectangle{
		X:      0,
		Y:      0,
		Width:  int(screenWidth),
		Height: int(screenHeight),
	}
	
	// Try to get the actual work area (screen minus taskbar)
	// We can't use MonitorFromWindow before the window is created, so we use GetSystemMetrics
	// for work area dimensions instead
	workWidth := win.GetSystemMetrics(win.SM_CXFULLSCREEN)
	workHeight := win.GetSystemMetrics(win.SM_CYFULLSCREEN)
	
	if workWidth > 0 && workHeight > 0 {
		workArea.Width = int(workWidth)
		workArea.Height = int(workHeight)
	}
	
	return workArea
}

// getMonitorWorkAreaForWindow returns the work area of the monitor containing the window
func getMonitorWorkAreaForWindow(hwnd win.HWND) walk.Rectangle {
	// Get the monitor handle for this window
	hMonitor := win.MonitorFromWindow(hwnd, win.MONITOR_DEFAULTTONEAREST)
	
	// Get monitor info
	var mi win.MONITORINFO
	mi.CbSize = uint32(unsafe.Sizeof(mi))
	
	if win.GetMonitorInfo(hMonitor, &mi) {
		// Return the work area (screen minus taskbar)
		return walk.Rectangle{
			X:      int(mi.RcWork.Left),
			Y:      int(mi.RcWork.Top),
			Width:  int(mi.RcWork.Right - mi.RcWork.Left),
			Height: int(mi.RcWork.Bottom - mi.RcWork.Top),
		}
	}
	
	// Fallback to GetSystemMetrics
	return getScreenWorkArea()
}

// constrainWindowToScreen ensures the window size doesn't exceed the screen work area
func constrainWindowToScreen(mw *walk.MainWindow) {
	if mw == nil {
		return
	}
	
	workArea := getMonitorWorkAreaForWindow(mw.Handle())
	
	// Get current window bounds
	bounds := mw.Bounds()
	
	// Constrain width and height to work area
	newWidth := bounds.Width
	newHeight := bounds.Height
	
	if newWidth > workArea.Width {
		newWidth = workArea.Width
	}
	if newHeight > workArea.Height {
		newHeight = workArea.Height
	}
	
	// Update if size changed
	if newWidth != bounds.Width || newHeight != bounds.Height {
		bounds.Width = newWidth
		bounds.Height = newHeight
		_ = mw.SetBounds(bounds)
	}
	
	// Also ensure the window is visible on screen
	if bounds.X < workArea.X {
		bounds.X = workArea.X
		_ = mw.SetBounds(bounds)
	}
	if bounds.Y < workArea.Y {
		bounds.Y = workArea.Y
		_ = mw.SetBounds(bounds)
	}
	if bounds.X + bounds.Width > workArea.X + workArea.Width {
		bounds.X = workArea.X + workArea.Width - bounds.Width
		if bounds.X < workArea.X {
			bounds.X = workArea.X
		}
		_ = mw.SetBounds(bounds)
	}
	if bounds.Y + bounds.Height > workArea.Y + workArea.Height {
		bounds.Y = workArea.Y + workArea.Height - bounds.Height
		if bounds.Y < workArea.Y {
			bounds.Y = workArea.Y
		}
		_ = mw.SetBounds(bounds)
	}
}

// calculateInitialWindowSize returns an appropriate initial window size
// that fits within the screen, with some margin
func calculateInitialWindowSize() declarative.Size {
	workArea := getScreenWorkArea()
	
	// Use 70% of work area width and height, but cap at reasonable maximum
	// and ensure we meet minimum requirements
	const (
		minWidth  = 1100
		minHeight = 700
		maxWidth  = 1400
		maxHeight = 900
	)
	
	// Calculate 70% of work area
	width := int(float64(workArea.Width) * 0.7)
	height := int(float64(workArea.Height) * 0.7)
	
	// Constrain to reasonable bounds
	if width < minWidth {
		width = minWidth
	}
	if width > maxWidth {
		width = maxWidth
	}
	if width > workArea.Width {
		width = workArea.Width - 40 // Leave some margin
	}
	
	if height < minHeight {
		height = minHeight
	}
	if height > maxHeight {
		height = maxHeight
	}
	if height > workArea.Height {
		height = workArea.Height - 80 // Leave margin for taskbar
	}
	
	return declarative.Size{Width: width, Height: height}
}
