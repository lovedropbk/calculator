//go:build windows

package main

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"

	"github.com/lxn/walk"
	"github.com/lxn/win"
)

// WindowState represents the saved state of the main window
type WindowState struct {
	X           int  `json:"x"`
	Y           int  `json:"y"`
	Width       int  `json:"width"`
	Height      int  `json:"height"`
	IsMaximized bool `json:"is_maximized"`
	MonitorDPI  int  `json:"monitor_dpi"`
	Version     int  `json:"version"` // For future compatibility
}

// WindowStateManager handles saving and restoring window state
type WindowStateManager struct {
	filePath string
}

// NewWindowStateManager creates a new window state manager
func NewWindowStateManager() *WindowStateManager {
	// Use the same directory as sticky state
	homeDir, err := os.UserHomeDir()
	if err != nil {
		homeDir = "."
	}

	stateDir := filepath.Join(homeDir, ".financial-calculator")
	_ = os.MkdirAll(stateDir, 0755)

	return &WindowStateManager{
		filePath: filepath.Join(stateDir, "window_state.json"),
	}
}

// SaveWindowState saves the current window state to disk
func (m *WindowStateManager) SaveWindowState(mw *walk.MainWindow) error {
	if mw == nil {
		return fmt.Errorf("main window is nil")
	}

	// Check if window is maximized
	isMaximized := win.IsZoomed(mw.Handle())

	// Get window bounds (in current DPI)
	bounds := mw.Bounds()

	state := WindowState{
		X:           bounds.X,
		Y:           bounds.Y,
		Width:       bounds.Width,
		Height:      bounds.Height,
		IsMaximized: isMaximized,
		MonitorDPI:  mw.DPI(),
		Version:     1,
	}

	data, err := json.MarshalIndent(state, "", "  ")
	if err != nil {
		return fmt.Errorf("failed to marshal window state: %w", err)
	}

	err = os.WriteFile(m.filePath, data, 0644)
	if err != nil {
		return fmt.Errorf("failed to write window state file: %w", err)
	}

	return nil
}

// LoadWindowState loads the saved window state from disk
func (m *WindowStateManager) LoadWindowState() (*WindowState, error) {
	data, err := os.ReadFile(m.filePath)
	if err != nil {
		if os.IsNotExist(err) {
			return nil, nil // No saved state, not an error
		}
		return nil, fmt.Errorf("failed to read window state file: %w", err)
	}

	var state WindowState
	err = json.Unmarshal(data, &state)
	if err != nil {
		return nil, fmt.Errorf("failed to unmarshal window state: %w", err)
	}

	return &state, nil
}

// RestoreWindowState applies the saved state to the window
func (m *WindowStateManager) RestoreWindowState(mw *walk.MainWindow, state *WindowState, dpiCtx *DPIContext) error {
	if mw == nil || state == nil {
		return fmt.Errorf("window or state is nil")
	}

	// Adjust for DPI changes
	adjustedState := *state
	if state.MonitorDPI > 0 && dpiCtx != nil {
		currentDPI := dpiCtx.GetCurrentDPI()
		if state.MonitorDPI != currentDPI {
			scaleFactor := float64(currentDPI) / float64(state.MonitorDPI)
			adjustedState.Width = int(float64(state.Width) * scaleFactor)
			adjustedState.Height = int(float64(state.Height) * scaleFactor)
			// Don't scale X, Y here - we'll validate position separately
		}
	}

	// Ensure window is visible on screen
	workArea := getMonitorWorkAreaForWindow(mw.Handle())
	constrainWindowStateToWorkArea(&adjustedState, workArea)

	// Apply bounds
	newBounds := walk.Rectangle{
		X:      adjustedState.X,
		Y:      adjustedState.Y,
		Width:  adjustedState.Width,
		Height: adjustedState.Height,
	}

	err := mw.SetBounds(newBounds)
	if err != nil {
		return fmt.Errorf("failed to set window bounds: %w", err)
	}

	// Apply maximized state
	if adjustedState.IsMaximized {
		win.ShowWindow(mw.Handle(), win.SW_MAXIMIZE)
	}

	return nil
}

// constrainWindowStateToWorkArea ensures the window state is visible on screen
func constrainWindowStateToWorkArea(state *WindowState, workArea walk.Rectangle) {
	// Ensure window is not larger than work area
	if state.Width > workArea.Width {
		state.Width = workArea.Width
	}
	if state.Height > workArea.Height {
		state.Height = workArea.Height
	}

	// Ensure window is positioned within work area
	if state.X < workArea.X {
		state.X = workArea.X
	}
	if state.Y < workArea.Y {
		state.Y = workArea.Y
	}

	// Ensure window doesn't extend beyond work area
	if state.X+state.Width > workArea.X+workArea.Width {
		state.X = workArea.X + workArea.Width - state.Width
		if state.X < workArea.X {
			state.X = workArea.X
		}
	}
	if state.Y+state.Height > workArea.Y+workArea.Height {
		state.Y = workArea.Y + workArea.Height - state.Height
		if state.Y < workArea.Y {
			state.Y = workArea.Y
		}
	}
}

