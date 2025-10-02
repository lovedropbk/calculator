# Window Scaling Quick Start Guide

## What's New

Your application now supports:

✅ **High-DPI displays** - Perfect scaling on 4K monitors  
✅ **Responsive window resizing** - Content adapts automatically  
✅ **Multi-monitor support** - Seamless transitions between monitors  
✅ **Window state persistence** - Remembers size and position  
✅ **Smart constraints** - Window always stays on-screen  

---

## Key Features

### 1. DPI Awareness
The app automatically detects your monitor's DPI and scales all UI elements appropriately:
- **96 DPI (100%)** - Standard displays
- **120 DPI (125%)** - Slightly scaled
- **144 DPI (150%)** - High-DPI laptops
- **192 DPI (200%)** - 4K displays

### 2. Responsive Layouts
Three adaptive layout modes based on window width:
- **Compact** (<900px) - Essential columns only
- **Normal** (900-1400px) - Balanced layout
- **Wide** (>1400px) - All columns with generous spacing

### 3. Window Persistence
- Size and position saved automatically on close
- Restored on next launch
- Adapts if your monitor setup changed

### 4. Smart Constraints
- **MinSize:** 900×600 (scaled to your DPI)
- **MaxSize:** 2400×1600 (scaled to your DPI)
- Window always constrained to visible screen area

---

## User Experience

### First Launch
- Window appears centered on your primary monitor
- Size: 1200×800 (ideal for most workflows)
- Scaled appropriately for your display

### Resizing
- **Drag edges** - Window resizes smoothly
- **Maximize** - Fills screen (taskbar excluded)
- **Restore** - Returns to previous size
- **Table columns** - Automatically adjust to window width

### Multi-Monitor Setup
- **Drag to another monitor** - UI rescales if DPI differs
- **Unplug monitor** - Window moves to primary monitor
- **Change monitor order** - Window stays accessible

### Session Persistence
- Size, position, and maximize state saved on close
- State file: `%USERPROFILE%\.financial-calculator\window_state.json`
- Delete this file to reset to defaults

---

## Testing Your Setup

### Test 1: DPI Scaling
1. Right-click desktop → Display Settings
2. Note your current "Scale" setting
3. Launch the app
4. Verify UI elements are appropriately sized
5. Text should be readable, buttons clickable

### Test 2: Responsive Resize
1. Launch the app
2. Resize window from minimum to maximum
3. Watch table columns adapt
4. Try narrow width - optional columns hide
5. Try wide width - columns get more space

### Test 3: State Persistence
1. Resize window to custom size
2. Move to a specific position
3. Close application
4. Relaunch
5. Window should restore to same size/position

### Test 4: Multi-Monitor (if applicable)
1. Drag window to second monitor
2. Note if UI rescales (it will if DPI differs)
3. Close app while on second monitor
4. Relaunch - should restore on second monitor

---

## Troubleshooting

### Problem: UI Too Small on 4K Monitor
**Solution:** Check Windows Display Settings → Scale is set appropriately (150% or 200% recommended for 4K)

### Problem: Window Off-Screen
**Solution:** Delete `%USERPROFILE%\.financial-calculator\window_state.json` and relaunch

### Problem: Columns Too Narrow
**Solution:** Increase window width - columns will expand automatically

### Problem: Window Won't Resize
**Solution:** Ensure you're not at min/max size constraints. Try double-clicking title bar to maximize/restore.

---

## Technical Details

For implementation details, architecture diagrams, and API reference, see:
**[window-scaling-implementation.md](./window-scaling-implementation.md)**

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **Alt+Space, X** | Maximize window |
| **Alt+Space, R** | Restore window |
| **Alt+Space, S** | Resize mode |
| **Alt+Space, M** | Move mode |

---

## File Locations

**Window State:**
```
%USERPROFILE%\.financial-calculator\window_state.json
```

**Logs:**
```
walk/bin/startup.log
```

---

## What Changed

### New Files Added
- `walk/cmd/fc-walk/dpi_context.go` - DPI management
- `walk/cmd/fc-walk/responsive_layout.go` - Layout system
- `walk/cmd/fc-walk/window_state.go` - State persistence

### Updated Files
- `walk/cmd/fc-walk/main.go` - Event handlers & initialization
- `walk/cmd/fc-walk/screen_bounds.go` - Multi-monitor support
- `walk/cmd/fc-walk/responsive_runtime.go` - Integration

### Backward Compatibility
✅ **100% Compatible** - No breaking changes, existing functionality preserved

---

**Enjoy the improved user experience!** 🎉
