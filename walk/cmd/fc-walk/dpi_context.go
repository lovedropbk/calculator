//go:build windows

package main

import (
	"sync"

	"github.com/lxn/walk"
	"github.com/lxn/win"
)

// DPIContext manages DPI scaling for the application
type DPIContext struct {
	mu          sync.RWMutex
	BaseDPI     int     // Standard Windows DPI (96)
	CurrentDPI  int     // Current screen DPI
	ScaleFactor float64 // CurrentDPI / BaseDPI
}

// NewDPIContext creates a new DPI context from the main window
func NewDPIContext(window *walk.MainWindow) *DPIContext {
	dpi := 96 // Default fallback
	if window != nil {
		dpi = window.DPI()
		if dpi <= 0 {
			dpi = 96
		}
	}

	return &DPIContext{
		BaseDPI:     96,
		CurrentDPI:  dpi,
		ScaleFactor: float64(dpi) / 96.0,
	}
}

// NewDPIContextForScreen creates a DPI context for the primary screen
func NewDPIContextForScreen() *DPIContext {
	// Get system DPI
	hdc := win.GetDC(0)
	defer win.ReleaseDC(0, hdc)

	dpi := int(win.GetDeviceCaps(hdc, win.LOGPIXELSX))
	if dpi <= 0 {
		dpi = 96
	}

	return &DPIContext{
		BaseDPI:     96,
		CurrentDPI:  dpi,
		ScaleFactor: float64(dpi) / 96.0,
	}
}

// Scale converts a base pixel value to DPI-scaled pixels
func (ctx *DPIContext) Scale(basePixels int) int {
	if ctx == nil {
		return basePixels
	}
	ctx.mu.RLock()
	defer ctx.mu.RUnlock()
	return int(float64(basePixels)*ctx.ScaleFactor + 0.5)
}

// ScaleFloat converts a base pixel value to DPI-scaled pixels (float result)
func (ctx *DPIContext) ScaleFloat(basePixels float64) float64 {
	if ctx == nil {
		return basePixels
	}
	ctx.mu.RLock()
	defer ctx.mu.RUnlock()
	return basePixels * ctx.ScaleFactor
}

// Unscale converts DPI-scaled pixels back to base pixels
func (ctx *DPIContext) Unscale(scaledPixels int) int {
	if ctx == nil {
		return scaledPixels
	}
	ctx.mu.RLock()
	defer ctx.mu.RUnlock()
	if ctx.ScaleFactor == 0 {
		return scaledPixels
	}
	return int(float64(scaledPixels)/ctx.ScaleFactor + 0.5)
}

// UpdateDPI updates the DPI context when the window DPI changes
func (ctx *DPIContext) UpdateDPI(newDPI int) bool {
	if newDPI <= 0 {
		return false
	}

	ctx.mu.Lock()
	defer ctx.mu.Unlock()

	if ctx.CurrentDPI == newDPI {
		return false // No change
	}

	ctx.CurrentDPI = newDPI
	ctx.ScaleFactor = float64(newDPI) / float64(ctx.BaseDPI)
	return true // DPI changed
}

// GetScaleFactor returns the current scale factor (thread-safe)
func (ctx *DPIContext) GetScaleFactor() float64 {
	if ctx == nil {
		return 1.0
	}
	ctx.mu.RLock()
	defer ctx.mu.RUnlock()
	return ctx.ScaleFactor
}

// GetCurrentDPI returns the current DPI value (thread-safe)
func (ctx *DPIContext) GetCurrentDPI() int {
	if ctx == nil {
		return 96
	}
	ctx.mu.RLock()
	defer ctx.mu.RUnlock()
	return ctx.CurrentDPI
}

// ScaleRect scales a rectangle by DPI
func (ctx *DPIContext) ScaleRect(rect walk.Rectangle) walk.Rectangle {
	if ctx == nil {
		return rect
	}
	return walk.Rectangle{
		X:      ctx.Scale(rect.X),
		Y:      ctx.Scale(rect.Y),
		Width:  ctx.Scale(rect.Width),
		Height: ctx.Scale(rect.Height),
	}
}

// UnscaleRect unscales a rectangle to base DPI
func (ctx *DPIContext) UnscaleRect(rect walk.Rectangle) walk.Rectangle {
	if ctx == nil {
		return rect
	}
	return walk.Rectangle{
		X:      ctx.Unscale(rect.X),
		Y:      ctx.Unscale(rect.Y),
		Width:  ctx.Unscale(rect.Width),
		Height: ctx.Unscale(rect.Height),
	}
}

// DPIScaleMode represents different DPI scaling modes
type DPIScaleMode int

const (
	// DPIScaleAuto follows system DPI settings
	DPIScaleAuto DPIScaleMode = iota
	// DPIScaleSmall is 90% of system DPI
	DPIScaleSmall
	// DPIScaleNormal is 100% of system DPI
	DPIScaleNormal
	// DPIScaleLarge is 110% of system DPI
	DPIScaleLarge
	// DPIScaleHuge is 125% of system DPI
	DPIScaleHuge
)

// ApplyScaleMode applies a user preference scale mode
func (ctx *DPIContext) ApplyScaleMode(mode DPIScaleMode) {
	if ctx == nil {
		return
	}

	ctx.mu.Lock()
	defer ctx.mu.Unlock()

	baseFactor := float64(ctx.CurrentDPI) / float64(ctx.BaseDPI)

	switch mode {
	case DPIScaleSmall:
		ctx.ScaleFactor = baseFactor * 0.90
	case DPIScaleNormal:
		ctx.ScaleFactor = baseFactor
	case DPIScaleLarge:
		ctx.ScaleFactor = baseFactor * 1.10
	case DPIScaleHuge:
		ctx.ScaleFactor = baseFactor * 1.25
	default: // DPIScaleAuto
		ctx.ScaleFactor = baseFactor
	}
}

// GetEffectivePixels converts scaled pixels back to effective pixels for layout decisions
func (ctx *DPIContext) GetEffectivePixels(scaledPixels int) int {
	if ctx == nil {
		return scaledPixels
	}
	ctx.mu.RLock()
	defer ctx.mu.RUnlock()
	if ctx.ScaleFactor <= 0 {
		return scaledPixels
	}
	return int(float64(scaledPixels) / ctx.ScaleFactor)
}
