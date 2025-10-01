//go:build windows

package main

import (
	"syscall"
	"unsafe"

	"github.com/lxn/walk"
	"github.com/lxn/win"
)

// MonitorInfo contains information about a monitor including its DPI
type MonitorInfo struct {
	WorkArea walk.Rectangle
	DPI      int
	Handle   win.HMONITOR
}

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
	info := getMonitorInfoForWindow(hwnd)
	return info.WorkArea
}

// getMonitorInfoForWindow returns detailed monitor information for the window
func getMonitorInfoForWindow(hwnd win.HWND) MonitorInfo {
	// Get the monitor handle for this window
	hMonitor := win.MonitorFromWindow(hwnd, win.MONITOR_DEFAULTTONEAREST)
	
	// Get monitor info
	var mi win.MONITORINFO
	mi.CbSize = uint32(unsafe.Sizeof(mi))
	
	info := MonitorInfo{
		Handle: hMonitor,
		DPI:    96, // Default
	}
	
	if win.GetMonitorInfo(hMonitor, &mi) {
		// Set work area (screen minus taskbar)
		info.WorkArea = walk.Rectangle{
			X:      int(mi.RcWork.Left),
			Y:      int(mi.RcWork.Top),
			Width:  int(mi.RcWork.Right - mi.RcWork.Left),
			Height: int(mi.RcWork.Bottom - mi.RcWork.Top),
		}
	} else {
		// Fallback to GetSystemMetrics
		info.WorkArea = getScreenWorkArea()
	}
	
	// Try to get monitor-specific DPI (Windows 8.1+)
	// This requires GetDpiForMonitor from Shcore.dll
	var dpiX, dpiY uint32
	
	// Load Shcore.dll using syscall
	shcore, err := syscall.LoadLibrary("Shcore.dll")
	if err == nil {
		defer syscall.FreeLibrary(shcore)
		
		// Get GetDpiForMonitor function
		getDpiForMonitor, err := syscall.GetProcAddress(shcore, "GetDpiForMonitor")
		if err == nil {
			// Call GetDpiForMonitor
			// HRESULT GetDpiForMonitor(HMONITOR hmonitor, MONITOR_DPI_TYPE dpiType, UINT *dpiX, UINT *dpiY)
			const MDT_EFFECTIVE_DPI = 0
			ret, _, _ := syscall.Syscall6(
				uintptr(getDpiForMonitor),
				4,
				uintptr(hMonitor), 
				uintptr(MDT_EFFECTIVE_DPI), 
				uintptr(unsafe.Pointer(&dpiX)), 
				uintptr(unsafe.Pointer(&dpiY)),
				0,
				0)
			
			if ret == 0 && dpiX > 0 { // S_OK = 0
				info.DPI = int(dpiX)
			}
		}
	}
	
	// Fallback: use system DPI
	if info.DPI == 96 {
		hdc := win.GetDC(0)
		if hdc != 0 {
			info.DPI = int(win.GetDeviceCaps(hdc, win.LOGPIXELSX))
			win.ReleaseDC(0, hdc)
		}
	}
	
	return info
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

// isWindowOnScreen checks if the window is visible on any monitor
func isWindowOnScreen(bounds walk.Rectangle) bool {
	// Simple check: ensure bounds are not completely off-screen
	// Get work area of primary monitor
	workArea := getScreenWorkArea()
	
	// Check if window center is within reasonable bounds
	centerX := bounds.X + bounds.Width/2
	centerY := bounds.Y + bounds.Height/2
	
	// Allow some tolerance (window can be partially off-screen)
	return centerX >= -bounds.Width && centerX <= workArea.Width+bounds.Width &&
		centerY >= -bounds.Height && centerY <= workArea.Height+bounds.Height
}