// CalculateInitialWindowSize determines the best initial window size
func CalculateInitialWindowSize(dpiCtx *DPIContext) walk.Size {
	workArea := getScreenWorkArea()

	// Base sizes at 96 DPI
	const (
		minContentWidthBase  = 900  // Lowered from 1100 for better small screen support
		minContentHeightBase = 600  // Lowered from 700
		idealWidthBase       = 1200 // Ideal for normal workflows
		idealHeightBase      = 800
		maxWidthBase         = 1800 // Reasonable maximum
		maxHeightBase        = 1200
	)

	// Scale to current DPI
	minContentWidth := dpiCtx.Scale(minContentWidthBase)
	minContentHeight := dpiCtx.Scale(minContentHeightBase)
	idealWidth := dpiCtx.Scale(idealWidthBase)
	idealHeight := dpiCtx.Scale(idealHeightBase)
	maxWidth := dpiCtx.Scale(maxWidthBase)
	maxHeight := dpiCtx.Scale(maxHeightBase)

	// Don't exceed 90% of work area
	maxAllowedWidth := int(float64(workArea.Width) * 0.90)
	maxAllowedHeight := int(float64(workArea.Height) * 0.85)

	// Choose width
	width := idealWidth
	if width < minContentWidth {
		width = minContentWidth
	}
	if width > maxWidth {
		width = maxWidth
	}
	if width > maxAllowedWidth {
		width = maxAllowedWidth
	}
	if width < minContentWidth && workArea.Width > 0 {
		// Screen is too small for minimum, use what we have
		width = maxAllowedWidth
	}

	// Choose height
	height := idealHeight
	if height < minContentHeight {
		height = minContentHeight
	}
	if height > maxHeight {
		height = maxHeight
	}
	if height > maxAllowedHeight {
		height = maxAllowedHeight
	}
	if height < minContentHeight && workArea.Height > 0 {
		height = maxAllowedHeight
	}

	return walk.Size{Width: width, Height: height}
}

// CenterWindowOnScreen centers the window on the primary monitor
func CenterWindowOnScreen(mw *walk.MainWindow) {
	if mw == nil {
		return
	}

	workArea := getMonitorWorkAreaForWindow(mw.Handle())
	bounds := mw.Bounds()

	// Calculate center position
	x := workArea.X + (workArea.Width-bounds.Width)/2
	y := workArea.Y + (workArea.Height-bounds.Height)/2

	// Ensure position is valid
	if x < workArea.X {
		x = workArea.X
	}
	if y < workArea.Y {
		y = workArea.Y
	}

	newBounds := walk.Rectangle{
		X:      x,
		Y:      y,
		Width:  bounds.Width,
		Height: bounds.Height,
	}

	_ = mw.SetBounds(newBounds)
}

// ResetWindowSize resets the window to default size and centers it
func ResetWindowSize(mw *walk.MainWindow, dpiCtx *DPIContext) {
	if mw == nil {
		return
	}

	size := CalculateInitialWindowSize(dpiCtx)
	bounds := mw.Bounds()
	bounds.Width = size.Width
	bounds.Height = size.Height

	_ = mw.SetBounds(bounds)
	CenterWindowOnScreen(mw)
}

// GetMinMaxWindowSize returns the minimum and maximum window sizes
func GetMinMaxWindowSize(dpiCtx *DPIContext) (minSize, maxSize walk.Size) {
	// Minimum: reduced for small screen support
	minSize = walk.Size{
		Width:  dpiCtx.Scale(900),
		Height: dpiCtx.Scale(600),
	}

	// Maximum: prevent unwieldy large windows
	maxSize = walk.Size{
		Width:  dpiCtx.Scale(2400),
		Height: dpiCtx.Scale(1600),
	}

	return
}

// DebouncedStateSaver handles debounced saving of window state
type DebouncedStateSaver struct {
	manager       *WindowStateManager
	mw            *walk.MainWindow
	saveRequested bool
}

// NewDebouncedStateSaver creates a new debounced state saver
func NewDebouncedStateSaver(manager *WindowStateManager, mw *walk.MainWindow) *DebouncedStateSaver {
	return &DebouncedStateSaver{
		manager: manager,
		mw:      mw,
	}
}

// RequestSave marks that a save is needed (actual save happens on a timer or on close)
func (d *DebouncedStateSaver) RequestSave() {
	d.saveRequested = true
}

// SaveIfRequested saves the state if a save was requested
func (d *DebouncedStateSaver) SaveIfRequested() {
	if d.saveRequested && d.manager != nil && d.mw != nil {
		_ = d.manager.SaveWindowState(d.mw)
		d.saveRequested = false
	}
}

// ForceSave immediately saves the state
func (d *DebouncedStateSaver) ForceSave() {
	if d.manager != nil && d.mw != nil {
		_ = d.manager.SaveWindowState(d.mw)
		d.saveRequested = false
	}
}
