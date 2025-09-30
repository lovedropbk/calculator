Go
Skip to Main Content
Search packages or symbols

Why Gosubmenu dropdown icon
Learn
Docssubmenu dropdown icon
Packages
Communitysubmenu dropdown icon
Discover Packages
 
gitcode.net/mirrors/lxn/walk

Go
walk
package
module

Version: v0.0.0-...-c389da5 Latest 
Published: Jan 12, 2021 
License: BSD-3-Clause 
Imports: 29 
Imported by: 0
Details
unchecked Valid go.mod file 
checked Redistributable license 
unchecked Tagged version 
unchecked Stable version 
Learn more about best practices
Repository
Repository URL not available.
Links
Open Source Insights Logo Open Source Insights
Jump to ...
 README ¶
About Walk
==========

Walk is a "Windows Application Library Kit" for the Go Programming Language.

Its primarily useful for Desktop GUI development, but there is some more stuff.

Setup
=====

Make sure you have a working Go installation.
See [Getting Started](http://golang.org/doc/install.html)

##### Note
Walk currently requires Go 1.11.x or later.

##### To Install
Now run `go get github.com/lxn/walk`

Using Walk
==========

The preferred way to create GUIs with Walk is to use its declarative sub package,
as illustrated in this small example:

##### `test.go`

```go
package main

import (
	"github.com/lxn/walk"
	. "github.com/lxn/walk/declarative"
	"strings"
)

func main() {
	var inTE, outTE *walk.TextEdit

	MainWindow{
		Title:   "SCREAMO",
		MinSize: Size{600, 400},
		Layout:  VBox{},
		Children: []Widget{
			HSplitter{
				Children: []Widget{
					TextEdit{AssignTo: &inTE},
					TextEdit{AssignTo: &outTE, ReadOnly: true},
				},
			},
			PushButton{
				Text: "SCREAM",
				OnClicked: func() {
					outTE.SetText(strings.ToUpper(inTE.Text()))
				},
			},
		},
	}.Run()
}
```

##### Create Manifest `test.manifest`

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
    <assemblyIdentity version="1.0.0.0" processorArchitecture="*" name="SomeFunkyNameHere" type="win32"/>
    <dependency>
        <dependentAssembly>
            <assemblyIdentity type="win32" name="Microsoft.Windows.Common-Controls" version="6.0.0.0" processorArchitecture="*" publicKeyToken="6595b64144ccf1df" language="*"/>
        </dependentAssembly>
    </dependency>
    <application xmlns="urn:schemas-microsoft-com:asm.v3">
        <windowsSettings>
            <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2, PerMonitor</dpiAwareness>
            <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">True</dpiAware>
        </windowsSettings>
    </application>
</assembly>
```

Then either compile the manifest using the [rsrc tool](https://github.com/akavel/rsrc), like this:

	go get github.com/akavel/rsrc
	rsrc -manifest test.manifest -o rsrc.syso

or rename the `test.manifest` file to `test.exe.manifest` and distribute it with the application instead.

##### Build app

In the directory containing `test.go` run

	go build
	
To get rid of the cmd window, instead run

	go build -ldflags="-H windowsgui"

##### Run app
	
	test.exe
	
##### Sample Output (Windows 7)

![alt tag](http://i.imgur.com/lUrgE2Q.png)

##### More Examples
There are some [examples](examples) that should get you started.

Application Manifest Files
==========================
Walk requires Common Controls 6. This means that you must put an appropriate
application manifest file either next to your executable or embedded as a
resource.

You can copy one of the application manifest files that come with the examples.

To embed a manifest file as a resource, you can use the [rsrc tool](https://github.com/akavel/rsrc).

IMPORTANT: If you don't embed a manifest as a resource, then you should not launch
your executable before the manifest file is in place.
If you do anyway, the program will not run properly. And worse, Windows will not
recognize a manifest file, you later drop next to the executable. To fix this,
rebuild your executable and only launch it with a manifest file in place.

CGo Optimizations
=================

The usual default message loop includes calls to win32 API functions, which incurs a decent amount
of runtime overhead coming from Go. As an alternative to this, you may compile Walk using an
optional C implementation of the main message loop, by passing the `walk_use_cgo` build tag:

   go build -tags walk_use_cgo
Expand ▾
 Documentation ¶
Rendered for windows/amd64
Index ¶
Constants
Variables
func AltDown() bool
func AppDataPath() (string, error)
func AppendToWalkInit(fn func())
func CommonAppDataPath() (string, error)
func ControlDown() bool
func DriveNames() ([]string, error)
func FormatFloat(f float64, prec int) string
func FormatFloatGrouped(f float64, prec int) string
func InitWidget(widget Widget, parent Window, className string, style, exStyle uint32) error
func InitWindow(window, parent Window, className string, style, exStyle uint32) error
func InitWrapperWindow(window Window) error
func IntFrom96DPI(value, dpi int) int
func IntTo96DPI(value, dpi int) int
func LocalAppDataPath() (string, error)
func LogErrors() bool
func MouseWheelEventDelta(button MouseButton) int
func MouseWheelEventKeyState(button MouseButton) int
func MsgBox(owner Form, title, message string, style MsgBoxStyle) int
func MustRegisterWindowClass(className string)
func MustRegisterWindowClassWithStyle(className string, style uint32)
func MustRegisterWindowClassWithWndProcPtr(className string, wndProcPtr uintptr)
func MustRegisterWindowClassWithWndProcPtrAndStyle(className string, wndProcPtr uintptr, style uint32)
func PanicOnError() bool
func ParseFloat(s string) (float64, error)
func PersonalPath() (string, error)
func RegistryKeyString(rootKey *RegistryKey, subKeyPath, valueName string) (value string, err error)
func RegistryKeyUint32(rootKey *RegistryKey, subKeyPath, valueName string) (value uint32, err error)
func SetLogErrors(v bool)
func SetPanicOnError(v bool)
func SetTranslationFunc(f TranslationFunction)
func SetWindowFont(hwnd win.HWND, font *Font)
func ShiftDown() bool
func SystemPath() (string, error)
type AccRole
type AccState
type Accessibility
func (a *Accessibility) SetAccelerator(acc string) error
func (a *Accessibility) SetDefaultAction(defAction string) error
func (a *Accessibility) SetDescription(acc string) error
func (a *Accessibility) SetHelp(help string) error
func (a *Accessibility) SetName(name string) error
func (a *Accessibility) SetRole(role AccRole) error
func (a *Accessibility) SetRoleMap(roleMap string) error
func (a *Accessibility) SetState(state AccState) error
func (a *Accessibility) SetStateMap(stateMap string) error
func (a *Accessibility) SetValueMap(valueMap string) error
type Action
func NewAction() *Action
func NewMenuAction(menu *Menu) *Action
func NewSeparatorAction() *Action
func (a *Action) Checkable() bool
func (a *Action) Checked() bool
func (a *Action) CheckedCondition() Condition
func (a *Action) Default() bool
func (a *Action) DefaultCondition() Condition
func (a *Action) Enabled() bool
func (a *Action) EnabledCondition() Condition
func (a *Action) Exclusive() bool
func (a *Action) Image() Image
func (a *Action) IsSeparator() bool
func (a *Action) Menu() *Menu
func (a *Action) SetCheckable(value bool) (err error)
func (a *Action) SetChecked(value bool) (err error)
func (a *Action) SetCheckedCondition(c Condition)
func (a *Action) SetDefault(value bool) (err error)
func (a *Action) SetDefaultCondition(c Condition)
func (a *Action) SetEnabled(value bool) (err error)
func (a *Action) SetEnabledCondition(c Condition)
func (a *Action) SetExclusive(value bool) (err error)
func (a *Action) SetImage(value Image) (err error)
func (a *Action) SetShortcut(shortcut Shortcut) (err error)
func (a *Action) SetText(value string) (err error)
func (a *Action) SetToolTip(value string) (err error)
func (a *Action) SetVisible(value bool) (err error)
func (a *Action) SetVisibleCondition(c Condition)
func (a *Action) Shortcut() Shortcut
func (a *Action) Text() string
func (a *Action) ToolTip() string
func (a *Action) Triggered() *Event
func (a *Action) Visible() bool
func (a *Action) VisibleCondition() Condition
type ActionList
func (l *ActionList) Add(action *Action) error
func (l *ActionList) AddMenu(menu *Menu) (*Action, error)
func (l *ActionList) At(index int) *Action
func (l *ActionList) Clear() error
func (l *ActionList) Contains(action *Action) bool
func (l *ActionList) Index(action *Action) int
func (l *ActionList) Insert(index int, action *Action) error
func (l *ActionList) InsertMenu(index int, menu *Menu) (*Action, error)
func (l *ActionList) Len() int
func (l *ActionList) Remove(action *Action) error
func (l *ActionList) RemoveAt(index int) error
type Alignment1D
type Alignment2D
type Application
func App() *Application
func (app *Application) ActiveForm() Form
func (app *Application) Exit(exitCode int)
func (app *Application) ExitCode() int
func (app *Application) OrganizationName() string
func (app *Application) Panicking() *ErrorEvent
func (app *Application) ProductName() string
func (app *Application) SetOrganizationName(value string)
func (app *Application) SetProductName(value string)
func (app *Application) SetSettings(value Settings)
func (app *Application) Settings() Settings
type ApplyDPIer
type ApplyFonter
type ApplySysColorser
type BindingValueProvider
type Bitmap
func BitmapFrom(src interface{}, dpi int) (*Bitmap, error)
func NewBitmap(size Size) (*Bitmap, error)deprecated
func NewBitmapForDPI(size Size, dpi int) (*Bitmap, error)
func NewBitmapFromFile(filePath string) (*Bitmap, error)deprecated
func NewBitmapFromFileForDPI(filePath string, dpi int) (*Bitmap, error)
func NewBitmapFromIcon(icon *Icon, size Size) (*Bitmap, error)deprecated
func NewBitmapFromIconForDPI(icon *Icon, size Size, dpi int) (*Bitmap, error)
func NewBitmapFromImage(im image.Image) (*Bitmap, error)deprecated
func NewBitmapFromImageForDPI(im image.Image, dpi int) (*Bitmap, error)
func NewBitmapFromImageWithSize(image Image, size Size) (*Bitmap, error)
func NewBitmapFromResource(name string) (*Bitmap, error)deprecated
func NewBitmapFromResourceForDPI(name string, dpi int) (*Bitmap, error)
func NewBitmapFromResourceId(id int) (*Bitmap, error)deprecated
func NewBitmapFromResourceIdForDPI(id int, dpi int) (*Bitmap, error)
func NewBitmapFromWindow(window Window) (*Bitmap, error)
func NewBitmapWithTransparentPixels(size Size) (*Bitmap, error)deprecated
func NewBitmapWithTransparentPixelsForDPI(size Size, dpi int) (*Bitmap, error)
func (bmp *Bitmap) Dispose()
func (bmp *Bitmap) Size() Size
func (bmp *Bitmap) ToImage() (*image.RGBA, error)
type BitmapBrush
func NewBitmapBrush(bitmap *Bitmap) (*BitmapBrush, error)
func (b *BitmapBrush) Bitmap() *Bitmap
func (bb *BitmapBrush) Dispose()
type BorderGlowEffect
func NewBorderGlowEffect(color Color) (*BorderGlowEffect, error)
func (wgeb *BorderGlowEffect) Dispose()
func (bge *BorderGlowEffect) Draw(widget Widget, canvas *Canvas) error
type BoxLayout
func NewHBoxLayout() *BoxLayout
func NewVBoxLayout() *BoxLayout
func (l *BoxLayout) CreateLayoutItem(ctx *LayoutContext) ContainerLayoutItem
func (l *BoxLayout) Orientation() Orientation
func (l *BoxLayout) SetOrientation(value Orientation) error
func (l *BoxLayout) SetStretchFactor(widget Widget, factor int) error
func (l *BoxLayout) StretchFactor(widget Widget) int
type Brush
func NullBrush() Brush
type Button
func (b *Button) ApplyDPI(dpi int)
func (b *Button) Checked() bool
func (b *Button) CheckedChanged() *Event
func (b *Button) Clicked() *Event
func (b *Button) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (b *Button) Image() Image
func (b *Button) ImageChanged() *Event
func (b *Button) Persistent() bool
func (b *Button) RestoreState() error
func (b *Button) SaveState() error
func (b *Button) SetChecked(checked bool)
func (b *Button) SetImage(image Image) error
func (b *Button) SetPersistent(value bool)
func (b *Button) SetText(value string) error
func (b *Button) Text() string
func (b *Button) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type CancelEvent
func (e *CancelEvent) Attach(handler CancelEventHandler) int
func (e *CancelEvent) Detach(handle int)
func (e *CancelEvent) Once(handler CancelEventHandler)
type CancelEventHandler
type CancelEventPublisher
func (p *CancelEventPublisher) Event() *CancelEvent
func (p *CancelEventPublisher) Publish(canceled *bool)
type Canvas
func NewCanvasFromImage(image Image) (*Canvas, error)
func (c *Canvas) Bounds() Rectangle
func (c *Canvas) BoundsPixels() Rectangle
func (c *Canvas) DPI() int
func (c *Canvas) Dispose()
func (c *Canvas) DrawBitmapPart(bmp *Bitmap, dst, src Rectangle) error
func (c *Canvas) DrawBitmapPartWithOpacity(bmp *Bitmap, dst, src Rectangle, opacity byte) errordeprecated
func (c *Canvas) DrawBitmapPartWithOpacityPixels(bmp *Bitmap, dst, src Rectangle, opacity byte) error
func (c *Canvas) DrawBitmapWithOpacity(bmp *Bitmap, bounds Rectangle, opacity byte) errordeprecated
func (c *Canvas) DrawBitmapWithOpacityPixels(bmp *Bitmap, bounds Rectangle, opacity byte) error
func (c *Canvas) DrawEllipse(pen Pen, bounds Rectangle) errordeprecated
func (c *Canvas) DrawEllipsePixels(pen Pen, bounds Rectangle) error
func (c *Canvas) DrawImage(image Image, location Point) errordeprecated
func (c *Canvas) DrawImagePixels(image Image, location Point) error
func (c *Canvas) DrawImageStretched(image Image, bounds Rectangle) errordeprecated
func (c *Canvas) DrawImageStretchedPixels(image Image, bounds Rectangle) error
func (c *Canvas) DrawLine(pen Pen, from, to Point) errordeprecated
func (c *Canvas) DrawLinePixels(pen Pen, from, to Point) error
func (c *Canvas) DrawPolyline(pen Pen, points []Point) errordeprecated
func (c *Canvas) DrawPolylinePixels(pen Pen, points []Point) error
func (c *Canvas) DrawRectangle(pen Pen, bounds Rectangle) errordeprecated
func (c *Canvas) DrawRectanglePixels(pen Pen, bounds Rectangle) error
func (c *Canvas) DrawRoundedRectangle(pen Pen, bounds Rectangle, ellipseSize Size) errordeprecated
func (c *Canvas) DrawRoundedRectanglePixels(pen Pen, bounds Rectangle, ellipseSize Size) error
func (c *Canvas) DrawText(text string, font *Font, color Color, bounds Rectangle, format DrawTextFormat) errordeprecated
func (c *Canvas) DrawTextPixels(text string, font *Font, color Color, bounds Rectangle, format DrawTextFormat) error
func (c *Canvas) FillEllipse(brush Brush, bounds Rectangle) errordeprecated
func (c *Canvas) FillEllipsePixels(brush Brush, bounds Rectangle) error
func (c *Canvas) FillRectangle(brush Brush, bounds Rectangle) errordeprecated
func (c *Canvas) FillRectanglePixels(brush Brush, bounds Rectangle) error
func (c *Canvas) FillRoundedRectangle(brush Brush, bounds Rectangle, ellipseSize Size) errordeprecated
func (c *Canvas) FillRoundedRectanglePixels(brush Brush, bounds Rectangle, ellipseSize Size) error
func (c *Canvas) GradientFillRectangle(color1, color2 Color, orientation Orientation, bounds Rectangle) errordeprecated
func (c *Canvas) GradientFillRectanglePixels(color1, color2 Color, orientation Orientation, bounds Rectangle) error
func (c *Canvas) HDC() win.HDC
func (c *Canvas) MeasureAndModifyTextPixels(text string, font *Font, bounds Rectangle, format DrawTextFormat) (boundsMeasured Rectangle, textDisplayed string, err error)
func (c *Canvas) MeasureText(text string, font *Font, bounds Rectangle, format DrawTextFormat) (boundsMeasured Rectangle, runesFitted int, err error)deprecated
func (c *Canvas) MeasureTextPixels(text string, font *Font, bounds Rectangle, format DrawTextFormat) (boundsMeasured Rectangle, runesFitted int, err error)
type CaseMode
type CellStyle
func (cs *CellStyle) Bounds() Rectangle
func (cs *CellStyle) BoundsPixels() Rectangle
func (cs *CellStyle) Canvas() *Canvas
func (cs *CellStyle) Col() int
func (cs *CellStyle) Row() int
type CellStyler
type CheckBox
func NewCheckBox(parent Container) (*CheckBox, error)
func (cb *CheckBox) CheckState() CheckState
func (cb *CheckBox) CheckStateChanged() *Event
func (cb *CheckBox) RestoreState() error
func (cb *CheckBox) SaveState() error
func (cb *CheckBox) SetCheckState(state CheckState)
func (cb *CheckBox) SetTextOnLeftSide(textLeft bool) error
func (cb *CheckBox) SetTristate(tristate bool) error
func (cb *CheckBox) TextOnLeftSide() bool
func (cb *CheckBox) Tristate() bool
func (cb *CheckBox) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type CheckState
type ClipboardService
func Clipboard() *ClipboardService
func (c *ClipboardService) Clear() error
func (c *ClipboardService) ContainsText() (available bool, err error)
func (c *ClipboardService) ContentsChanged() *Event
func (c *ClipboardService) SetText(s string) error
func (c *ClipboardService) Text() (text string, err error)
type CloseEvent
func (e *CloseEvent) Attach(handler CloseEventHandler) int
func (e *CloseEvent) Detach(handle int)
func (e *CloseEvent) Once(handler CloseEventHandler)
type CloseEventHandler
type CloseEventPublisher
func (p *CloseEventPublisher) Event() *CloseEvent
func (p *CloseEventPublisher) Publish(canceled *bool, reason CloseReason)
type CloseReason
type Color
func RGB(r, g, b byte) Color
func (c Color) B() byte
func (c Color) G() byte
func (c Color) R() byte
type ComboBox
func NewComboBox(parent Container) (*ComboBox, error)
func NewDropDownBox(parent Container) (*ComboBox, error)
func (cb *ComboBox) BindingMember() string
func (cb *ComboBox) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (cb *ComboBox) CurrentIndex() int
func (cb *ComboBox) CurrentIndexChanged() *Event
func (cb *ComboBox) DisplayMember() string
func (cb *ComboBox) Editable() bool
func (cb *ComboBox) EditingFinished() *Event
func (cb *ComboBox) Format() string
func (cb *ComboBox) MaxLength() int
func (cb *ComboBox) Model() interface{}
func (*ComboBox) NeedsWmSize() bool
func (cb *ComboBox) Persistent() bool
func (cb *ComboBox) Precision() int
func (cb *ComboBox) RestoreState() error
func (cb *ComboBox) SaveState() error
func (cb *ComboBox) SetBindingMember(bindingMember string) error
func (cb *ComboBox) SetCurrentIndex(value int) error
func (cb *ComboBox) SetDisplayMember(displayMember string) error
func (cb *ComboBox) SetFormat(value string)
func (cb *ComboBox) SetMaxLength(value int)
func (cb *ComboBox) SetModel(mdl interface{}) error
func (cb *ComboBox) SetPersistent(value bool)
func (cb *ComboBox) SetPrecision(value int)
func (cb *ComboBox) SetText(value string) error
func (cb *ComboBox) SetTextSelection(start, end int)
func (cb *ComboBox) Text() string
func (cb *ComboBox) TextChanged() *Event
func (cb *ComboBox) TextSelection() (start, end int)
func (cb *ComboBox) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type Composite
func NewComposite(parent Container) (*Composite, error)
func NewCompositeWithStyle(parent Window, style uint32) (*Composite, error)
type Condition
func NewAllCondition(items ...Condition) Condition
func NewAnyCondition(items ...Condition) Condition
func NewNegatedCondition(other Condition) Condition
type Container
type ContainerBase
func (cb *ContainerBase) ApplyDPI(dpi int)
func (cb *ContainerBase) ApplySysColors()
func (cb *ContainerBase) AsContainerBase() *ContainerBase
func (cb *ContainerBase) AsWidgetBase() *WidgetBase
func (cb *ContainerBase) Children() *WidgetList
func (cb *ContainerBase) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (cb *ContainerBase) DataBinder() *DataBinder
func (cb *ContainerBase) Layout() Layout
func (cb *ContainerBase) NextChildID() int32
func (cb *ContainerBase) Persistent() bool
func (cb *ContainerBase) RestoreState() error
func (cb *ContainerBase) SaveState() error
func (cb *ContainerBase) SetDataBinder(db *DataBinder)
func (cb *ContainerBase) SetLayout(value Layout) error
func (cb *ContainerBase) SetPersistent(value bool)
func (cb *ContainerBase) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type ContainerLayoutItem
func CreateLayoutItemsForContainer(container Container) ContainerLayoutItem
func CreateLayoutItemsForContainerWithContext(container Container, ctx *LayoutContext) ContainerLayoutItem
type ContainerLayoutItemBase
func (clib *ContainerLayoutItemBase) AsContainerLayoutItemBase() *ContainerLayoutItemBase
func (clib *ContainerLayoutItemBase) Children() []LayoutItem
func (clib *ContainerLayoutItemBase) HasHeightForWidth() bool
func (clib *ContainerLayoutItemBase) MinSizeEffectiveForChild(child LayoutItem) Size
func (clib *ContainerLayoutItemBase) SetChildren(children []LayoutItem)
type CosmeticPen
func NewCosmeticPen(style PenStyle, color Color) (*CosmeticPen, error)
func (p *CosmeticPen) Color() Color
func (p *CosmeticPen) Dispose()
func (p *CosmeticPen) Style() PenStyle
func (p *CosmeticPen) Width() int
type Cursor
func CursorAppStarting() Cursor
func CursorArrow() Cursor
func CursorCross() Cursor
func CursorHand() Cursor
func CursorHelp() Cursor
func CursorIBeam() Cursor
func CursorIcon() Cursor
func CursorNo() Cursor
func CursorSize() Cursor
func CursorSizeAll() Cursor
func CursorSizeNESW() Cursor
func CursorSizeNS() Cursor
func CursorSizeNWSE() Cursor
func CursorSizeWE() Cursor
func CursorUpArrow() Cursor
func CursorWait() Cursor
func NewCursorFromImage(im image.Image, hotspot image.Point) (Cursor, error)
type CustomWidget
func NewCustomWidget(parent Container, style uint, paint PaintFunc) (*CustomWidget, error)deprecated
func NewCustomWidgetPixels(parent Container, style uint, paintPixels PaintFunc) (*CustomWidget, error)
func (cw *CustomWidget) ClearsBackground() bool
func (*CustomWidget) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (cw *CustomWidget) InvalidatesOnResize() bool
func (cw *CustomWidget) PaintMode() PaintMode
func (cw *CustomWidget) SetClearsBackground(value bool)
func (cw *CustomWidget) SetInvalidatesOnResize(value bool)
func (cw *CustomWidget) SetPaintMode(value PaintMode)
func (cw *CustomWidget) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type DataBinder
func NewDataBinder() *DataBinder
func (db *DataBinder) AutoSubmit() bool
func (db *DataBinder) AutoSubmitDelay() time.Duration
func (db *DataBinder) AutoSubmitSuspended() bool
func (db *DataBinder) BoundWidgets() []Widget
func (db *DataBinder) CanSubmit() bool
func (db *DataBinder) CanSubmitChanged() *Event
func (db *DataBinder) DataSource() interface{}
func (db *DataBinder) DataSourceChanged() *Event
func (db *DataBinder) Dirty() bool
func (db *DataBinder) ErrorPresenter() ErrorPresenter
func (db *DataBinder) Expression(path string) Expression
func (db *DataBinder) Reset() error
func (db *DataBinder) ResetFinished() *Event
func (db *DataBinder) SetAutoSubmit(autoSubmit bool)
func (db *DataBinder) SetAutoSubmitDelay(delay time.Duration)
func (db *DataBinder) SetAutoSubmitSuspended(suspended bool)
func (db *DataBinder) SetBoundWidgets(boundWidgets []Widget)
func (db *DataBinder) SetDataSource(dataSource interface{}) error
func (db *DataBinder) SetErrorPresenter(ep ErrorPresenter)
func (db *DataBinder) Submit() error
func (db *DataBinder) Submitted() *Event
type DataField
type DateEdit
func NewDateEdit(parent Container) (*DateEdit, error)
func NewDateEditWithNoneOption(parent Container) (*DateEdit, error)
func (de *DateEdit) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (de *DateEdit) Date() time.Time
func (de *DateEdit) DateChanged() *Event
func (de *DateEdit) Format() string
func (*DateEdit) NeedsWmSize() bool
func (de *DateEdit) Range() (min, max time.Time)
func (de *DateEdit) SetDate(date time.Time) error
func (de *DateEdit) SetFormat(format string) error
func (de *DateEdit) SetRange(min, max time.Time) error
func (de *DateEdit) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type DateLabel
func NewDateLabel(parent Container) (*DateLabel, error)
func (s *DateLabel) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (dl *DateLabel) Date() time.Time
func (s *DateLabel) Dispose()
func (dl *DateLabel) Format() string
func (dl *DateLabel) SetDate(date time.Time) error
func (dl *DateLabel) SetFormat(format string) error
func (dl *DateLabel) SetTextAlignment(alignment Alignment1D) error
func (s *DateLabel) SetTextColor(c Color)
func (dl *DateLabel) TextAlignment() Alignment1D
func (s *DateLabel) TextColor() Color
func (s *DateLabel) WndProc(hwnd win.HWND, msg uint32, wp, lp uintptr) uintptr
type DelegateCondition
func NewDelegateCondition(satisfied func() bool, changed *Event) *DelegateCondition
func (dc *DelegateCondition) Changed() *Event
func (dc *DelegateCondition) Satisfied() bool
func (dc *DelegateCondition) Value() interface{}
type Dialog
func NewDialog(owner Form) (*Dialog, error)
func NewDialogWithFixedSize(owner Form) (*Dialog, error)
func (dlg *Dialog) Accept()
func (dlg *Dialog) Cancel()
func (dlg *Dialog) CancelButton() *PushButton
func (dlg *Dialog) Close(result int)
func (dlg *Dialog) DefaultButton() *PushButton
func (dlg *Dialog) Result() int
func (dlg *Dialog) Run() int
func (dlg *Dialog) SetCancelButton(button *PushButton) error
func (dlg *Dialog) SetDefaultButton(button *PushButton) error
func (dlg *Dialog) Show()
func (dlg *Dialog) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type Disposable
type Disposables
func (d *Disposables) Add(item Disposable)
func (d *Disposables) Spare()
func (d *Disposables) Treat()
type DrawTextFormat
type DropFilesEvent
func (e *DropFilesEvent) Attach(handler DropFilesEventHandler) int
func (e *DropFilesEvent) Detach(handle int)
func (e *DropFilesEvent) Once(handler DropFilesEventHandler)
type DropFilesEventHandler
type DropFilesEventPublisher
func (p *DropFilesEventPublisher) Event(hWnd win.HWND) *DropFilesEvent
func (p *DropFilesEventPublisher) Publish(hDrop win.HDROP)
type DropShadowEffect
func NewDropShadowEffect(color Color) (*DropShadowEffect, error)
func (wgeb *DropShadowEffect) Dispose()
func (dse *DropShadowEffect) Draw(widget Widget, canvas *Canvas) error
type EllipsisMode
type Error
func (err *Error) Error() string
func (err *Error) Inner() error
func (err *Error) Message() string
func (err *Error) Stack() []byte
type ErrorEvent
func (e *ErrorEvent) Attach(handler ErrorEventHandler) int
func (e *ErrorEvent) Detach(handle int)
func (e *ErrorEvent) Once(handler ErrorEventHandler)
type ErrorEventHandler
type ErrorEventPublisher
func (p *ErrorEventPublisher) Event() *ErrorEvent
func (p *ErrorEventPublisher) Publish(err error)
type ErrorPresenter
type Event
func (e *Event) Attach(handler EventHandler) int
func (e *Event) Detach(handle int)
func (e *Event) Once(handler EventHandler)
type EventHandler
type EventPublisher
func (p *EventPublisher) Event() *Event
func (p *EventPublisher) Publish()
type Expression
func NewReflectExpression(root Expression, path string) Expression
type ExtractableIcon
type FileDialog
func (dlg *FileDialog) ShowBrowseFolder(owner Form) (accepted bool, err error)
func (dlg *FileDialog) ShowOpen(owner Form) (accepted bool, err error)
func (dlg *FileDialog) ShowOpenMultiple(owner Form) (accepted bool, err error)
func (dlg *FileDialog) ShowSave(owner Form) (accepted bool, err error)
type FlowLayout
func NewFlowLayout() *FlowLayout
func (l *FlowLayout) CreateLayoutItem(ctx *LayoutContext) ContainerLayoutItem
func (l *FlowLayout) SetStretchFactor(widget Widget, factor int) error
func (l *FlowLayout) StretchFactor(widget Widget) int
type Font
func NewFont(family string, pointSize int, style FontStyle) (*Font, error)
func (f *Font) Bold() bool
func (f *Font) Dispose()
func (f *Font) Family() string
func (f *Font) Italic() bool
func (f *Font) PointSize() int
func (f *Font) StrikeOut() bool
func (f *Font) Style() FontStyle
func (f *Font) Underline() bool
type FontMemResource
func NewFontMemResourceById(id int) (*FontMemResource, error)
func NewFontMemResourceByName(name string) (*FontMemResource, error)
func (fmr *FontMemResource) Dispose()
type FontStyle
type Form
type FormBase
func (fb *FormBase) Activate() error
func (fb *FormBase) Activating() *Event
func (fb *FormBase) ApplySysColors()
func (fb *FormBase) AsContainerBase() *ContainerBase
func (fb *FormBase) AsFormBase() *FormBase
func (fb *FormBase) Background() Brush
func (fb *FormBase) Children() *WidgetList
func (fb *FormBase) Close() error
func (fb *FormBase) Closing() *CloseEvent
func (fb *FormBase) ContextMenu() *Menu
func (fb *FormBase) ContextMenuLocation() Point
func (fb *FormBase) DataBinder() *DataBinder
func (fb *FormBase) Deactivating() *Event
func (fb *FormBase) Dispose()
func (fb *FormBase) Hide()
func (fb *FormBase) Icon() Image
func (fb *FormBase) IconChanged() *Event
func (fb *FormBase) Layout() Layout
func (fb *FormBase) MouseDown() *MouseEvent
func (fb *FormBase) MouseMove() *MouseEvent
func (fb *FormBase) MouseUp() *MouseEvent
func (fb *FormBase) Owner() Form
func (fb *FormBase) Persistent() bool
func (fb *FormBase) ProgressIndicator() *ProgressIndicator
func (fb *FormBase) RestoreState() error
func (fb *FormBase) RightToLeftLayout() bool
func (fb *FormBase) Run() int
func (fb *FormBase) SaveState() error
func (fb *FormBase) SetBackground(background Brush)
func (fb *FormBase) SetBoundsPixels(bounds Rectangle) error
func (fb *FormBase) SetContextMenu(contextMenu *Menu)
func (fb *FormBase) SetDataBinder(db *DataBinder)
func (fb *FormBase) SetIcon(icon Image) error
func (fb *FormBase) SetLayout(value Layout) error
func (fb *FormBase) SetOwner(value Form) error
func (fb *FormBase) SetPersistent(value bool)
func (fb *FormBase) SetRightToLeftLayout(rtl bool) error
func (fb *FormBase) SetSuspended(suspended bool)
func (fb *FormBase) SetTitle(value string) error
func (fb *FormBase) Show()
func (fb *FormBase) Starting() *Event
func (fb *FormBase) Title() string
func (fb *FormBase) TitleChanged() *Event
func (fb *FormBase) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type GeometricPen
func NewGeometricPen(style PenStyle, width int, brush Brush) (*GeometricPen, error)
func (p *GeometricPen) Brush() Brush
func (p *GeometricPen) Dispose()
func (p *GeometricPen) Style() PenStyle
func (p *GeometricPen) Width() int
type Geometry
type GradientBrush
func NewGradientBrush(vertexes []GradientVertex, triangles []GradientTriangle) (*GradientBrush, error)
func NewHorizontalGradientBrush(stops []GradientStop) (*GradientBrush, error)
func NewVerticalGradientBrush(stops []GradientStop) (*GradientBrush, error)
func (bb *GradientBrush) Dispose()
type GradientComposite
func NewGradientComposite(parent Container) (*GradientComposite, error)
func NewGradientCompositeWithStyle(parent Container, style uint32) (*GradientComposite, error)
func (gc *GradientComposite) Color1() Color
func (gc *GradientComposite) Color2() Color
func (gc *GradientComposite) Dispose()
func (gc *GradientComposite) SetColor1(c Color) (err error)
func (gc *GradientComposite) SetColor2(c Color) (err error)
func (gc *GradientComposite) SetVertical(vertical bool) (err error)
func (gc *GradientComposite) Vertical() bool
func (gc *GradientComposite) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type GradientStop
type GradientTriangle
type GradientVertex
type GridLayout
func NewGridLayout() *GridLayout
func (l *GridLayout) ColumnStretchFactor(column int) int
func (l *GridLayout) CreateLayoutItem(ctx *LayoutContext) ContainerLayoutItem
func (l *GridLayout) Range(widget Widget) (r Rectangle, ok bool)
func (l *GridLayout) RowStretchFactor(row int) int
func (l *GridLayout) SetColumnStretchFactor(column, factor int) error
func (l *GridLayout) SetRange(widget Widget, r Rectangle) error
func (l *GridLayout) SetRowStretchFactor(row, factor int) error
type GroupBox
func NewGroupBox(parent Container) (*GroupBox, error)
func (gb *GroupBox) ApplyDPI(dpi int)
func (gb *GroupBox) AsContainerBase() *ContainerBase
func (gb *GroupBox) Checkable() bool
func (gb *GroupBox) Checked() bool
func (gb *GroupBox) CheckedChanged() *Event
func (gb *GroupBox) Children() *WidgetList
func (gb *GroupBox) ClientBoundsPixels() Rectangle
func (gb *GroupBox) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (gb *GroupBox) DataBinder() *DataBinder
func (gb *GroupBox) Layout() Layout
func (gb *GroupBox) MouseDown() *MouseEvent
func (gb *GroupBox) MouseMove() *MouseEvent
func (gb *GroupBox) MouseUp() *MouseEvent
func (gb *GroupBox) Persistent() bool
func (gb *GroupBox) RestoreState() error
func (gb *GroupBox) SaveState() error
func (gb *GroupBox) SetCheckable(checkable bool)
func (gb *GroupBox) SetChecked(checked bool)
func (gb *GroupBox) SetDataBinder(dataBinder *DataBinder)
func (gb *GroupBox) SetLayout(value Layout) error
func (gb *GroupBox) SetPersistent(value bool)
func (gb *GroupBox) SetSuspended(suspend bool)
func (gb *GroupBox) SetTitle(title string) error
func (gb *GroupBox) Title() string
func (gb *GroupBox) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type HasChilder
type HatchBrush
func NewHatchBrush(color Color, style HatchStyle) (*HatchBrush, error)
func (b *HatchBrush) Color() Color
func (bb *HatchBrush) Dispose()
func (b *HatchBrush) Style() HatchStyle
type HatchStyle
type HeightForWidther
type IDProvider
type Icon
func IconApplication() *Icon
func IconError() *Icon
func IconFrom(src interface{}, dpi int) (*Icon, error)
func IconInformation() *Icon
func IconQuestion() *Icon
func IconShield() *Icon
func IconWarning() *Icon
func IconWinLogo() *Icon
func NewIconExtractedFromFile(filePath string, index, _ int) (*Icon, error)
func NewIconExtractedFromFileWithSize(filePath string, index, size int) (*Icon, error)
func NewIconFromBitmap(bmp *Bitmap) (ic *Icon, err error)
func NewIconFromFile(filePath string) (*Icon, error)
func NewIconFromFileWithSize(filePath string, size Size) (*Icon, error)
func NewIconFromHICON(hIcon win.HICON) (ic *Icon, err error)deprecated
func NewIconFromHICONForDPI(hIcon win.HICON, dpi int) (ic *Icon, err error)
func NewIconFromImage(im image.Image) (ic *Icon, err error)deprecated
func NewIconFromImageForDPI(im image.Image, dpi int) (ic *Icon, err error)
func NewIconFromImageWithSize(image Image, size Size) (*Icon, error)
func NewIconFromResource(name string) (*Icon, error)
func NewIconFromResourceId(id int) (*Icon, error)
func NewIconFromResourceIdWithSize(id int, size Size) (*Icon, error)
func NewIconFromResourceWithSize(name string, size Size) (*Icon, error)
func NewIconFromSysDLL(dllBaseName string, index int) (*Icon, error)
func NewIconFromSysDLLWithSize(dllBaseName string, index, size int) (*Icon, error)
func (i *Icon) Dispose()
func (i *Icon) Size() Size
type IconCache
func NewIconCache() *IconCache
func (ic *IconCache) Bitmap(image Image, dpi int) (*Bitmap, error)
func (ic *IconCache) Clear()
func (ic *IconCache) Dispose()
func (ic *IconCache) Icon(image Image, dpi int) (*Icon, error)
type IdealSizer
type Image
func ImageFrom(src interface{}) (img Image, err error)
func NewImageFromFile(filePath string) (Image, error)deprecated
func NewImageFromFileForDPI(filePath string, dpi int) (Image, error)
type ImageList
func NewImageList(imageSize Size, maskColor Color) (*ImageList, error)deprecated
func NewImageListForDPI(imageSize Size, maskColor Color, dpi int) (*ImageList, error)
func (il *ImageList) Add(bitmap, maskBitmap *Bitmap) (int, error)
func (il *ImageList) AddIcon(icon *Icon) (int32, error)
func (il *ImageList) AddImage(image interface{}) (int32, error)
func (il *ImageList) AddMasked(bitmap *Bitmap) (int32, error)
func (il *ImageList) Dispose()
func (il *ImageList) DrawPixels(canvas *Canvas, index int, bounds Rectangle) error
func (il *ImageList) Handle() win.HIMAGELIST
func (il *ImageList) MaskColor() Color
type ImageProvider
type ImageView
func NewImageView(parent Container) (*ImageView, error)
func (iv *ImageView) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (iv *ImageView) Image() Image
func (iv *ImageView) ImageChanged() *Event
func (iv *ImageView) Margin() int
func (iv *ImageView) MarginChanged() *Event
func (iv *ImageView) Mode() ImageViewMode
func (iv *ImageView) SetImage(image Image) error
func (iv *ImageView) SetMargin(margin int) error
func (iv *ImageView) SetMode(mode ImageViewMode)
type ImageViewMode
type Imager
type IniFileSettings
func NewIniFileSettings(fileName string) *IniFileSettings
func (ifs *IniFileSettings) ExpireDuration() time.Duration
func (ifs *IniFileSettings) FilePath() string
func (ifs *IniFileSettings) Get(key string) (string, bool)
func (ifs *IniFileSettings) Load() error
func (ifs *IniFileSettings) Portable() bool
func (ifs *IniFileSettings) Put(key, value string) error
func (ifs *IniFileSettings) PutExpiring(key, value string) error
func (ifs *IniFileSettings) Remove(key string) error
func (ifs *IniFileSettings) Save() error
func (ifs *IniFileSettings) SetExpireDuration(expireDuration time.Duration)
func (ifs *IniFileSettings) SetPortable(portable bool)
func (ifs *IniFileSettings) Timestamp(key string) (time.Time, bool)
type IntEvent
func (e *IntEvent) Attach(handler IntEventHandler) int
func (e *IntEvent) Detach(handle int)
func (e *IntEvent) Once(handler IntEventHandler)
type IntEventHandler
type IntEventPublisher
func (p *IntEventPublisher) Event() *IntEvent
func (p *IntEventPublisher) Publish(n int)
type IntRangeEvent
func (e *IntRangeEvent) Attach(handler IntRangeEventHandler) int
func (e *IntRangeEvent) Detach(handle int)
func (e *IntRangeEvent) Once(handler IntRangeEventHandler)
type IntRangeEventHandler
type IntRangeEventPublisher
func (p *IntRangeEventPublisher) Event() *IntRangeEvent
func (p *IntRangeEventPublisher) Publish(from, to int)
type ItemChecker
type Key
func (k Key) String() string
type KeyEvent
func (e *KeyEvent) Attach(handler KeyEventHandler) int
func (e *KeyEvent) Detach(handle int)
func (e *KeyEvent) Once(handler KeyEventHandler)
type KeyEventHandler
type KeyEventPublisher
func (p *KeyEventPublisher) Event() *KeyEvent
func (p *KeyEventPublisher) Publish(key Key)
type Label
func NewLabel(parent Container) (*Label, error)
func NewLabelWithStyle(parent Container, style uint32) (*Label, error)
func (s *Label) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (s *Label) Dispose()
func (l *Label) EllipsisMode() EllipsisMode
func (l *Label) SetEllipsisMode(mode EllipsisMode) error
func (l *Label) SetText(text string) error
func (l *Label) SetTextAlignment(alignment Alignment1D) error
func (s *Label) SetTextColor(c Color)
func (l *Label) Text() string
func (l *Label) TextAlignment() Alignment1D
func (s *Label) TextColor() Color
func (s *Label) WndProc(hwnd win.HWND, msg uint32, wp, lp uintptr) uintptr
type Layout
type LayoutBase
func (l *LayoutBase) Alignment() Alignment2D
func (l *LayoutBase) Container() Container
func (l *LayoutBase) Margins() Margins
func (l *LayoutBase) SetAlignment(alignment Alignment2D) error
func (l *LayoutBase) SetContainer(value Container)
func (l *LayoutBase) SetMargins(value Margins) error
func (l *LayoutBase) SetSpacing(value int) error
func (l *LayoutBase) Spacing() int
type LayoutContext
func (ctx *LayoutContext) DPI() int
type LayoutFlags
type LayoutItem
func NewGreedyLayoutItem() LayoutItem
type LayoutItemBase
func (lib *LayoutItemBase) AsLayoutItemBase() *LayoutItemBase
func (lib *LayoutItemBase) Context() *LayoutContext
func (lib *LayoutItemBase) Geometry() *Geometry
func (lib *LayoutItemBase) Handle() win.HWND
func (lib *LayoutItemBase) Parent() ContainerLayoutItem
func (lib *LayoutItemBase) Visible() bool
type LayoutResult
type LayoutResultItem
type LineEdit
func NewLineEdit(parent Container) (*LineEdit, error)
func (le *LineEdit) CaseMode() CaseMode
func (le *LineEdit) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (le *LineEdit) CueBanner() string
func (le *LineEdit) EditingFinished() *Event
func (le *LineEdit) MaxLength() int
func (*LineEdit) NeedsWmSize() bool
func (le *LineEdit) PasswordMode() bool
func (le *LineEdit) ReadOnly() bool
func (le *LineEdit) SetCaseMode(mode CaseMode) error
func (le *LineEdit) SetCueBanner(value string) error
func (le *LineEdit) SetMaxLength(value int)
func (le *LineEdit) SetPasswordMode(value bool)
func (le *LineEdit) SetReadOnly(readOnly bool) error
func (le *LineEdit) SetText(value string) error
func (le *LineEdit) SetTextAlignment(alignment Alignment1D) error
func (le *LineEdit) SetTextColor(c Color)
func (le *LineEdit) SetTextSelection(start, end int)
func (le *LineEdit) Text() string
func (le *LineEdit) TextAlignment() Alignment1D
func (le *LineEdit) TextChanged() *Event
func (le *LineEdit) TextColor() Color
func (le *LineEdit) TextSelection() (start, end int)
func (le *LineEdit) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type LinkLabel
func NewLinkLabel(parent Container) (*LinkLabel, error)
func (ll *LinkLabel) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (ll *LinkLabel) LinkActivated() *LinkLabelLinkEvent
func (ll *LinkLabel) SetText(value string) error
func (ll *LinkLabel) Text() string
func (ll *LinkLabel) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type LinkLabelLink
func (lll *LinkLabelLink) Enabled() (bool, error)
func (lll *LinkLabelLink) Focused() (bool, error)
func (lll *LinkLabelLink) Id() string
func (lll *LinkLabelLink) Index() int
func (lll *LinkLabelLink) SetEnabled(enabled bool) error
func (lll *LinkLabelLink) SetFocused(focused bool) error
func (lll *LinkLabelLink) SetVisited(visited bool) error
func (lll *LinkLabelLink) URL() string
func (lll *LinkLabelLink) Visited() (bool, error)
type LinkLabelLinkEvent
func (e *LinkLabelLinkEvent) Attach(handler LinkLabelLinkEventHandler) int
func (e *LinkLabelLinkEvent) Detach(handle int)
type LinkLabelLinkEventHandler
type LinkLabelLinkEventPublisher
func (p *LinkLabelLinkEventPublisher) Event() *LinkLabelLinkEvent
func (p *LinkLabelLinkEventPublisher) Publish(link *LinkLabelLink)
type ListBox
func NewListBox(parent Container) (*ListBox, error)
func NewListBoxWithStyle(parent Container, style uint32) (*ListBox, error)
func (lb *ListBox) ApplyDPI(dpi int)
func (lb *ListBox) ApplySysColors()
func (lb *ListBox) BindingMember() string
func (lb *ListBox) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (lb *ListBox) CurrentIndex() int
func (lb *ListBox) CurrentIndexChanged() *Event
func (lb *ListBox) DisplayMember() string
func (lb *ListBox) EnsureItemVisible(index int)
func (lb *ListBox) Format() string
func (lb *ListBox) ItemActivated() *Event
func (lb *ListBox) ItemStyler() ListItemStyler
func (lb *ListBox) ItemVisible(index int) bool
func (*ListBox) LayoutFlags() LayoutFlags
func (lb *ListBox) Model() interface{}
func (lb *ListBox) Precision() int
func (lb *ListBox) SelectedIndexes() []int
func (lb *ListBox) SelectedIndexesChanged() *Event
func (lb *ListBox) SetBindingMember(bindingMember string) error
func (lb *ListBox) SetCurrentIndex(value int) error
func (lb *ListBox) SetDisplayMember(displayMember string) error
func (lb *ListBox) SetFormat(value string)
func (lb *ListBox) SetItemStyler(styler ListItemStyler)
func (lb *ListBox) SetModel(mdl interface{}) error
func (lb *ListBox) SetPrecision(value int)
func (lb *ListBox) SetSelectedIndexes(indexes []int)
func (lb *ListBox) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type ListItemStyle
func (lis *ListItemStyle) Bounds() Rectangle
func (lis *ListItemStyle) BoundsPixels() Rectangle
func (lis *ListItemStyle) Canvas() *Canvas
func (lis *ListItemStyle) DrawBackground() error
func (lis *ListItemStyle) DrawText(text string, bounds Rectangle, format DrawTextFormat) error
func (lis *ListItemStyle) Index() int
type ListItemStyler
type ListModel
type ListModelBase
func (lmb *ListModelBase) ItemChanged() *IntEvent
func (lmb *ListModelBase) ItemsInserted() *IntRangeEvent
func (lmb *ListModelBase) ItemsRemoved() *IntRangeEvent
func (lmb *ListModelBase) ItemsReset() *Event
func (lmb *ListModelBase) PublishItemChanged(index int)
func (lmb *ListModelBase) PublishItemsInserted(from, to int)
func (lmb *ListModelBase) PublishItemsRemoved(from, to int)
func (lmb *ListModelBase) PublishItemsReset()
type MainWindow
func NewMainWindow() (*MainWindow, error)
func NewMainWindowWithCfg(cfg *MainWindowCfg) (*MainWindow, error)
func NewMainWindowWithName(name string) (*MainWindow, error)
func (mw *MainWindow) ClientBoundsPixels() Rectangle
func (mw *MainWindow) Fullscreen() bool
func (mw *MainWindow) Menu() *Menu
func (mw *MainWindow) SetFullscreen(fullscreen bool) error
func (mw *MainWindow) SetToolBar(tb *ToolBar)
func (mw *MainWindow) SetVisible(visible bool)
func (mw *MainWindow) StatusBar() *StatusBar
func (mw *MainWindow) ToolBar() *ToolBar
func (mw *MainWindow) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type MainWindowCfg
type Margins
func MarginsFrom96DPI(value Margins, dpi int) Margins
func MarginsTo96DPI(value Margins, dpi int) Margins
type Menu
func NewMenu() (*Menu, error)
func (m *Menu) Actions() *ActionList
func (m *Menu) Dispose()
func (m *Menu) IsDisposed() bool
type Metafile
func NewMetafile(referenceCanvas *Canvas) (*Metafile, error)
func NewMetafileFromFile(filePath string) (*Metafile, error)
func (mf *Metafile) Dispose()
func (mf *Metafile) Save(filePath string) error
func (mf *Metafile) Size() Size
type MinSizeForSizer
type MinSizer
type Modifiers
func ModifiersDown() Modifiers
func (m Modifiers) String() string
type MouseButton
type MouseEvent
func (e *MouseEvent) Attach(handler MouseEventHandler) int
func (e *MouseEvent) Detach(handle int)
func (e *MouseEvent) Once(handler MouseEventHandler)
type MouseEventHandler
type MouseEventPublisher
func (p *MouseEventPublisher) Event() *MouseEvent
func (p *MouseEventPublisher) Publish(x, y int, button MouseButton)
type MsgBoxStyle
type MutableCondition
func NewMutableCondition() *MutableCondition
func (mc *MutableCondition) Changed() *Event
func (mc *MutableCondition) Satisfied() bool
func (mc *MutableCondition) SetSatisfied(satisfied bool) error
func (mc *MutableCondition) Value() interface{}
type NotifyIcon
func NewNotifyIcon(form Form) (*NotifyIcon, error)
func (ni *NotifyIcon) ContextMenu() *Menu
func (ni *NotifyIcon) DPI() int
func (ni *NotifyIcon) Dispose() error
func (ni *NotifyIcon) Icon() Image
func (ni *NotifyIcon) MessageClicked() *Event
func (ni *NotifyIcon) MouseDown() *MouseEvent
func (ni *NotifyIcon) MouseUp() *MouseEvent
func (ni *NotifyIcon) SetIcon(icon Image) error
func (ni *NotifyIcon) SetToolTip(toolTip string) error
func (ni *NotifyIcon) SetVisible(visible bool) error
func (ni *NotifyIcon) ShowCustom(title, info string, icon Image) error
func (ni *NotifyIcon) ShowError(title, info string) error
func (ni *NotifyIcon) ShowInfo(title, info string) error
func (ni *NotifyIcon) ShowMessage(title, info string) error
func (ni *NotifyIcon) ShowWarning(title, info string) error
func (ni *NotifyIcon) ToolTip() string
func (ni *NotifyIcon) Visible() bool
type NumberEdit
func NewNumberEdit(parent Container) (*NumberEdit, error)
func (ne *NumberEdit) Background() Brush
func (ne *NumberEdit) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (ne *NumberEdit) Decimals() int
func (ne *NumberEdit) Increment() float64
func (ne *NumberEdit) MaxValue() float64
func (ne *NumberEdit) MinValue() float64
func (*NumberEdit) NeedsWmSize() bool
func (ne *NumberEdit) Prefix() string
func (ne *NumberEdit) PrefixChanged() *Event
func (ne *NumberEdit) ReadOnly() bool
func (ne *NumberEdit) SetBackground(bg Brush)
func (ne *NumberEdit) SetDecimals(decimals int) error
func (ne *NumberEdit) SetFocus() error
func (ne *NumberEdit) SetIncrement(increment float64) error
func (ne *NumberEdit) SetPrefix(prefix string) error
func (ne *NumberEdit) SetRange(min, max float64) error
func (ne *NumberEdit) SetReadOnly(readOnly bool) error
func (ne *NumberEdit) SetSpinButtonsVisible(visible bool) error
func (ne *NumberEdit) SetSuffix(suffix string) error
func (ne *NumberEdit) SetTextColor(c Color)
func (ne *NumberEdit) SetTextSelection(start, end int)
func (ne *NumberEdit) SetToolTipText(s string) error
func (ne *NumberEdit) SetValue(value float64) error
func (ne *NumberEdit) SpinButtonsVisible() bool
func (ne *NumberEdit) Suffix() string
func (ne *NumberEdit) SuffixChanged() *Event
func (ne *NumberEdit) TextColor() Color
func (ne *NumberEdit) TextSelection() (start, end int)
func (ne *NumberEdit) Value() float64
func (ne *NumberEdit) ValueChanged() *Event
func (ne *NumberEdit) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type NumberLabel
func NewNumberLabel(parent Container) (*NumberLabel, error)
func (s *NumberLabel) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (nl *NumberLabel) Decimals() int
func (s *NumberLabel) Dispose()
func (nl *NumberLabel) SetDecimals(decimals int) error
func (nl *NumberLabel) SetSuffix(suffix string) error
func (nl *NumberLabel) SetTextAlignment(alignment Alignment1D) error
func (s *NumberLabel) SetTextColor(c Color)
func (nl *NumberLabel) SetValue(value float64) error
func (nl *NumberLabel) Suffix() string
func (nl *NumberLabel) TextAlignment() Alignment1D
func (s *NumberLabel) TextColor() Color
func (nl *NumberLabel) Value() float64
func (s *NumberLabel) WndProc(hwnd win.HWND, msg uint32, wp, lp uintptr) uintptr
type Orientation
type PIState
type PaintFunc
type PaintFuncImage
func NewPaintFuncImage(size Size, paint func(canvas *Canvas, bounds Rectangle) error) *PaintFuncImage
func NewPaintFuncImagePixels(size Size, paint func(canvas *Canvas, bounds Rectangle) error) *PaintFuncImage
func NewPaintFuncImagePixelsWithDispose(size Size, paint func(canvas *Canvas, bounds Rectangle) error, dispose func()) *PaintFuncImage
func NewPaintFuncImageWithDispose(size Size, paint func(canvas *Canvas, bounds Rectangle) error, dispose func()) *PaintFuncImage
func (pfi *PaintFuncImage) Dispose()
func (pfi *PaintFuncImage) Size() Size
type PaintMode
type Pen
func NullPen() Pen
type PenStyle
type Persistable
type Point
func PointFrom96DPI(value Point, dpi int) Point
func PointTo96DPI(value Point, dpi int) Point
type Populator
type ProgressBar
func NewProgressBar(parent Container) (*ProgressBar, error)
func (pb *ProgressBar) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (pb *ProgressBar) MarqueeMode() bool
func (pb *ProgressBar) MaxValue() int
func (pb *ProgressBar) MinValue() int
func (pb *ProgressBar) SetMarqueeMode(marqueeMode bool) error
func (pb *ProgressBar) SetRange(min, max int)
func (pb *ProgressBar) SetValue(value int)
func (pb *ProgressBar) Value() int
type ProgressIndicator
func (pi *ProgressIndicator) Completed() uint32
func (pi *ProgressIndicator) SetCompleted(completed uint32) error
func (pi *ProgressIndicator) SetOverlayIcon(icon *Icon, description string) error
func (pi *ProgressIndicator) SetState(state PIState) error
func (pi *ProgressIndicator) SetTotal(total uint32)
func (pi *ProgressIndicator) State() PIState
func (pi *ProgressIndicator) Total() uint32
type Property
func NewBoolProperty(get func() bool, set func(b bool) error, changed *Event) Property
func NewProperty(get func() interface{}, set func(v interface{}) error, changed *Event) Property
func NewReadOnlyBoolProperty(get func() bool, changed *Event) Property
func NewReadOnlyProperty(get func() interface{}, changed *Event) Property
type PushButton
func NewPushButton(parent Container) (*PushButton, error)
func (pb *PushButton) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (pb *PushButton) ImageAboveText() bool
func (pb *PushButton) SetImageAboveText(value bool) error
func (pb *PushButton) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type RadioButton
func NewRadioButton(parent Container) (*RadioButton, error)
func (rb *RadioButton) Group() *RadioButtonGroup
func (rb *RadioButton) SetTextOnLeftSide(textLeft bool) error
func (rb *RadioButton) SetValue(value interface{})
func (rb *RadioButton) TextOnLeftSide() bool
func (rb *RadioButton) Value() interface{}
func (rb *RadioButton) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type RadioButtonGroup
func (rbg *RadioButtonGroup) Buttons() []*RadioButton
func (rbg *RadioButtonGroup) CheckedButton() *RadioButton
type RangeValidator
func NewRangeValidator(min, max float64) (*RangeValidator, error)
func (rv *RangeValidator) Max() float64
func (rv *RangeValidator) Min() float64
func (rv *RangeValidator) Reset(min, max float64) error
func (rv *RangeValidator) Validate(v interface{}) error
type Rectangle
func RectangleFrom96DPI(value Rectangle, dpi int) Rectangle
func RectangleTo96DPI(value Rectangle, dpi int) Rectangle
func (r Rectangle) Bottom() int
func (r Rectangle) IsZero() bool
func (r Rectangle) Left() int
func (r Rectangle) Location() Point
func (r Rectangle) Right() int
func (r *Rectangle) SetLocation(p Point) Rectangle
func (r *Rectangle) SetSize(s Size) Rectangle
func (r Rectangle) Size() Size
func (r Rectangle) Top() int
type ReflectListModel
type ReflectListModelBase
func (rlmb *ReflectListModelBase) Value(index int) interface{}
type ReflectTableModel
type ReflectTableModelBase
func (rtmb *ReflectTableModelBase) Value(row, col int) interface{}
type RegexpValidator
func NewRegexpValidator(pattern string) (*RegexpValidator, error)
func (rv *RegexpValidator) Pattern() string
func (rv *RegexpValidator) Validate(v interface{}) error
type RegistryKey
func ClassesRootKey() *RegistryKey
func CurrentUserKey() *RegistryKey
func LocalMachineKey() *RegistryKey
type ResourceManager
func (rm *ResourceManager) Bitmap(name string) (*Bitmap, error)deprecated
func (rm *ResourceManager) BitmapForDPI(name string, dpi int) (*Bitmap, error)
func (rm *ResourceManager) Icon(name string) (*Icon, error)
func (rm *ResourceManager) Image(name string) (Image, error)
func (rm *ResourceManager) RootDirPath() string
func (rm *ResourceManager) SetRootDirPath(rootDirPath string) error
type ScrollView
func NewScrollView(parent Container) (*ScrollView, error)
func (sv *ScrollView) ApplyDPI(dpi int)
func (sv *ScrollView) AsContainerBase() *ContainerBase
func (sv *ScrollView) Children() *WidgetList
func (sv *ScrollView) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (sv *ScrollView) DataBinder() *DataBinder
func (sv *ScrollView) Layout() Layout
func (sv *ScrollView) MouseDown() *MouseEvent
func (sv *ScrollView) MouseMove() *MouseEvent
func (sv *ScrollView) MouseUp() *MouseEvent
func (sv *ScrollView) Name() string
func (sv *ScrollView) Persistent() bool
func (sv *ScrollView) RestoreState() error
func (sv *ScrollView) SaveState() error
func (sv *ScrollView) Scrollbars() (horizontal, vertical bool)
func (sv *ScrollView) SetDataBinder(dataBinder *DataBinder)
func (sv *ScrollView) SetLayout(value Layout) error
func (sv *ScrollView) SetName(name string)
func (sv *ScrollView) SetPersistent(value bool)
func (sv *ScrollView) SetScrollbars(horizontal, vertical bool)
func (sv *ScrollView) SetSuspended(suspend bool)
func (sv *ScrollView) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type Separator
func NewHSeparator(parent Container) (*Separator, error)
func NewVSeparator(parent Container) (*Separator, error)
func (s *Separator) CreateLayoutItem(ctx *LayoutContext) LayoutItem
type Settings
type Shortcut
func (s Shortcut) String() string
type Size
func SizeFrom96DPI(value Size, dpi int) Size
func SizeTo96DPI(value Size, dpi int) Size
func (s Size) IsZero() bool
type Slider
func NewSlider(parent Container) (*Slider, error)
func NewSliderWithCfg(parent Container, cfg *SliderCfg) (*Slider, error)
func NewSliderWithOrientation(parent Container, orientation Orientation) (*Slider, error)
func (sl *Slider) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (sl *Slider) LineSize() int
func (sl *Slider) MaxValue() int
func (sl *Slider) MinValue() int
func (*Slider) NeedsWmSize() bool
func (sl *Slider) PageSize() int
func (sl *Slider) Persistent() bool
func (sl *Slider) RestoreState() error
func (sl *Slider) SaveState() error
func (sl *Slider) SetLineSize(lineSize int)
func (sl *Slider) SetPageSize(pageSize int)
func (sl *Slider) SetPersistent(value bool)
func (sl *Slider) SetRange(min, max int)
func (sl *Slider) SetTracking(tracking bool)
func (sl *Slider) SetValue(value int)
func (sl *Slider) Tracking() bool
func (sl *Slider) Value() int
func (sl *Slider) ValueChanged() *Event
func (sl *Slider) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type SliderCfg
type SolidColorBrush
func NewSolidColorBrush(color Color) (*SolidColorBrush, error)
func (b *SolidColorBrush) Color() Color
func (bb *SolidColorBrush) Dispose()
type SortOrder
type SortedReflectTableModelBase
func (srtmb *SortedReflectTableModelBase) Sort(col int, order SortOrder) error
type Sorter
type SorterBase
func (sb *SorterBase) ColumnSortable(col int) bool
func (sb *SorterBase) Sort(col int, order SortOrder) error
func (sb *SorterBase) SortChanged() *Event
func (sb *SorterBase) SortOrder() SortOrder
func (sb *SorterBase) SortedColumn() int
type Spacer
func NewHSpacer(parent Container) (*Spacer, error)
func NewHSpacerFixed(parent Container, width int) (*Spacer, error)
func NewSpacerWithCfg(parent Container, cfg *SpacerCfg) (*Spacer, error)
func NewVSpacer(parent Container) (*Spacer, error)
func NewVSpacerFixed(parent Container, height int) (*Spacer, error)
func (s *Spacer) CreateLayoutItem(ctx *LayoutContext) LayoutItem
type SpacerCfg
type SplitButton
func NewSplitButton(parent Container) (*SplitButton, error)
func (sb *SplitButton) Dispose()
func (sb *SplitButton) ImageAboveText() bool
func (sb *SplitButton) Menu() *Menu
func (sb *SplitButton) SetImageAboveText(value bool) error
func (sb *SplitButton) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type Splitter
func NewHSplitter(parent Container) (*Splitter, error)
func NewVSplitter(parent Container) (*Splitter, error)
func (s *Splitter) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (s *Splitter) Fixed(widget Widget) bool
func (s *Splitter) HandleWidth() int
func (s *Splitter) Orientation() Orientation
func (s *Splitter) Persistent() bool
func (s *Splitter) RestoreState() error
func (s *Splitter) SaveState() error
func (s *Splitter) SetFixed(widget Widget, fixed bool) error
func (s *Splitter) SetHandleWidth(value int) error
func (s *Splitter) SetLayout(value Layout) error
func (s *Splitter) SetPersistent(value bool)
func (s *Splitter) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type StatusBar
func NewStatusBar(parent Container) (*StatusBar, error)
func (sb *StatusBar) ApplyDPI(dpi int)
func (*StatusBar) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (sb *StatusBar) Items() *StatusBarItemList
func (sb *StatusBar) SetVisible(visible bool)
func (sb *StatusBar) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type StatusBarItem
func NewStatusBarItem() *StatusBarItem
func (sbi *StatusBarItem) Clicked() *Event
func (sbi *StatusBarItem) Icon() *Icon
func (sbi *StatusBarItem) SetIcon(icon *Icon) error
func (sbi *StatusBarItem) SetText(text string) error
func (sbi *StatusBarItem) SetToolTipText(toolTipText string) error
func (sbi *StatusBarItem) SetWidth(width int) error
func (sbi *StatusBarItem) Text() string
func (sbi *StatusBarItem) ToolTipText() string
func (sbi *StatusBarItem) Width() int
type StatusBarItemList
func (l *StatusBarItemList) Add(item *StatusBarItem) error
func (l *StatusBarItemList) At(index int) *StatusBarItem
func (l *StatusBarItemList) Clear() error
func (l *StatusBarItemList) Contains(item *StatusBarItem) bool
func (l *StatusBarItemList) Index(item *StatusBarItem) int
func (l *StatusBarItemList) Insert(index int, item *StatusBarItem) error
func (l *StatusBarItemList) Len() int
func (l *StatusBarItemList) Remove(item *StatusBarItem) error
func (l *StatusBarItemList) RemoveAt(index int) error
type StringEvent
func (e *StringEvent) Attach(handler StringEventHandler) int
func (e *StringEvent) Detach(handle int)
func (e *StringEvent) Once(handler StringEventHandler)
type StringEventHandler
type StringEventPublisher
func (p *StringEventPublisher) Event() *StringEvent
func (p *StringEventPublisher) Publish(s string)
type SystemColor
type SystemColorBrush
func NewSystemColorBrush(sysColor SystemColor) (*SystemColorBrush, error)
func (b *SystemColorBrush) Color() Color
func (*SystemColorBrush) Dispose()
func (b *SystemColorBrush) SystemColor() SystemColor
type TabPage
func NewTabPage() (*TabPage, error)
func (tp *TabPage) Background() Brush
func (tp *TabPage) Enabled() bool
func (tp *TabPage) Font() *Font
func (tp *TabPage) Image() Image
func (tp *TabPage) SetImage(value Image) error
func (tp *TabPage) SetTitle(value string) error
func (tp *TabPage) Title() string
type TabPageList
func (l *TabPageList) Add(item *TabPage) error
func (l *TabPageList) At(index int) *TabPage
func (l *TabPageList) Clear() error
func (l *TabPageList) Contains(item *TabPage) bool
func (l *TabPageList) Index(item *TabPage) int
func (l *TabPageList) Insert(index int, item *TabPage) error
func (l *TabPageList) Len() int
func (l *TabPageList) Remove(item *TabPage) error
func (l *TabPageList) RemoveAt(index int) error
type TabWidget
func NewTabWidget(parent Container) (*TabWidget, error)
func (tw *TabWidget) ApplyDPI(dpi int)
func (tw *TabWidget) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (tw *TabWidget) CurrentIndex() int
func (tw *TabWidget) CurrentIndexChanged() *Event
func (tw *TabWidget) Dispose()
func (tw *TabWidget) Pages() *TabPageList
func (tw *TabWidget) Persistent() bool
func (tw *TabWidget) RestoreState() error
func (tw *TabWidget) SaveState() error
func (tw *TabWidget) SetCurrentIndex(index int) error
func (tw *TabWidget) SetPersistent(value bool)
func (tw *TabWidget) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type TableModel
type TableModelBase
func (tmb *TableModelBase) PublishRowChanged(row int)
func (tmb *TableModelBase) PublishRowsChanged(from, to int)
func (tmb *TableModelBase) PublishRowsInserted(from, to int)
func (tmb *TableModelBase) PublishRowsRemoved(from, to int)
func (tmb *TableModelBase) PublishRowsReset()
func (tmb *TableModelBase) RowChanged() *IntEvent
func (tmb *TableModelBase) RowsChanged() *IntRangeEvent
func (tmb *TableModelBase) RowsInserted() *IntRangeEvent
func (tmb *TableModelBase) RowsRemoved() *IntRangeEvent
func (tmb *TableModelBase) RowsReset() *Event
type TableView
func NewTableView(parent Container) (*TableView, error)
func NewTableViewWithCfg(parent Container, cfg *TableViewCfg) (*TableView, error)
func NewTableViewWithStyle(parent Container, style uint32) (*TableView, error)
func (tv *TableView) AlternatingRowBG() bool
func (tv *TableView) ApplyDPI(dpi int)
func (tv *TableView) ApplySysColors()
func (tv *TableView) CellStyler() CellStyler
func (tv *TableView) CheckBoxes() bool
func (tv *TableView) ColumnClicked() *IntEvent
func (tv *TableView) Columns() *TableViewColumnList
func (tv *TableView) ColumnsOrderable() bool
func (tv *TableView) ColumnsSizable() bool
func (tv *TableView) ContextMenuLocation() Point
func (*TableView) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (tv *TableView) CurrentIndex() int
func (tv *TableView) CurrentIndexChanged() *Event
func (tv *TableView) CurrentItemChanged() *Event
func (tv *TableView) Dispose()
func (tv *TableView) EnsureItemVisible(index int)
func (tv *TableView) Focused() bool
func (tv *TableView) Gridlines() bool
func (tv *TableView) HeaderHidden() bool
func (tv *TableView) IgnoreNowhere() bool
func (tv *TableView) IndexAt(x, y int) int
func (tv *TableView) Invalidate() error
func (tv *TableView) ItemActivated() *Event
func (tv *TableView) ItemChecker() ItemChecker
func (tv *TableView) ItemCountChanged() *Event
func (tv *TableView) ItemStateChangedEventDelay() int
func (tv *TableView) ItemVisible(index int) bool
func (tv *TableView) LastColumnStretched() bool
func (tv *TableView) Model() interface{}
func (tv *TableView) MultiSelection() bool
func (tv *TableView) Persistent() bool
func (tv *TableView) RestoreState() error
func (tv *TableView) RestoringCurrentItemOnReset() bool
func (tv *TableView) RowsPerPage() int
func (tv *TableView) SaveState() error
func (tv *TableView) ScrollbarOrientation() Orientation
func (tv *TableView) SelectedIndexes() []int
func (tv *TableView) SelectedIndexesChanged() *Event
func (tv *TableView) SelectionHiddenWithoutFocus() bool
func (tv *TableView) SetAlternatingRowBG(enabled bool)
func (tv *TableView) SetCellStyler(styler CellStyler)
func (tv *TableView) SetCheckBoxes(checkBoxes bool)
func (tv *TableView) SetColumnsOrderable(enabled bool)
func (tv *TableView) SetColumnsSizable(b bool) error
func (tv *TableView) SetCurrentIndex(index int) error
func (tv *TableView) SetGridlines(enabled bool)
func (tv *TableView) SetHeaderHidden(hidden bool) error
func (tv *TableView) SetIgnoreNowhere(value bool)
func (tv *TableView) SetItemChecker(itemChecker ItemChecker)
func (tv *TableView) SetItemStateChangedEventDelay(delay int)
func (tv *TableView) SetLastColumnStretched(value bool) error
func (tv *TableView) SetModel(mdl interface{}) error
func (tv *TableView) SetMultiSelection(multiSel bool) error
func (tv *TableView) SetPersistent(value bool)
func (tv *TableView) SetRestoringCurrentItemOnReset(restoring bool)
func (tv *TableView) SetScrollbarOrientation(orientation Orientation)
func (tv *TableView) SetSelectedIndexes(indexes []int) error
func (tv *TableView) SetSelectionHiddenWithoutFocus(hidden bool) error
func (tv *TableView) SortableByHeaderClick() bool
func (tv *TableView) StretchLastColumn() error
func (tv *TableView) TableModel() TableModel
func (tv *TableView) UpdateItem(index int) error
func (tv *TableView) VisibleColumnsInDisplayOrder() []*TableViewColumn
func (tv *TableView) WndProc(hwnd win.HWND, msg uint32, wp, lp uintptr) uintptr
type TableViewCfg
type TableViewColumn
func NewTableViewColumn() *TableViewColumn
func (tvc *TableViewColumn) Alignment() Alignment1D
func (tvc *TableViewColumn) DataMember() string
func (tvc *TableViewColumn) DataMemberEffective() string
func (tvc *TableViewColumn) Format() string
func (tvc *TableViewColumn) FormatFunc() func(value interface{}) string
func (tvc *TableViewColumn) Frozen() bool
func (tvc *TableViewColumn) LessFunc() func(i, j int) bool
func (tvc *TableViewColumn) Name() string
func (tvc *TableViewColumn) Precision() int
func (tvc *TableViewColumn) SetAlignment(alignment Alignment1D) (err error)
func (tvc *TableViewColumn) SetDataMember(dataMember string)
func (tvc *TableViewColumn) SetFormat(format string) (err error)
func (tvc *TableViewColumn) SetFormatFunc(formatFunc func(value interface{}) string)
func (tvc *TableViewColumn) SetFrozen(frozen bool) (err error)
func (tvc *TableViewColumn) SetLessFunc(lessFunc func(i, j int) bool)
func (tvc *TableViewColumn) SetName(name string)
func (tvc *TableViewColumn) SetPrecision(precision int) (err error)
func (tvc *TableViewColumn) SetTitle(title string) (err error)
func (tvc *TableViewColumn) SetTitleOverride(titleOverride string) (err error)
func (tvc *TableViewColumn) SetVisible(visible bool) (err error)
func (tvc *TableViewColumn) SetWidth(width int) (err error)
func (tvc *TableViewColumn) Title() string
func (tvc *TableViewColumn) TitleEffective() string
func (tvc *TableViewColumn) TitleOverride() string
func (tvc *TableViewColumn) Visible() bool
func (tvc *TableViewColumn) Width() int
type TableViewColumnList
func (l *TableViewColumnList) Add(item *TableViewColumn) error
func (l *TableViewColumnList) At(index int) *TableViewColumn
func (l *TableViewColumnList) ByName(name string) *TableViewColumn
func (l *TableViewColumnList) Clear() error
func (l *TableViewColumnList) Contains(item *TableViewColumn) bool
func (l *TableViewColumnList) Index(item *TableViewColumn) int
func (l *TableViewColumnList) Insert(index int, item *TableViewColumn) error
func (l *TableViewColumnList) Len() int
func (l *TableViewColumnList) Remove(item *TableViewColumn) error
func (l *TableViewColumnList) RemoveAt(index int) error
type TextEdit
func NewTextEdit(parent Container) (*TextEdit, error)
func NewTextEditWithStyle(parent Container, style uint32) (*TextEdit, error)
func (te *TextEdit) AppendText(value string)
func (te *TextEdit) CompactHeight() bool
func (te *TextEdit) ContextMenuLocation() Point
func (te *TextEdit) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (te *TextEdit) MaxLength() int
func (*TextEdit) NeedsWmSize() bool
func (te *TextEdit) ReadOnly() bool
func (te *TextEdit) ReplaceSelectedText(text string, canUndo bool)
func (te *TextEdit) ScrollToCaret()
func (te *TextEdit) SetCompactHeight(enabled bool)
func (te *TextEdit) SetMaxLength(value int)
func (te *TextEdit) SetReadOnly(readOnly bool) error
func (te *TextEdit) SetText(text string) (err error)
func (te *TextEdit) SetTextAlignment(alignment Alignment1D) error
func (te *TextEdit) SetTextColor(c Color)
func (te *TextEdit) SetTextSelection(start, end int)
func (te *TextEdit) Text() string
func (te *TextEdit) TextAlignment() Alignment1D
func (te *TextEdit) TextChanged() *Event
func (te *TextEdit) TextColor() Color
func (te *TextEdit) TextLength() int
func (te *TextEdit) TextSelection() (start, end int)
func (te *TextEdit) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type TextLabel
func NewTextLabel(parent Container) (*TextLabel, error)
func NewTextLabelWithStyle(parent Container, style uint32) (*TextLabel, error)
func (tl *TextLabel) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (s *TextLabel) Dispose()
func (tl *TextLabel) SetText(text string) error
func (tl *TextLabel) SetTextAlignment(alignment Alignment2D) error
func (s *TextLabel) SetTextColor(c Color)
func (tl *TextLabel) Text() string
func (tl *TextLabel) TextAlignment() Alignment2D
func (s *TextLabel) TextColor() Color
func (s *TextLabel) WndProc(hwnd win.HWND, msg uint32, wp, lp uintptr) uintptr
type ToolBar
func NewToolBar(parent Container) (*ToolBar, error)
func NewToolBarWithOrientationAndButtonStyle(parent Container, orientation Orientation, buttonStyle ToolBarButtonStyle) (*ToolBar, error)
func NewVerticalToolBar(parent Container) (*ToolBar, error)
func (tb *ToolBar) Actions() *ActionList
func (tb *ToolBar) ApplyDPI(dpi int)
func (tb *ToolBar) ButtonStyle() ToolBarButtonStyle
func (tb *ToolBar) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (tb *ToolBar) DefaultButtonWidth() int
func (tb *ToolBar) Dispose()
func (tb *ToolBar) ImageList() *ImageList
func (tb *ToolBar) MaxTextRows() int
func (tb *ToolBar) Orientation() Orientation
func (tb *ToolBar) SetDefaultButtonWidth(width int) error
func (tb *ToolBar) SetImageList(value *ImageList)
func (tb *ToolBar) SetMaxTextRows(maxTextRows int) error
func (tb *ToolBar) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type ToolBarButtonStyle
type ToolButton
func NewToolButton(parent Container) (*ToolButton, error)
func (tb *ToolButton) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (tb *ToolButton) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type ToolTip
func NewToolTip() (*ToolTip, error)
func (tt *ToolTip) AddTool(tool Widget) error
func (tt *ToolTip) RemoveTool(tool Widget) error
func (tt *ToolTip) SetErrorTitle(title string) error
func (tt *ToolTip) SetInfoTitle(title string) error
func (tt *ToolTip) SetText(tool Widget, text string) error
func (tt *ToolTip) SetTitle(title string) error
func (tt *ToolTip) SetWarningTitle(title string) error
func (tt *ToolTip) Text(tool Widget) string
func (tt *ToolTip) Title() string
type ToolTipErrorPresenter
func NewToolTipErrorPresenter() (*ToolTipErrorPresenter, error)
func (ttep *ToolTipErrorPresenter) Dispose()
func (ttep *ToolTipErrorPresenter) PresentError(err error, widget Widget)
type TranslationFunction
func TranslationFunc() TranslationFunction
type TreeItem
type TreeItemEvent
func (e *TreeItemEvent) Attach(handler TreeItemEventHandler) int
func (e *TreeItemEvent) Detach(handle int)
func (e *TreeItemEvent) Once(handler TreeItemEventHandler)
type TreeItemEventHandler
type TreeItemEventPublisher
func (p *TreeItemEventPublisher) Event() *TreeItemEvent
func (p *TreeItemEventPublisher) Publish(item TreeItem)
type TreeModel
type TreeModelBase
func (tmb *TreeModelBase) ItemChanged() *TreeItemEvent
func (tmb *TreeModelBase) ItemInserted() *TreeItemEvent
func (tmb *TreeModelBase) ItemRemoved() *TreeItemEvent
func (tmb *TreeModelBase) ItemsReset() *TreeItemEvent
func (tmb *TreeModelBase) LazyPopulation() bool
func (tmb *TreeModelBase) PublishItemChanged(item TreeItem)
func (tmb *TreeModelBase) PublishItemInserted(item TreeItem)
func (tmb *TreeModelBase) PublishItemRemoved(item TreeItem)
func (tmb *TreeModelBase) PublishItemsReset(parent TreeItem)
type TreeView
func NewTreeView(parent Container) (*TreeView, error)
func (tv *TreeView) ApplyDPI(dpi int)
func (tv *TreeView) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (tv *TreeView) CurrentItem() TreeItem
func (tv *TreeView) CurrentItemChanged() *Event
func (tv *TreeView) Dispose()
func (tv *TreeView) EnsureVisible(item TreeItem) error
func (tv *TreeView) Expanded(item TreeItem) bool
func (tv *TreeView) ExpandedChanged() *TreeItemEvent
func (tv *TreeView) ItemActivated() *Event
func (tv *TreeView) ItemAt(x, y int) TreeItem
func (tv *TreeView) ItemHeight() int
func (tv *TreeView) Model() TreeModel
func (*TreeView) NeedsWmSize() bool
func (tv *TreeView) SetBackground(bg Brush)
func (tv *TreeView) SetCurrentItem(item TreeItem) error
func (tv *TreeView) SetExpanded(item TreeItem, expanded bool) error
func (tv *TreeView) SetItemHeight(height int)
func (tv *TreeView) SetModel(model TreeModel) error
func (tv *TreeView) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type ValidationError
func NewValidationError(title, message string) *ValidationError
func (ve *ValidationError) Error() string
func (ve *ValidationError) Message() string
func (ve *ValidationError) Title() string
type Validator
func SelectionRequiredValidator() Validator
type WebView
func NewWebView(parent Container) (*WebView, error)
func (wv *WebView) BrowserVisible() bool
func (wv *WebView) BrowserVisibleChanged() *Event
func (wv *WebView) CanGoBack() bool
func (wv *WebView) CanGoBackChanged() *Event
func (wv *WebView) CanGoForward() bool
func (wv *WebView) CanGoForwardChanged() *Event
func (wv *WebView) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (wv *WebView) Dispose()
func (wv *WebView) DocumentCompleted() *StringEvent
func (wv *WebView) DocumentTitle() string
func (wv *WebView) DocumentTitleChanged() *Event
func (wv *WebView) Downloaded() *Event
func (wv *WebView) Downloading() *Event
func (wv *WebView) IsTheaterMode() bool
func (wv *WebView) NativeContextMenuEnabled() bool
func (wv *WebView) NativeContextMenuEnabledChanged() *Event
func (wv *WebView) Navigated() *StringEvent
func (wv *WebView) NavigatedError() *WebViewNavigatedErrorEvent
func (wv *WebView) Navigating() *WebViewNavigatingEvent
func (wv *WebView) NewWindow() *WebViewNewWindowEvent
func (wv *WebView) ProgressChanged() *Event
func (wv *WebView) ProgressMax() int32
func (wv *WebView) ProgressValue() int32
func (wv *WebView) Quitting() *Event
func (wv *WebView) Refresh() error
func (wv *WebView) SetNativeContextMenuEnabled(value bool)
func (wv *WebView) SetShortcutsEnabled(value bool)
func (wv *WebView) SetURL(url string) error
func (wv *WebView) ShortcutsEnabled() bool
func (wv *WebView) ShortcutsEnabledChanged() *Event
func (wv *WebView) StatusBarVisible() bool
func (wv *WebView) StatusBarVisibleChanged() *Event
func (wv *WebView) StatusText() string
func (wv *WebView) StatusTextChanged() *Event
func (wv *WebView) TheaterModeChanged() *Event
func (wv *WebView) ToolBarEnabled() bool
func (wv *WebView) ToolBarEnabledChanged() *Event
func (wv *WebView) ToolBarVisible() bool
func (wv *WebView) ToolBarVisibleChanged() *Event
func (wv *WebView) URL() (url string, err error)
func (wv *WebView) URLChanged() *Event
func (wv *WebView) WindowClosing() *WebViewWindowClosingEvent
func (wv *WebView) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type WebViewNavigatedErrorEvent
func (e *WebViewNavigatedErrorEvent) Attach(handler WebViewNavigatedErrorEventHandler) int
func (e *WebViewNavigatedErrorEvent) Detach(handle int)
type WebViewNavigatedErrorEventData
func (eventData *WebViewNavigatedErrorEventData) Canceled() bool
func (eventData *WebViewNavigatedErrorEventData) SetCanceled(value bool)
func (eventData *WebViewNavigatedErrorEventData) StatusCode() int32
func (eventData *WebViewNavigatedErrorEventData) TargetFrameName() string
func (eventData *WebViewNavigatedErrorEventData) Url() string
type WebViewNavigatedErrorEventHandler
type WebViewNavigatedErrorEventPublisher
func (p *WebViewNavigatedErrorEventPublisher) Event() *WebViewNavigatedErrorEvent
func (p *WebViewNavigatedErrorEventPublisher) Publish(eventData *WebViewNavigatedErrorEventData)
type WebViewNavigatingEvent
func (e *WebViewNavigatingEvent) Attach(handler WebViewNavigatingEventHandler) int
func (e *WebViewNavigatingEvent) Detach(handle int)
type WebViewNavigatingEventData
func (eventData *WebViewNavigatingEventData) Canceled() bool
func (eventData *WebViewNavigatingEventData) Flags() int32
func (eventData *WebViewNavigatingEventData) Headers() string
func (eventData *WebViewNavigatingEventData) PostData() string
func (eventData *WebViewNavigatingEventData) SetCanceled(value bool)
func (eventData *WebViewNavigatingEventData) TargetFrameName() string
func (eventData *WebViewNavigatingEventData) Url() string
type WebViewNavigatingEventHandler
type WebViewNavigatingEventPublisher
func (p *WebViewNavigatingEventPublisher) Event() *WebViewNavigatingEvent
func (p *WebViewNavigatingEventPublisher) Publish(eventData *WebViewNavigatingEventData)
type WebViewNewWindowEvent
func (e *WebViewNewWindowEvent) Attach(handler WebViewNewWindowEventHandler) int
func (e *WebViewNewWindowEvent) Detach(handle int)
type WebViewNewWindowEventData
func (eventData *WebViewNewWindowEventData) Canceled() bool
func (eventData *WebViewNewWindowEventData) Flags() uint32
func (eventData *WebViewNewWindowEventData) SetCanceled(value bool)
func (eventData *WebViewNewWindowEventData) Url() string
func (eventData *WebViewNewWindowEventData) UrlContext() string
type WebViewNewWindowEventHandler
type WebViewNewWindowEventPublisher
func (p *WebViewNewWindowEventPublisher) Event() *WebViewNewWindowEvent
func (p *WebViewNewWindowEventPublisher) Publish(eventData *WebViewNewWindowEventData)
type WebViewWindowClosingEvent
func (e *WebViewWindowClosingEvent) Attach(handler WebViewWindowClosingEventHandler) int
func (e *WebViewWindowClosingEvent) Detach(handle int)
type WebViewWindowClosingEventData
func (eventData *WebViewWindowClosingEventData) Canceled() bool
func (eventData *WebViewWindowClosingEventData) IsChildWindow() bool
func (eventData *WebViewWindowClosingEventData) SetCanceled(value bool)
type WebViewWindowClosingEventHandler
type WebViewWindowClosingEventPublisher
func (p *WebViewWindowClosingEventPublisher) Event() *WebViewWindowClosingEvent
func (p *WebViewWindowClosingEventPublisher) Publish(eventData *WebViewWindowClosingEventData)
type Widget
func DescendantByName(container Container, name string) Widget
type WidgetBase
func (wb *WidgetBase) Alignment() Alignment2D
func (wb *WidgetBase) AlwaysConsumeSpace() bool
func (wb *WidgetBase) AsWidgetBase() *WidgetBase
func (wb *WidgetBase) Bounds() Rectangle
func (wb *WidgetBase) BoundsPixels() Rectangle
func (wb *WidgetBase) BringToTop() error
func (wb *WidgetBase) Dispose()
func (wb *WidgetBase) Enabled() bool
func (wb *WidgetBase) Font() *Font
func (wb *WidgetBase) ForEachAncestor(f func(window Window) bool)
func (wb *WidgetBase) GraphicsEffects() *WidgetGraphicsEffectList
func (wb *WidgetBase) LayoutFlags() LayoutFlags
func (wb *WidgetBase) MinSizeHint() Size
func (wb *WidgetBase) Parent() Container
func (wb *WidgetBase) SetAlignment(alignment Alignment2D) error
func (wb *WidgetBase) SetAlwaysConsumeSpace(b bool) error
func (wb *WidgetBase) SetMinMaxSize(min, max Size) (err error)
func (wb *WidgetBase) SetParent(parent Container) (err error)
func (wb *WidgetBase) SetToolTipText(s string) error
func (wb *WidgetBase) SizeHint() Size
func (wb *WidgetBase) ToolTipText() string
type WidgetGraphicsEffect
type WidgetGraphicsEffectList
func (l *WidgetGraphicsEffectList) Add(effect WidgetGraphicsEffect) error
func (l *WidgetGraphicsEffectList) At(index int) WidgetGraphicsEffect
func (l *WidgetGraphicsEffectList) Clear() error
func (l *WidgetGraphicsEffectList) Contains(effect WidgetGraphicsEffect) bool
func (l *WidgetGraphicsEffectList) Index(effect WidgetGraphicsEffect) int
func (l *WidgetGraphicsEffectList) Insert(index int, effect WidgetGraphicsEffect) error
func (l *WidgetGraphicsEffectList) Len() int
func (l *WidgetGraphicsEffectList) Remove(effect WidgetGraphicsEffect) error
func (l *WidgetGraphicsEffectList) RemoveAt(index int) error
type WidgetList
func (l *WidgetList) Add(item Widget) error
func (l *WidgetList) At(index int) Widget
func (l *WidgetList) Clear() error
func (l *WidgetList) Contains(item Widget) bool
func (l *WidgetList) Index(item Widget) int
func (l *WidgetList) Insert(index int, item Widget) error
func (l *WidgetList) Len() int
func (l *WidgetList) Remove(item Widget) error
func (l *WidgetList) RemoveAt(index int) error
type Window
func FocusedWindow() Window
type WindowBase
func (wb *WindowBase) Accessibility() *Accessibility
func (wb *WindowBase) AddDisposable(d Disposable)
func (wb *WindowBase) ApplyDPI(dpi int)
func (wb *WindowBase) ApplySysColors()
func (wb *WindowBase) AsWindowBase() *WindowBase
func (wb *WindowBase) Background() Brush
func (wb *WindowBase) Bounds() Rectangle
func (wb *WindowBase) BoundsChanged() *Event
func (wb *WindowBase) BoundsPixels() Rectangle
func (wb *WindowBase) BringToTop() error
func (wb *WindowBase) ClientBounds() Rectangle
func (wb *WindowBase) ClientBoundsPixels() Rectangle
func (wb *WindowBase) ContextMenu() *Menu
func (wb *WindowBase) ContextMenuLocation() Point
func (wb *WindowBase) CreateCanvas() (*Canvas, error)
func (wb *WindowBase) Cursor() Cursor
func (wb *WindowBase) DPI() int
func (wb *WindowBase) Dispose()
func (wb *WindowBase) Disposing() *Event
func (wb *WindowBase) DoubleBuffering() bool
func (wb *WindowBase) DropFiles() *DropFilesEvent
func (wb *WindowBase) Enabled() bool
func (wb *WindowBase) Focused() bool
func (wb *WindowBase) FocusedChanged() *Event
func (wb *WindowBase) Font() *Font
func (wb *WindowBase) ForEachDescendant(f func(widget Widget) bool)
func (wb *WindowBase) Form() Form
func (wb *WindowBase) Handle() win.HWND
func (wb *WindowBase) Height() int
func (wb *WindowBase) HeightPixels() int
func (wb *WindowBase) IntFrom96DPI(value int) int
func (wb *WindowBase) IntTo96DPI(value int) int
func (wb *WindowBase) Invalidate() error
func (wb *WindowBase) IsDisposed() bool
func (wb *WindowBase) KeyDown() *KeyEvent
func (wb *WindowBase) KeyPress() *KeyEvent
func (wb *WindowBase) KeyUp() *KeyEvent
func (wb *WindowBase) MarginsFrom96DPI(value Margins) Margins
func (wb *WindowBase) MarginsTo96DPI(value Margins) Margins
func (wb *WindowBase) MaxSize() Size
func (wb *WindowBase) MaxSizePixels() Size
func (wb *WindowBase) MinSize() Size
func (wb *WindowBase) MinSizePixels() Size
func (wb *WindowBase) MouseDown() *MouseEvent
func (wb *WindowBase) MouseMove() *MouseEvent
func (wb *WindowBase) MouseUp() *MouseEvent
func (wb *WindowBase) MouseWheel() *MouseEvent
func (wb *WindowBase) MustRegisterProperty(name string, property Property)
func (wb *WindowBase) Name() string
func (wb *WindowBase) PointFrom96DPI(value Point) Point
func (wb *WindowBase) PointTo96DPI(value Point) Point
func (wb *WindowBase) Property(name string) Property
func (wb *WindowBase) ReadState() (string, error)
func (wb *WindowBase) RectangleFrom96DPI(value Rectangle) Rectangle
func (wb *WindowBase) RectangleTo96DPI(value Rectangle) Rectangle
func (wb *WindowBase) RequestLayout()
func (wb *WindowBase) RestoreState() (err error)
func (wb *WindowBase) RightToLeftReading() bool
func (wb *WindowBase) SaveState() (err error)
func (wb *WindowBase) Screenshot() (*image.RGBA, error)
func (wb *WindowBase) SendMessage(msg uint32, wParam, lParam uintptr) uintptr
func (wb *WindowBase) SetBackground(background Brush)
func (wb *WindowBase) SetBounds(bounds Rectangle) error
func (wb *WindowBase) SetBoundsPixels(bounds Rectangle) error
func (wb *WindowBase) SetClientSize(value Size) error
func (wb *WindowBase) SetClientSizePixels(value Size) error
func (wb *WindowBase) SetContextMenu(value *Menu)
func (wb *WindowBase) SetCursor(value Cursor)
func (wb *WindowBase) SetDoubleBuffering(enabled bool) error
func (wb *WindowBase) SetEnabled(enabled bool)
func (wb *WindowBase) SetFocus() error
func (wb *WindowBase) SetFont(font *Font)
func (wb *WindowBase) SetHeight(value int) error
func (wb *WindowBase) SetHeightPixels(value int) error
func (wb *WindowBase) SetMinMaxSize(min, max Size) error
func (wb *WindowBase) SetMinMaxSizePixels(min, max Size) error
func (wb *WindowBase) SetName(name string)
func (wb *WindowBase) SetRightToLeftReading(rtl bool) error
func (wb *WindowBase) SetSize(size Size) error
func (wb *WindowBase) SetSizePixels(size Size) error
func (wb *WindowBase) SetSuspended(suspend bool)
func (wb *WindowBase) SetVisible(visible bool)
func (wb *WindowBase) SetWidth(value int) error
func (wb *WindowBase) SetWidthPixels(value int) error
func (wb *WindowBase) SetX(value int) error
func (wb *WindowBase) SetXPixels(value int) error
func (wb *WindowBase) SetY(value int) error
func (wb *WindowBase) SetYPixels(value int) error
func (wb *WindowBase) ShortcutActions() *ActionList
func (wb *WindowBase) Size() Size
func (wb *WindowBase) SizeChanged() *Event
func (wb *WindowBase) SizeFrom96DPI(value Size) Size
func (wb *WindowBase) SizePixels() Size
func (wb *WindowBase) SizeTo96DPI(value Size) Size
func (wb *WindowBase) Suspended() bool
func (wb *WindowBase) Synchronize(f func())
func (wb *WindowBase) Visible() bool
func (wb *WindowBase) VisibleChanged() *Event
func (wb *WindowBase) Width() int
func (wb *WindowBase) WidthPixels() int
func (wb *WindowBase) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
func (wb *WindowBase) WriteState(state string) error
func (wb *WindowBase) X() int
func (wb *WindowBase) XPixels() int
func (wb *WindowBase) Y() int
func (wb *WindowBase) YPixels() int
type WindowGroup
func (g *WindowGroup) ActiveForm() Form
func (g *WindowGroup) Add(delta int)
func (g *WindowGroup) CreateToolTip() (*ToolTip, error)
func (g *WindowGroup) Done()
func (g *WindowGroup) Refs() int
func (g *WindowGroup) RunSynchronized()
func (g *WindowGroup) SetActiveForm(form Form)
func (g *WindowGroup) Synchronize(f func())
func (g *WindowGroup) ThreadID() uint32
func (g *WindowGroup) ToolTip() *ToolTip
Constants ¶
View Source
const (
	NoOrientation Orientation = 0
	Horizontal                = 1 << 0
	Vertical                  = 1 << 1
)
View Source
const (
	DlgCmdNone     = 0
	DlgCmdOK       = win.IDOK
	DlgCmdCancel   = win.IDCANCEL
	DlgCmdAbort    = win.IDABORT
	DlgCmdRetry    = win.IDRETRY
	DlgCmdIgnore   = win.IDIGNORE
	DlgCmdYes      = win.IDYES
	DlgCmdNo       = win.IDNO
	DlgCmdClose    = win.IDCLOSE
	DlgCmdHelp     = win.IDHELP
	DlgCmdTryAgain = win.IDTRYAGAIN
	DlgCmdContinue = win.IDCONTINUE
	DlgCmdTimeout  = win.IDTIMEOUT
)
View Source
const (
	EllipsisNone EllipsisMode = 0
	EllipsisEnd               = EllipsisMode(win.SS_ENDELLIPSIS)
	EllipsisPath              = EllipsisMode(win.SS_PATHELLIPSIS)
)
Variables ¶
View Source
var (
	ErrPropertyReadOnly       = errors.New("read-only property")
	ErrPropertyNotValidatable = errors.New("property not validatable")
)
View Source
var (
	ErrInvalidType = errors.New("invalid type")
)
Functions ¶
func AltDown ¶
func AltDown() bool
func AppDataPath ¶
func AppDataPath() (string, error)
func AppendToWalkInit ¶
func AppendToWalkInit(fn func())
func CommonAppDataPath ¶
func CommonAppDataPath() (string, error)
func ControlDown ¶
func ControlDown() bool
func DriveNames ¶
func DriveNames() ([]string, error)
func FormatFloat ¶
func FormatFloat(f float64, prec int) string
func FormatFloatGrouped ¶
func FormatFloatGrouped(f float64, prec int) string
func InitWidget ¶
func InitWidget(widget Widget, parent Window, className string, style, exStyle uint32) error
InitWidget initializes a Widget.

func InitWindow ¶
func InitWindow(window, parent Window, className string, style, exStyle uint32) error
InitWindow initializes a window.

Widgets should be initialized using InitWidget instead.

func InitWrapperWindow ¶
func InitWrapperWindow(window Window) error
InitWrapperWindow initializes a window that wraps (embeds) another window.

Calling this method is necessary, if you want to be able to override the WndProc method of the embedded window. The embedded window should only be used as inseparable part of the wrapper window to avoid undefined behavior.

func IntFrom96DPI ¶
func IntFrom96DPI(value, dpi int) int
IntFrom96DPI converts from 1/96" units to native pixels.

func IntTo96DPI ¶
func IntTo96DPI(value, dpi int) int
IntTo96DPI converts from native pixels to 1/96" units.

func LocalAppDataPath ¶
func LocalAppDataPath() (string, error)
func LogErrors ¶
func LogErrors() bool
func MouseWheelEventDelta ¶
func MouseWheelEventDelta(button MouseButton) int
func MouseWheelEventKeyState ¶
func MouseWheelEventKeyState(button MouseButton) int
func MsgBox ¶
func MsgBox(owner Form, title, message string, style MsgBoxStyle) int
func MustRegisterWindowClass ¶
func MustRegisterWindowClass(className string)
MustRegisterWindowClass registers the specified window class.

MustRegisterWindowClass must be called once for every window type that is not based on any system provided control, before calling InitChildWidget or InitWidget. Calling MustRegisterWindowClass twice with the same className results in a panic.

func MustRegisterWindowClassWithStyle ¶
func MustRegisterWindowClassWithStyle(className string, style uint32)
func MustRegisterWindowClassWithWndProcPtr ¶
func MustRegisterWindowClassWithWndProcPtr(className string, wndProcPtr uintptr)
func MustRegisterWindowClassWithWndProcPtrAndStyle ¶
func MustRegisterWindowClassWithWndProcPtrAndStyle(className string, wndProcPtr uintptr, style uint32)
func PanicOnError ¶
func PanicOnError() bool
func ParseFloat ¶
func ParseFloat(s string) (float64, error)
func PersonalPath ¶
func PersonalPath() (string, error)
func RegistryKeyString ¶
func RegistryKeyString(rootKey *RegistryKey, subKeyPath, valueName string) (value string, err error)
func RegistryKeyUint32 ¶
func RegistryKeyUint32(rootKey *RegistryKey, subKeyPath, valueName string) (value uint32, err error)
func SetLogErrors ¶
func SetLogErrors(v bool)
func SetPanicOnError ¶
func SetPanicOnError(v bool)
func SetTranslationFunc ¶
func SetTranslationFunc(f TranslationFunction)
func SetWindowFont ¶
func SetWindowFont(hwnd win.HWND, font *Font)
func ShiftDown ¶
func ShiftDown() bool
func SystemPath ¶
func SystemPath() (string, error)
Types ¶
type AccRole ¶
type AccRole int32
AccRole enum defines the role of the window/control in UI.

const (
	AccRoleTitlebar           AccRole = win.ROLE_SYSTEM_TITLEBAR
	AccRoleMenubar            AccRole = win.ROLE_SYSTEM_MENUBAR
	AccRoleScrollbar          AccRole = win.ROLE_SYSTEM_SCROLLBAR
	AccRoleGrip               AccRole = win.ROLE_SYSTEM_GRIP
	AccRoleSound              AccRole = win.ROLE_SYSTEM_SOUND
	AccRoleCursor             AccRole = win.ROLE_SYSTEM_CURSOR
	AccRoleCaret              AccRole = win.ROLE_SYSTEM_CARET
	AccRoleAlert              AccRole = win.ROLE_SYSTEM_ALERT
	AccRoleWindow             AccRole = win.ROLE_SYSTEM_WINDOW
	AccRoleClient             AccRole = win.ROLE_SYSTEM_CLIENT
	AccRoleMenuPopup          AccRole = win.ROLE_SYSTEM_MENUPOPUP
	AccRoleMenuItem           AccRole = win.ROLE_SYSTEM_MENUITEM
	AccRoleTooltip            AccRole = win.ROLE_SYSTEM_TOOLTIP
	AccRoleApplication        AccRole = win.ROLE_SYSTEM_APPLICATION
	AccRoleDocument           AccRole = win.ROLE_SYSTEM_DOCUMENT
	AccRolePane               AccRole = win.ROLE_SYSTEM_PANE
	AccRoleChart              AccRole = win.ROLE_SYSTEM_CHART
	AccRoleDialog             AccRole = win.ROLE_SYSTEM_DIALOG
	AccRoleBorder             AccRole = win.ROLE_SYSTEM_BORDER
	AccRoleGrouping           AccRole = win.ROLE_SYSTEM_GROUPING
	AccRoleSeparator          AccRole = win.ROLE_SYSTEM_SEPARATOR
	AccRoleToolbar            AccRole = win.ROLE_SYSTEM_TOOLBAR
	AccRoleStatusbar          AccRole = win.ROLE_SYSTEM_STATUSBAR
	AccRoleTable              AccRole = win.ROLE_SYSTEM_TABLE
	AccRoleColumnHeader       AccRole = win.ROLE_SYSTEM_COLUMNHEADER
	AccRoleRowHeader          AccRole = win.ROLE_SYSTEM_ROWHEADER
	AccRoleColumn             AccRole = win.ROLE_SYSTEM_COLUMN
	AccRoleRow                AccRole = win.ROLE_SYSTEM_ROW
	AccRoleCell               AccRole = win.ROLE_SYSTEM_CELL
	AccRoleLink               AccRole = win.ROLE_SYSTEM_LINK
	AccRoleHelpBalloon        AccRole = win.ROLE_SYSTEM_HELPBALLOON
	AccRoleCharacter          AccRole = win.ROLE_SYSTEM_CHARACTER
	AccRoleList               AccRole = win.ROLE_SYSTEM_LIST
	AccRoleListItem           AccRole = win.ROLE_SYSTEM_LISTITEM
	AccRoleOutline            AccRole = win.ROLE_SYSTEM_OUTLINE
	AccRoleOutlineItem        AccRole = win.ROLE_SYSTEM_OUTLINEITEM
	AccRolePagetab            AccRole = win.ROLE_SYSTEM_PAGETAB
	AccRolePropertyPage       AccRole = win.ROLE_SYSTEM_PROPERTYPAGE
	AccRoleIndicator          AccRole = win.ROLE_SYSTEM_INDICATOR
	AccRoleGraphic            AccRole = win.ROLE_SYSTEM_GRAPHIC
	AccRoleStatictext         AccRole = win.ROLE_SYSTEM_STATICTEXT
	AccRoleText               AccRole = win.ROLE_SYSTEM_TEXT
	AccRolePushbutton         AccRole = win.ROLE_SYSTEM_PUSHBUTTON
	AccRoleCheckbutton        AccRole = win.ROLE_SYSTEM_CHECKBUTTON
	AccRoleRadiobutton        AccRole = win.ROLE_SYSTEM_RADIOBUTTON
	AccRoleCombobox           AccRole = win.ROLE_SYSTEM_COMBOBOX
	AccRoleDroplist           AccRole = win.ROLE_SYSTEM_DROPLIST
	AccRoleProgressbar        AccRole = win.ROLE_SYSTEM_PROGRESSBAR
	AccRoleDial               AccRole = win.ROLE_SYSTEM_DIAL
	AccRoleHotkeyfield        AccRole = win.ROLE_SYSTEM_HOTKEYFIELD
	AccRoleSlider             AccRole = win.ROLE_SYSTEM_SLIDER
	AccRoleSpinbutton         AccRole = win.ROLE_SYSTEM_SPINBUTTON
	AccRoleDiagram            AccRole = win.ROLE_SYSTEM_DIAGRAM
	AccRoleAnimation          AccRole = win.ROLE_SYSTEM_ANIMATION
	AccRoleEquation           AccRole = win.ROLE_SYSTEM_EQUATION
	AccRoleButtonDropdown     AccRole = win.ROLE_SYSTEM_BUTTONDROPDOWN
	AccRoleButtonMenu         AccRole = win.ROLE_SYSTEM_BUTTONMENU
	AccRoleButtonDropdownGrid AccRole = win.ROLE_SYSTEM_BUTTONDROPDOWNGRID
	AccRoleWhitespace         AccRole = win.ROLE_SYSTEM_WHITESPACE
	AccRolePageTabList        AccRole = win.ROLE_SYSTEM_PAGETABLIST
	AccRoleClock              AccRole = win.ROLE_SYSTEM_CLOCK
	AccRoleSplitButton        AccRole = win.ROLE_SYSTEM_SPLITBUTTON
	AccRoleIPAddress          AccRole = win.ROLE_SYSTEM_IPADDRESS
	AccRoleOutlineButton      AccRole = win.ROLE_SYSTEM_OUTLINEBUTTON
)
Window/control system roles

type AccState ¶
type AccState int32
AccState enum defines the state of the window/control

const (
	AccStateNormal          AccState = win.STATE_SYSTEM_NORMAL
	AccStateUnavailable     AccState = win.STATE_SYSTEM_UNAVAILABLE
	AccStateSelected        AccState = win.STATE_SYSTEM_SELECTED
	AccStateFocused         AccState = win.STATE_SYSTEM_FOCUSED
	AccStatePressed         AccState = win.STATE_SYSTEM_PRESSED
	AccStateChecked         AccState = win.STATE_SYSTEM_CHECKED
	AccStateMixed           AccState = win.STATE_SYSTEM_MIXED
	AccStateIndeterminate   AccState = win.STATE_SYSTEM_INDETERMINATE
	AccStateReadonly        AccState = win.STATE_SYSTEM_READONLY
	AccStateHotTracked      AccState = win.STATE_SYSTEM_HOTTRACKED
	AccStateDefault         AccState = win.STATE_SYSTEM_DEFAULT
	AccStateExpanded        AccState = win.STATE_SYSTEM_EXPANDED
	AccStateCollapsed       AccState = win.STATE_SYSTEM_COLLAPSED
	AccStateBusy            AccState = win.STATE_SYSTEM_BUSY
	AccStateFloating        AccState = win.STATE_SYSTEM_FLOATING
	AccStateMarqueed        AccState = win.STATE_SYSTEM_MARQUEED
	AccStateAnimated        AccState = win.STATE_SYSTEM_ANIMATED
	AccStateInvisible       AccState = win.STATE_SYSTEM_INVISIBLE
	AccStateOffscreen       AccState = win.STATE_SYSTEM_OFFSCREEN
	AccStateSizeable        AccState = win.STATE_SYSTEM_SIZEABLE
	AccStateMoveable        AccState = win.STATE_SYSTEM_MOVEABLE
	AccStateSelfVoicing     AccState = win.STATE_SYSTEM_SELFVOICING
	AccStateFocusable       AccState = win.STATE_SYSTEM_FOCUSABLE
	AccStateSelectable      AccState = win.STATE_SYSTEM_SELECTABLE
	AccStateLinked          AccState = win.STATE_SYSTEM_LINKED
	AccStateTraversed       AccState = win.STATE_SYSTEM_TRAVERSED
	AccStateMultiselectable AccState = win.STATE_SYSTEM_MULTISELECTABLE
	AccStateExtselectable   AccState = win.STATE_SYSTEM_EXTSELECTABLE
	AccStateAlertLow        AccState = win.STATE_SYSTEM_ALERT_LOW
	AccStateAlertMedium     AccState = win.STATE_SYSTEM_ALERT_MEDIUM
	AccStateAlertHigh       AccState = win.STATE_SYSTEM_ALERT_HIGH
	AccStateProtected       AccState = win.STATE_SYSTEM_PROTECTED
	AccStateHasPopup        AccState = win.STATE_SYSTEM_HASPOPUP
	AccStateValid           AccState = win.STATE_SYSTEM_VALID
)
Window/control states

type Accessibility ¶
type Accessibility struct {
	// contains filtered or unexported fields
}
Accessibility provides basic Dynamic Annotation of windows and controls.

func (*Accessibility) SetAccelerator ¶
func (a *Accessibility) SetAccelerator(acc string) error
SetAccelerator sets window accelerator name using Dynamic Annotation.

func (*Accessibility) SetDefaultAction ¶
func (a *Accessibility) SetDefaultAction(defAction string) error
SetDefaultAction sets window default action using Dynamic Annotation.

func (*Accessibility) SetDescription ¶
func (a *Accessibility) SetDescription(acc string) error
SetDescription sets window description using Dynamic Annotation.

func (*Accessibility) SetHelp ¶
func (a *Accessibility) SetHelp(help string) error
SetHelp sets window help using Dynamic Annotation.

func (*Accessibility) SetName ¶
func (a *Accessibility) SetName(name string) error
SetName sets window name using Dynamic Annotation.

func (*Accessibility) SetRole ¶
func (a *Accessibility) SetRole(role AccRole) error
SetRole sets window role using Dynamic Annotation. The role must be set when the window is created and is not to be modified later.

func (*Accessibility) SetRoleMap ¶
func (a *Accessibility) SetRoleMap(roleMap string) error
SetRoleMap sets window role map using Dynamic Annotation. The role map must be set when the window is created and is not to be modified later.

func (*Accessibility) SetState ¶
func (a *Accessibility) SetState(state AccState) error
SetState sets window state using Dynamic Annotation.

func (*Accessibility) SetStateMap ¶
func (a *Accessibility) SetStateMap(stateMap string) error
SetStateMap sets window state map using Dynamic Annotation. The state map must be set when the window is created and is not to be modified later.

func (*Accessibility) SetValueMap ¶
func (a *Accessibility) SetValueMap(valueMap string) error
SetValueMap sets window value map using Dynamic Annotation. The value map must be set when the window is created and is not to be modified later.

type Action ¶
type Action struct {
	// contains filtered or unexported fields
}
func NewAction ¶
func NewAction() *Action
func NewMenuAction ¶
func NewMenuAction(menu *Menu) *Action
func NewSeparatorAction ¶
func NewSeparatorAction() *Action
func (*Action) Checkable ¶
func (a *Action) Checkable() bool
func (*Action) Checked ¶
func (a *Action) Checked() bool
func (*Action) CheckedCondition ¶
func (a *Action) CheckedCondition() Condition
func (*Action) Default ¶
func (a *Action) Default() bool
func (*Action) DefaultCondition ¶
func (a *Action) DefaultCondition() Condition
func (*Action) Enabled ¶
func (a *Action) Enabled() bool
func (*Action) EnabledCondition ¶
func (a *Action) EnabledCondition() Condition
func (*Action) Exclusive ¶
func (a *Action) Exclusive() bool
func (*Action) Image ¶
func (a *Action) Image() Image
func (*Action) IsSeparator ¶
func (a *Action) IsSeparator() bool
func (*Action) Menu ¶
func (a *Action) Menu() *Menu
func (*Action) SetCheckable ¶
func (a *Action) SetCheckable(value bool) (err error)
func (*Action) SetChecked ¶
func (a *Action) SetChecked(value bool) (err error)
func (*Action) SetCheckedCondition ¶
func (a *Action) SetCheckedCondition(c Condition)
func (*Action) SetDefault ¶
func (a *Action) SetDefault(value bool) (err error)
func (*Action) SetDefaultCondition ¶
func (a *Action) SetDefaultCondition(c Condition)
func (*Action) SetEnabled ¶
func (a *Action) SetEnabled(value bool) (err error)
func (*Action) SetEnabledCondition ¶
func (a *Action) SetEnabledCondition(c Condition)
func (*Action) SetExclusive ¶
func (a *Action) SetExclusive(value bool) (err error)
func (*Action) SetImage ¶
func (a *Action) SetImage(value Image) (err error)
func (*Action) SetShortcut ¶
func (a *Action) SetShortcut(shortcut Shortcut) (err error)
func (*Action) SetText ¶
func (a *Action) SetText(value string) (err error)
func (*Action) SetToolTip ¶
func (a *Action) SetToolTip(value string) (err error)
func (*Action) SetVisible ¶
func (a *Action) SetVisible(value bool) (err error)
func (*Action) SetVisibleCondition ¶
func (a *Action) SetVisibleCondition(c Condition)
func (*Action) Shortcut ¶
func (a *Action) Shortcut() Shortcut
func (*Action) Text ¶
func (a *Action) Text() string
func (*Action) ToolTip ¶
func (a *Action) ToolTip() string
func (*Action) Triggered ¶
func (a *Action) Triggered() *Event
func (*Action) Visible ¶
func (a *Action) Visible() bool
func (*Action) VisibleCondition ¶
func (a *Action) VisibleCondition() Condition
type ActionList ¶
type ActionList struct {
	// contains filtered or unexported fields
}
func (*ActionList) Add ¶
func (l *ActionList) Add(action *Action) error
func (*ActionList) AddMenu ¶
func (l *ActionList) AddMenu(menu *Menu) (*Action, error)
func (*ActionList) At ¶
func (l *ActionList) At(index int) *Action
func (*ActionList) Clear ¶
func (l *ActionList) Clear() error
func (*ActionList) Contains ¶
func (l *ActionList) Contains(action *Action) bool
func (*ActionList) Index ¶
func (l *ActionList) Index(action *Action) int
func (*ActionList) Insert ¶
func (l *ActionList) Insert(index int, action *Action) error
func (*ActionList) InsertMenu ¶
func (l *ActionList) InsertMenu(index int, menu *Menu) (*Action, error)
func (*ActionList) Len ¶
func (l *ActionList) Len() int
func (*ActionList) Remove ¶
func (l *ActionList) Remove(action *Action) error
func (*ActionList) RemoveAt ¶
func (l *ActionList) RemoveAt(index int) error
type Alignment1D ¶
type Alignment1D uint
const (
	AlignDefault Alignment1D = iota
	AlignNear
	AlignCenter
	AlignFar
)
type Alignment2D ¶
type Alignment2D uint
const (
	AlignHVDefault Alignment2D = iota
	AlignHNearVNear
	AlignHCenterVNear
	AlignHFarVNear
	AlignHNearVCenter
	AlignHCenterVCenter
	AlignHFarVCenter
	AlignHNearVFar
	AlignHCenterVFar
	AlignHFarVFar
)
type Application ¶
type Application struct {
	// contains filtered or unexported fields
}
func App ¶
func App() *Application
func (*Application) ActiveForm ¶
func (app *Application) ActiveForm() Form
ActiveForm returns the currently active form for the caller's thread. It returns nil if no form is active or the caller's thread does not have any windows associated with it. It should be called from within synchronized functions.

func (*Application) Exit ¶
func (app *Application) Exit(exitCode int)
func (*Application) ExitCode ¶
func (app *Application) ExitCode() int
func (*Application) OrganizationName ¶
func (app *Application) OrganizationName() string
func (*Application) Panicking ¶
func (app *Application) Panicking() *ErrorEvent
func (*Application) ProductName ¶
func (app *Application) ProductName() string
func (*Application) SetOrganizationName ¶
func (app *Application) SetOrganizationName(value string)
func (*Application) SetProductName ¶
func (app *Application) SetProductName(value string)
func (*Application) SetSettings ¶
func (app *Application) SetSettings(value Settings)
func (*Application) Settings ¶
func (app *Application) Settings() Settings
type ApplyDPIer ¶
type ApplyDPIer interface {
	ApplyDPI(dpi int)
}
type ApplyFonter ¶
type ApplyFonter interface {
	ApplyFont(font *Font)
}
type ApplySysColorser ¶
type ApplySysColorser interface {
	ApplySysColors()
}
type BindingValueProvider ¶
type BindingValueProvider interface {
	BindingValue(index int) interface{}
}
BindingValueProvider is the interface that a model must implement to support data binding with widgets like ComboBox.

type Bitmap ¶
type Bitmap struct {
	// contains filtered or unexported fields
}
func BitmapFrom ¶
func BitmapFrom(src interface{}, dpi int) (*Bitmap, error)
func NewBitmap
deprecated
func NewBitmapForDPI ¶
func NewBitmapForDPI(size Size, dpi int) (*Bitmap, error)
NewBitmapForDPI creates an opaque bitmap with given size in native pixels and DPI.

func NewBitmapFromFile
deprecated
func NewBitmapFromFileForDPI ¶
func NewBitmapFromFileForDPI(filePath string, dpi int) (*Bitmap, error)
NewBitmapFromFileForDPI creates new bitmap from a bitmap file at given DPI.

func NewBitmapFromIcon
deprecated
func NewBitmapFromIconForDPI ¶
func NewBitmapFromIconForDPI(icon *Icon, size Size, dpi int) (*Bitmap, error)
NewBitmapFromIconForDPI creates a new bitmap with given size in native pixels and DPI and paints the icon on it.

func NewBitmapFromImage
deprecated
func NewBitmapFromImageForDPI ¶
func NewBitmapFromImageForDPI(im image.Image, dpi int) (*Bitmap, error)
NewBitmapFromImageForDPI creates a Bitmap from image.Image at given DPI.

func NewBitmapFromImageWithSize ¶
func NewBitmapFromImageWithSize(image Image, size Size) (*Bitmap, error)
NewBitmapFromImageWithSize creates a bitmap with given size in native units and paints the image on it streched.

func NewBitmapFromResource
deprecated
func NewBitmapFromResourceForDPI ¶
func NewBitmapFromResourceForDPI(name string, dpi int) (*Bitmap, error)
NewBitmapFromResourceForDPI creates a Bitmap at given DPI from resource by name.

func NewBitmapFromResourceId
deprecated
func NewBitmapFromResourceIdForDPI ¶
func NewBitmapFromResourceIdForDPI(id int, dpi int) (*Bitmap, error)
NewBitmapFromResourceIdForDPI creates a Bitmap at given DPI from resource by ID.

func NewBitmapFromWindow ¶
func NewBitmapFromWindow(window Window) (*Bitmap, error)
func NewBitmapWithTransparentPixels
deprecated
func NewBitmapWithTransparentPixelsForDPI ¶
func NewBitmapWithTransparentPixelsForDPI(size Size, dpi int) (*Bitmap, error)
NewBitmapWithTransparentPixelsForDPI creates a transparent bitmap with given size in native pixels and DPI.

func (*Bitmap) Dispose ¶
func (bmp *Bitmap) Dispose()
func (*Bitmap) Size ¶
func (bmp *Bitmap) Size() Size
Size returns bitmap size in 1/96" units.

func (*Bitmap) ToImage ¶
func (bmp *Bitmap) ToImage() (*image.RGBA, error)
type BitmapBrush ¶
type BitmapBrush struct {
	// contains filtered or unexported fields
}
func NewBitmapBrush ¶
func NewBitmapBrush(bitmap *Bitmap) (*BitmapBrush, error)
func (*BitmapBrush) Bitmap ¶
func (b *BitmapBrush) Bitmap() *Bitmap
func (*BitmapBrush) Dispose ¶
func (bb *BitmapBrush) Dispose()
type BorderGlowEffect ¶
type BorderGlowEffect struct {
	// contains filtered or unexported fields
}
func NewBorderGlowEffect ¶
func NewBorderGlowEffect(color Color) (*BorderGlowEffect, error)
func (*BorderGlowEffect) Dispose ¶
func (wgeb *BorderGlowEffect) Dispose()
func (*BorderGlowEffect) Draw ¶
func (bge *BorderGlowEffect) Draw(widget Widget, canvas *Canvas) error
type BoxLayout ¶
type BoxLayout struct {
	LayoutBase
	// contains filtered or unexported fields
}
func NewHBoxLayout ¶
func NewHBoxLayout() *BoxLayout
func NewVBoxLayout ¶
func NewVBoxLayout() *BoxLayout
func (*BoxLayout) CreateLayoutItem ¶
func (l *BoxLayout) CreateLayoutItem(ctx *LayoutContext) ContainerLayoutItem
func (*BoxLayout) Orientation ¶
func (l *BoxLayout) Orientation() Orientation
func (*BoxLayout) SetOrientation ¶
func (l *BoxLayout) SetOrientation(value Orientation) error
func (*BoxLayout) SetStretchFactor ¶
func (l *BoxLayout) SetStretchFactor(widget Widget, factor int) error
func (*BoxLayout) StretchFactor ¶
func (l *BoxLayout) StretchFactor(widget Widget) int
type Brush ¶
type Brush interface {
	Dispose()
	// contains filtered or unexported methods
}
func NullBrush ¶
func NullBrush() Brush
type Button ¶
type Button struct {
	WidgetBase
	// contains filtered or unexported fields
}
func (*Button) ApplyDPI ¶
func (b *Button) ApplyDPI(dpi int)
func (*Button) Checked ¶
func (b *Button) Checked() bool
func (*Button) CheckedChanged ¶
func (b *Button) CheckedChanged() *Event
func (*Button) Clicked ¶
func (b *Button) Clicked() *Event
func (*Button) CreateLayoutItem ¶
func (b *Button) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*Button) Image ¶
func (b *Button) Image() Image
func (*Button) ImageChanged ¶
func (b *Button) ImageChanged() *Event
func (*Button) Persistent ¶
func (b *Button) Persistent() bool
func (*Button) RestoreState ¶
func (b *Button) RestoreState() error
func (*Button) SaveState ¶
func (b *Button) SaveState() error
func (*Button) SetChecked ¶
func (b *Button) SetChecked(checked bool)
func (*Button) SetImage ¶
func (b *Button) SetImage(image Image) error
func (*Button) SetPersistent ¶
func (b *Button) SetPersistent(value bool)
func (*Button) SetText ¶
func (b *Button) SetText(value string) error
func (*Button) Text ¶
func (b *Button) Text() string
func (*Button) WndProc ¶
func (b *Button) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type CancelEvent ¶
type CancelEvent struct {
	// contains filtered or unexported fields
}
func (*CancelEvent) Attach ¶
func (e *CancelEvent) Attach(handler CancelEventHandler) int
func (*CancelEvent) Detach ¶
func (e *CancelEvent) Detach(handle int)
func (*CancelEvent) Once ¶
func (e *CancelEvent) Once(handler CancelEventHandler)
type CancelEventHandler ¶
type CancelEventHandler func(canceled *bool)
type CancelEventPublisher ¶
type CancelEventPublisher struct {
	// contains filtered or unexported fields
}
func (*CancelEventPublisher) Event ¶
func (p *CancelEventPublisher) Event() *CancelEvent
func (*CancelEventPublisher) Publish ¶
func (p *CancelEventPublisher) Publish(canceled *bool)
type Canvas ¶
type Canvas struct {
	// contains filtered or unexported fields
}
func NewCanvasFromImage ¶
func NewCanvasFromImage(image Image) (*Canvas, error)
func (*Canvas) Bounds ¶
func (c *Canvas) Bounds() Rectangle
func (*Canvas) BoundsPixels ¶
func (c *Canvas) BoundsPixels() Rectangle
func (*Canvas) DPI ¶
func (c *Canvas) DPI() int
func (*Canvas) Dispose ¶
func (c *Canvas) Dispose()
func (*Canvas) DrawBitmapPart ¶
func (c *Canvas) DrawBitmapPart(bmp *Bitmap, dst, src Rectangle) error
DrawBitmapPart draws bitmap at given location in native pixels.

func (*Canvas) DrawBitmapPartWithOpacity
deprecated
func (*Canvas) DrawBitmapPartWithOpacityPixels ¶
func (c *Canvas) DrawBitmapPartWithOpacityPixels(bmp *Bitmap, dst, src Rectangle, opacity byte) error
DrawBitmapPartWithOpacityPixels draws bitmap at given location in native pixels.

func (*Canvas) DrawBitmapWithOpacity
deprecated
func (*Canvas) DrawBitmapWithOpacityPixels ¶
func (c *Canvas) DrawBitmapWithOpacityPixels(bmp *Bitmap, bounds Rectangle, opacity byte) error
DrawBitmapWithOpacityPixels draws bitmap with opacity at given location in native pixels stretched.

func (*Canvas) DrawEllipse
deprecated
func (*Canvas) DrawEllipsePixels ¶
func (c *Canvas) DrawEllipsePixels(pen Pen, bounds Rectangle) error
DrawEllipsePixels draws an ellipse in native pixels.

func (*Canvas) DrawImage
deprecated
func (*Canvas) DrawImagePixels ¶
func (c *Canvas) DrawImagePixels(image Image, location Point) error
DrawImagePixels draws image at given location (upper left) in native pixels unstretched.

func (*Canvas) DrawImageStretched
deprecated
func (*Canvas) DrawImageStretchedPixels ¶
func (c *Canvas) DrawImageStretchedPixels(image Image, bounds Rectangle) error
DrawImageStretchedPixels draws image at given location in native pixels stretched.

func (*Canvas) DrawLine
deprecated
func (*Canvas) DrawLinePixels ¶
func (c *Canvas) DrawLinePixels(pen Pen, from, to Point) error
DrawLinePixels draws a line between two points in native pixels.

func (*Canvas) DrawPolyline
deprecated
func (*Canvas) DrawPolylinePixels ¶
func (c *Canvas) DrawPolylinePixels(pen Pen, points []Point) error
DrawPolylinePixels draws a line between given points in native pixels.

func (*Canvas) DrawRectangle
deprecated
func (*Canvas) DrawRectanglePixels ¶
func (c *Canvas) DrawRectanglePixels(pen Pen, bounds Rectangle) error
DrawRectanglePixels draws a rectangle in native pixels.

func (*Canvas) DrawRoundedRectangle
deprecated
func (*Canvas) DrawRoundedRectanglePixels ¶
func (c *Canvas) DrawRoundedRectanglePixels(pen Pen, bounds Rectangle, ellipseSize Size) error
DrawRoundedRectanglePixels draws a rounded rectangle in native pixels.

func (*Canvas) DrawText
deprecated
func (*Canvas) DrawTextPixels ¶
func (c *Canvas) DrawTextPixels(text string, font *Font, color Color, bounds Rectangle, format DrawTextFormat) error
DrawTextPixels draws text at given location in native pixels.

func (*Canvas) FillEllipse
deprecated
func (*Canvas) FillEllipsePixels ¶
func (c *Canvas) FillEllipsePixels(brush Brush, bounds Rectangle) error
FillEllipsePixels draws a filled in native pixels.

func (*Canvas) FillRectangle
deprecated
func (*Canvas) FillRectanglePixels ¶
func (c *Canvas) FillRectanglePixels(brush Brush, bounds Rectangle) error
FillRectanglePixels draws a filled rectangle in native pixels.

func (*Canvas) FillRoundedRectangle
deprecated
func (*Canvas) FillRoundedRectanglePixels ¶
func (c *Canvas) FillRoundedRectanglePixels(brush Brush, bounds Rectangle, ellipseSize Size) error
FillRoundedRectanglePixels draws a filled rounded rectangle in native pixels.

func (*Canvas) GradientFillRectangle
deprecated
func (*Canvas) GradientFillRectanglePixels ¶
func (c *Canvas) GradientFillRectanglePixels(color1, color2 Color, orientation Orientation, bounds Rectangle) error
GradientFillRectanglePixels draws a gradient filled rectangle in native pixels.

func (*Canvas) HDC ¶
func (c *Canvas) HDC() win.HDC
func (*Canvas) MeasureAndModifyTextPixels ¶
func (c *Canvas) MeasureAndModifyTextPixels(text string, font *Font, bounds Rectangle, format DrawTextFormat) (boundsMeasured Rectangle, textDisplayed string, err error)
MeasureAndModifyTextPixels measures text size and also supports modification of the text which occurs if it does not fit into the specified bounds.

Input and output bounds are in native pixels.

func (*Canvas) MeasureText
deprecated
func (*Canvas) MeasureTextPixels ¶
func (c *Canvas) MeasureTextPixels(text string, font *Font, bounds Rectangle, format DrawTextFormat) (boundsMeasured Rectangle, runesFitted int, err error)
MeasureTextPixels measures text size. Input and output bounds are in native pixels.

type CaseMode ¶
type CaseMode uint32
const (
	CaseModeMixed CaseMode = iota
	CaseModeUpper
	CaseModeLower
)
type CellStyle ¶
type CellStyle struct {
	BackgroundColor Color
	TextColor       Color
	Font            *Font

	// Image is the image to display in the cell.
	//
	// Supported types are *walk.Bitmap, *walk.Icon and string. A string will be
	// interpreted as a file path and the icon associated with the file will be
	// used. It is not supported to use strings together with the other options
	// in the same model instance.
	Image interface{}
	// contains filtered or unexported fields
}
CellStyle carries information about the display style of a cell in a tabular widget like TableView.

func (*CellStyle) Bounds ¶
func (cs *CellStyle) Bounds() Rectangle
func (*CellStyle) BoundsPixels ¶
func (cs *CellStyle) BoundsPixels() Rectangle
func (*CellStyle) Canvas ¶
func (cs *CellStyle) Canvas() *Canvas
func (*CellStyle) Col ¶
func (cs *CellStyle) Col() int
func (*CellStyle) Row ¶
func (cs *CellStyle) Row() int
type CellStyler ¶
type CellStyler interface {
	// StyleCell is called for each cell to pick up cell style information.
	StyleCell(style *CellStyle)
}
CellStyler is the interface that must be implemented to provide a tabular widget like TableView with cell display style information.

type CheckBox ¶
type CheckBox struct {
	Button
	// contains filtered or unexported fields
}
func NewCheckBox ¶
func NewCheckBox(parent Container) (*CheckBox, error)
func (*CheckBox) CheckState ¶
func (cb *CheckBox) CheckState() CheckState
func (*CheckBox) CheckStateChanged ¶
func (cb *CheckBox) CheckStateChanged() *Event
func (*CheckBox) RestoreState ¶
func (cb *CheckBox) RestoreState() error
func (*CheckBox) SaveState ¶
func (cb *CheckBox) SaveState() error
func (*CheckBox) SetCheckState ¶
func (cb *CheckBox) SetCheckState(state CheckState)
func (*CheckBox) SetTextOnLeftSide ¶
func (cb *CheckBox) SetTextOnLeftSide(textLeft bool) error
func (*CheckBox) SetTristate ¶
func (cb *CheckBox) SetTristate(tristate bool) error
func (*CheckBox) TextOnLeftSide ¶
func (cb *CheckBox) TextOnLeftSide() bool
func (*CheckBox) Tristate ¶
func (cb *CheckBox) Tristate() bool
func (*CheckBox) WndProc ¶
func (cb *CheckBox) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type CheckState ¶
type CheckState int
const (
	CheckUnchecked     CheckState = win.BST_UNCHECKED
	CheckChecked       CheckState = win.BST_CHECKED
	CheckIndeterminate CheckState = win.BST_INDETERMINATE
)
type ClipboardService ¶
type ClipboardService struct {
	// contains filtered or unexported fields
}
ClipboardService provides access to the system clipboard.

func Clipboard ¶
func Clipboard() *ClipboardService
Clipboard returns an object that provides access to the system clipboard.

func (*ClipboardService) Clear ¶
func (c *ClipboardService) Clear() error
Clear clears the contents of the clipboard.

func (*ClipboardService) ContainsText ¶
func (c *ClipboardService) ContainsText() (available bool, err error)
ContainsText returns whether the clipboard currently contains text data.

func (*ClipboardService) ContentsChanged ¶
func (c *ClipboardService) ContentsChanged() *Event
ContentsChanged returns an Event that you can attach to for handling clipboard content changes.

func (*ClipboardService) SetText ¶
func (c *ClipboardService) SetText(s string) error
SetText sets the current text data of the clipboard.

func (*ClipboardService) Text ¶
func (c *ClipboardService) Text() (text string, err error)
Text returns the current text data of the clipboard.

type CloseEvent ¶
type CloseEvent struct {
	// contains filtered or unexported fields
}
func (*CloseEvent) Attach ¶
func (e *CloseEvent) Attach(handler CloseEventHandler) int
func (*CloseEvent) Detach ¶
func (e *CloseEvent) Detach(handle int)
func (*CloseEvent) Once ¶
func (e *CloseEvent) Once(handler CloseEventHandler)
type CloseEventHandler ¶
type CloseEventHandler func(canceled *bool, reason CloseReason)
type CloseEventPublisher ¶
type CloseEventPublisher struct {
	// contains filtered or unexported fields
}
func (*CloseEventPublisher) Event ¶
func (p *CloseEventPublisher) Event() *CloseEvent
func (*CloseEventPublisher) Publish ¶
func (p *CloseEventPublisher) Publish(canceled *bool, reason CloseReason)
type CloseReason ¶
type CloseReason byte
const (
	CloseReasonUnknown CloseReason = iota
	CloseReasonUser
)
type Color ¶
type Color uint32
func RGB ¶
func RGB(r, g, b byte) Color
func (Color) B ¶
func (c Color) B() byte
func (Color) G ¶
func (c Color) G() byte
func (Color) R ¶
func (c Color) R() byte
type ComboBox ¶
type ComboBox struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewComboBox ¶
func NewComboBox(parent Container) (*ComboBox, error)
func NewDropDownBox ¶
func NewDropDownBox(parent Container) (*ComboBox, error)
func (*ComboBox) BindingMember ¶
func (cb *ComboBox) BindingMember() string
BindingMember returns the member from the model of the ComboBox that is bound to a field of the data source managed by an associated DataBinder.

This is only applicable to walk.ReflectListModel models and simple slices of pointers to struct.

func (*ComboBox) CreateLayoutItem ¶
func (cb *ComboBox) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*ComboBox) CurrentIndex ¶
func (cb *ComboBox) CurrentIndex() int
func (*ComboBox) CurrentIndexChanged ¶
func (cb *ComboBox) CurrentIndexChanged() *Event
func (*ComboBox) DisplayMember ¶
func (cb *ComboBox) DisplayMember() string
DisplayMember returns the member from the model of the ComboBox that is displayed in the ComboBox.

This is only applicable to walk.ReflectListModel models and simple slices of pointers to struct.

func (*ComboBox) Editable ¶
func (cb *ComboBox) Editable() bool
func (*ComboBox) EditingFinished ¶
func (cb *ComboBox) EditingFinished() *Event
func (*ComboBox) Format ¶
func (cb *ComboBox) Format() string
func (*ComboBox) MaxLength ¶
func (cb *ComboBox) MaxLength() int
func (*ComboBox) Model ¶
func (cb *ComboBox) Model() interface{}
Model returns the model of the ComboBox.

func (*ComboBox) NeedsWmSize ¶
func (*ComboBox) NeedsWmSize() bool
func (*ComboBox) Persistent ¶
func (cb *ComboBox) Persistent() bool
func (*ComboBox) Precision ¶
func (cb *ComboBox) Precision() int
func (*ComboBox) RestoreState ¶
func (cb *ComboBox) RestoreState() error
func (*ComboBox) SaveState ¶
func (cb *ComboBox) SaveState() error
func (*ComboBox) SetBindingMember ¶
func (cb *ComboBox) SetBindingMember(bindingMember string) error
SetBindingMember sets the member from the model of the ComboBox that is bound to a field of the data source managed by an associated DataBinder.

This is only applicable to walk.ReflectListModel models and simple slices of pointers to struct.

For a model consisting of items of type S, data source field of type T and bindingMember "Foo", this can be one of the following:

A field		Foo T
A method	func (s S) Foo() T
A method	func (s S) Foo() (T, error)
If bindingMember is not a simple member name like "Foo", but a path to a member like "A.B.Foo", members "A" and "B" both must be one of the options mentioned above, but with T having type pointer to struct.

func (*ComboBox) SetCurrentIndex ¶
func (cb *ComboBox) SetCurrentIndex(value int) error
func (*ComboBox) SetDisplayMember ¶
func (cb *ComboBox) SetDisplayMember(displayMember string) error
SetDisplayMember sets the member from the model of the ComboBox that is displayed in the ComboBox.

This is only applicable to walk.ReflectListModel models and simple slices of pointers to struct.

For a model consisting of items of type S, the type of the specified member T and displayMember "Foo", this can be one of the following:

A field		Foo T
A method	func (s S) Foo() T
A method	func (s S) Foo() (T, error)
If displayMember is not a simple member name like "Foo", but a path to a member like "A.B.Foo", members "A" and "B" both must be one of the options mentioned above, but with T having type pointer to struct.

func (*ComboBox) SetFormat ¶
func (cb *ComboBox) SetFormat(value string)
func (*ComboBox) SetMaxLength ¶
func (cb *ComboBox) SetMaxLength(value int)
func (*ComboBox) SetModel ¶
func (cb *ComboBox) SetModel(mdl interface{}) error
SetModel sets the model of the ComboBox.

It is required that mdl either implements walk.ListModel or walk.ReflectListModel or be a slice of pointers to struct or a []string.

func (*ComboBox) SetPersistent ¶
func (cb *ComboBox) SetPersistent(value bool)
func (*ComboBox) SetPrecision ¶
func (cb *ComboBox) SetPrecision(value int)
func (*ComboBox) SetText ¶
func (cb *ComboBox) SetText(value string) error
func (*ComboBox) SetTextSelection ¶
func (cb *ComboBox) SetTextSelection(start, end int)
func (*ComboBox) Text ¶
func (cb *ComboBox) Text() string
func (*ComboBox) TextChanged ¶
func (cb *ComboBox) TextChanged() *Event
func (*ComboBox) TextSelection ¶
func (cb *ComboBox) TextSelection() (start, end int)
func (*ComboBox) WndProc ¶
func (cb *ComboBox) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type Composite ¶
type Composite struct {
	ContainerBase
}
func NewComposite ¶
func NewComposite(parent Container) (*Composite, error)
func NewCompositeWithStyle ¶
func NewCompositeWithStyle(parent Window, style uint32) (*Composite, error)
type Condition ¶
type Condition interface {
	Expression
	Satisfied() bool
}
func NewAllCondition ¶
func NewAllCondition(items ...Condition) Condition
func NewAnyCondition ¶
func NewAnyCondition(items ...Condition) Condition
func NewNegatedCondition ¶
func NewNegatedCondition(other Condition) Condition
type Container ¶
type Container interface {
	Window
	AsContainerBase() *ContainerBase
	Children() *WidgetList
	Layout() Layout
	SetLayout(value Layout) error
	DataBinder() *DataBinder
	SetDataBinder(dbm *DataBinder)
}
type ContainerBase ¶
type ContainerBase struct {
	WidgetBase
	// contains filtered or unexported fields
}
func (*ContainerBase) ApplyDPI ¶
func (cb *ContainerBase) ApplyDPI(dpi int)
func (*ContainerBase) ApplySysColors ¶
func (cb *ContainerBase) ApplySysColors()
func (*ContainerBase) AsContainerBase ¶
func (cb *ContainerBase) AsContainerBase() *ContainerBase
func (*ContainerBase) AsWidgetBase ¶
func (cb *ContainerBase) AsWidgetBase() *WidgetBase
func (*ContainerBase) Children ¶
func (cb *ContainerBase) Children() *WidgetList
func (*ContainerBase) CreateLayoutItem ¶
func (cb *ContainerBase) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*ContainerBase) DataBinder ¶
func (cb *ContainerBase) DataBinder() *DataBinder
func (*ContainerBase) Layout ¶
func (cb *ContainerBase) Layout() Layout
func (*ContainerBase) NextChildID ¶
func (cb *ContainerBase) NextChildID() int32
func (*ContainerBase) Persistent ¶
func (cb *ContainerBase) Persistent() bool
func (*ContainerBase) RestoreState ¶
func (cb *ContainerBase) RestoreState() error
func (*ContainerBase) SaveState ¶
func (cb *ContainerBase) SaveState() error
func (*ContainerBase) SetDataBinder ¶
func (cb *ContainerBase) SetDataBinder(db *DataBinder)
func (*ContainerBase) SetLayout ¶
func (cb *ContainerBase) SetLayout(value Layout) error
func (*ContainerBase) SetPersistent ¶
func (cb *ContainerBase) SetPersistent(value bool)
func (*ContainerBase) WndProc ¶
func (cb *ContainerBase) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type ContainerLayoutItem ¶
type ContainerLayoutItem interface {
	LayoutItem
	MinSizer
	MinSizeForSizer
	HeightForWidther
	AsContainerLayoutItemBase() *ContainerLayoutItemBase

	// MinSizeEffectiveForChild returns minimum effective size for a child in native pixels.
	MinSizeEffectiveForChild(child LayoutItem) Size

	PerformLayout() []LayoutResultItem
	Children() []LayoutItem
	// contains filtered or unexported methods
}
func CreateLayoutItemsForContainer ¶
func CreateLayoutItemsForContainer(container Container) ContainerLayoutItem
func CreateLayoutItemsForContainerWithContext ¶
func CreateLayoutItemsForContainerWithContext(container Container, ctx *LayoutContext) ContainerLayoutItem
type ContainerLayoutItemBase ¶
type ContainerLayoutItemBase struct {
	LayoutItemBase
	// contains filtered or unexported fields
}
func (*ContainerLayoutItemBase) AsContainerLayoutItemBase ¶
func (clib *ContainerLayoutItemBase) AsContainerLayoutItemBase() *ContainerLayoutItemBase
func (*ContainerLayoutItemBase) Children ¶
func (clib *ContainerLayoutItemBase) Children() []LayoutItem
func (*ContainerLayoutItemBase) HasHeightForWidth ¶
func (clib *ContainerLayoutItemBase) HasHeightForWidth() bool
func (*ContainerLayoutItemBase) MinSizeEffectiveForChild ¶
func (clib *ContainerLayoutItemBase) MinSizeEffectiveForChild(child LayoutItem) Size
func (*ContainerLayoutItemBase) SetChildren ¶
func (clib *ContainerLayoutItemBase) SetChildren(children []LayoutItem)
type CosmeticPen ¶
type CosmeticPen struct {
	// contains filtered or unexported fields
}
func NewCosmeticPen ¶
func NewCosmeticPen(style PenStyle, color Color) (*CosmeticPen, error)
func (*CosmeticPen) Color ¶
func (p *CosmeticPen) Color() Color
func (*CosmeticPen) Dispose ¶
func (p *CosmeticPen) Dispose()
func (*CosmeticPen) Style ¶
func (p *CosmeticPen) Style() PenStyle
func (*CosmeticPen) Width ¶
func (p *CosmeticPen) Width() int
type Cursor ¶
type Cursor interface {
	Dispose()
	// contains filtered or unexported methods
}
func CursorAppStarting ¶
func CursorAppStarting() Cursor
func CursorArrow ¶
func CursorArrow() Cursor
func CursorCross ¶
func CursorCross() Cursor
func CursorHand ¶
func CursorHand() Cursor
func CursorHelp ¶
func CursorHelp() Cursor
func CursorIBeam ¶
func CursorIBeam() Cursor
func CursorIcon ¶
func CursorIcon() Cursor
func CursorNo ¶
func CursorNo() Cursor
func CursorSize ¶
func CursorSize() Cursor
func CursorSizeAll ¶
func CursorSizeAll() Cursor
func CursorSizeNESW ¶
func CursorSizeNESW() Cursor
func CursorSizeNS ¶
func CursorSizeNS() Cursor
func CursorSizeNWSE ¶
func CursorSizeNWSE() Cursor
func CursorSizeWE ¶
func CursorSizeWE() Cursor
func CursorUpArrow ¶
func CursorUpArrow() Cursor
func CursorWait ¶
func CursorWait() Cursor
func NewCursorFromImage ¶
func NewCursorFromImage(im image.Image, hotspot image.Point) (Cursor, error)
type CustomWidget ¶
type CustomWidget struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewCustomWidget
deprecated
func NewCustomWidgetPixels ¶
func NewCustomWidgetPixels(parent Container, style uint, paintPixels PaintFunc) (*CustomWidget, error)
NewCustomWidgetPixels creates and initializes a new custom draw widget.

func (*CustomWidget) ClearsBackground ¶
func (cw *CustomWidget) ClearsBackground() bool
deprecated, use PaintMode

func (*CustomWidget) CreateLayoutItem ¶
func (*CustomWidget) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*CustomWidget) InvalidatesOnResize ¶
func (cw *CustomWidget) InvalidatesOnResize() bool
func (*CustomWidget) PaintMode ¶
func (cw *CustomWidget) PaintMode() PaintMode
func (*CustomWidget) SetClearsBackground ¶
func (cw *CustomWidget) SetClearsBackground(value bool)
deprecated, use SetPaintMode

func (*CustomWidget) SetInvalidatesOnResize ¶
func (cw *CustomWidget) SetInvalidatesOnResize(value bool)
func (*CustomWidget) SetPaintMode ¶
func (cw *CustomWidget) SetPaintMode(value PaintMode)
func (*CustomWidget) WndProc ¶
func (cw *CustomWidget) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type DataBinder ¶
type DataBinder struct {
	// contains filtered or unexported fields
}
func NewDataBinder ¶
func NewDataBinder() *DataBinder
func (*DataBinder) AutoSubmit ¶
func (db *DataBinder) AutoSubmit() bool
func (*DataBinder) AutoSubmitDelay ¶
func (db *DataBinder) AutoSubmitDelay() time.Duration
func (*DataBinder) AutoSubmitSuspended ¶
func (db *DataBinder) AutoSubmitSuspended() bool
func (*DataBinder) BoundWidgets ¶
func (db *DataBinder) BoundWidgets() []Widget
func (*DataBinder) CanSubmit ¶
func (db *DataBinder) CanSubmit() bool
func (*DataBinder) CanSubmitChanged ¶
func (db *DataBinder) CanSubmitChanged() *Event
func (*DataBinder) DataSource ¶
func (db *DataBinder) DataSource() interface{}
func (*DataBinder) DataSourceChanged ¶
func (db *DataBinder) DataSourceChanged() *Event
func (*DataBinder) Dirty ¶
func (db *DataBinder) Dirty() bool
func (*DataBinder) ErrorPresenter ¶
func (db *DataBinder) ErrorPresenter() ErrorPresenter
func (*DataBinder) Expression ¶
func (db *DataBinder) Expression(path string) Expression
func (*DataBinder) Reset ¶
func (db *DataBinder) Reset() error
func (*DataBinder) ResetFinished ¶
func (db *DataBinder) ResetFinished() *Event
func (*DataBinder) SetAutoSubmit ¶
func (db *DataBinder) SetAutoSubmit(autoSubmit bool)
func (*DataBinder) SetAutoSubmitDelay ¶
func (db *DataBinder) SetAutoSubmitDelay(delay time.Duration)
func (*DataBinder) SetAutoSubmitSuspended ¶
func (db *DataBinder) SetAutoSubmitSuspended(suspended bool)
func (*DataBinder) SetBoundWidgets ¶
func (db *DataBinder) SetBoundWidgets(boundWidgets []Widget)
func (*DataBinder) SetDataSource ¶
func (db *DataBinder) SetDataSource(dataSource interface{}) error
func (*DataBinder) SetErrorPresenter ¶
func (db *DataBinder) SetErrorPresenter(ep ErrorPresenter)
func (*DataBinder) Submit ¶
func (db *DataBinder) Submit() error
func (*DataBinder) Submitted ¶
func (db *DataBinder) Submitted() *Event
type DataField ¶
type DataField interface {
	CanSet() bool
	Get() interface{}
	Set(interface{}) error
	Zero() interface{}
}
type DateEdit ¶
type DateEdit struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewDateEdit ¶
func NewDateEdit(parent Container) (*DateEdit, error)
func NewDateEditWithNoneOption ¶
func NewDateEditWithNoneOption(parent Container) (*DateEdit, error)
func (*DateEdit) CreateLayoutItem ¶
func (de *DateEdit) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*DateEdit) Date ¶
func (de *DateEdit) Date() time.Time
func (*DateEdit) DateChanged ¶
func (de *DateEdit) DateChanged() *Event
func (*DateEdit) Format ¶
func (de *DateEdit) Format() string
func (*DateEdit) NeedsWmSize ¶
func (*DateEdit) NeedsWmSize() bool
func (*DateEdit) Range ¶
func (de *DateEdit) Range() (min, max time.Time)
func (*DateEdit) SetDate ¶
func (de *DateEdit) SetDate(date time.Time) error
func (*DateEdit) SetFormat ¶
func (de *DateEdit) SetFormat(format string) error
func (*DateEdit) SetRange ¶
func (de *DateEdit) SetRange(min, max time.Time) error
func (*DateEdit) WndProc ¶
func (de *DateEdit) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type DateLabel ¶
type DateLabel struct {
	// contains filtered or unexported fields
}
func NewDateLabel ¶
func NewDateLabel(parent Container) (*DateLabel, error)
func (*DateLabel) CreateLayoutItem ¶
func (s *DateLabel) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*DateLabel) Date ¶
func (dl *DateLabel) Date() time.Time
func (*DateLabel) Dispose ¶
func (s *DateLabel) Dispose()
func (*DateLabel) Format ¶
func (dl *DateLabel) Format() string
func (*DateLabel) SetDate ¶
func (dl *DateLabel) SetDate(date time.Time) error
func (*DateLabel) SetFormat ¶
func (dl *DateLabel) SetFormat(format string) error
func (*DateLabel) SetTextAlignment ¶
func (dl *DateLabel) SetTextAlignment(alignment Alignment1D) error
func (*DateLabel) SetTextColor ¶
func (s *DateLabel) SetTextColor(c Color)
func (*DateLabel) TextAlignment ¶
func (dl *DateLabel) TextAlignment() Alignment1D
func (*DateLabel) TextColor ¶
func (s *DateLabel) TextColor() Color
func (*DateLabel) WndProc ¶
func (s *DateLabel) WndProc(hwnd win.HWND, msg uint32, wp, lp uintptr) uintptr
type DelegateCondition ¶
type DelegateCondition struct {
	// contains filtered or unexported fields
}
func NewDelegateCondition ¶
func NewDelegateCondition(satisfied func() bool, changed *Event) *DelegateCondition
func (*DelegateCondition) Changed ¶
func (dc *DelegateCondition) Changed() *Event
func (*DelegateCondition) Satisfied ¶
func (dc *DelegateCondition) Satisfied() bool
func (*DelegateCondition) Value ¶
func (dc *DelegateCondition) Value() interface{}
type Dialog ¶
type Dialog struct {
	FormBase
	// contains filtered or unexported fields
}
func NewDialog ¶
func NewDialog(owner Form) (*Dialog, error)
func NewDialogWithFixedSize ¶
func NewDialogWithFixedSize(owner Form) (*Dialog, error)
func (*Dialog) Accept ¶
func (dlg *Dialog) Accept()
func (*Dialog) Cancel ¶
func (dlg *Dialog) Cancel()
func (*Dialog) CancelButton ¶
func (dlg *Dialog) CancelButton() *PushButton
func (*Dialog) Close ¶
func (dlg *Dialog) Close(result int)
func (*Dialog) DefaultButton ¶
func (dlg *Dialog) DefaultButton() *PushButton
func (*Dialog) Result ¶
func (dlg *Dialog) Result() int
func (*Dialog) Run ¶
func (dlg *Dialog) Run() int
func (*Dialog) SetCancelButton ¶
func (dlg *Dialog) SetCancelButton(button *PushButton) error
func (*Dialog) SetDefaultButton ¶
func (dlg *Dialog) SetDefaultButton(button *PushButton) error
func (*Dialog) Show ¶
func (dlg *Dialog) Show()
func (*Dialog) WndProc ¶
func (dlg *Dialog) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type Disposable ¶
type Disposable interface {
	Dispose()
}
type Disposables ¶
type Disposables struct {
	// contains filtered or unexported fields
}
func (*Disposables) Add ¶
func (d *Disposables) Add(item Disposable)
func (*Disposables) Spare ¶
func (d *Disposables) Spare()
func (*Disposables) Treat ¶
func (d *Disposables) Treat()
type DrawTextFormat ¶
type DrawTextFormat uint
DrawText format flags

const (
	TextTop                  DrawTextFormat = win.DT_TOP
	TextLeft                 DrawTextFormat = win.DT_LEFT
	TextCenter               DrawTextFormat = win.DT_CENTER
	TextRight                DrawTextFormat = win.DT_RIGHT
	TextVCenter              DrawTextFormat = win.DT_VCENTER
	TextBottom               DrawTextFormat = win.DT_BOTTOM
	TextWordbreak            DrawTextFormat = win.DT_WORDBREAK
	TextSingleLine           DrawTextFormat = win.DT_SINGLELINE
	TextExpandTabs           DrawTextFormat = win.DT_EXPANDTABS
	TextTabstop              DrawTextFormat = win.DT_TABSTOP
	TextNoClip               DrawTextFormat = win.DT_NOCLIP
	TextExternalLeading      DrawTextFormat = win.DT_EXTERNALLEADING
	TextCalcRect             DrawTextFormat = win.DT_CALCRECT
	TextNoPrefix             DrawTextFormat = win.DT_NOPREFIX
	TextInternal             DrawTextFormat = win.DT_INTERNAL
	TextEditControl          DrawTextFormat = win.DT_EDITCONTROL
	TextPathEllipsis         DrawTextFormat = win.DT_PATH_ELLIPSIS
	TextEndEllipsis          DrawTextFormat = win.DT_END_ELLIPSIS
	TextModifyString         DrawTextFormat = win.DT_MODIFYSTRING
	TextRTLReading           DrawTextFormat = win.DT_RTLREADING
	TextWordEllipsis         DrawTextFormat = win.DT_WORD_ELLIPSIS
	TextNoFullWidthCharBreak DrawTextFormat = win.DT_NOFULLWIDTHCHARBREAK
	TextHidePrefix           DrawTextFormat = win.DT_HIDEPREFIX
	TextPrefixOnly           DrawTextFormat = win.DT_PREFIXONLY
)
type DropFilesEvent ¶
type DropFilesEvent struct {
	// contains filtered or unexported fields
}
func (*DropFilesEvent) Attach ¶
func (e *DropFilesEvent) Attach(handler DropFilesEventHandler) int
func (*DropFilesEvent) Detach ¶
func (e *DropFilesEvent) Detach(handle int)
func (*DropFilesEvent) Once ¶
func (e *DropFilesEvent) Once(handler DropFilesEventHandler)
type DropFilesEventHandler ¶
type DropFilesEventHandler func([]string)
type DropFilesEventPublisher ¶
type DropFilesEventPublisher struct {
	// contains filtered or unexported fields
}
func (*DropFilesEventPublisher) Event ¶
func (p *DropFilesEventPublisher) Event(hWnd win.HWND) *DropFilesEvent
func (*DropFilesEventPublisher) Publish ¶
func (p *DropFilesEventPublisher) Publish(hDrop win.HDROP)
type DropShadowEffect ¶
type DropShadowEffect struct {
	// contains filtered or unexported fields
}
func NewDropShadowEffect ¶
func NewDropShadowEffect(color Color) (*DropShadowEffect, error)
func (*DropShadowEffect) Dispose ¶
func (wgeb *DropShadowEffect) Dispose()
func (*DropShadowEffect) Draw ¶
func (dse *DropShadowEffect) Draw(widget Widget, canvas *Canvas) error
type EllipsisMode ¶
type EllipsisMode int
type Error ¶
type Error struct {
	// contains filtered or unexported fields
}
func (*Error) Error ¶
func (err *Error) Error() string
func (*Error) Inner ¶
func (err *Error) Inner() error
func (*Error) Message ¶
func (err *Error) Message() string
func (*Error) Stack ¶
func (err *Error) Stack() []byte
type ErrorEvent ¶
type ErrorEvent struct {
	// contains filtered or unexported fields
}
func (*ErrorEvent) Attach ¶
func (e *ErrorEvent) Attach(handler ErrorEventHandler) int
func (*ErrorEvent) Detach ¶
func (e *ErrorEvent) Detach(handle int)
func (*ErrorEvent) Once ¶
func (e *ErrorEvent) Once(handler ErrorEventHandler)
type ErrorEventHandler ¶
type ErrorEventHandler func(err error)
type ErrorEventPublisher ¶
type ErrorEventPublisher struct {
	// contains filtered or unexported fields
}
func (*ErrorEventPublisher) Event ¶
func (p *ErrorEventPublisher) Event() *ErrorEvent
func (*ErrorEventPublisher) Publish ¶
func (p *ErrorEventPublisher) Publish(err error)
type ErrorPresenter ¶
type ErrorPresenter interface {
	PresentError(err error, widget Widget)
}
type Event ¶
type Event struct {
	// contains filtered or unexported fields
}
func (*Event) Attach ¶
func (e *Event) Attach(handler EventHandler) int
func (*Event) Detach ¶
func (e *Event) Detach(handle int)
func (*Event) Once ¶
func (e *Event) Once(handler EventHandler)
type EventHandler ¶
type EventHandler func()
type EventPublisher ¶
type EventPublisher struct {
	// contains filtered or unexported fields
}
func (*EventPublisher) Event ¶
func (p *EventPublisher) Event() *Event
func (*EventPublisher) Publish ¶
func (p *EventPublisher) Publish()
type Expression ¶
type Expression interface {
	Value() interface{}
	Changed() *Event
}
func NewReflectExpression ¶
func NewReflectExpression(root Expression, path string) Expression
type ExtractableIcon ¶
type ExtractableIcon interface {
	FilePath_() string
	Index_() int
	Size_() int
}
type FileDialog ¶
type FileDialog struct {
	Title          string
	FilePath       string
	FilePaths      []string
	InitialDirPath string
	Filter         string
	FilterIndex    int
	Flags          uint32
	ShowReadOnlyCB bool
}
func (*FileDialog) ShowBrowseFolder ¶
func (dlg *FileDialog) ShowBrowseFolder(owner Form) (accepted bool, err error)
func (*FileDialog) ShowOpen ¶
func (dlg *FileDialog) ShowOpen(owner Form) (accepted bool, err error)
func (*FileDialog) ShowOpenMultiple ¶
func (dlg *FileDialog) ShowOpenMultiple(owner Form) (accepted bool, err error)
func (*FileDialog) ShowSave ¶
func (dlg *FileDialog) ShowSave(owner Form) (accepted bool, err error)
type FlowLayout ¶
type FlowLayout struct {
	LayoutBase
	// contains filtered or unexported fields
}
func NewFlowLayout ¶
func NewFlowLayout() *FlowLayout
func (*FlowLayout) CreateLayoutItem ¶
func (l *FlowLayout) CreateLayoutItem(ctx *LayoutContext) ContainerLayoutItem
func (*FlowLayout) SetStretchFactor ¶
func (l *FlowLayout) SetStretchFactor(widget Widget, factor int) error
func (*FlowLayout) StretchFactor ¶
func (l *FlowLayout) StretchFactor(widget Widget) int
type Font ¶
type Font struct {
	// contains filtered or unexported fields
}
Font represents a typographic typeface that is used for text drawing operations and on many GUI widgets.

func NewFont ¶
func NewFont(family string, pointSize int, style FontStyle) (*Font, error)
NewFont returns a new Font with the specified attributes.

func (*Font) Bold ¶
func (f *Font) Bold() bool
Bold returns if text drawn using the Font appears with greater weight than normal.

func (*Font) Dispose ¶
func (f *Font) Dispose()
Dispose releases the os resources that were allocated for the Font.

The Font can no longer be used for drawing operations or with GUI widgets after calling this method. It is safe to call Dispose multiple times.

func (*Font) Family ¶
func (f *Font) Family() string
Family returns the family name of the Font.

func (*Font) Italic ¶
func (f *Font) Italic() bool
Italic returns if text drawn using the Font appears slanted.

func (*Font) PointSize ¶
func (f *Font) PointSize() int
PointSize returns the size of the Font in point units.

func (*Font) StrikeOut ¶
func (f *Font) StrikeOut() bool
StrikeOut returns if text drawn using the Font appears striked out.

func (*Font) Style ¶
func (f *Font) Style() FontStyle
Style returns the combination of style flags of the Font.

func (*Font) Underline ¶
func (f *Font) Underline() bool
Underline returns if text drawn using the font appears underlined.

type FontMemResource ¶
type FontMemResource struct {
	// contains filtered or unexported fields
}
FontMemResource represents a font resource loaded into memory from the application's resources.

func NewFontMemResourceById ¶
func NewFontMemResourceById(id int) (*FontMemResource, error)
NewFontMemResourceById function loads a font resource from the executable's resources using the resource ID. The font must be embedded into resources using corresponding operator in the application's RC script.

func NewFontMemResourceByName ¶
func NewFontMemResourceByName(name string) (*FontMemResource, error)
NewFontMemResourceByName function loads a font resource from the executable's resources using the resource name. The font must be embedded into resources using corresponding operator in the application's RC script.

func (*FontMemResource) Dispose ¶
func (fmr *FontMemResource) Dispose()
Dispose removes the font resource from memory

type FontStyle ¶
type FontStyle byte
const (
	FontBold      FontStyle = 0x01
	FontItalic    FontStyle = 0x02
	FontUnderline FontStyle = 0x04
	FontStrikeOut FontStyle = 0x08
)
Font style flags

type Form ¶
type Form interface {
	Container
	AsFormBase() *FormBase
	Run() int
	Starting() *Event
	Closing() *CloseEvent
	Activating() *Event
	Deactivating() *Event
	Activate() error
	Show()
	Hide()
	Title() string
	SetTitle(title string) error
	TitleChanged() *Event
	Icon() Image
	SetIcon(icon Image) error
	IconChanged() *Event
	Owner() Form
	SetOwner(owner Form) error
	ProgressIndicator() *ProgressIndicator

	// RightToLeftLayout returns whether coordinates on the x axis of the
	// Form increase from right to left.
	RightToLeftLayout() bool

	// SetRightToLeftLayout sets whether coordinates on the x axis of the
	// Form increase from right to left.
	SetRightToLeftLayout(rtl bool) error
}
type FormBase ¶
type FormBase struct {
	WindowBase
	// contains filtered or unexported fields
}
func (*FormBase) Activate ¶
func (fb *FormBase) Activate() error
func (*FormBase) Activating ¶
func (fb *FormBase) Activating() *Event
func (*FormBase) ApplySysColors ¶
func (fb *FormBase) ApplySysColors()
func (*FormBase) AsContainerBase ¶
func (fb *FormBase) AsContainerBase() *ContainerBase
func (*FormBase) AsFormBase ¶
func (fb *FormBase) AsFormBase() *FormBase
func (*FormBase) Background ¶
func (fb *FormBase) Background() Brush
func (*FormBase) Children ¶
func (fb *FormBase) Children() *WidgetList
func (*FormBase) Close ¶
func (fb *FormBase) Close() error
func (*FormBase) Closing ¶
func (fb *FormBase) Closing() *CloseEvent
func (*FormBase) ContextMenu ¶
func (fb *FormBase) ContextMenu() *Menu
func (*FormBase) ContextMenuLocation ¶
func (fb *FormBase) ContextMenuLocation() Point
func (*FormBase) DataBinder ¶
func (fb *FormBase) DataBinder() *DataBinder
func (*FormBase) Deactivating ¶
func (fb *FormBase) Deactivating() *Event
func (*FormBase) Dispose ¶
func (fb *FormBase) Dispose()
func (*FormBase) Hide ¶
func (fb *FormBase) Hide()
func (*FormBase) Icon ¶
func (fb *FormBase) Icon() Image
func (*FormBase) IconChanged ¶
func (fb *FormBase) IconChanged() *Event
func (*FormBase) Layout ¶
func (fb *FormBase) Layout() Layout
func (*FormBase) MouseDown ¶
func (fb *FormBase) MouseDown() *MouseEvent
func (*FormBase) MouseMove ¶
func (fb *FormBase) MouseMove() *MouseEvent
func (*FormBase) MouseUp ¶
func (fb *FormBase) MouseUp() *MouseEvent
func (*FormBase) Owner ¶
func (fb *FormBase) Owner() Form
func (*FormBase) Persistent ¶
func (fb *FormBase) Persistent() bool
func (*FormBase) ProgressIndicator ¶
func (fb *FormBase) ProgressIndicator() *ProgressIndicator
func (*FormBase) RestoreState ¶
func (fb *FormBase) RestoreState() error
func (*FormBase) RightToLeftLayout ¶
func (fb *FormBase) RightToLeftLayout() bool
RightToLeftLayout returns whether coordinates on the x axis of the FormBase increase from right to left.

func (*FormBase) Run ¶
func (fb *FormBase) Run() int
func (*FormBase) SaveState ¶
func (fb *FormBase) SaveState() error
func (*FormBase) SetBackground ¶
func (fb *FormBase) SetBackground(background Brush)
func (*FormBase) SetBoundsPixels ¶
func (fb *FormBase) SetBoundsPixels(bounds Rectangle) error
func (*FormBase) SetContextMenu ¶
func (fb *FormBase) SetContextMenu(contextMenu *Menu)
func (*FormBase) SetDataBinder ¶
func (fb *FormBase) SetDataBinder(db *DataBinder)
func (*FormBase) SetIcon ¶
func (fb *FormBase) SetIcon(icon Image) error
func (*FormBase) SetLayout ¶
func (fb *FormBase) SetLayout(value Layout) error
func (*FormBase) SetOwner ¶
func (fb *FormBase) SetOwner(value Form) error
func (*FormBase) SetPersistent ¶
func (fb *FormBase) SetPersistent(value bool)
func (*FormBase) SetRightToLeftLayout ¶
func (fb *FormBase) SetRightToLeftLayout(rtl bool) error
SetRightToLeftLayout sets whether coordinates on the x axis of the FormBase increase from right to left.

func (*FormBase) SetSuspended ¶
func (fb *FormBase) SetSuspended(suspended bool)
func (*FormBase) SetTitle ¶
func (fb *FormBase) SetTitle(value string) error
func (*FormBase) Show ¶
func (fb *FormBase) Show()
func (*FormBase) Starting ¶
func (fb *FormBase) Starting() *Event
func (*FormBase) Title ¶
func (fb *FormBase) Title() string
func (*FormBase) TitleChanged ¶
func (fb *FormBase) TitleChanged() *Event
func (*FormBase) WndProc ¶
func (fb *FormBase) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type GeometricPen ¶
type GeometricPen struct {
	// contains filtered or unexported fields
}
func NewGeometricPen ¶
func NewGeometricPen(style PenStyle, width int, brush Brush) (*GeometricPen, error)
NewGeometricPen prepares new geometric pen. width parameter is specified in 1/96" units.

func (*GeometricPen) Brush ¶
func (p *GeometricPen) Brush() Brush
func (*GeometricPen) Dispose ¶
func (p *GeometricPen) Dispose()
func (*GeometricPen) Style ¶
func (p *GeometricPen) Style() PenStyle
func (*GeometricPen) Width ¶
func (p *GeometricPen) Width() int
Width returns pen width in 1/96" units.

type Geometry ¶
type Geometry struct {
	Alignment                   Alignment2D
	MinSize                     Size // in native pixels
	MaxSize                     Size // in native pixels
	IdealSize                   Size // in native pixels
	Size                        Size // in native pixels
	ClientSize                  Size // in native pixels
	ConsumingSpaceWhenInvisible bool
}
type GradientBrush ¶
type GradientBrush struct {
	// contains filtered or unexported fields
}
func NewGradientBrush ¶
func NewGradientBrush(vertexes []GradientVertex, triangles []GradientTriangle) (*GradientBrush, error)
func NewHorizontalGradientBrush ¶
func NewHorizontalGradientBrush(stops []GradientStop) (*GradientBrush, error)
func NewVerticalGradientBrush ¶
func NewVerticalGradientBrush(stops []GradientStop) (*GradientBrush, error)
func (*GradientBrush) Dispose ¶
func (bb *GradientBrush) Dispose()
type GradientComposite ¶
type GradientComposite struct {
	*Composite
	// contains filtered or unexported fields
}
func NewGradientComposite ¶
func NewGradientComposite(parent Container) (*GradientComposite, error)
func NewGradientCompositeWithStyle ¶
func NewGradientCompositeWithStyle(parent Container, style uint32) (*GradientComposite, error)
func (*GradientComposite) Color1 ¶
func (gc *GradientComposite) Color1() Color
func (*GradientComposite) Color2 ¶
func (gc *GradientComposite) Color2() Color
func (*GradientComposite) Dispose ¶
func (gc *GradientComposite) Dispose()
func (*GradientComposite) SetColor1 ¶
func (gc *GradientComposite) SetColor1(c Color) (err error)
func (*GradientComposite) SetColor2 ¶
func (gc *GradientComposite) SetColor2(c Color) (err error)
func (*GradientComposite) SetVertical ¶
func (gc *GradientComposite) SetVertical(vertical bool) (err error)
func (*GradientComposite) Vertical ¶
func (gc *GradientComposite) Vertical() bool
func (*GradientComposite) WndProc ¶
func (gc *GradientComposite) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type GradientStop ¶
type GradientStop struct {
	Offset float64
	Color  Color
}
type GradientTriangle ¶
type GradientTriangle struct {
	Vertex1 int
	Vertex2 int
	Vertex3 int
}
type GradientVertex ¶
type GradientVertex struct {
	X     float64
	Y     float64
	Color Color
}
type GridLayout ¶
type GridLayout struct {
	LayoutBase
	// contains filtered or unexported fields
}
func NewGridLayout ¶
func NewGridLayout() *GridLayout
func (*GridLayout) ColumnStretchFactor ¶
func (l *GridLayout) ColumnStretchFactor(column int) int
func (*GridLayout) CreateLayoutItem ¶
func (l *GridLayout) CreateLayoutItem(ctx *LayoutContext) ContainerLayoutItem
func (*GridLayout) Range ¶
func (l *GridLayout) Range(widget Widget) (r Rectangle, ok bool)
func (*GridLayout) RowStretchFactor ¶
func (l *GridLayout) RowStretchFactor(row int) int
func (*GridLayout) SetColumnStretchFactor ¶
func (l *GridLayout) SetColumnStretchFactor(column, factor int) error
func (*GridLayout) SetRange ¶
func (l *GridLayout) SetRange(widget Widget, r Rectangle) error
func (*GridLayout) SetRowStretchFactor ¶
func (l *GridLayout) SetRowStretchFactor(row, factor int) error
type GroupBox ¶
type GroupBox struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewGroupBox ¶
func NewGroupBox(parent Container) (*GroupBox, error)
func (*GroupBox) ApplyDPI ¶
func (gb *GroupBox) ApplyDPI(dpi int)
func (*GroupBox) AsContainerBase ¶
func (gb *GroupBox) AsContainerBase() *ContainerBase
func (*GroupBox) Checkable ¶
func (gb *GroupBox) Checkable() bool
func (*GroupBox) Checked ¶
func (gb *GroupBox) Checked() bool
func (*GroupBox) CheckedChanged ¶
func (gb *GroupBox) CheckedChanged() *Event
func (*GroupBox) Children ¶
func (gb *GroupBox) Children() *WidgetList
func (*GroupBox) ClientBoundsPixels ¶
func (gb *GroupBox) ClientBoundsPixels() Rectangle
func (*GroupBox) CreateLayoutItem ¶
func (gb *GroupBox) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*GroupBox) DataBinder ¶
func (gb *GroupBox) DataBinder() *DataBinder
func (*GroupBox) Layout ¶
func (gb *GroupBox) Layout() Layout
func (*GroupBox) MouseDown ¶
func (gb *GroupBox) MouseDown() *MouseEvent
func (*GroupBox) MouseMove ¶
func (gb *GroupBox) MouseMove() *MouseEvent
func (*GroupBox) MouseUp ¶
func (gb *GroupBox) MouseUp() *MouseEvent
func (*GroupBox) Persistent ¶
func (gb *GroupBox) Persistent() bool
func (*GroupBox) RestoreState ¶
func (gb *GroupBox) RestoreState() error
func (*GroupBox) SaveState ¶
func (gb *GroupBox) SaveState() error
func (*GroupBox) SetCheckable ¶
func (gb *GroupBox) SetCheckable(checkable bool)
func (*GroupBox) SetChecked ¶
func (gb *GroupBox) SetChecked(checked bool)
func (*GroupBox) SetDataBinder ¶
func (gb *GroupBox) SetDataBinder(dataBinder *DataBinder)
func (*GroupBox) SetLayout ¶
func (gb *GroupBox) SetLayout(value Layout) error
func (*GroupBox) SetPersistent ¶
func (gb *GroupBox) SetPersistent(value bool)
func (*GroupBox) SetSuspended ¶
func (gb *GroupBox) SetSuspended(suspend bool)
func (*GroupBox) SetTitle ¶
func (gb *GroupBox) SetTitle(title string) error
func (*GroupBox) Title ¶
func (gb *GroupBox) Title() string
func (*GroupBox) WndProc ¶
func (gb *GroupBox) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type HasChilder ¶
type HasChilder interface {
	HasChild() bool
}
HasChilder enables widgets like TreeView to determine if an item has any child, without enforcing to fully count all children.

type HatchBrush ¶
type HatchBrush struct {
	// contains filtered or unexported fields
}
func NewHatchBrush ¶
func NewHatchBrush(color Color, style HatchStyle) (*HatchBrush, error)
func (*HatchBrush) Color ¶
func (b *HatchBrush) Color() Color
func (*HatchBrush) Dispose ¶
func (bb *HatchBrush) Dispose()
func (*HatchBrush) Style ¶
func (b *HatchBrush) Style() HatchStyle
type HatchStyle ¶
type HatchStyle int
const (
	HatchHorizontal       HatchStyle = win.HS_HORIZONTAL
	HatchVertical         HatchStyle = win.HS_VERTICAL
	HatchForwardDiagonal  HatchStyle = win.HS_FDIAGONAL
	HatchBackwardDiagonal HatchStyle = win.HS_BDIAGONAL
	HatchCross            HatchStyle = win.HS_CROSS
	HatchDiagonalCross    HatchStyle = win.HS_DIAGCROSS
)
type HeightForWidther ¶
type HeightForWidther interface {
	HasHeightForWidth() bool

	// HeightForWidth returns appropriate height if element has given width. width parameter and
	// return value are in native pixels.
	HeightForWidth(width int) int
}
type IDProvider ¶
type IDProvider interface {
	ID(index int) interface{}
}
IDProvider is the interface that must be implemented by models to enable widgets like TableView to attempt keeping the current item when the model publishes a reset event.

type Icon ¶
type Icon struct {
	// contains filtered or unexported fields
}
Icon is a bitmap that supports transparency and combining multiple variants of an image in different resolutions.

func IconApplication ¶
func IconApplication() *Icon
func IconError ¶
func IconError() *Icon
func IconFrom ¶
func IconFrom(src interface{}, dpi int) (*Icon, error)
func IconInformation ¶
func IconInformation() *Icon
func IconQuestion ¶
func IconQuestion() *Icon
func IconShield ¶
func IconShield() *Icon
func IconWarning ¶
func IconWarning() *Icon
func IconWinLogo ¶
func IconWinLogo() *Icon
func NewIconExtractedFromFile ¶
func NewIconExtractedFromFile(filePath string, index, _ int) (*Icon, error)
NewIconExtractedFromFile returns a new Icon, as identified by index of size 16x16 from filePath.

func NewIconExtractedFromFileWithSize ¶
func NewIconExtractedFromFileWithSize(filePath string, index, size int) (*Icon, error)
NewIconExtractedFromFileWithSize returns a new Icon, as identified by index of the desired size from filePath.

func NewIconFromBitmap ¶
func NewIconFromBitmap(bmp *Bitmap) (ic *Icon, err error)
NewIconFromBitmap returns a new Icon, using the specified Bitmap as source.

func NewIconFromFile ¶
func NewIconFromFile(filePath string) (*Icon, error)
NewIconFromFile returns a new Icon, using the specified icon image file and default size.

func NewIconFromFileWithSize ¶
func NewIconFromFileWithSize(filePath string, size Size) (*Icon, error)
NewIconFromFileWithSize returns a new Icon, using the specified icon image file and size.

func NewIconFromHICON
deprecated
func NewIconFromHICONForDPI ¶
func NewIconFromHICONForDPI(hIcon win.HICON, dpi int) (ic *Icon, err error)
NewIconFromHICONForDPI returns a new Icon at given DPI, using the specified win.HICON as source.

func NewIconFromImage
deprecated
func NewIconFromImageForDPI ¶
func NewIconFromImageForDPI(im image.Image, dpi int) (ic *Icon, err error)
NewIconFromImageForDPI returns a new Icon at given DPI, using the specified image.Image as source.

func NewIconFromImageWithSize ¶
func NewIconFromImageWithSize(image Image, size Size) (*Icon, error)
NewIconFromImageWithSize returns a new Icon of the given size in native pixels, using the specified Image as source.

func NewIconFromResource ¶
func NewIconFromResource(name string) (*Icon, error)
NewIconFromResource returns a new Icon of default size, using the specified icon resource.

func NewIconFromResourceId ¶
func NewIconFromResourceId(id int) (*Icon, error)
NewIconFromResourceId returns a new Icon of default size, using the specified icon resource.

func NewIconFromResourceIdWithSize ¶
func NewIconFromResourceIdWithSize(id int, size Size) (*Icon, error)
NewIconFromResourceIdWithSize returns a new Icon of size size, using the specified icon resource.

func NewIconFromResourceWithSize ¶
func NewIconFromResourceWithSize(name string, size Size) (*Icon, error)
NewIconFromResourceWithSize returns a new Icon of size size, using the specified icon resource.

func NewIconFromSysDLL ¶
func NewIconFromSysDLL(dllBaseName string, index int) (*Icon, error)
NewIconFromSysDLL returns a new Icon, as identified by index of size 16x16 from the system DLL identified by dllBaseName.

func NewIconFromSysDLLWithSize ¶
func NewIconFromSysDLLWithSize(dllBaseName string, index, size int) (*Icon, error)
NewIconFromSysDLLWithSize returns a new Icon, as identified by index of the desired size from the system DLL identified by dllBaseName.

func (*Icon) Dispose ¶
func (i *Icon) Dispose()
Dispose releases the operating system resources associated with the Icon.

func (*Icon) Size ¶
func (i *Icon) Size() Size
Size returns icon size in 1/96" units.

type IconCache ¶
type IconCache struct {
	// contains filtered or unexported fields
}
func NewIconCache ¶
func NewIconCache() *IconCache
func (*IconCache) Bitmap ¶
func (ic *IconCache) Bitmap(image Image, dpi int) (*Bitmap, error)
func (*IconCache) Clear ¶
func (ic *IconCache) Clear()
func (*IconCache) Dispose ¶
func (ic *IconCache) Dispose()
func (*IconCache) Icon ¶
func (ic *IconCache) Icon(image Image, dpi int) (*Icon, error)
type IdealSizer ¶
type IdealSizer interface {
	// IdealSize returns ideal window size in native pixels.
	IdealSize() Size
}
type Image ¶
type Image interface {
	Dispose()

	// Size returns image size in 1/96" units.
	Size() Size
	// contains filtered or unexported methods
}
func ImageFrom ¶
func ImageFrom(src interface{}) (img Image, err error)
func NewImageFromFile
deprecated
func NewImageFromFileForDPI ¶
func NewImageFromFileForDPI(filePath string, dpi int) (Image, error)
NewImageFromFileForDPI loads image from file at given DPI. Supported types are .ico, .emf, .bmp, .png...

type ImageList ¶
type ImageList struct {
	// contains filtered or unexported fields
}
func NewImageList
deprecated
func NewImageListForDPI ¶
func NewImageListForDPI(imageSize Size, maskColor Color, dpi int) (*ImageList, error)
NewImageListForDPI creates an empty image list for image size at given DPI. imageSize is specified in native pixels.

func (*ImageList) Add ¶
func (il *ImageList) Add(bitmap, maskBitmap *Bitmap) (int, error)
func (*ImageList) AddIcon ¶
func (il *ImageList) AddIcon(icon *Icon) (int32, error)
func (*ImageList) AddImage ¶
func (il *ImageList) AddImage(image interface{}) (int32, error)
func (*ImageList) AddMasked ¶
func (il *ImageList) AddMasked(bitmap *Bitmap) (int32, error)
func (*ImageList) Dispose ¶
func (il *ImageList) Dispose()
func (*ImageList) DrawPixels ¶
func (il *ImageList) DrawPixels(canvas *Canvas, index int, bounds Rectangle) error
func (*ImageList) Handle ¶
func (il *ImageList) Handle() win.HIMAGELIST
func (*ImageList) MaskColor ¶
func (il *ImageList) MaskColor() Color
type ImageProvider ¶
type ImageProvider interface {
	// Image returns the image to display for the item at index index.
	//
	// Supported types are *walk.Bitmap, *walk.Icon and string. A string will be
	// interpreted as a file path and the icon associated with the file will be
	// used. It is not supported to use strings together with the other options
	// in the same model instance.
	Image(index int) interface{}
}
ImageProvider is the interface that a model must implement to support displaying an item image.

type ImageView ¶
type ImageView struct {
	*CustomWidget
	// contains filtered or unexported fields
}
func NewImageView ¶
func NewImageView(parent Container) (*ImageView, error)
func (*ImageView) CreateLayoutItem ¶
func (iv *ImageView) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*ImageView) Image ¶
func (iv *ImageView) Image() Image
func (*ImageView) ImageChanged ¶
func (iv *ImageView) ImageChanged() *Event
func (*ImageView) Margin ¶
func (iv *ImageView) Margin() int
func (*ImageView) MarginChanged ¶
func (iv *ImageView) MarginChanged() *Event
func (*ImageView) Mode ¶
func (iv *ImageView) Mode() ImageViewMode
func (*ImageView) SetImage ¶
func (iv *ImageView) SetImage(image Image) error
func (*ImageView) SetMargin ¶
func (iv *ImageView) SetMargin(margin int) error
func (*ImageView) SetMode ¶
func (iv *ImageView) SetMode(mode ImageViewMode)
type ImageViewMode ¶
type ImageViewMode int
const (
	ImageViewModeIdeal ImageViewMode = iota
	ImageViewModeCorner
	ImageViewModeCenter
	ImageViewModeShrink
	ImageViewModeZoom
	ImageViewModeStretch
)
type Imager ¶
type Imager interface {
	// Image returns the image to display for an item.
	//
	// Supported types are *walk.Bitmap, *walk.Icon and string. A string will be
	// interpreted as a file path and the icon associated with the file will be
	// used. It is not supported to use strings together with the other options
	// in the same model instance.
	Image() interface{}
}
Imager provides access to an image of objects like tree items.

type IniFileSettings ¶
type IniFileSettings struct {
	// contains filtered or unexported fields
}
func NewIniFileSettings ¶
func NewIniFileSettings(fileName string) *IniFileSettings
func (*IniFileSettings) ExpireDuration ¶
func (ifs *IniFileSettings) ExpireDuration() time.Duration
func (*IniFileSettings) FilePath ¶
func (ifs *IniFileSettings) FilePath() string
func (*IniFileSettings) Get ¶
func (ifs *IniFileSettings) Get(key string) (string, bool)
func (*IniFileSettings) Load ¶
func (ifs *IniFileSettings) Load() error
func (*IniFileSettings) Portable ¶
func (ifs *IniFileSettings) Portable() bool
func (*IniFileSettings) Put ¶
func (ifs *IniFileSettings) Put(key, value string) error
func (*IniFileSettings) PutExpiring ¶
func (ifs *IniFileSettings) PutExpiring(key, value string) error
func (*IniFileSettings) Remove ¶
func (ifs *IniFileSettings) Remove(key string) error
func (*IniFileSettings) Save ¶
func (ifs *IniFileSettings) Save() error
func (*IniFileSettings) SetExpireDuration ¶
func (ifs *IniFileSettings) SetExpireDuration(expireDuration time.Duration)
func (*IniFileSettings) SetPortable ¶
func (ifs *IniFileSettings) SetPortable(portable bool)
func (*IniFileSettings) Timestamp ¶
func (ifs *IniFileSettings) Timestamp(key string) (time.Time, bool)
type IntEvent ¶
type IntEvent struct {
	// contains filtered or unexported fields
}
func (*IntEvent) Attach ¶
func (e *IntEvent) Attach(handler IntEventHandler) int
func (*IntEvent) Detach ¶
func (e *IntEvent) Detach(handle int)
func (*IntEvent) Once ¶
func (e *IntEvent) Once(handler IntEventHandler)
type IntEventHandler ¶
type IntEventHandler func(n int)
type IntEventPublisher ¶
type IntEventPublisher struct {
	// contains filtered or unexported fields
}
func (*IntEventPublisher) Event ¶
func (p *IntEventPublisher) Event() *IntEvent
func (*IntEventPublisher) Publish ¶
func (p *IntEventPublisher) Publish(n int)
type IntRangeEvent ¶
type IntRangeEvent struct {
	// contains filtered or unexported fields
}
func (*IntRangeEvent) Attach ¶
func (e *IntRangeEvent) Attach(handler IntRangeEventHandler) int
func (*IntRangeEvent) Detach ¶
func (e *IntRangeEvent) Detach(handle int)
func (*IntRangeEvent) Once ¶
func (e *IntRangeEvent) Once(handler IntRangeEventHandler)
type IntRangeEventHandler ¶
type IntRangeEventHandler func(from, to int)
type IntRangeEventPublisher ¶
type IntRangeEventPublisher struct {
	// contains filtered or unexported fields
}
func (*IntRangeEventPublisher) Event ¶
func (p *IntRangeEventPublisher) Event() *IntRangeEvent
func (*IntRangeEventPublisher) Publish ¶
func (p *IntRangeEventPublisher) Publish(from, to int)
type ItemChecker ¶
type ItemChecker interface {
	// Checked returns if the specified item is checked.
	Checked(index int) bool

	// SetChecked sets if the specified item is checked.
	SetChecked(index int, checked bool) error
}
ItemChecker is the interface that a model must implement to support check boxes in a widget like TableView.

type Key ¶
type Key uint16
const (
	KeyLButton           Key = win.VK_LBUTTON
	KeyRButton           Key = win.VK_RBUTTON
	KeyCancel            Key = win.VK_CANCEL
	KeyMButton           Key = win.VK_MBUTTON
	KeyXButton1          Key = win.VK_XBUTTON1
	KeyXButton2          Key = win.VK_XBUTTON2
	KeyBack              Key = win.VK_BACK
	KeyTab               Key = win.VK_TAB
	KeyClear             Key = win.VK_CLEAR
	KeyReturn            Key = win.VK_RETURN
	KeyShift             Key = win.VK_SHIFT
	KeyControl           Key = win.VK_CONTROL
	KeyAlt               Key = win.VK_MENU
	KeyMenu              Key = win.VK_MENU
	KeyPause             Key = win.VK_PAUSE
	KeyCapital           Key = win.VK_CAPITAL
	KeyKana              Key = win.VK_KANA
	KeyHangul            Key = win.VK_HANGUL
	KeyJunja             Key = win.VK_JUNJA
	KeyFinal             Key = win.VK_FINAL
	KeyHanja             Key = win.VK_HANJA
	KeyKanji             Key = win.VK_KANJI
	KeyEscape            Key = win.VK_ESCAPE
	KeyConvert           Key = win.VK_CONVERT
	KeyNonconvert        Key = win.VK_NONCONVERT
	KeyAccept            Key = win.VK_ACCEPT
	KeyModeChange        Key = win.VK_MODECHANGE
	KeySpace             Key = win.VK_SPACE
	KeyPrior             Key = win.VK_PRIOR
	KeyNext              Key = win.VK_NEXT
	KeyEnd               Key = win.VK_END
	KeyHome              Key = win.VK_HOME
	KeyLeft              Key = win.VK_LEFT
	KeyUp                Key = win.VK_UP
	KeyRight             Key = win.VK_RIGHT
	KeyDown              Key = win.VK_DOWN
	KeySelect            Key = win.VK_SELECT
	KeyPrint             Key = win.VK_PRINT
	KeyExecute           Key = win.VK_EXECUTE
	KeySnapshot          Key = win.VK_SNAPSHOT
	KeyInsert            Key = win.VK_INSERT
	KeyDelete            Key = win.VK_DELETE
	KeyHelp              Key = win.VK_HELP
	Key0                 Key = 0x30
	Key1                 Key = 0x31
	Key2                 Key = 0x32
	Key3                 Key = 0x33
	Key4                 Key = 0x34
	Key5                 Key = 0x35
	Key6                 Key = 0x36
	Key7                 Key = 0x37
	Key8                 Key = 0x38
	Key9                 Key = 0x39
	KeyA                 Key = 0x41
	KeyB                 Key = 0x42
	KeyC                 Key = 0x43
	KeyD                 Key = 0x44
	KeyE                 Key = 0x45
	KeyF                 Key = 0x46
	KeyG                 Key = 0x47
	KeyH                 Key = 0x48
	KeyI                 Key = 0x49
	KeyJ                 Key = 0x4A
	KeyK                 Key = 0x4B
	KeyL                 Key = 0x4C
	KeyM                 Key = 0x4D
	KeyN                 Key = 0x4E
	KeyO                 Key = 0x4F
	KeyP                 Key = 0x50
	KeyQ                 Key = 0x51
	KeyR                 Key = 0x52
	KeyS                 Key = 0x53
	KeyT                 Key = 0x54
	KeyU                 Key = 0x55
	KeyV                 Key = 0x56
	KeyW                 Key = 0x57
	KeyX                 Key = 0x58
	KeyY                 Key = 0x59
	KeyZ                 Key = 0x5A
	KeyLWin              Key = win.VK_LWIN
	KeyRWin              Key = win.VK_RWIN
	KeyApps              Key = win.VK_APPS
	KeySleep             Key = win.VK_SLEEP
	KeyNumpad0           Key = win.VK_NUMPAD0
	KeyNumpad1           Key = win.VK_NUMPAD1
	KeyNumpad2           Key = win.VK_NUMPAD2
	KeyNumpad3           Key = win.VK_NUMPAD3
	KeyNumpad4           Key = win.VK_NUMPAD4
	KeyNumpad5           Key = win.VK_NUMPAD5
	KeyNumpad6           Key = win.VK_NUMPAD6
	KeyNumpad7           Key = win.VK_NUMPAD7
	KeyNumpad8           Key = win.VK_NUMPAD8
	KeyNumpad9           Key = win.VK_NUMPAD9
	KeyMultiply          Key = win.VK_MULTIPLY
	KeyAdd               Key = win.VK_ADD
	KeySeparator         Key = win.VK_SEPARATOR
	KeySubtract          Key = win.VK_SUBTRACT
	KeyDecimal           Key = win.VK_DECIMAL
	KeyDivide            Key = win.VK_DIVIDE
	KeyF1                Key = win.VK_F1
	KeyF2                Key = win.VK_F2
	KeyF3                Key = win.VK_F3
	KeyF4                Key = win.VK_F4
	KeyF5                Key = win.VK_F5
	KeyF6                Key = win.VK_F6
	KeyF7                Key = win.VK_F7
	KeyF8                Key = win.VK_F8
	KeyF9                Key = win.VK_F9
	KeyF10               Key = win.VK_F10
	KeyF11               Key = win.VK_F11
	KeyF12               Key = win.VK_F12
	KeyF13               Key = win.VK_F13
	KeyF14               Key = win.VK_F14
	KeyF15               Key = win.VK_F15
	KeyF16               Key = win.VK_F16
	KeyF17               Key = win.VK_F17
	KeyF18               Key = win.VK_F18
	KeyF19               Key = win.VK_F19
	KeyF20               Key = win.VK_F20
	KeyF21               Key = win.VK_F21
	KeyF22               Key = win.VK_F22
	KeyF23               Key = win.VK_F23
	KeyF24               Key = win.VK_F24
	KeyNumlock           Key = win.VK_NUMLOCK
	KeyScroll            Key = win.VK_SCROLL
	KeyLShift            Key = win.VK_LSHIFT
	KeyRShift            Key = win.VK_RSHIFT
	KeyLControl          Key = win.VK_LCONTROL
	KeyRControl          Key = win.VK_RCONTROL
	KeyLAlt              Key = win.VK_LMENU
	KeyLMenu             Key = win.VK_LMENU
	KeyRAlt              Key = win.VK_RMENU
	KeyRMenu             Key = win.VK_RMENU
	KeyBrowserBack       Key = win.VK_BROWSER_BACK
	KeyBrowserForward    Key = win.VK_BROWSER_FORWARD
	KeyBrowserRefresh    Key = win.VK_BROWSER_REFRESH
	KeyBrowserStop       Key = win.VK_BROWSER_STOP
	KeyBrowserSearch     Key = win.VK_BROWSER_SEARCH
	KeyBrowserFavorites  Key = win.VK_BROWSER_FAVORITES
	KeyBrowserHome       Key = win.VK_BROWSER_HOME
	KeyVolumeMute        Key = win.VK_VOLUME_MUTE
	KeyVolumeDown        Key = win.VK_VOLUME_DOWN
	KeyVolumeUp          Key = win.VK_VOLUME_UP
	KeyMediaNextTrack    Key = win.VK_MEDIA_NEXT_TRACK
	KeyMediaPrevTrack    Key = win.VK_MEDIA_PREV_TRACK
	KeyMediaStop         Key = win.VK_MEDIA_STOP
	KeyMediaPlayPause    Key = win.VK_MEDIA_PLAY_PAUSE
	KeyLaunchMail        Key = win.VK_LAUNCH_MAIL
	KeyLaunchMediaSelect Key = win.VK_LAUNCH_MEDIA_SELECT
	KeyLaunchApp1        Key = win.VK_LAUNCH_APP1
	KeyLaunchApp2        Key = win.VK_LAUNCH_APP2
	KeyOEM1              Key = win.VK_OEM_1
	KeyOEMPlus           Key = win.VK_OEM_PLUS
	KeyOEMComma          Key = win.VK_OEM_COMMA
	KeyOEMMinus          Key = win.VK_OEM_MINUS
	KeyOEMPeriod         Key = win.VK_OEM_PERIOD
	KeyOEM2              Key = win.VK_OEM_2
	KeyOEM3              Key = win.VK_OEM_3
	KeyOEM4              Key = win.VK_OEM_4
	KeyOEM5              Key = win.VK_OEM_5
	KeyOEM6              Key = win.VK_OEM_6
	KeyOEM7              Key = win.VK_OEM_7
	KeyOEM8              Key = win.VK_OEM_8
	KeyOEM102            Key = win.VK_OEM_102
	KeyProcessKey        Key = win.VK_PROCESSKEY
	KeyPacket            Key = win.VK_PACKET
	KeyAttn              Key = win.VK_ATTN
	KeyCRSel             Key = win.VK_CRSEL
	KeyEXSel             Key = win.VK_EXSEL
	KeyErEOF             Key = win.VK_EREOF
	KeyPlay              Key = win.VK_PLAY
	KeyZoom              Key = win.VK_ZOOM
	KeyNoName            Key = win.VK_NONAME
	KeyPA1               Key = win.VK_PA1
	KeyOEMClear          Key = win.VK_OEM_CLEAR
)
func (Key) String ¶
func (k Key) String() string
type KeyEvent ¶
type KeyEvent struct {
	// contains filtered or unexported fields
}
func (*KeyEvent) Attach ¶
func (e *KeyEvent) Attach(handler KeyEventHandler) int
func (*KeyEvent) Detach ¶
func (e *KeyEvent) Detach(handle int)
func (*KeyEvent) Once ¶
func (e *KeyEvent) Once(handler KeyEventHandler)
type KeyEventHandler ¶
type KeyEventHandler func(key Key)
type KeyEventPublisher ¶
type KeyEventPublisher struct {
	// contains filtered or unexported fields
}
func (*KeyEventPublisher) Event ¶
func (p *KeyEventPublisher) Event() *KeyEvent
func (*KeyEventPublisher) Publish ¶
func (p *KeyEventPublisher) Publish(key Key)
type Label ¶
type Label struct {
	// contains filtered or unexported fields
}
func NewLabel ¶
func NewLabel(parent Container) (*Label, error)
func NewLabelWithStyle ¶
func NewLabelWithStyle(parent Container, style uint32) (*Label, error)
func (*Label) CreateLayoutItem ¶
func (s *Label) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*Label) Dispose ¶
func (s *Label) Dispose()
func (*Label) EllipsisMode ¶
func (l *Label) EllipsisMode() EllipsisMode
func (*Label) SetEllipsisMode ¶
func (l *Label) SetEllipsisMode(mode EllipsisMode) error
func (*Label) SetText ¶
func (l *Label) SetText(text string) error
func (*Label) SetTextAlignment ¶
func (l *Label) SetTextAlignment(alignment Alignment1D) error
func (*Label) SetTextColor ¶
func (s *Label) SetTextColor(c Color)
func (*Label) Text ¶
func (l *Label) Text() string
func (*Label) TextAlignment ¶
func (l *Label) TextAlignment() Alignment1D
func (*Label) TextColor ¶
func (s *Label) TextColor() Color
func (*Label) WndProc ¶
func (s *Label) WndProc(hwnd win.HWND, msg uint32, wp, lp uintptr) uintptr
type Layout ¶
type Layout interface {
	Container() Container
	SetContainer(value Container)
	Margins() Margins
	SetMargins(value Margins) error
	Spacing() int
	SetSpacing(value int) error
	CreateLayoutItem(ctx *LayoutContext) ContainerLayoutItem
	// contains filtered or unexported methods
}
type LayoutBase ¶
type LayoutBase struct {
	// contains filtered or unexported fields
}
func (*LayoutBase) Alignment ¶
func (l *LayoutBase) Alignment() Alignment2D
func (*LayoutBase) Container ¶
func (l *LayoutBase) Container() Container
func (*LayoutBase) Margins ¶
func (l *LayoutBase) Margins() Margins
func (*LayoutBase) SetAlignment ¶
func (l *LayoutBase) SetAlignment(alignment Alignment2D) error
func (*LayoutBase) SetContainer ¶
func (l *LayoutBase) SetContainer(value Container)
func (*LayoutBase) SetMargins ¶
func (l *LayoutBase) SetMargins(value Margins) error
func (*LayoutBase) SetSpacing ¶
func (l *LayoutBase) SetSpacing(value int) error
func (*LayoutBase) Spacing ¶
func (l *LayoutBase) Spacing() int
type LayoutContext ¶
type LayoutContext struct {
	// contains filtered or unexported fields
}
func (*LayoutContext) DPI ¶
func (ctx *LayoutContext) DPI() int
type LayoutFlags ¶
type LayoutFlags byte
LayoutFlags specify how a Widget wants to be treated when used with a Layout.

These flags are interpreted in respect to Widget.SizeHint.

const (
	// ShrinkableHorz allows a Widget to be shrunk horizontally.
	ShrinkableHorz LayoutFlags = 1 << iota

	// ShrinkableVert allows a Widget to be shrunk vertically.
	ShrinkableVert

	// GrowableHorz allows a Widget to be enlarged horizontally.
	GrowableHorz

	// GrowableVert allows a Widget to be enlarged vertically.
	GrowableVert

	// GreedyHorz specifies that the widget prefers to take up as much space as
	// possible, horizontally.
	GreedyHorz

	// GreedyVert specifies that the widget prefers to take up as much space as
	// possible, vertically.
	GreedyVert
)
type LayoutItem ¶
type LayoutItem interface {
	AsLayoutItemBase() *LayoutItemBase
	Context() *LayoutContext
	Handle() win.HWND
	Geometry() *Geometry
	Parent() ContainerLayoutItem
	Visible() bool
	LayoutFlags() LayoutFlags
}
func NewGreedyLayoutItem ¶
func NewGreedyLayoutItem() LayoutItem
type LayoutItemBase ¶
type LayoutItemBase struct {
	// contains filtered or unexported fields
}
func (*LayoutItemBase) AsLayoutItemBase ¶
func (lib *LayoutItemBase) AsLayoutItemBase() *LayoutItemBase
func (*LayoutItemBase) Context ¶
func (lib *LayoutItemBase) Context() *LayoutContext
func (*LayoutItemBase) Geometry ¶
func (lib *LayoutItemBase) Geometry() *Geometry
func (*LayoutItemBase) Handle ¶
func (lib *LayoutItemBase) Handle() win.HWND
func (*LayoutItemBase) Parent ¶
func (lib *LayoutItemBase) Parent() ContainerLayoutItem
func (*LayoutItemBase) Visible ¶
func (lib *LayoutItemBase) Visible() bool
type LayoutResult ¶
type LayoutResult struct {
	// contains filtered or unexported fields
}
type LayoutResultItem ¶
type LayoutResultItem struct {
	Item   LayoutItem
	Bounds Rectangle // in native pixels
}
type LineEdit ¶
type LineEdit struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewLineEdit ¶
func NewLineEdit(parent Container) (*LineEdit, error)
func (*LineEdit) CaseMode ¶
func (le *LineEdit) CaseMode() CaseMode
func (*LineEdit) CreateLayoutItem ¶
func (le *LineEdit) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*LineEdit) CueBanner ¶
func (le *LineEdit) CueBanner() string
func (*LineEdit) EditingFinished ¶
func (le *LineEdit) EditingFinished() *Event
func (*LineEdit) MaxLength ¶
func (le *LineEdit) MaxLength() int
func (*LineEdit) NeedsWmSize ¶
func (*LineEdit) NeedsWmSize() bool
func (*LineEdit) PasswordMode ¶
func (le *LineEdit) PasswordMode() bool
func (*LineEdit) ReadOnly ¶
func (le *LineEdit) ReadOnly() bool
func (*LineEdit) SetCaseMode ¶
func (le *LineEdit) SetCaseMode(mode CaseMode) error
func (*LineEdit) SetCueBanner ¶
func (le *LineEdit) SetCueBanner(value string) error
func (*LineEdit) SetMaxLength ¶
func (le *LineEdit) SetMaxLength(value int)
func (*LineEdit) SetPasswordMode ¶
func (le *LineEdit) SetPasswordMode(value bool)
func (*LineEdit) SetReadOnly ¶
func (le *LineEdit) SetReadOnly(readOnly bool) error
func (*LineEdit) SetText ¶
func (le *LineEdit) SetText(value string) error
func (*LineEdit) SetTextAlignment ¶
func (le *LineEdit) SetTextAlignment(alignment Alignment1D) error
func (*LineEdit) SetTextColor ¶
func (le *LineEdit) SetTextColor(c Color)
func (*LineEdit) SetTextSelection ¶
func (le *LineEdit) SetTextSelection(start, end int)
func (*LineEdit) Text ¶
func (le *LineEdit) Text() string
func (*LineEdit) TextAlignment ¶
func (le *LineEdit) TextAlignment() Alignment1D
func (*LineEdit) TextChanged ¶
func (le *LineEdit) TextChanged() *Event
func (*LineEdit) TextColor ¶
func (le *LineEdit) TextColor() Color
func (*LineEdit) TextSelection ¶
func (le *LineEdit) TextSelection() (start, end int)
func (*LineEdit) WndProc ¶
func (le *LineEdit) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type LinkLabel ¶
type LinkLabel struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewLinkLabel ¶
func NewLinkLabel(parent Container) (*LinkLabel, error)
func (*LinkLabel) CreateLayoutItem ¶
func (ll *LinkLabel) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*LinkLabel) LinkActivated ¶
func (ll *LinkLabel) LinkActivated() *LinkLabelLinkEvent
func (*LinkLabel) SetText ¶
func (ll *LinkLabel) SetText(value string) error
func (*LinkLabel) Text ¶
func (ll *LinkLabel) Text() string
func (*LinkLabel) WndProc ¶
func (ll *LinkLabel) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type LinkLabelLink ¶
type LinkLabelLink struct {
	// contains filtered or unexported fields
}
func (*LinkLabelLink) Enabled ¶
func (lll *LinkLabelLink) Enabled() (bool, error)
func (*LinkLabelLink) Focused ¶
func (lll *LinkLabelLink) Focused() (bool, error)
func (*LinkLabelLink) Id ¶
func (lll *LinkLabelLink) Id() string
func (*LinkLabelLink) Index ¶
func (lll *LinkLabelLink) Index() int
func (*LinkLabelLink) SetEnabled ¶
func (lll *LinkLabelLink) SetEnabled(enabled bool) error
func (*LinkLabelLink) SetFocused ¶
func (lll *LinkLabelLink) SetFocused(focused bool) error
func (*LinkLabelLink) SetVisited ¶
func (lll *LinkLabelLink) SetVisited(visited bool) error
func (*LinkLabelLink) URL ¶
func (lll *LinkLabelLink) URL() string
func (*LinkLabelLink) Visited ¶
func (lll *LinkLabelLink) Visited() (bool, error)
type LinkLabelLinkEvent ¶
type LinkLabelLinkEvent struct {
	// contains filtered or unexported fields
}
func (*LinkLabelLinkEvent) Attach ¶
func (e *LinkLabelLinkEvent) Attach(handler LinkLabelLinkEventHandler) int
func (*LinkLabelLinkEvent) Detach ¶
func (e *LinkLabelLinkEvent) Detach(handle int)
type LinkLabelLinkEventHandler ¶
type LinkLabelLinkEventHandler func(link *LinkLabelLink)
type LinkLabelLinkEventPublisher ¶
type LinkLabelLinkEventPublisher struct {
	// contains filtered or unexported fields
}
func (*LinkLabelLinkEventPublisher) Event ¶
func (p *LinkLabelLinkEventPublisher) Event() *LinkLabelLinkEvent
func (*LinkLabelLinkEventPublisher) Publish ¶
func (p *LinkLabelLinkEventPublisher) Publish(link *LinkLabelLink)
type ListBox ¶
type ListBox struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewListBox ¶
func NewListBox(parent Container) (*ListBox, error)
func NewListBoxWithStyle ¶
func NewListBoxWithStyle(parent Container, style uint32) (*ListBox, error)
func (*ListBox) ApplyDPI ¶
func (lb *ListBox) ApplyDPI(dpi int)
func (*ListBox) ApplySysColors ¶
func (lb *ListBox) ApplySysColors()
func (*ListBox) BindingMember ¶
func (lb *ListBox) BindingMember() string
BindingMember returns the member from the model of the ListBox that is bound to a field of the data source managed by an associated DataBinder.

This is only applicable to walk.ReflectListModel models and simple slices of pointers to struct.

func (*ListBox) CreateLayoutItem ¶
func (lb *ListBox) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*ListBox) CurrentIndex ¶
func (lb *ListBox) CurrentIndex() int
func (*ListBox) CurrentIndexChanged ¶
func (lb *ListBox) CurrentIndexChanged() *Event
func (*ListBox) DisplayMember ¶
func (lb *ListBox) DisplayMember() string
DisplayMember returns the member from the model of the ListBox that is displayed in the ListBox.

This is only applicable to walk.ReflectListModel models and simple slices of pointers to struct.

func (*ListBox) EnsureItemVisible ¶
func (lb *ListBox) EnsureItemVisible(index int)
func (*ListBox) Format ¶
func (lb *ListBox) Format() string
func (*ListBox) ItemActivated ¶
func (lb *ListBox) ItemActivated() *Event
func (*ListBox) ItemStyler ¶
func (lb *ListBox) ItemStyler() ListItemStyler
func (*ListBox) ItemVisible ¶
func (lb *ListBox) ItemVisible(index int) bool
func (*ListBox) LayoutFlags ¶
func (*ListBox) LayoutFlags() LayoutFlags
func (*ListBox) Model ¶
func (lb *ListBox) Model() interface{}
Model returns the model of the ListBox.

func (*ListBox) Precision ¶
func (lb *ListBox) Precision() int
func (*ListBox) SelectedIndexes ¶
func (lb *ListBox) SelectedIndexes() []int
func (*ListBox) SelectedIndexesChanged ¶
func (lb *ListBox) SelectedIndexesChanged() *Event
func (*ListBox) SetBindingMember ¶
func (lb *ListBox) SetBindingMember(bindingMember string) error
SetBindingMember sets the member from the model of the ListBox that is bound to a field of the data source managed by an associated DataBinder.

This is only applicable to walk.ReflectListModel models and simple slices of pointers to struct.

For a model consisting of items of type S, data source field of type T and bindingMember "Foo", this can be one of the following:

A field		Foo T
A method	func (s S) Foo() T
A method	func (s S) Foo() (T, error)
If bindingMember is not a simple member name like "Foo", but a path to a member like "A.B.Foo", members "A" and "B" both must be one of the options mentioned above, but with T having type pointer to struct.

func (*ListBox) SetCurrentIndex ¶
func (lb *ListBox) SetCurrentIndex(value int) error
func (*ListBox) SetDisplayMember ¶
func (lb *ListBox) SetDisplayMember(displayMember string) error
SetDisplayMember sets the member from the model of the ListBox that is displayed in the ListBox.

This is only applicable to walk.ReflectListModel models and simple slices of pointers to struct.

For a model consisting of items of type S, the type of the specified member T and displayMember "Foo", this can be one of the following:

A field		Foo T
A method	func (s S) Foo() T
A method	func (s S) Foo() (T, error)
If displayMember is not a simple member name like "Foo", but a path to a member like "A.B.Foo", members "A" and "B" both must be one of the options mentioned above, but with T having type pointer to struct.

func (*ListBox) SetFormat ¶
func (lb *ListBox) SetFormat(value string)
func (*ListBox) SetItemStyler ¶
func (lb *ListBox) SetItemStyler(styler ListItemStyler)
func (*ListBox) SetModel ¶
func (lb *ListBox) SetModel(mdl interface{}) error
SetModel sets the model of the ListBox.

It is required that mdl either implements walk.ListModel or walk.ReflectListModel or be a slice of pointers to struct or a []string.

func (*ListBox) SetPrecision ¶
func (lb *ListBox) SetPrecision(value int)
func (*ListBox) SetSelectedIndexes ¶
func (lb *ListBox) SetSelectedIndexes(indexes []int)
func (*ListBox) WndProc ¶
func (lb *ListBox) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type ListItemStyle ¶
type ListItemStyle struct {
	BackgroundColor Color
	TextColor       Color

	LineColor Color
	Font      *Font
	// contains filtered or unexported fields
}
ListItemStyle carries information about the display style of an item in a list widget like ListBox.

func (*ListItemStyle) Bounds ¶
func (lis *ListItemStyle) Bounds() Rectangle
func (*ListItemStyle) BoundsPixels ¶
func (lis *ListItemStyle) BoundsPixels() Rectangle
func (*ListItemStyle) Canvas ¶
func (lis *ListItemStyle) Canvas() *Canvas
func (*ListItemStyle) DrawBackground ¶
func (lis *ListItemStyle) DrawBackground() error
func (*ListItemStyle) DrawText ¶
func (lis *ListItemStyle) DrawText(text string, bounds Rectangle, format DrawTextFormat) error
DrawText draws text inside given bounds specified in native pixels.

func (*ListItemStyle) Index ¶
func (lis *ListItemStyle) Index() int
type ListItemStyler ¶
type ListItemStyler interface {
	// ItemHeightDependsOnWidth returns whether item height depends on width.
	ItemHeightDependsOnWidth() bool

	// DefaultItemHeight returns the initial height in native pixels for any item.
	DefaultItemHeight() int

	// ItemHeight is called for each item to retrieve the height of the item. width parameter and
	// return value are specified in native pixels.
	ItemHeight(index int, width int) int

	// StyleItem is called for each item to pick up item style information.
	StyleItem(style *ListItemStyle)
}
ListItemStyler is the interface that must be implemented to provide a list widget like ListBox with item display style information.

type ListModel ¶
type ListModel interface {
	// ItemCount returns the number of items in the model.
	ItemCount() int

	// Value returns the value that should be displayed for the given index.
	Value(index int) interface{}

	// ItemsReset returns the event that the model should publish when the
	// number of its items changes.
	ItemsReset() *Event

	// ItemChanged returns the event that the model should publish when an item
	// was changed.
	ItemChanged() *IntEvent

	// ItemsInserted returns the event that the model should publish when a
	// contiguous range of items was inserted.
	ItemsInserted() *IntRangeEvent

	// ItemsRemoved returns the event that the model should publish when a
	// contiguous range of items was removed.
	ItemsRemoved() *IntRangeEvent
}
ListModel is the interface that a model must implement to support widgets like ComboBox.

type ListModelBase ¶
type ListModelBase struct {
	// contains filtered or unexported fields
}
ListModelBase implements the ItemsReset and ItemChanged methods of the ListModel interface.

func (*ListModelBase) ItemChanged ¶
func (lmb *ListModelBase) ItemChanged() *IntEvent
func (*ListModelBase) ItemsInserted ¶
func (lmb *ListModelBase) ItemsInserted() *IntRangeEvent
func (*ListModelBase) ItemsRemoved ¶
func (lmb *ListModelBase) ItemsRemoved() *IntRangeEvent
func (*ListModelBase) ItemsReset ¶
func (lmb *ListModelBase) ItemsReset() *Event
func (*ListModelBase) PublishItemChanged ¶
func (lmb *ListModelBase) PublishItemChanged(index int)
func (*ListModelBase) PublishItemsInserted ¶
func (lmb *ListModelBase) PublishItemsInserted(from, to int)
func (*ListModelBase) PublishItemsRemoved ¶
func (lmb *ListModelBase) PublishItemsRemoved(from, to int)
func (*ListModelBase) PublishItemsReset ¶
func (lmb *ListModelBase) PublishItemsReset()
type MainWindow ¶
type MainWindow struct {
	FormBase
	// contains filtered or unexported fields
}
func NewMainWindow ¶
func NewMainWindow() (*MainWindow, error)
func NewMainWindowWithCfg ¶
func NewMainWindowWithCfg(cfg *MainWindowCfg) (*MainWindow, error)
func NewMainWindowWithName ¶
func NewMainWindowWithName(name string) (*MainWindow, error)
func (*MainWindow) ClientBoundsPixels ¶
func (mw *MainWindow) ClientBoundsPixels() Rectangle
func (*MainWindow) Fullscreen ¶
func (mw *MainWindow) Fullscreen() bool
func (*MainWindow) Menu ¶
func (mw *MainWindow) Menu() *Menu
func (*MainWindow) SetFullscreen ¶
func (mw *MainWindow) SetFullscreen(fullscreen bool) error
func (*MainWindow) SetToolBar ¶
func (mw *MainWindow) SetToolBar(tb *ToolBar)
func (*MainWindow) SetVisible ¶
func (mw *MainWindow) SetVisible(visible bool)
func (*MainWindow) StatusBar ¶
func (mw *MainWindow) StatusBar() *StatusBar
func (*MainWindow) ToolBar ¶
func (mw *MainWindow) ToolBar() *ToolBar
func (*MainWindow) WndProc ¶
func (mw *MainWindow) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type MainWindowCfg ¶
type MainWindowCfg struct {
	Name   string
	Bounds Rectangle
}
type Margins ¶
type Margins struct {
	HNear, VNear, HFar, VFar int
}
Margins define margins in 1/96" units or native pixels.

func MarginsFrom96DPI ¶
func MarginsFrom96DPI(value Margins, dpi int) Margins
MarginsFrom96DPI converts from 1/96" units to native pixels.

func MarginsTo96DPI ¶
func MarginsTo96DPI(value Margins, dpi int) Margins
MarginsTo96DPI converts from native pixels to 1/96" units.

type Menu ¶
type Menu struct {
	// contains filtered or unexported fields
}
func NewMenu ¶
func NewMenu() (*Menu, error)
func (*Menu) Actions ¶
func (m *Menu) Actions() *ActionList
func (*Menu) Dispose ¶
func (m *Menu) Dispose()
func (*Menu) IsDisposed ¶
func (m *Menu) IsDisposed() bool
type Metafile ¶
type Metafile struct {
	// contains filtered or unexported fields
}
func NewMetafile ¶
func NewMetafile(referenceCanvas *Canvas) (*Metafile, error)
func NewMetafileFromFile ¶
func NewMetafileFromFile(filePath string) (*Metafile, error)
func (*Metafile) Dispose ¶
func (mf *Metafile) Dispose()
func (*Metafile) Save ¶
func (mf *Metafile) Save(filePath string) error
func (*Metafile) Size ¶
func (mf *Metafile) Size() Size
Size returns image size in 1/96" units.

type MinSizeForSizer ¶
type MinSizeForSizer interface {
	// MinSize returns minimum window size for given size. Both sizes are in native pixels.
	MinSizeForSize(size Size) Size
}
type MinSizer ¶
type MinSizer interface {
	// MinSize returns minimum window size in native pixels.
	MinSize() Size
}
type Modifiers ¶
type Modifiers byte
const (
	ModShift Modifiers = 1 << iota
	ModControl
	ModAlt
)
func ModifiersDown ¶
func ModifiersDown() Modifiers
func (Modifiers) String ¶
func (m Modifiers) String() string
type MouseButton ¶
type MouseButton int
const (
	LeftButton   MouseButton = win.MK_LBUTTON
	RightButton  MouseButton = win.MK_RBUTTON
	MiddleButton MouseButton = win.MK_MBUTTON
)
type MouseEvent ¶
type MouseEvent struct {
	// contains filtered or unexported fields
}
func (*MouseEvent) Attach ¶
func (e *MouseEvent) Attach(handler MouseEventHandler) int
func (*MouseEvent) Detach ¶
func (e *MouseEvent) Detach(handle int)
func (*MouseEvent) Once ¶
func (e *MouseEvent) Once(handler MouseEventHandler)
type MouseEventHandler ¶
type MouseEventHandler func(x, y int, button MouseButton)
MouseEventHandler is called for mouse events. x and y are measured in native pixels.

type MouseEventPublisher ¶
type MouseEventPublisher struct {
	// contains filtered or unexported fields
}
func (*MouseEventPublisher) Event ¶
func (p *MouseEventPublisher) Event() *MouseEvent
func (*MouseEventPublisher) Publish ¶
func (p *MouseEventPublisher) Publish(x, y int, button MouseButton)
Publish publishes mouse event. x and y are measured in native pixels.

type MsgBoxStyle ¶
type MsgBoxStyle uint
const (
	MsgBoxOK                  MsgBoxStyle = win.MB_OK
	MsgBoxOKCancel            MsgBoxStyle = win.MB_OKCANCEL
	MsgBoxAbortRetryIgnore    MsgBoxStyle = win.MB_ABORTRETRYIGNORE
	MsgBoxYesNoCancel         MsgBoxStyle = win.MB_YESNOCANCEL
	MsgBoxYesNo               MsgBoxStyle = win.MB_YESNO
	MsgBoxRetryCancel         MsgBoxStyle = win.MB_RETRYCANCEL
	MsgBoxCancelTryContinue   MsgBoxStyle = win.MB_CANCELTRYCONTINUE
	MsgBoxIconHand            MsgBoxStyle = win.MB_ICONHAND
	MsgBoxIconQuestion        MsgBoxStyle = win.MB_ICONQUESTION
	MsgBoxIconExclamation     MsgBoxStyle = win.MB_ICONEXCLAMATION
	MsgBoxIconAsterisk        MsgBoxStyle = win.MB_ICONASTERISK
	MsgBoxUserIcon            MsgBoxStyle = win.MB_USERICON
	MsgBoxIconWarning         MsgBoxStyle = win.MB_ICONWARNING
	MsgBoxIconError           MsgBoxStyle = win.MB_ICONERROR
	MsgBoxIconInformation     MsgBoxStyle = win.MB_ICONINFORMATION
	MsgBoxIconStop            MsgBoxStyle = win.MB_ICONSTOP
	MsgBoxDefButton1          MsgBoxStyle = win.MB_DEFBUTTON1
	MsgBoxDefButton2          MsgBoxStyle = win.MB_DEFBUTTON2
	MsgBoxDefButton3          MsgBoxStyle = win.MB_DEFBUTTON3
	MsgBoxDefButton4          MsgBoxStyle = win.MB_DEFBUTTON4
	MsgBoxApplModal           MsgBoxStyle = win.MB_APPLMODAL
	MsgBoxSystemModal         MsgBoxStyle = win.MB_SYSTEMMODAL
	MsgBoxTaskModal           MsgBoxStyle = win.MB_TASKMODAL
	MsgBoxHelp                MsgBoxStyle = win.MB_HELP
	MsgBoxSetForeground       MsgBoxStyle = win.MB_SETFOREGROUND
	MsgBoxDefaultDesktopOnly  MsgBoxStyle = win.MB_DEFAULT_DESKTOP_ONLY
	MsgBoxTopMost             MsgBoxStyle = win.MB_TOPMOST
	MsgBoxRight               MsgBoxStyle = win.MB_RIGHT
	MsgBoxRTLReading          MsgBoxStyle = win.MB_RTLREADING
	MsgBoxServiceNotification MsgBoxStyle = win.MB_SERVICE_NOTIFICATION
)
type MutableCondition ¶
type MutableCondition struct {
	// contains filtered or unexported fields
}
func NewMutableCondition ¶
func NewMutableCondition() *MutableCondition
func (*MutableCondition) Changed ¶
func (mc *MutableCondition) Changed() *Event
func (*MutableCondition) Satisfied ¶
func (mc *MutableCondition) Satisfied() bool
func (*MutableCondition) SetSatisfied ¶
func (mc *MutableCondition) SetSatisfied(satisfied bool) error
func (*MutableCondition) Value ¶
func (mc *MutableCondition) Value() interface{}
type NotifyIcon ¶
type NotifyIcon struct {
	// contains filtered or unexported fields
}
NotifyIcon represents an icon in the taskbar notification area.

func NewNotifyIcon ¶
func NewNotifyIcon(form Form) (*NotifyIcon, error)
NewNotifyIcon creates and returns a new NotifyIcon.

The NotifyIcon is initially not visible.

func (*NotifyIcon) ContextMenu ¶
func (ni *NotifyIcon) ContextMenu() *Menu
ContextMenu returns the context menu of the NotifyIcon.

func (*NotifyIcon) DPI ¶
func (ni *NotifyIcon) DPI() int
func (*NotifyIcon) Dispose ¶
func (ni *NotifyIcon) Dispose() error
Dispose releases the operating system resources associated with the NotifyIcon.

The associated Icon is not disposed of.

func (*NotifyIcon) Icon ¶
func (ni *NotifyIcon) Icon() Image
Icon returns the Icon of the NotifyIcon.

func (*NotifyIcon) MessageClicked ¶
func (ni *NotifyIcon) MessageClicked() *Event
MessageClicked occurs when the user clicks a message shown with ShowMessage or one of its iconed variants.

func (*NotifyIcon) MouseDown ¶
func (ni *NotifyIcon) MouseDown() *MouseEvent
MouseDown returns the event that is published when a mouse button is pressed while the cursor is over the NotifyIcon.

func (*NotifyIcon) MouseUp ¶
func (ni *NotifyIcon) MouseUp() *MouseEvent
MouseDown returns the event that is published when a mouse button is released while the cursor is over the NotifyIcon.

func (*NotifyIcon) SetIcon ¶
func (ni *NotifyIcon) SetIcon(icon Image) error
SetIcon sets the Icon of the NotifyIcon.

func (*NotifyIcon) SetToolTip ¶
func (ni *NotifyIcon) SetToolTip(toolTip string) error
SetToolTip sets the tool tip text of the NotifyIcon.

func (*NotifyIcon) SetVisible ¶
func (ni *NotifyIcon) SetVisible(visible bool) error
SetVisible sets if the NotifyIcon is visible.

func (*NotifyIcon) ShowCustom ¶
func (ni *NotifyIcon) ShowCustom(title, info string, icon Image) error
ShowCustom displays a custom icon message balloon above the NotifyIcon. If icon is nil, the main notification icon is used instead of a custom one.

The NotifyIcon must be visible before calling this method.

func (*NotifyIcon) ShowError ¶
func (ni *NotifyIcon) ShowError(title, info string) error
ShowError displays an error message balloon above the NotifyIcon.

The NotifyIcon must be visible before calling this method.

func (*NotifyIcon) ShowInfo ¶
func (ni *NotifyIcon) ShowInfo(title, info string) error
ShowInfo displays an info message balloon above the NotifyIcon.

The NotifyIcon must be visible before calling this method.

func (*NotifyIcon) ShowMessage ¶
func (ni *NotifyIcon) ShowMessage(title, info string) error
ShowMessage displays a neutral message balloon above the NotifyIcon.

The NotifyIcon must be visible before calling this method.

func (*NotifyIcon) ShowWarning ¶
func (ni *NotifyIcon) ShowWarning(title, info string) error
ShowWarning displays a warning message balloon above the NotifyIcon.

The NotifyIcon must be visible before calling this method.

func (*NotifyIcon) ToolTip ¶
func (ni *NotifyIcon) ToolTip() string
ToolTip returns the tool tip text of the NotifyIcon.

func (*NotifyIcon) Visible ¶
func (ni *NotifyIcon) Visible() bool
Visible returns if the NotifyIcon is visible.

type NumberEdit ¶
type NumberEdit struct {
	WidgetBase
	// contains filtered or unexported fields
}
NumberEdit is a widget that is suited to edit numeric values.

func NewNumberEdit ¶
func NewNumberEdit(parent Container) (*NumberEdit, error)
NewNumberEdit returns a new NumberEdit widget as child of parent.

func (*NumberEdit) Background ¶
func (ne *NumberEdit) Background() Brush
Background returns the background Brush of the NumberEdit.

By default this is nil.

func (*NumberEdit) CreateLayoutItem ¶
func (ne *NumberEdit) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*NumberEdit) Decimals ¶
func (ne *NumberEdit) Decimals() int
Decimals returns the number of decimal places in the NumberEdit.

func (*NumberEdit) Increment ¶
func (ne *NumberEdit) Increment() float64
Increment returns the amount by which the NumberEdit increments or decrements its value, when the user presses the KeyDown or KeyUp keys, or when the mouse wheel is rotated.

func (*NumberEdit) MaxValue ¶
func (ne *NumberEdit) MaxValue() float64
MinValue returns the maximum value the NumberEdit will accept.

func (*NumberEdit) MinValue ¶
func (ne *NumberEdit) MinValue() float64
MinValue returns the minimum value the NumberEdit will accept.

func (*NumberEdit) NeedsWmSize ¶
func (*NumberEdit) NeedsWmSize() bool
func (*NumberEdit) Prefix ¶
func (ne *NumberEdit) Prefix() string
Prefix returns the text that appears in the NumberEdit before the number.

func (*NumberEdit) PrefixChanged ¶
func (ne *NumberEdit) PrefixChanged() *Event
PrefixChanged returns the event that is published when the prefix changed.

func (*NumberEdit) ReadOnly ¶
func (ne *NumberEdit) ReadOnly() bool
ReadOnly returns whether the NumberEdit is in read-only mode.

func (*NumberEdit) SetBackground ¶
func (ne *NumberEdit) SetBackground(bg Brush)
SetBackground sets the background Brush of the NumberEdit.

func (*NumberEdit) SetDecimals ¶
func (ne *NumberEdit) SetDecimals(decimals int) error
SetDecimals sets the number of decimal places in the NumberEdit.

func (*NumberEdit) SetFocus ¶
func (ne *NumberEdit) SetFocus() error
SetFocus sets the keyboard input focus to the NumberEdit.

func (*NumberEdit) SetIncrement ¶
func (ne *NumberEdit) SetIncrement(increment float64) error
SetIncrement sets the amount by which the NumberEdit increments or decrements its value, when the user presses the KeyDown or KeyUp keys, or when the mouse wheel is rotated.

func (*NumberEdit) SetPrefix ¶
func (ne *NumberEdit) SetPrefix(prefix string) error
SetPrefix sets the text that appears in the NumberEdit before the number.

func (*NumberEdit) SetRange ¶
func (ne *NumberEdit) SetRange(min, max float64) error
SetRange sets the minimum and maximum values the NumberEdit will accept.

If the current value is out of this range, it will be adjusted.

func (*NumberEdit) SetReadOnly ¶
func (ne *NumberEdit) SetReadOnly(readOnly bool) error
SetReadOnly sets whether the NumberEdit is in read-only mode.

func (*NumberEdit) SetSpinButtonsVisible ¶
func (ne *NumberEdit) SetSpinButtonsVisible(visible bool) error
SetSpinButtonsVisible sets whether the NumberEdit appears with spin buttons.

func (*NumberEdit) SetSuffix ¶
func (ne *NumberEdit) SetSuffix(suffix string) error
SetSuffix sets the text that appears in the NumberEdit after the number.

func (*NumberEdit) SetTextColor ¶
func (ne *NumberEdit) SetTextColor(c Color)
TextColor sets the Color used to draw the text of the NumberEdit.

func (*NumberEdit) SetTextSelection ¶
func (ne *NumberEdit) SetTextSelection(start, end int)
SetTextSelection sets the range of the current text selection of the NumberEdit.

func (*NumberEdit) SetToolTipText ¶
func (ne *NumberEdit) SetToolTipText(s string) error
func (*NumberEdit) SetValue ¶
func (ne *NumberEdit) SetValue(value float64) error
SetValue sets the value of the NumberEdit.

func (*NumberEdit) SpinButtonsVisible ¶
func (ne *NumberEdit) SpinButtonsVisible() bool
SpinButtonsVisible returns whether the NumberEdit appears with spin buttons.

func (*NumberEdit) Suffix ¶
func (ne *NumberEdit) Suffix() string
Suffix returns the text that appears in the NumberEdit after the number.

func (*NumberEdit) SuffixChanged ¶
func (ne *NumberEdit) SuffixChanged() *Event
SuffixChanged returns the event that is published when the suffix changed.

func (*NumberEdit) TextColor ¶
func (ne *NumberEdit) TextColor() Color
TextColor returns the Color used to draw the text of the NumberEdit.

func (*NumberEdit) TextSelection ¶
func (ne *NumberEdit) TextSelection() (start, end int)
TextSelection returns the range of the current text selection of the NumberEdit.

func (*NumberEdit) Value ¶
func (ne *NumberEdit) Value() float64
Value returns the value of the NumberEdit.

func (*NumberEdit) ValueChanged ¶
func (ne *NumberEdit) ValueChanged() *Event
ValueChanged returns an Event that can be used to track changes to Value.

func (*NumberEdit) WndProc ¶
func (ne *NumberEdit) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
WndProc is the window procedure of the NumberEdit.

When implementing your own WndProc to add or modify behavior, call the WndProc of the embedded NumberEdit for messages you don't handle yourself.

type NumberLabel ¶
type NumberLabel struct {
	// contains filtered or unexported fields
}
func NewNumberLabel ¶
func NewNumberLabel(parent Container) (*NumberLabel, error)
func (*NumberLabel) CreateLayoutItem ¶
func (s *NumberLabel) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*NumberLabel) Decimals ¶
func (nl *NumberLabel) Decimals() int
func (*NumberLabel) Dispose ¶
func (s *NumberLabel) Dispose()
func (*NumberLabel) SetDecimals ¶
func (nl *NumberLabel) SetDecimals(decimals int) error
func (*NumberLabel) SetSuffix ¶
func (nl *NumberLabel) SetSuffix(suffix string) error
func (*NumberLabel) SetTextAlignment ¶
func (nl *NumberLabel) SetTextAlignment(alignment Alignment1D) error
func (*NumberLabel) SetTextColor ¶
func (s *NumberLabel) SetTextColor(c Color)
func (*NumberLabel) SetValue ¶
func (nl *NumberLabel) SetValue(value float64) error
func (*NumberLabel) Suffix ¶
func (nl *NumberLabel) Suffix() string
func (*NumberLabel) TextAlignment ¶
func (nl *NumberLabel) TextAlignment() Alignment1D
func (*NumberLabel) TextColor ¶
func (s *NumberLabel) TextColor() Color
func (*NumberLabel) Value ¶
func (nl *NumberLabel) Value() float64
func (*NumberLabel) WndProc ¶
func (s *NumberLabel) WndProc(hwnd win.HWND, msg uint32, wp, lp uintptr) uintptr
type Orientation ¶
type Orientation byte
type PIState ¶
type PIState int
const (
	PINoProgress    PIState = win.TBPF_NOPROGRESS
	PIIndeterminate PIState = win.TBPF_INDETERMINATE
	PINormal        PIState = win.TBPF_NORMAL
	PIError         PIState = win.TBPF_ERROR
	PIPaused        PIState = win.TBPF_PAUSED
)
type PaintFunc ¶
type PaintFunc func(canvas *Canvas, updateBounds Rectangle) error
PaintFunc paints custom widget content. updateBounds is specified in 1/96" or native pixels.

type PaintFuncImage ¶
type PaintFuncImage struct {
	// contains filtered or unexported fields
}
func NewPaintFuncImage ¶
func NewPaintFuncImage(size Size, paint func(canvas *Canvas, bounds Rectangle) error) *PaintFuncImage
NewPaintFuncImage creates new PaintFuncImage struct. size parameter and paint function bounds parameter are specified in 1/96" units.

func NewPaintFuncImagePixels ¶
func NewPaintFuncImagePixels(size Size, paint func(canvas *Canvas, bounds Rectangle) error) *PaintFuncImage
NewPaintFuncImagePixels creates new PaintFuncImage struct. size parameter is specified in 1/96" units. paint function bounds parameter is specified in native pixels.

func NewPaintFuncImagePixelsWithDispose ¶
func NewPaintFuncImagePixelsWithDispose(size Size, paint func(canvas *Canvas, bounds Rectangle) error, dispose func()) *PaintFuncImage
NewPaintFuncImagePixelsWithDispose creates new PaintFuncImage struct. size parameter is specified in 1/96" units. paint function bounds parameter is specified in native pixels.

func NewPaintFuncImageWithDispose ¶
func NewPaintFuncImageWithDispose(size Size, paint func(canvas *Canvas, bounds Rectangle) error, dispose func()) *PaintFuncImage
NewPaintFuncImageWithDispose creates new PaintFuncImage struct. size parameter and paint function bounds parameter are specified in 1/96" units.

func (*PaintFuncImage) Dispose ¶
func (pfi *PaintFuncImage) Dispose()
func (*PaintFuncImage) Size ¶
func (pfi *PaintFuncImage) Size() Size
Size returns image size in 1/96" units.

type PaintMode ¶
type PaintMode int
const (
	PaintNormal   PaintMode = iota // erase background before PaintFunc
	PaintNoErase                   // PaintFunc clears background, single buffered
	PaintBuffered                  // PaintFunc clears background, double buffered
)
type Pen ¶
type Pen interface {
	Dispose()
	Style() PenStyle

	// Width returns pen width in 1/96" units.
	Width() int
	// contains filtered or unexported methods
}
func NullPen ¶
func NullPen() Pen
type PenStyle ¶
type PenStyle int
const (
	PenSolid       PenStyle = win.PS_SOLID
	PenDash        PenStyle = win.PS_DASH
	PenDot         PenStyle = win.PS_DOT
	PenDashDot     PenStyle = win.PS_DASHDOT
	PenDashDotDot  PenStyle = win.PS_DASHDOTDOT
	PenNull        PenStyle = win.PS_NULL
	PenInsideFrame PenStyle = win.PS_INSIDEFRAME
	PenUserStyle   PenStyle = win.PS_USERSTYLE
	PenAlternate   PenStyle = win.PS_ALTERNATE
)
Pen styles

const (
	PenCapRound  PenStyle = win.PS_ENDCAP_ROUND
	PenCapSquare PenStyle = win.PS_ENDCAP_SQUARE
	PenCapFlat   PenStyle = win.PS_ENDCAP_FLAT
)
Pen cap styles (geometric pens only)

const (
	PenJoinBevel PenStyle = win.PS_JOIN_BEVEL
	PenJoinMiter PenStyle = win.PS_JOIN_MITER
	PenJoinRound PenStyle = win.PS_JOIN_ROUND
)
Pen join styles (geometric pens only)

type Persistable ¶
type Persistable interface {
	Persistent() bool
	SetPersistent(value bool)
	SaveState() error
	RestoreState() error
}
type Point ¶
type Point struct {
	X, Y int
}
Point defines 2D coordinate in 1/96" units ot native pixels.

func PointFrom96DPI ¶
func PointFrom96DPI(value Point, dpi int) Point
PointFrom96DPI converts from 1/96" units to native pixels.

func PointTo96DPI ¶
func PointTo96DPI(value Point, dpi int) Point
PointTo96DPI converts from native pixels to 1/96" units.

type Populator ¶
type Populator interface {
	// Populate initializes the slot specified by index.
	//
	// For best performance it is probably a good idea to populate more than a
	// single slot of the slice at once.
	Populate(index int) error
}
Populator is an interface that can be implemented by Reflect*Models and slice types to populate themselves on demand.

Widgets like TableView, ListBox and ComboBox support lazy population of a Reflect*Model or slice, if it implements this interface.

type ProgressBar ¶
type ProgressBar struct {
	WidgetBase
}
func NewProgressBar ¶
func NewProgressBar(parent Container) (*ProgressBar, error)
func (*ProgressBar) CreateLayoutItem ¶
func (pb *ProgressBar) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*ProgressBar) MarqueeMode ¶
func (pb *ProgressBar) MarqueeMode() bool
func (*ProgressBar) MaxValue ¶
func (pb *ProgressBar) MaxValue() int
func (*ProgressBar) MinValue ¶
func (pb *ProgressBar) MinValue() int
func (*ProgressBar) SetMarqueeMode ¶
func (pb *ProgressBar) SetMarqueeMode(marqueeMode bool) error
func (*ProgressBar) SetRange ¶
func (pb *ProgressBar) SetRange(min, max int)
func (*ProgressBar) SetValue ¶
func (pb *ProgressBar) SetValue(value int)
func (*ProgressBar) Value ¶
func (pb *ProgressBar) Value() int
type ProgressIndicator ¶
type ProgressIndicator struct {
	// contains filtered or unexported fields
}
func (*ProgressIndicator) Completed ¶
func (pi *ProgressIndicator) Completed() uint32
func (*ProgressIndicator) SetCompleted ¶
func (pi *ProgressIndicator) SetCompleted(completed uint32) error
func (*ProgressIndicator) SetOverlayIcon ¶
func (pi *ProgressIndicator) SetOverlayIcon(icon *Icon, description string) error
func (*ProgressIndicator) SetState ¶
func (pi *ProgressIndicator) SetState(state PIState) error
func (*ProgressIndicator) SetTotal ¶
func (pi *ProgressIndicator) SetTotal(total uint32)
func (*ProgressIndicator) State ¶
func (pi *ProgressIndicator) State() PIState
func (*ProgressIndicator) Total ¶
func (pi *ProgressIndicator) Total() uint32
type Property ¶
type Property interface {
	Expression
	ReadOnly() bool
	Get() interface{}
	Set(value interface{}) error
	Source() interface{}
	SetSource(source interface{}) error
	Validatable() bool
	Validator() Validator
	SetValidator(validator Validator) error
}
func NewBoolProperty ¶
func NewBoolProperty(get func() bool, set func(b bool) error, changed *Event) Property
func NewProperty ¶
func NewProperty(get func() interface{}, set func(v interface{}) error, changed *Event) Property
func NewReadOnlyBoolProperty ¶
func NewReadOnlyBoolProperty(get func() bool, changed *Event) Property
func NewReadOnlyProperty ¶
func NewReadOnlyProperty(get func() interface{}, changed *Event) Property
type PushButton ¶
type PushButton struct {
	Button
}
func NewPushButton ¶
func NewPushButton(parent Container) (*PushButton, error)
func (*PushButton) CreateLayoutItem ¶
func (pb *PushButton) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*PushButton) ImageAboveText ¶
func (pb *PushButton) ImageAboveText() bool
func (*PushButton) SetImageAboveText ¶
func (pb *PushButton) SetImageAboveText(value bool) error
func (*PushButton) WndProc ¶
func (pb *PushButton) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type RadioButton ¶
type RadioButton struct {
	Button
	// contains filtered or unexported fields
}
func NewRadioButton ¶
func NewRadioButton(parent Container) (*RadioButton, error)
func (*RadioButton) Group ¶
func (rb *RadioButton) Group() *RadioButtonGroup
func (*RadioButton) SetTextOnLeftSide ¶
func (rb *RadioButton) SetTextOnLeftSide(textLeft bool) error
func (*RadioButton) SetValue ¶
func (rb *RadioButton) SetValue(value interface{})
func (*RadioButton) TextOnLeftSide ¶
func (rb *RadioButton) TextOnLeftSide() bool
func (*RadioButton) Value ¶
func (rb *RadioButton) Value() interface{}
func (*RadioButton) WndProc ¶
func (rb *RadioButton) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type RadioButtonGroup ¶
type RadioButtonGroup struct {
	// contains filtered or unexported fields
}
func (*RadioButtonGroup) Buttons ¶
func (rbg *RadioButtonGroup) Buttons() []*RadioButton
func (*RadioButtonGroup) CheckedButton ¶
func (rbg *RadioButtonGroup) CheckedButton() *RadioButton
type RangeValidator ¶
type RangeValidator struct {
	// contains filtered or unexported fields
}
func NewRangeValidator ¶
func NewRangeValidator(min, max float64) (*RangeValidator, error)
func (*RangeValidator) Max ¶
func (rv *RangeValidator) Max() float64
func (*RangeValidator) Min ¶
func (rv *RangeValidator) Min() float64
func (*RangeValidator) Reset ¶
func (rv *RangeValidator) Reset(min, max float64) error
func (*RangeValidator) Validate ¶
func (rv *RangeValidator) Validate(v interface{}) error
type Rectangle ¶
type Rectangle struct {
	X, Y, Width, Height int
}
Rectangle defines upper left corner with width and height region in 1/96" units, or native pixels, or grid rows and columns.

func RectangleFrom96DPI ¶
func RectangleFrom96DPI(value Rectangle, dpi int) Rectangle
RectangleFrom96DPI converts from 1/96" units to native pixels.

func RectangleTo96DPI ¶
func RectangleTo96DPI(value Rectangle, dpi int) Rectangle
RectangleTo96DPI converts from native pixels to 1/96" units.

func (Rectangle) Bottom ¶
func (r Rectangle) Bottom() int
func (Rectangle) IsZero ¶
func (r Rectangle) IsZero() bool
func (Rectangle) Left ¶
func (r Rectangle) Left() int
func (Rectangle) Location ¶
func (r Rectangle) Location() Point
func (Rectangle) Right ¶
func (r Rectangle) Right() int
func (*Rectangle) SetLocation ¶
func (r *Rectangle) SetLocation(p Point) Rectangle
func (*Rectangle) SetSize ¶
func (r *Rectangle) SetSize(s Size) Rectangle
func (Rectangle) Size ¶
func (r Rectangle) Size() Size
func (Rectangle) Top ¶
func (r Rectangle) Top() int
type ReflectListModel ¶
type ReflectListModel interface {
	// Items returns the model data, which must be a slice of pointer to struct.
	Items() interface{}

	// ItemsReset returns the event that the model should publish when the
	// number of its items changes.
	ItemsReset() *Event

	// ItemChanged returns the event that the model should publish when an item
	// was changed.
	ItemChanged() *IntEvent

	// ItemsInserted returns the event that the model should publish when a
	// contiguous range of items was inserted.
	ItemsInserted() *IntRangeEvent

	// ItemsRemoved returns the event that the model should publish when a
	// contiguous range of items was removed.
	ItemsRemoved() *IntRangeEvent
	// contains filtered or unexported methods
}
ReflectListModel provides an alternative to the ListModel interface. It uses reflection to obtain data.

type ReflectListModelBase ¶
type ReflectListModelBase struct {
	ListModelBase
	// contains filtered or unexported fields
}
ReflectListModelBase implements the ItemsReset and ItemChanged methods of the ReflectListModel interface.

func (*ReflectListModelBase) Value ¶
func (rlmb *ReflectListModelBase) Value(index int) interface{}
type ReflectTableModel ¶
type ReflectTableModel interface {
	// Items returns the model data, which must be a slice of pointer to struct.
	Items() interface{}

	// RowsReset returns the event that the model should publish when the
	// number of its items changes.
	RowsReset() *Event

	// RowChanged returns the event that the model should publish when an item
	// was changed.
	RowChanged() *IntEvent

	// RowsChanged returns the event that the model should publish when a
	// contiguous range of items was changed.
	RowsChanged() *IntRangeEvent

	// RowsInserted returns the event that the model should publish when a
	// contiguous range of items was inserted. If the model supports sorting, it
	// is assumed to be sorted before the model publishes the event.
	RowsInserted() *IntRangeEvent

	// RowsRemoved returns the event that the model should publish when a
	// contiguous range of items was removed.
	RowsRemoved() *IntRangeEvent
	// contains filtered or unexported methods
}
ReflectTableModel provides an alternative to the TableModel interface. It uses reflection to obtain data.

type ReflectTableModelBase ¶
type ReflectTableModelBase struct {
	TableModelBase
	// contains filtered or unexported fields
}
ReflectTableModelBase implements the ItemsReset and ItemChanged methods of the ReflectTableModel interface.

func (*ReflectTableModelBase) Value ¶
func (rtmb *ReflectTableModelBase) Value(row, col int) interface{}
type RegexpValidator ¶
type RegexpValidator struct {
	// contains filtered or unexported fields
}
func NewRegexpValidator ¶
func NewRegexpValidator(pattern string) (*RegexpValidator, error)
func (*RegexpValidator) Pattern ¶
func (rv *RegexpValidator) Pattern() string
func (*RegexpValidator) Validate ¶
func (rv *RegexpValidator) Validate(v interface{}) error
type RegistryKey ¶
type RegistryKey struct {
	// contains filtered or unexported fields
}
func ClassesRootKey ¶
func ClassesRootKey() *RegistryKey
func CurrentUserKey ¶
func CurrentUserKey() *RegistryKey
func LocalMachineKey ¶
func LocalMachineKey() *RegistryKey
type ResourceManager ¶
type ResourceManager struct {
	// contains filtered or unexported fields
}
ResourceManager is a cache for sharing resources like bitmaps and icons. The resources can be either embedded in the running executable file or located below a specified root directory in the file system.

var Resources ResourceManager
Resources is the singleton instance of ResourceManager.

func (*ResourceManager) Bitmap
deprecated
func (*ResourceManager) BitmapForDPI ¶
func (rm *ResourceManager) BitmapForDPI(name string, dpi int) (*Bitmap, error)
BitmapForDPI loads a bitmap from file or resource identified by name, or an error if it could not be found. When bitmap is loaded, given DPI is assumed.

func (*ResourceManager) Icon ¶
func (rm *ResourceManager) Icon(name string) (*Icon, error)
Icon returns the Icon identified by name, or an error if it could not be found.

func (*ResourceManager) Image ¶
func (rm *ResourceManager) Image(name string) (Image, error)
Image returns the Image identified by name, or an error if it could not be found.

func (*ResourceManager) RootDirPath ¶
func (rm *ResourceManager) RootDirPath() string
RootDirPath returns the root directory path where resources are to be loaded from.

func (*ResourceManager) SetRootDirPath ¶
func (rm *ResourceManager) SetRootDirPath(rootDirPath string) error
SetRootDirPath sets the root directory path where resources are to be loaded from.

type ScrollView ¶
type ScrollView struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewScrollView ¶
func NewScrollView(parent Container) (*ScrollView, error)
func (*ScrollView) ApplyDPI ¶
func (sv *ScrollView) ApplyDPI(dpi int)
func (*ScrollView) AsContainerBase ¶
func (sv *ScrollView) AsContainerBase() *ContainerBase
func (*ScrollView) Children ¶
func (sv *ScrollView) Children() *WidgetList
func (*ScrollView) CreateLayoutItem ¶
func (sv *ScrollView) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*ScrollView) DataBinder ¶
func (sv *ScrollView) DataBinder() *DataBinder
func (*ScrollView) Layout ¶
func (sv *ScrollView) Layout() Layout
func (*ScrollView) MouseDown ¶
func (sv *ScrollView) MouseDown() *MouseEvent
func (*ScrollView) MouseMove ¶
func (sv *ScrollView) MouseMove() *MouseEvent
func (*ScrollView) MouseUp ¶
func (sv *ScrollView) MouseUp() *MouseEvent
func (*ScrollView) Name ¶
func (sv *ScrollView) Name() string
func (*ScrollView) Persistent ¶
func (sv *ScrollView) Persistent() bool
func (*ScrollView) RestoreState ¶
func (sv *ScrollView) RestoreState() error
func (*ScrollView) SaveState ¶
func (sv *ScrollView) SaveState() error
func (*ScrollView) Scrollbars ¶
func (sv *ScrollView) Scrollbars() (horizontal, vertical bool)
func (*ScrollView) SetDataBinder ¶
func (sv *ScrollView) SetDataBinder(dataBinder *DataBinder)
func (*ScrollView) SetLayout ¶
func (sv *ScrollView) SetLayout(value Layout) error
func (*ScrollView) SetName ¶
func (sv *ScrollView) SetName(name string)
func (*ScrollView) SetPersistent ¶
func (sv *ScrollView) SetPersistent(value bool)
func (*ScrollView) SetScrollbars ¶
func (sv *ScrollView) SetScrollbars(horizontal, vertical bool)
func (*ScrollView) SetSuspended ¶
func (sv *ScrollView) SetSuspended(suspend bool)
func (*ScrollView) WndProc ¶
func (sv *ScrollView) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type Separator ¶
type Separator struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewHSeparator ¶
func NewHSeparator(parent Container) (*Separator, error)
func NewVSeparator ¶
func NewVSeparator(parent Container) (*Separator, error)
func (*Separator) CreateLayoutItem ¶
func (s *Separator) CreateLayoutItem(ctx *LayoutContext) LayoutItem
type Settings ¶
type Settings interface {
	Get(key string) (string, bool)
	Timestamp(key string) (time.Time, bool)
	Put(key, value string) error
	PutExpiring(key, value string) error
	Remove(key string) error
	ExpireDuration() time.Duration
	SetExpireDuration(expireDuration time.Duration)
	Load() error
	Save() error
}
type Shortcut ¶
type Shortcut struct {
	Modifiers Modifiers
	Key       Key
}
func (Shortcut) String ¶
func (s Shortcut) String() string
type Size ¶
type Size struct {
	Width, Height int
}
Size defines width and height in 1/96" units or native pixels, or dialog base units.

When Size is used for DPI metrics, it defines a 1"x1" rectangle in native pixels.

func SizeFrom96DPI ¶
func SizeFrom96DPI(value Size, dpi int) Size
SizeFrom96DPI converts from 1/96" units to native pixels.

func SizeTo96DPI ¶
func SizeTo96DPI(value Size, dpi int) Size
SizeTo96DPI converts from native pixels to 1/96" units.

func (Size) IsZero ¶
func (s Size) IsZero() bool
type Slider ¶
type Slider struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewSlider ¶
func NewSlider(parent Container) (*Slider, error)
func NewSliderWithCfg ¶
func NewSliderWithCfg(parent Container, cfg *SliderCfg) (*Slider, error)
func NewSliderWithOrientation ¶
func NewSliderWithOrientation(parent Container, orientation Orientation) (*Slider, error)
func (*Slider) CreateLayoutItem ¶
func (sl *Slider) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*Slider) LineSize ¶
func (sl *Slider) LineSize() int
func (*Slider) MaxValue ¶
func (sl *Slider) MaxValue() int
func (*Slider) MinValue ¶
func (sl *Slider) MinValue() int
func (*Slider) NeedsWmSize ¶
func (*Slider) NeedsWmSize() bool
func (*Slider) PageSize ¶
func (sl *Slider) PageSize() int
func (*Slider) Persistent ¶
func (sl *Slider) Persistent() bool
func (*Slider) RestoreState ¶
func (sl *Slider) RestoreState() error
func (*Slider) SaveState ¶
func (sl *Slider) SaveState() error
func (*Slider) SetLineSize ¶
func (sl *Slider) SetLineSize(lineSize int)
func (*Slider) SetPageSize ¶
func (sl *Slider) SetPageSize(pageSize int)
func (*Slider) SetPersistent ¶
func (sl *Slider) SetPersistent(value bool)
func (*Slider) SetRange ¶
func (sl *Slider) SetRange(min, max int)
func (*Slider) SetTracking ¶
func (sl *Slider) SetTracking(tracking bool)
func (*Slider) SetValue ¶
func (sl *Slider) SetValue(value int)
func (*Slider) Tracking ¶
func (sl *Slider) Tracking() bool
func (*Slider) Value ¶
func (sl *Slider) Value() int
func (*Slider) ValueChanged ¶
func (sl *Slider) ValueChanged() *Event
ValueChanged returns an Event that can be used to track changes to Value.

func (*Slider) WndProc ¶
func (sl *Slider) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type SliderCfg ¶
type SliderCfg struct {
	Orientation    Orientation
	ToolTipsHidden bool
}
type SolidColorBrush ¶
type SolidColorBrush struct {
	// contains filtered or unexported fields
}
func NewSolidColorBrush ¶
func NewSolidColorBrush(color Color) (*SolidColorBrush, error)
func (*SolidColorBrush) Color ¶
func (b *SolidColorBrush) Color() Color
func (*SolidColorBrush) Dispose ¶
func (bb *SolidColorBrush) Dispose()
type SortOrder ¶
type SortOrder int
SortOrder specifies the order by which items are sorted.

const (
	// SortAscending specifies ascending sort order.
	SortAscending SortOrder = iota

	// SortDescending specifies descending sort order.
	SortDescending
)
type SortedReflectTableModelBase ¶
type SortedReflectTableModelBase struct {
	ReflectTableModelBase
	SorterBase
	// contains filtered or unexported fields
}
SortedReflectTableModelBase implements the RowsReset and RowChanged methods of the ReflectTableModel interface as well as the Sorter interface for pre-implemented in-memory sorting.

func (*SortedReflectTableModelBase) Sort ¶
func (srtmb *SortedReflectTableModelBase) Sort(col int, order SortOrder) error
type Sorter ¶
type Sorter interface {
	// ColumnSortable returns whether column col is sortable.
	ColumnSortable(col int) bool

	// Sort sorts column col in order order.
	//
	// If col is -1 then no column is to be sorted. Sort must publish the event
	// returned from SortChanged() after sorting.
	Sort(col int, order SortOrder) error

	// SortChanged returns an event that is published after sorting.
	SortChanged() *Event

	// SortedColumn returns the index of the currently sorted column, or -1 if
	// no column is currently sorted.
	SortedColumn() int

	// SortOrder returns the current sort order.
	SortOrder() SortOrder
}
Sorter is the interface that a model must implement to support sorting with a widget like TableView.

type SorterBase ¶
type SorterBase struct {
	// contains filtered or unexported fields
}
SorterBase implements the Sorter interface.

You still need to provide your own implementation of at least the Sort method to actually sort and reset the model. Your Sort method should call the SorterBase implementation so the SortChanged event, that e.g. a TableView widget depends on, is published.

func (*SorterBase) ColumnSortable ¶
func (sb *SorterBase) ColumnSortable(col int) bool
func (*SorterBase) Sort ¶
func (sb *SorterBase) Sort(col int, order SortOrder) error
func (*SorterBase) SortChanged ¶
func (sb *SorterBase) SortChanged() *Event
func (*SorterBase) SortOrder ¶
func (sb *SorterBase) SortOrder() SortOrder
func (*SorterBase) SortedColumn ¶
func (sb *SorterBase) SortedColumn() int
type Spacer ¶
type Spacer struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewHSpacer ¶
func NewHSpacer(parent Container) (*Spacer, error)
func NewHSpacerFixed ¶
func NewHSpacerFixed(parent Container, width int) (*Spacer, error)
func NewSpacerWithCfg ¶
func NewSpacerWithCfg(parent Container, cfg *SpacerCfg) (*Spacer, error)
func NewVSpacer ¶
func NewVSpacer(parent Container) (*Spacer, error)
func NewVSpacerFixed ¶
func NewVSpacerFixed(parent Container, height int) (*Spacer, error)
func (*Spacer) CreateLayoutItem ¶
func (s *Spacer) CreateLayoutItem(ctx *LayoutContext) LayoutItem
type SpacerCfg ¶
type SpacerCfg struct {
	LayoutFlags       LayoutFlags
	SizeHint          Size // in 1/96" units
	GreedyLocallyOnly bool
}
type SplitButton ¶
type SplitButton struct {
	Button
	// contains filtered or unexported fields
}
func NewSplitButton ¶
func NewSplitButton(parent Container) (*SplitButton, error)
func (*SplitButton) Dispose ¶
func (sb *SplitButton) Dispose()
func (*SplitButton) ImageAboveText ¶
func (sb *SplitButton) ImageAboveText() bool
func (*SplitButton) Menu ¶
func (sb *SplitButton) Menu() *Menu
func (*SplitButton) SetImageAboveText ¶
func (sb *SplitButton) SetImageAboveText(value bool) error
func (*SplitButton) WndProc ¶
func (sb *SplitButton) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type Splitter ¶
type Splitter struct {
	ContainerBase
	// contains filtered or unexported fields
}
func NewHSplitter ¶
func NewHSplitter(parent Container) (*Splitter, error)
func NewVSplitter ¶
func NewVSplitter(parent Container) (*Splitter, error)
func (*Splitter) CreateLayoutItem ¶
func (s *Splitter) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*Splitter) Fixed ¶
func (s *Splitter) Fixed(widget Widget) bool
func (*Splitter) HandleWidth ¶
func (s *Splitter) HandleWidth() int
func (*Splitter) Orientation ¶
func (s *Splitter) Orientation() Orientation
func (*Splitter) Persistent ¶
func (s *Splitter) Persistent() bool
func (*Splitter) RestoreState ¶
func (s *Splitter) RestoreState() error
func (*Splitter) SaveState ¶
func (s *Splitter) SaveState() error
func (*Splitter) SetFixed ¶
func (s *Splitter) SetFixed(widget Widget, fixed bool) error
func (*Splitter) SetHandleWidth ¶
func (s *Splitter) SetHandleWidth(value int) error
func (*Splitter) SetLayout ¶
func (s *Splitter) SetLayout(value Layout) error
func (*Splitter) SetPersistent ¶
func (s *Splitter) SetPersistent(value bool)
func (*Splitter) WndProc ¶
func (s *Splitter) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type StatusBar ¶
type StatusBar struct {
	WidgetBase
	// contains filtered or unexported fields
}
StatusBar is a widget that displays status messages.

func NewStatusBar ¶
func NewStatusBar(parent Container) (*StatusBar, error)
NewStatusBar returns a new StatusBar as child of container parent.

func (*StatusBar) ApplyDPI ¶
func (sb *StatusBar) ApplyDPI(dpi int)
func (*StatusBar) CreateLayoutItem ¶
func (*StatusBar) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*StatusBar) Items ¶
func (sb *StatusBar) Items() *StatusBarItemList
Items returns the list of items in the StatusBar.

func (*StatusBar) SetVisible ¶
func (sb *StatusBar) SetVisible(visible bool)
SetVisible sets whether the StatusBar is visible.

func (*StatusBar) WndProc ¶
func (sb *StatusBar) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type StatusBarItem ¶
type StatusBarItem struct {
	// contains filtered or unexported fields
}
StatusBarItem represents a section of a StatusBar that can have its own icon, text, tool tip text and width.

func NewStatusBarItem ¶
func NewStatusBarItem() *StatusBarItem
NewStatusBarItem returns a new StatusBarItem.

func (*StatusBarItem) Clicked ¶
func (sbi *StatusBarItem) Clicked() *Event
func (*StatusBarItem) Icon ¶
func (sbi *StatusBarItem) Icon() *Icon
Icon returns the Icon of the StatusBarItem.

func (*StatusBarItem) SetIcon ¶
func (sbi *StatusBarItem) SetIcon(icon *Icon) error
SetIcon sets the Icon of the StatusBarItem.

func (*StatusBarItem) SetText ¶
func (sbi *StatusBarItem) SetText(text string) error
SetText sets the text of the StatusBarItem.

func (*StatusBarItem) SetToolTipText ¶
func (sbi *StatusBarItem) SetToolTipText(toolTipText string) error
SetToolTipText sets the tool tip text of the StatusBarItem.

func (*StatusBarItem) SetWidth ¶
func (sbi *StatusBarItem) SetWidth(width int) error
SetWidth sets the width of the StatusBarItem.

func (*StatusBarItem) Text ¶
func (sbi *StatusBarItem) Text() string
Text returns the text of the StatusBarItem.

func (*StatusBarItem) ToolTipText ¶
func (sbi *StatusBarItem) ToolTipText() string
ToolTipText returns the tool tip text of the StatusBarItem.

func (*StatusBarItem) Width ¶
func (sbi *StatusBarItem) Width() int
Width returns the width of the StatusBarItem.

type StatusBarItemList ¶
type StatusBarItemList struct {
	// contains filtered or unexported fields
}
func (*StatusBarItemList) Add ¶
func (l *StatusBarItemList) Add(item *StatusBarItem) error
func (*StatusBarItemList) At ¶
func (l *StatusBarItemList) At(index int) *StatusBarItem
func (*StatusBarItemList) Clear ¶
func (l *StatusBarItemList) Clear() error
func (*StatusBarItemList) Contains ¶
func (l *StatusBarItemList) Contains(item *StatusBarItem) bool
func (*StatusBarItemList) Index ¶
func (l *StatusBarItemList) Index(item *StatusBarItem) int
func (*StatusBarItemList) Insert ¶
func (l *StatusBarItemList) Insert(index int, item *StatusBarItem) error
func (*StatusBarItemList) Len ¶
func (l *StatusBarItemList) Len() int
func (*StatusBarItemList) Remove ¶
func (l *StatusBarItemList) Remove(item *StatusBarItem) error
func (*StatusBarItemList) RemoveAt ¶
func (l *StatusBarItemList) RemoveAt(index int) error
type StringEvent ¶
type StringEvent struct {
	// contains filtered or unexported fields
}
func (*StringEvent) Attach ¶
func (e *StringEvent) Attach(handler StringEventHandler) int
func (*StringEvent) Detach ¶
func (e *StringEvent) Detach(handle int)
func (*StringEvent) Once ¶
func (e *StringEvent) Once(handler StringEventHandler)
type StringEventHandler ¶
type StringEventHandler func(s string)
type StringEventPublisher ¶
type StringEventPublisher struct {
	// contains filtered or unexported fields
}
func (*StringEventPublisher) Event ¶
func (p *StringEventPublisher) Event() *StringEvent
func (*StringEventPublisher) Publish ¶
func (p *StringEventPublisher) Publish(s string)
type SystemColor ¶
type SystemColor int
const (
	SysColor3DDkShadow              SystemColor = win.COLOR_3DDKSHADOW
	SysColor3DFace                  SystemColor = win.COLOR_3DFACE
	SysColor3DHighlight             SystemColor = win.COLOR_3DHIGHLIGHT
	SysColor3DLight                 SystemColor = win.COLOR_3DLIGHT
	SysColor3DShadow                SystemColor = win.COLOR_3DSHADOW
	SysColorActiveBorder            SystemColor = win.COLOR_ACTIVEBORDER
	SysColorActiveCaption           SystemColor = win.COLOR_ACTIVECAPTION
	SysColorAppWorkspace            SystemColor = win.COLOR_APPWORKSPACE
	SysColorBackground              SystemColor = win.COLOR_BACKGROUND
	SysColorDesktop                 SystemColor = win.COLOR_DESKTOP
	SysColorBtnFace                 SystemColor = win.COLOR_BTNFACE
	SysColorBtnHighlight            SystemColor = win.COLOR_BTNHIGHLIGHT
	SysColorBtnShadow               SystemColor = win.COLOR_BTNSHADOW
	SysColorBtnText                 SystemColor = win.COLOR_BTNTEXT
	SysColorCaptionText             SystemColor = win.COLOR_CAPTIONTEXT
	SysColorGrayText                SystemColor = win.COLOR_GRAYTEXT
	SysColorHighlight               SystemColor = win.COLOR_HIGHLIGHT
	SysColorHighlightText           SystemColor = win.COLOR_HIGHLIGHTTEXT
	SysColorInactiveBorder          SystemColor = win.COLOR_INACTIVEBORDER
	SysColorInactiveCaption         SystemColor = win.COLOR_INACTIVECAPTION
	SysColorInactiveCaptionText     SystemColor = win.COLOR_INACTIVECAPTIONTEXT
	SysColorInfoBk                  SystemColor = win.COLOR_INFOBK
	SysColorInfoText                SystemColor = win.COLOR_INFOTEXT
	SysColorMenu                    SystemColor = win.COLOR_MENU
	SysColorMenuText                SystemColor = win.COLOR_MENUTEXT
	SysColorScrollBar               SystemColor = win.COLOR_SCROLLBAR
	SysColorWindow                  SystemColor = win.COLOR_WINDOW
	SysColorWindowFrame             SystemColor = win.COLOR_WINDOWFRAME
	SysColorWindowText              SystemColor = win.COLOR_WINDOWTEXT
	SysColorHotLight                SystemColor = win.COLOR_HOTLIGHT
	SysColorGradientActiveCaption   SystemColor = win.COLOR_GRADIENTACTIVECAPTION
	SysColorGradientInactiveCaption SystemColor = win.COLOR_GRADIENTINACTIVECAPTION
)
type SystemColorBrush ¶
type SystemColorBrush struct {
	// contains filtered or unexported fields
}
func NewSystemColorBrush ¶
func NewSystemColorBrush(sysColor SystemColor) (*SystemColorBrush, error)
func (*SystemColorBrush) Color ¶
func (b *SystemColorBrush) Color() Color
func (*SystemColorBrush) Dispose ¶
func (*SystemColorBrush) Dispose()
func (*SystemColorBrush) SystemColor ¶
func (b *SystemColorBrush) SystemColor() SystemColor
type TabPage ¶
type TabPage struct {
	ContainerBase
	// contains filtered or unexported fields
}
func NewTabPage ¶
func NewTabPage() (*TabPage, error)
func (*TabPage) Background ¶
func (tp *TabPage) Background() Brush
func (*TabPage) Enabled ¶
func (tp *TabPage) Enabled() bool
func (*TabPage) Font ¶
func (tp *TabPage) Font() *Font
func (*TabPage) Image ¶
func (tp *TabPage) Image() Image
func (*TabPage) SetImage ¶
func (tp *TabPage) SetImage(value Image) error
func (*TabPage) SetTitle ¶
func (tp *TabPage) SetTitle(value string) error
func (*TabPage) Title ¶
func (tp *TabPage) Title() string
type TabPageList ¶
type TabPageList struct {
	// contains filtered or unexported fields
}
func (*TabPageList) Add ¶
func (l *TabPageList) Add(item *TabPage) error
func (*TabPageList) At ¶
func (l *TabPageList) At(index int) *TabPage
func (*TabPageList) Clear ¶
func (l *TabPageList) Clear() error
func (*TabPageList) Contains ¶
func (l *TabPageList) Contains(item *TabPage) bool
func (*TabPageList) Index ¶
func (l *TabPageList) Index(item *TabPage) int
func (*TabPageList) Insert ¶
func (l *TabPageList) Insert(index int, item *TabPage) error
func (*TabPageList) Len ¶
func (l *TabPageList) Len() int
func (*TabPageList) Remove ¶
func (l *TabPageList) Remove(item *TabPage) error
func (*TabPageList) RemoveAt ¶
func (l *TabPageList) RemoveAt(index int) error
type TabWidget ¶
type TabWidget struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewTabWidget ¶
func NewTabWidget(parent Container) (*TabWidget, error)
func (*TabWidget) ApplyDPI ¶
func (tw *TabWidget) ApplyDPI(dpi int)
func (*TabWidget) CreateLayoutItem ¶
func (tw *TabWidget) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*TabWidget) CurrentIndex ¶
func (tw *TabWidget) CurrentIndex() int
func (*TabWidget) CurrentIndexChanged ¶
func (tw *TabWidget) CurrentIndexChanged() *Event
func (*TabWidget) Dispose ¶
func (tw *TabWidget) Dispose()
func (*TabWidget) Pages ¶
func (tw *TabWidget) Pages() *TabPageList
func (*TabWidget) Persistent ¶
func (tw *TabWidget) Persistent() bool
func (*TabWidget) RestoreState ¶
func (tw *TabWidget) RestoreState() error
func (*TabWidget) SaveState ¶
func (tw *TabWidget) SaveState() error
func (*TabWidget) SetCurrentIndex ¶
func (tw *TabWidget) SetCurrentIndex(index int) error
func (*TabWidget) SetPersistent ¶
func (tw *TabWidget) SetPersistent(value bool)
func (*TabWidget) WndProc ¶
func (tw *TabWidget) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type TableModel ¶
type TableModel interface {
	// RowCount returns the number of rows in the model.
	RowCount() int

	// Value returns the value that should be displayed for the given cell.
	Value(row, col int) interface{}

	// RowsReset returns the event that the model should publish when the number
	// of its rows changes.
	RowsReset() *Event

	// RowChanged returns the event that the model should publish when a row was
	// changed.
	RowChanged() *IntEvent

	// RowsChanged returns the event that the model should publish when a
	// contiguous range of items was changed.
	RowsChanged() *IntRangeEvent

	// RowsInserted returns the event that the model should publish when a
	// contiguous range of items was inserted. If the model supports sorting, it
	// is assumed to be sorted before the model publishes the event.
	RowsInserted() *IntRangeEvent

	// RowsRemoved returns the event that the model should publish when a
	// contiguous range of items was removed.
	RowsRemoved() *IntRangeEvent
}
TableModel is the interface that a model must implement to support widgets like TableView.

type TableModelBase ¶
type TableModelBase struct {
	// contains filtered or unexported fields
}
TableModelBase implements the RowsReset and RowChanged methods of the TableModel interface.

func (*TableModelBase) PublishRowChanged ¶
func (tmb *TableModelBase) PublishRowChanged(row int)
func (*TableModelBase) PublishRowsChanged ¶
func (tmb *TableModelBase) PublishRowsChanged(from, to int)
func (*TableModelBase) PublishRowsInserted ¶
func (tmb *TableModelBase) PublishRowsInserted(from, to int)
func (*TableModelBase) PublishRowsRemoved ¶
func (tmb *TableModelBase) PublishRowsRemoved(from, to int)
func (*TableModelBase) PublishRowsReset ¶
func (tmb *TableModelBase) PublishRowsReset()
func (*TableModelBase) RowChanged ¶
func (tmb *TableModelBase) RowChanged() *IntEvent
func (*TableModelBase) RowsChanged ¶
func (tmb *TableModelBase) RowsChanged() *IntRangeEvent
func (*TableModelBase) RowsInserted ¶
func (tmb *TableModelBase) RowsInserted() *IntRangeEvent
func (*TableModelBase) RowsRemoved ¶
func (tmb *TableModelBase) RowsRemoved() *IntRangeEvent
func (*TableModelBase) RowsReset ¶
func (tmb *TableModelBase) RowsReset() *Event
type TableView ¶
type TableView struct {
	WidgetBase
	// contains filtered or unexported fields
}
TableView is a model based widget for record centric, tabular data.

TableView is implemented as a virtual mode list view to support quite large amounts of data.

func NewTableView ¶
func NewTableView(parent Container) (*TableView, error)
NewTableView creates and returns a *TableView as child of the specified Container.

func NewTableViewWithCfg ¶
func NewTableViewWithCfg(parent Container, cfg *TableViewCfg) (*TableView, error)
NewTableViewWithCfg creates and returns a *TableView as child of the specified Container and with the provided additional configuration.

func NewTableViewWithStyle ¶
func NewTableViewWithStyle(parent Container, style uint32) (*TableView, error)
NewTableViewWithStyle creates and returns a *TableView as child of the specified Container and with the provided additional style bits set.

func (*TableView) AlternatingRowBG ¶
func (tv *TableView) AlternatingRowBG() bool
AlternatingRowBG returns the alternating row background.

func (*TableView) ApplyDPI ¶
func (tv *TableView) ApplyDPI(dpi int)
func (*TableView) ApplySysColors ¶
func (tv *TableView) ApplySysColors()
func (*TableView) CellStyler ¶
func (tv *TableView) CellStyler() CellStyler
CellStyler returns the CellStyler of the TableView.

func (*TableView) CheckBoxes ¶
func (tv *TableView) CheckBoxes() bool
CheckBoxes returns if the *TableView has check boxes.

func (*TableView) ColumnClicked ¶
func (tv *TableView) ColumnClicked() *IntEvent
ColumnClicked returns the event that is published after a column header was clicked.

func (*TableView) Columns ¶
func (tv *TableView) Columns() *TableViewColumnList
Columns returns the list of columns.

func (*TableView) ColumnsOrderable ¶
func (tv *TableView) ColumnsOrderable() bool
ColumnsOrderable returns if the user can reorder columns by dragging and dropping column headers.

func (*TableView) ColumnsSizable ¶
func (tv *TableView) ColumnsSizable() bool
ColumnsSizable returns if the user can change column widths by dragging dividers in the header.

func (*TableView) ContextMenuLocation ¶
func (tv *TableView) ContextMenuLocation() Point
ContextMenuLocation returns selected item position in screen coordinates in native pixels.

func (*TableView) CreateLayoutItem ¶
func (*TableView) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*TableView) CurrentIndex ¶
func (tv *TableView) CurrentIndex() int
CurrentIndex returns the index of the current item, or -1 if there is no current item.

func (*TableView) CurrentIndexChanged ¶
func (tv *TableView) CurrentIndexChanged() *Event
CurrentIndexChanged is the event that is published after CurrentIndex has changed.

func (*TableView) CurrentItemChanged ¶
func (tv *TableView) CurrentItemChanged() *Event
CurrentItemChanged returns the event that is published after the current item has changed.

For this to work, the model must implement the IDProvider interface.

func (*TableView) Dispose ¶
func (tv *TableView) Dispose()
Dispose releases the operating system resources, associated with the *TableView.

func (*TableView) EnsureItemVisible ¶
func (tv *TableView) EnsureItemVisible(index int)
EnsureItemVisible ensures the item at position index is visible, scrolling if necessary.

func (*TableView) Focused ¶
func (tv *TableView) Focused() bool
func (*TableView) Gridlines ¶
func (tv *TableView) Gridlines() bool
Gridlines returns if the rows are separated by grid lines.

func (*TableView) HeaderHidden ¶
func (tv *TableView) HeaderHidden() bool
HeaderHidden returns whether the column header is hidden.

func (*TableView) IgnoreNowhere ¶
func (tv *TableView) IgnoreNowhere() bool
IgnoreNowhere returns if the *TableView should ignore left mouse clicks in the empty space. It forbids the user from unselecting the current index, or when multi selection is enabled, disables click drag selection.

func (*TableView) IndexAt ¶
func (tv *TableView) IndexAt(x, y int) int
IndexAt returns the item index at coordinates x, y of the TableView or -1, if that point is not inside any item.

func (*TableView) Invalidate ¶
func (tv *TableView) Invalidate() error
func (*TableView) ItemActivated ¶
func (tv *TableView) ItemActivated() *Event
ItemActivated returns the event that is published after an item was activated.

An item is activated when it is double clicked or the enter key is pressed when the item is selected.

func (*TableView) ItemChecker ¶
func (tv *TableView) ItemChecker() ItemChecker
ItemChecker returns the ItemChecker of the TableView.

func (*TableView) ItemCountChanged ¶
func (tv *TableView) ItemCountChanged() *Event
ItemCountChanged returns the event that is published when the number of items in the model of the TableView changed.

func (*TableView) ItemStateChangedEventDelay ¶
func (tv *TableView) ItemStateChangedEventDelay() int
ItemStateChangedEventDelay returns the delay in milliseconds, between the moment the state of an item in the *TableView changes and the moment the associated event is published.

By default there is no delay.

func (*TableView) ItemVisible ¶
func (tv *TableView) ItemVisible(index int) bool
ItemVisible returns whether the item at position index is visible.

func (*TableView) LastColumnStretched ¶
func (tv *TableView) LastColumnStretched() bool
LastColumnStretched returns if the last column should take up all remaining horizontal space of the *TableView.

func (*TableView) Model ¶
func (tv *TableView) Model() interface{}
Model returns the model of the TableView.

func (*TableView) MultiSelection ¶
func (tv *TableView) MultiSelection() bool
MultiSelection returns whether multiple items can be selected at once.

By default only a single item can be selected at once.

func (*TableView) Persistent ¶
func (tv *TableView) Persistent() bool
Persistent returns if the *TableView should persist its UI state, like column widths. See *App.Settings for details.

func (*TableView) RestoreState ¶
func (tv *TableView) RestoreState() error
RestoreState restores the UI state of the *TableView from the settings.

func (*TableView) RestoringCurrentItemOnReset ¶
func (tv *TableView) RestoringCurrentItemOnReset() bool
RestoringCurrentItemOnReset returns whether the TableView after its model has been reset should attempt to restore CurrentIndex to the item that was current before the reset.

For this to work, the model must implement the IDProvider interface.

func (*TableView) RowsPerPage ¶
func (tv *TableView) RowsPerPage() int
RowsPerPage returns the number of fully visible rows.

func (*TableView) SaveState ¶
func (tv *TableView) SaveState() error
SaveState writes the UI state of the *TableView to the settings.

func (*TableView) ScrollbarOrientation ¶
func (tv *TableView) ScrollbarOrientation() Orientation
func (*TableView) SelectedIndexes ¶
func (tv *TableView) SelectedIndexes() []int
SelectedIndexes returns the indexes of the currently selected items.

func (*TableView) SelectedIndexesChanged ¶
func (tv *TableView) SelectedIndexesChanged() *Event
SelectedIndexesChanged returns the event that is published when the list of selected item indexes changed.

func (*TableView) SelectionHiddenWithoutFocus ¶
func (tv *TableView) SelectionHiddenWithoutFocus() bool
SelectionHiddenWithoutFocus returns whether selection indicators are hidden when the TableView does not have the keyboard input focus.

func (*TableView) SetAlternatingRowBG ¶
func (tv *TableView) SetAlternatingRowBG(enabled bool)
SetAlternatingRowBG sets the alternating row background.

func (*TableView) SetCellStyler ¶
func (tv *TableView) SetCellStyler(styler CellStyler)
SetCellStyler sets the CellStyler of the TableView.

func (*TableView) SetCheckBoxes ¶
func (tv *TableView) SetCheckBoxes(checkBoxes bool)
SetCheckBoxes sets if the *TableView has check boxes.

func (*TableView) SetColumnsOrderable ¶
func (tv *TableView) SetColumnsOrderable(enabled bool)
SetColumnsOrderable sets if the user can reorder columns by dragging and dropping column headers.

func (*TableView) SetColumnsSizable ¶
func (tv *TableView) SetColumnsSizable(b bool) error
SetColumnsSizable sets if the user can change column widths by dragging dividers in the header.

func (*TableView) SetCurrentIndex ¶
func (tv *TableView) SetCurrentIndex(index int) error
SetCurrentIndex sets the index of the current item.

Call this with a value of -1 to have no current item.

func (*TableView) SetGridlines ¶
func (tv *TableView) SetGridlines(enabled bool)
SetGridlines sets if the rows are separated by grid lines.

func (*TableView) SetHeaderHidden ¶
func (tv *TableView) SetHeaderHidden(hidden bool) error
SetHeaderHidden sets whether the column header is hidden.

func (*TableView) SetIgnoreNowhere ¶
func (tv *TableView) SetIgnoreNowhere(value bool)
IgnoreNowhere sets if the *TableView should ignore left mouse clicks in the empty space. It forbids the user from unselecting the current index, or when multi selection is enabled, disables click drag selection.

func (*TableView) SetItemChecker ¶
func (tv *TableView) SetItemChecker(itemChecker ItemChecker)
SetItemChecker sets the ItemChecker of the TableView.

func (*TableView) SetItemStateChangedEventDelay ¶
func (tv *TableView) SetItemStateChangedEventDelay(delay int)
SetItemStateChangedEventDelay sets the delay in milliseconds, between the moment the state of an item in the *TableView changes and the moment the associated event is published.

An example where this may be useful is a master-details scenario. If the master TableView is configured to delay the event, you can avoid pointless updates of the details TableView, if the user uses arrow keys to rapidly navigate the master view.

func (*TableView) SetLastColumnStretched ¶
func (tv *TableView) SetLastColumnStretched(value bool) error
SetLastColumnStretched sets if the last column should take up all remaining horizontal space of the *TableView.

The effect of setting this is persistent.

func (*TableView) SetModel ¶
func (tv *TableView) SetModel(mdl interface{}) error
SetModel sets the model of the TableView.

It is required that mdl either implements walk.TableModel, walk.ReflectTableModel or be a slice of pointers to struct or a []map[string]interface{}. A walk.TableModel implementation must also implement walk.Sorter to support sorting, all other options get sorting for free. To support item check boxes and icons, mdl must implement walk.ItemChecker and walk.ImageProvider, respectively. On-demand model population for a walk.ReflectTableModel or slice requires mdl to implement walk.Populator.

func (*TableView) SetMultiSelection ¶
func (tv *TableView) SetMultiSelection(multiSel bool) error
SetMultiSelection sets whether multiple items can be selected at once.

func (*TableView) SetPersistent ¶
func (tv *TableView) SetPersistent(value bool)
SetPersistent sets if the *TableView should persist its UI state, like column widths. See *App.Settings for details.

func (*TableView) SetRestoringCurrentItemOnReset ¶
func (tv *TableView) SetRestoringCurrentItemOnReset(restoring bool)
SetRestoringCurrentItemOnReset sets whether the TableView after its model has been reset should attempt to restore CurrentIndex to the item that was current before the reset.

For this to work, the model must implement the IDProvider interface.

func (*TableView) SetScrollbarOrientation ¶
func (tv *TableView) SetScrollbarOrientation(orientation Orientation)
func (*TableView) SetSelectedIndexes ¶
func (tv *TableView) SetSelectedIndexes(indexes []int) error
SetSelectedIndexes sets the indexes of the currently selected items.

func (*TableView) SetSelectionHiddenWithoutFocus ¶
func (tv *TableView) SetSelectionHiddenWithoutFocus(hidden bool) error
SetSelectionHiddenWithoutFocus sets whether selection indicators are visible when the TableView does not have the keyboard input focus.

func (*TableView) SortableByHeaderClick ¶
func (tv *TableView) SortableByHeaderClick() bool
SortableByHeaderClick returns if the user can change sorting by clicking the header.

func (*TableView) StretchLastColumn ¶
func (tv *TableView) StretchLastColumn() error
StretchLastColumn makes the last column take up all remaining horizontal space of the *TableView.

The effect of this is not persistent.

func (*TableView) TableModel ¶
func (tv *TableView) TableModel() TableModel
TableModel returns the TableModel of the TableView.

func (*TableView) UpdateItem ¶
func (tv *TableView) UpdateItem(index int) error
UpdateItem ensures the item at index will be redrawn.

If the model supports sorting, it will be resorted.

func (*TableView) VisibleColumnsInDisplayOrder ¶
func (tv *TableView) VisibleColumnsInDisplayOrder() []*TableViewColumn
VisibleColumnsInDisplayOrder returns a slice of visible columns in display order.

func (*TableView) WndProc ¶
func (tv *TableView) WndProc(hwnd win.HWND, msg uint32, wp, lp uintptr) uintptr
type TableViewCfg ¶
type TableViewCfg struct {
	Style              uint32
	CustomHeaderHeight int // in native pixels?
	CustomRowHeight    int // in native pixels?
}
type TableViewColumn ¶
type TableViewColumn struct {
	// contains filtered or unexported fields
}
TableViewColumn represents a column in a TableView.

func NewTableViewColumn ¶
func NewTableViewColumn() *TableViewColumn
NewTableViewColumn returns a new TableViewColumn.

func (*TableViewColumn) Alignment ¶
func (tvc *TableViewColumn) Alignment() Alignment1D
Alignment returns the alignment of the TableViewColumn.

func (*TableViewColumn) DataMember ¶
func (tvc *TableViewColumn) DataMember() string
DataMember returns the data member this TableViewColumn is bound against.

func (*TableViewColumn) DataMemberEffective ¶
func (tvc *TableViewColumn) DataMemberEffective() string
DataMemberEffective returns the effective data member this TableViewColumn is bound against.

func (*TableViewColumn) Format ¶
func (tvc *TableViewColumn) Format() string
Format returns the format string for converting a value into a string.

func (*TableViewColumn) FormatFunc ¶
func (tvc *TableViewColumn) FormatFunc() func(value interface{}) string
FormatFunc returns the custom format func of this TableViewColumn.

func (*TableViewColumn) Frozen ¶
func (tvc *TableViewColumn) Frozen() bool
Frozen returns if the column is frozen.

func (*TableViewColumn) LessFunc ¶
func (tvc *TableViewColumn) LessFunc() func(i, j int) bool
LessFunc returns the less func of this TableViewColumn.

This function is used to provide custom sorting for models based on ReflectTableModel only.

func (*TableViewColumn) Name ¶
func (tvc *TableViewColumn) Name() string
Name returns the name of this TableViewColumn.

func (*TableViewColumn) Precision ¶
func (tvc *TableViewColumn) Precision() int
Precision returns the number of decimal places for formatting float32, float64 or big.Rat values.

func (*TableViewColumn) SetAlignment ¶
func (tvc *TableViewColumn) SetAlignment(alignment Alignment1D) (err error)
SetAlignment sets the alignment of the TableViewColumn.

func (*TableViewColumn) SetDataMember ¶
func (tvc *TableViewColumn) SetDataMember(dataMember string)
SetDataMember sets the data member this TableViewColumn is bound against.

func (*TableViewColumn) SetFormat ¶
func (tvc *TableViewColumn) SetFormat(format string) (err error)
SetFormat sets the format string for converting a value into a string.

func (*TableViewColumn) SetFormatFunc ¶
func (tvc *TableViewColumn) SetFormatFunc(formatFunc func(value interface{}) string)
FormatFunc sets the custom format func of this TableViewColumn.

func (*TableViewColumn) SetFrozen ¶
func (tvc *TableViewColumn) SetFrozen(frozen bool) (err error)
SetFrozen sets if the column is frozen.

func (*TableViewColumn) SetLessFunc ¶
func (tvc *TableViewColumn) SetLessFunc(lessFunc func(i, j int) bool)
SetLessFunc sets the less func of this TableViewColumn.

This function is used to provide custom sorting for models based on ReflectTableModel only.

func (*TableViewColumn) SetName ¶
func (tvc *TableViewColumn) SetName(name string)
SetName sets the name of this TableViewColumn.

func (*TableViewColumn) SetPrecision ¶
func (tvc *TableViewColumn) SetPrecision(precision int) (err error)
SetPrecision sets the number of decimal places for formatting float32, float64 or big.Rat values.

func (*TableViewColumn) SetTitle ¶
func (tvc *TableViewColumn) SetTitle(title string) (err error)
SetTitle sets the (default) text to display in the column header.

func (*TableViewColumn) SetTitleOverride ¶
func (tvc *TableViewColumn) SetTitleOverride(titleOverride string) (err error)
SetTitleOverride sets the (overridden by user) text to display in the column header.

func (*TableViewColumn) SetVisible ¶
func (tvc *TableViewColumn) SetVisible(visible bool) (err error)
SetVisible sets if the column is visible.

func (*TableViewColumn) SetWidth ¶
func (tvc *TableViewColumn) SetWidth(width int) (err error)
SetWidth sets the width of the column in pixels.

func (*TableViewColumn) Title ¶
func (tvc *TableViewColumn) Title() string
Title returns the (default) text to display in the column header.

func (*TableViewColumn) TitleEffective ¶
func (tvc *TableViewColumn) TitleEffective() string
TitleEffective returns the effective text to display in the column header.

func (*TableViewColumn) TitleOverride ¶
func (tvc *TableViewColumn) TitleOverride() string
TitleOverride returns the (overridden by user) text to display in the column header.

func (*TableViewColumn) Visible ¶
func (tvc *TableViewColumn) Visible() bool
Visible returns if the column is visible.

func (*TableViewColumn) Width ¶
func (tvc *TableViewColumn) Width() int
Width returns the width of the column in pixels.

type TableViewColumnList ¶
type TableViewColumnList struct {
	// contains filtered or unexported fields
}
func (*TableViewColumnList) Add ¶
func (l *TableViewColumnList) Add(item *TableViewColumn) error
Add adds a TableViewColumn to the end of the list.

func (*TableViewColumnList) At ¶
func (l *TableViewColumnList) At(index int) *TableViewColumn
At returns the TableViewColumn as the specified index.

Bounds are not checked.

func (*TableViewColumnList) ByName ¶
func (l *TableViewColumnList) ByName(name string) *TableViewColumn
ByName returns the TableViewColumn identified by name, or nil, if no column of that name is contained in the TableViewColumnList.

func (*TableViewColumnList) Clear ¶
func (l *TableViewColumnList) Clear() error
Clear removes all TableViewColumns from the list.

func (*TableViewColumnList) Contains ¶
func (l *TableViewColumnList) Contains(item *TableViewColumn) bool
Contains returns whether the specified TableViewColumn is found in the list.

func (*TableViewColumnList) Index ¶
func (l *TableViewColumnList) Index(item *TableViewColumn) int
Index returns the index of the specified TableViewColumn or -1 if it is not found.

func (*TableViewColumnList) Insert ¶
func (l *TableViewColumnList) Insert(index int, item *TableViewColumn) error
Insert inserts TableViewColumn item at position index.

A TableViewColumn cannot be contained in multiple TableViewColumnLists at the same time.

func (*TableViewColumnList) Len ¶
func (l *TableViewColumnList) Len() int
Len returns the number of TableViewColumns in the list.

func (*TableViewColumnList) Remove ¶
func (l *TableViewColumnList) Remove(item *TableViewColumn) error
Remove removes the specified TableViewColumn from the list.

func (*TableViewColumnList) RemoveAt ¶
func (l *TableViewColumnList) RemoveAt(index int) error
RemoveAt removes the TableViewColumn at position index.

type TextEdit ¶
type TextEdit struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewTextEdit ¶
func NewTextEdit(parent Container) (*TextEdit, error)
func NewTextEditWithStyle ¶
func NewTextEditWithStyle(parent Container, style uint32) (*TextEdit, error)
func (*TextEdit) AppendText ¶
func (te *TextEdit) AppendText(value string)
func (*TextEdit) CompactHeight ¶
func (te *TextEdit) CompactHeight() bool
func (*TextEdit) ContextMenuLocation ¶
func (te *TextEdit) ContextMenuLocation() Point
ContextMenuLocation returns carret position in screen coordinates in native pixels.

func (*TextEdit) CreateLayoutItem ¶
func (te *TextEdit) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*TextEdit) MaxLength ¶
func (te *TextEdit) MaxLength() int
func (*TextEdit) NeedsWmSize ¶
func (*TextEdit) NeedsWmSize() bool
func (*TextEdit) ReadOnly ¶
func (te *TextEdit) ReadOnly() bool
func (*TextEdit) ReplaceSelectedText ¶
func (te *TextEdit) ReplaceSelectedText(text string, canUndo bool)
func (*TextEdit) ScrollToCaret ¶
func (te *TextEdit) ScrollToCaret()
func (*TextEdit) SetCompactHeight ¶
func (te *TextEdit) SetCompactHeight(enabled bool)
func (*TextEdit) SetMaxLength ¶
func (te *TextEdit) SetMaxLength(value int)
func (*TextEdit) SetReadOnly ¶
func (te *TextEdit) SetReadOnly(readOnly bool) error
func (*TextEdit) SetText ¶
func (te *TextEdit) SetText(text string) (err error)
func (*TextEdit) SetTextAlignment ¶
func (te *TextEdit) SetTextAlignment(alignment Alignment1D) error
func (*TextEdit) SetTextColor ¶
func (te *TextEdit) SetTextColor(c Color)
func (*TextEdit) SetTextSelection ¶
func (te *TextEdit) SetTextSelection(start, end int)
func (*TextEdit) Text ¶
func (te *TextEdit) Text() string
func (*TextEdit) TextAlignment ¶
func (te *TextEdit) TextAlignment() Alignment1D
func (*TextEdit) TextChanged ¶
func (te *TextEdit) TextChanged() *Event
func (*TextEdit) TextColor ¶
func (te *TextEdit) TextColor() Color
func (*TextEdit) TextLength ¶
func (te *TextEdit) TextLength() int
func (*TextEdit) TextSelection ¶
func (te *TextEdit) TextSelection() (start, end int)
func (*TextEdit) WndProc ¶
func (te *TextEdit) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type TextLabel ¶
type TextLabel struct {
	// contains filtered or unexported fields
}
func NewTextLabel ¶
func NewTextLabel(parent Container) (*TextLabel, error)
func NewTextLabelWithStyle ¶
func NewTextLabelWithStyle(parent Container, style uint32) (*TextLabel, error)
func (*TextLabel) CreateLayoutItem ¶
func (tl *TextLabel) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*TextLabel) Dispose ¶
func (s *TextLabel) Dispose()
func (*TextLabel) SetText ¶
func (tl *TextLabel) SetText(text string) error
func (*TextLabel) SetTextAlignment ¶
func (tl *TextLabel) SetTextAlignment(alignment Alignment2D) error
func (*TextLabel) SetTextColor ¶
func (s *TextLabel) SetTextColor(c Color)
func (*TextLabel) Text ¶
func (tl *TextLabel) Text() string
func (*TextLabel) TextAlignment ¶
func (tl *TextLabel) TextAlignment() Alignment2D
func (*TextLabel) TextColor ¶
func (s *TextLabel) TextColor() Color
func (*TextLabel) WndProc ¶
func (s *TextLabel) WndProc(hwnd win.HWND, msg uint32, wp, lp uintptr) uintptr
type ToolBar ¶
type ToolBar struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewToolBar ¶
func NewToolBar(parent Container) (*ToolBar, error)
func NewToolBarWithOrientationAndButtonStyle ¶
func NewToolBarWithOrientationAndButtonStyle(parent Container, orientation Orientation, buttonStyle ToolBarButtonStyle) (*ToolBar, error)
func NewVerticalToolBar ¶
func NewVerticalToolBar(parent Container) (*ToolBar, error)
func (*ToolBar) Actions ¶
func (tb *ToolBar) Actions() *ActionList
func (*ToolBar) ApplyDPI ¶
func (tb *ToolBar) ApplyDPI(dpi int)
func (*ToolBar) ButtonStyle ¶
func (tb *ToolBar) ButtonStyle() ToolBarButtonStyle
func (*ToolBar) CreateLayoutItem ¶
func (tb *ToolBar) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*ToolBar) DefaultButtonWidth ¶
func (tb *ToolBar) DefaultButtonWidth() int
DefaultButtonWidth returns the default button width of the ToolBar.

The default value for a horizontal ToolBar is 0, resulting in automatic sizing behavior. For a vertical ToolBar, the default is 100 pixels.

func (*ToolBar) Dispose ¶
func (tb *ToolBar) Dispose()
func (*ToolBar) ImageList ¶
func (tb *ToolBar) ImageList() *ImageList
func (*ToolBar) MaxTextRows ¶
func (tb *ToolBar) MaxTextRows() int
func (*ToolBar) Orientation ¶
func (tb *ToolBar) Orientation() Orientation
func (*ToolBar) SetDefaultButtonWidth ¶
func (tb *ToolBar) SetDefaultButtonWidth(width int) error
SetDefaultButtonWidth sets the default button width of the ToolBar.

Calling this method affects all buttons in the ToolBar, no matter if they are added before or after the call. A width of 0 results in automatic sizing behavior. Negative values are not allowed.

func (*ToolBar) SetImageList ¶
func (tb *ToolBar) SetImageList(value *ImageList)
func (*ToolBar) SetMaxTextRows ¶
func (tb *ToolBar) SetMaxTextRows(maxTextRows int) error
func (*ToolBar) WndProc ¶
func (tb *ToolBar) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type ToolBarButtonStyle ¶
type ToolBarButtonStyle int
const (
	ToolBarButtonImageOnly ToolBarButtonStyle = iota
	ToolBarButtonTextOnly
	ToolBarButtonImageBeforeText
	ToolBarButtonImageAboveText
)
type ToolButton ¶
type ToolButton struct {
	Button
}
func NewToolButton ¶
func NewToolButton(parent Container) (*ToolButton, error)
func (*ToolButton) CreateLayoutItem ¶
func (tb *ToolButton) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*ToolButton) WndProc ¶
func (tb *ToolButton) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type ToolTip ¶
type ToolTip struct {
	WindowBase
}
func NewToolTip ¶
func NewToolTip() (*ToolTip, error)
func (*ToolTip) AddTool ¶
func (tt *ToolTip) AddTool(tool Widget) error
func (*ToolTip) RemoveTool ¶
func (tt *ToolTip) RemoveTool(tool Widget) error
func (*ToolTip) SetErrorTitle ¶
func (tt *ToolTip) SetErrorTitle(title string) error
func (*ToolTip) SetInfoTitle ¶
func (tt *ToolTip) SetInfoTitle(title string) error
func (*ToolTip) SetText ¶
func (tt *ToolTip) SetText(tool Widget, text string) error
func (*ToolTip) SetTitle ¶
func (tt *ToolTip) SetTitle(title string) error
func (*ToolTip) SetWarningTitle ¶
func (tt *ToolTip) SetWarningTitle(title string) error
func (*ToolTip) Text ¶
func (tt *ToolTip) Text(tool Widget) string
func (*ToolTip) Title ¶
func (tt *ToolTip) Title() string
type ToolTipErrorPresenter ¶
type ToolTipErrorPresenter struct {
	// contains filtered or unexported fields
}
func NewToolTipErrorPresenter ¶
func NewToolTipErrorPresenter() (*ToolTipErrorPresenter, error)
func (*ToolTipErrorPresenter) Dispose ¶
func (ttep *ToolTipErrorPresenter) Dispose()
func (*ToolTipErrorPresenter) PresentError ¶
func (ttep *ToolTipErrorPresenter) PresentError(err error, widget Widget)
type TranslationFunction ¶
type TranslationFunction func(source string, context ...string) string
func TranslationFunc ¶
func TranslationFunc() TranslationFunction
type TreeItem ¶
type TreeItem interface {
	// Text returns the text of the item.
	Text() string

	// Parent returns the parent of the item.
	Parent() TreeItem

	// ChildCount returns the number of children of the item.
	ChildCount() int

	// ChildAt returns the child at the specified index.
	ChildAt(index int) TreeItem
}
TreeItem represents an item in a TreeView widget.

type TreeItemEvent ¶
type TreeItemEvent struct {
	// contains filtered or unexported fields
}
func (*TreeItemEvent) Attach ¶
func (e *TreeItemEvent) Attach(handler TreeItemEventHandler) int
func (*TreeItemEvent) Detach ¶
func (e *TreeItemEvent) Detach(handle int)
func (*TreeItemEvent) Once ¶
func (e *TreeItemEvent) Once(handler TreeItemEventHandler)
type TreeItemEventHandler ¶
type TreeItemEventHandler func(item TreeItem)
type TreeItemEventPublisher ¶
type TreeItemEventPublisher struct {
	// contains filtered or unexported fields
}
func (*TreeItemEventPublisher) Event ¶
func (p *TreeItemEventPublisher) Event() *TreeItemEvent
func (*TreeItemEventPublisher) Publish ¶
func (p *TreeItemEventPublisher) Publish(item TreeItem)
type TreeModel ¶
type TreeModel interface {
	// LazyPopulation returns if the model prefers on-demand population.
	//
	// This is useful for models that potentially contain huge amounts of items,
	// e.g. a model that represents a file system.
	LazyPopulation() bool

	// RootCount returns the number of root items.
	RootCount() int

	// RootAt returns the root item at the specified index.
	RootAt(index int) TreeItem

	// ItemsReset returns the event that the model should publish when the
	// descendants of the specified item, or all items if no item is specified,
	// are reset.
	ItemsReset() *TreeItemEvent

	// ItemChanged returns the event that the model should publish when an item
	// was changed.
	ItemChanged() *TreeItemEvent

	// ItemInserted returns the event that the model should publish when an item
	// was inserted into the model.
	ItemInserted() *TreeItemEvent

	// ItemRemoved returns the event that the model should publish when an item
	// was removed from the model.
	ItemRemoved() *TreeItemEvent
}
TreeModel provides widgets like TreeView with item data.

type TreeModelBase ¶
type TreeModelBase struct {
	// contains filtered or unexported fields
}
TreeModelBase partially implements the TreeModel interface.

You still need to provide your own implementation of at least the RootCount and RootAt methods. If your model needs lazy population, you will also have to implement LazyPopulation.

func (*TreeModelBase) ItemChanged ¶
func (tmb *TreeModelBase) ItemChanged() *TreeItemEvent
func (*TreeModelBase) ItemInserted ¶
func (tmb *TreeModelBase) ItemInserted() *TreeItemEvent
func (*TreeModelBase) ItemRemoved ¶
func (tmb *TreeModelBase) ItemRemoved() *TreeItemEvent
func (*TreeModelBase) ItemsReset ¶
func (tmb *TreeModelBase) ItemsReset() *TreeItemEvent
func (*TreeModelBase) LazyPopulation ¶
func (tmb *TreeModelBase) LazyPopulation() bool
func (*TreeModelBase) PublishItemChanged ¶
func (tmb *TreeModelBase) PublishItemChanged(item TreeItem)
func (*TreeModelBase) PublishItemInserted ¶
func (tmb *TreeModelBase) PublishItemInserted(item TreeItem)
func (*TreeModelBase) PublishItemRemoved ¶
func (tmb *TreeModelBase) PublishItemRemoved(item TreeItem)
func (*TreeModelBase) PublishItemsReset ¶
func (tmb *TreeModelBase) PublishItemsReset(parent TreeItem)
type TreeView ¶
type TreeView struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewTreeView ¶
func NewTreeView(parent Container) (*TreeView, error)
func (*TreeView) ApplyDPI ¶
func (tv *TreeView) ApplyDPI(dpi int)
func (*TreeView) CreateLayoutItem ¶
func (tv *TreeView) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*TreeView) CurrentItem ¶
func (tv *TreeView) CurrentItem() TreeItem
func (*TreeView) CurrentItemChanged ¶
func (tv *TreeView) CurrentItemChanged() *Event
func (*TreeView) Dispose ¶
func (tv *TreeView) Dispose()
func (*TreeView) EnsureVisible ¶
func (tv *TreeView) EnsureVisible(item TreeItem) error
func (*TreeView) Expanded ¶
func (tv *TreeView) Expanded(item TreeItem) bool
func (*TreeView) ExpandedChanged ¶
func (tv *TreeView) ExpandedChanged() *TreeItemEvent
func (*TreeView) ItemActivated ¶
func (tv *TreeView) ItemActivated() *Event
func (*TreeView) ItemAt ¶
func (tv *TreeView) ItemAt(x, y int) TreeItem
ItemAt determines the location of the specified point in native pixels relative to the client area of a tree-view control.

func (*TreeView) ItemHeight ¶
func (tv *TreeView) ItemHeight() int
ItemHeight returns the height of each item in native pixels.

func (*TreeView) Model ¶
func (tv *TreeView) Model() TreeModel
func (*TreeView) NeedsWmSize ¶
func (*TreeView) NeedsWmSize() bool
func (*TreeView) SetBackground ¶
func (tv *TreeView) SetBackground(bg Brush)
func (*TreeView) SetCurrentItem ¶
func (tv *TreeView) SetCurrentItem(item TreeItem) error
func (*TreeView) SetExpanded ¶
func (tv *TreeView) SetExpanded(item TreeItem, expanded bool) error
func (*TreeView) SetItemHeight ¶
func (tv *TreeView) SetItemHeight(height int)
SetItemHeight sets the height of the tree-view items in native pixels.

func (*TreeView) SetModel ¶
func (tv *TreeView) SetModel(model TreeModel) error
func (*TreeView) WndProc ¶
func (tv *TreeView) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type ValidationError ¶
type ValidationError struct {
	// contains filtered or unexported fields
}
func NewValidationError ¶
func NewValidationError(title, message string) *ValidationError
func (*ValidationError) Error ¶
func (ve *ValidationError) Error() string
func (*ValidationError) Message ¶
func (ve *ValidationError) Message() string
func (*ValidationError) Title ¶
func (ve *ValidationError) Title() string
type Validator ¶
type Validator interface {
	Validate(v interface{}) error
}
func SelectionRequiredValidator ¶
func SelectionRequiredValidator() Validator
type WebView ¶
type WebView struct {
	WidgetBase
	// contains filtered or unexported fields
}
func NewWebView ¶
func NewWebView(parent Container) (*WebView, error)
func (*WebView) BrowserVisible ¶
func (wv *WebView) BrowserVisible() bool
func (*WebView) BrowserVisibleChanged ¶
func (wv *WebView) BrowserVisibleChanged() *Event
func (*WebView) CanGoBack ¶
func (wv *WebView) CanGoBack() bool
func (*WebView) CanGoBackChanged ¶
func (wv *WebView) CanGoBackChanged() *Event
func (*WebView) CanGoForward ¶
func (wv *WebView) CanGoForward() bool
func (*WebView) CanGoForwardChanged ¶
func (wv *WebView) CanGoForwardChanged() *Event
func (*WebView) CreateLayoutItem ¶
func (wv *WebView) CreateLayoutItem(ctx *LayoutContext) LayoutItem
func (*WebView) Dispose ¶
func (wv *WebView) Dispose()
func (*WebView) DocumentCompleted ¶
func (wv *WebView) DocumentCompleted() *StringEvent
func (*WebView) DocumentTitle ¶
func (wv *WebView) DocumentTitle() string
func (*WebView) DocumentTitleChanged ¶
func (wv *WebView) DocumentTitleChanged() *Event
func (*WebView) Downloaded ¶
func (wv *WebView) Downloaded() *Event
func (*WebView) Downloading ¶
func (wv *WebView) Downloading() *Event
func (*WebView) IsTheaterMode ¶
func (wv *WebView) IsTheaterMode() bool
func (*WebView) NativeContextMenuEnabled ¶
func (wv *WebView) NativeContextMenuEnabled() bool
func (*WebView) NativeContextMenuEnabledChanged ¶
func (wv *WebView) NativeContextMenuEnabledChanged() *Event
func (*WebView) Navigated ¶
func (wv *WebView) Navigated() *StringEvent
func (*WebView) NavigatedError ¶
func (wv *WebView) NavigatedError() *WebViewNavigatedErrorEvent
func (*WebView) Navigating ¶
func (wv *WebView) Navigating() *WebViewNavigatingEvent
func (*WebView) NewWindow ¶
func (wv *WebView) NewWindow() *WebViewNewWindowEvent
func (*WebView) ProgressChanged ¶
func (wv *WebView) ProgressChanged() *Event
func (*WebView) ProgressMax ¶
func (wv *WebView) ProgressMax() int32
func (*WebView) ProgressValue ¶
func (wv *WebView) ProgressValue() int32
func (*WebView) Quitting ¶
func (wv *WebView) Quitting() *Event
func (*WebView) Refresh ¶
func (wv *WebView) Refresh() error
func (*WebView) SetNativeContextMenuEnabled ¶
func (wv *WebView) SetNativeContextMenuEnabled(value bool)
func (*WebView) SetShortcutsEnabled ¶
func (wv *WebView) SetShortcutsEnabled(value bool)
func (*WebView) SetURL ¶
func (wv *WebView) SetURL(url string) error
func (*WebView) ShortcutsEnabled ¶
func (wv *WebView) ShortcutsEnabled() bool
func (*WebView) ShortcutsEnabledChanged ¶
func (wv *WebView) ShortcutsEnabledChanged() *Event
func (*WebView) StatusBarVisible ¶
func (wv *WebView) StatusBarVisible() bool
func (*WebView) StatusBarVisibleChanged ¶
func (wv *WebView) StatusBarVisibleChanged() *Event
func (*WebView) StatusText ¶
func (wv *WebView) StatusText() string
func (*WebView) StatusTextChanged ¶
func (wv *WebView) StatusTextChanged() *Event
func (*WebView) TheaterModeChanged ¶
func (wv *WebView) TheaterModeChanged() *Event
func (*WebView) ToolBarEnabled ¶
func (wv *WebView) ToolBarEnabled() bool
func (*WebView) ToolBarEnabledChanged ¶
func (wv *WebView) ToolBarEnabledChanged() *Event
func (*WebView) ToolBarVisible ¶
func (wv *WebView) ToolBarVisible() bool
func (*WebView) ToolBarVisibleChanged ¶
func (wv *WebView) ToolBarVisibleChanged() *Event
func (*WebView) URL ¶
func (wv *WebView) URL() (url string, err error)
func (*WebView) URLChanged ¶
func (wv *WebView) URLChanged() *Event
func (*WebView) WindowClosing ¶
func (wv *WebView) WindowClosing() *WebViewWindowClosingEvent
func (*WebView) WndProc ¶
func (wv *WebView) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
type WebViewNavigatedErrorEvent ¶
type WebViewNavigatedErrorEvent struct {
	// contains filtered or unexported fields
}
func (*WebViewNavigatedErrorEvent) Attach ¶
func (e *WebViewNavigatedErrorEvent) Attach(handler WebViewNavigatedErrorEventHandler) int
func (*WebViewNavigatedErrorEvent) Detach ¶
func (e *WebViewNavigatedErrorEvent) Detach(handle int)
type WebViewNavigatedErrorEventData ¶
type WebViewNavigatedErrorEventData struct {
	// contains filtered or unexported fields
}
func (*WebViewNavigatedErrorEventData) Canceled ¶
func (eventData *WebViewNavigatedErrorEventData) Canceled() bool
func (*WebViewNavigatedErrorEventData) SetCanceled ¶
func (eventData *WebViewNavigatedErrorEventData) SetCanceled(value bool)
func (*WebViewNavigatedErrorEventData) StatusCode ¶
func (eventData *WebViewNavigatedErrorEventData) StatusCode() int32
func (*WebViewNavigatedErrorEventData) TargetFrameName ¶
func (eventData *WebViewNavigatedErrorEventData) TargetFrameName() string
func (*WebViewNavigatedErrorEventData) Url ¶
func (eventData *WebViewNavigatedErrorEventData) Url() string
type WebViewNavigatedErrorEventHandler ¶
type WebViewNavigatedErrorEventHandler func(eventData *WebViewNavigatedErrorEventData)
type WebViewNavigatedErrorEventPublisher ¶
type WebViewNavigatedErrorEventPublisher struct {
	// contains filtered or unexported fields
}
func (*WebViewNavigatedErrorEventPublisher) Event ¶
func (p *WebViewNavigatedErrorEventPublisher) Event() *WebViewNavigatedErrorEvent
func (*WebViewNavigatedErrorEventPublisher) Publish ¶
func (p *WebViewNavigatedErrorEventPublisher) Publish(eventData *WebViewNavigatedErrorEventData)
type WebViewNavigatingEvent ¶
type WebViewNavigatingEvent struct {
	// contains filtered or unexported fields
}
func (*WebViewNavigatingEvent) Attach ¶
func (e *WebViewNavigatingEvent) Attach(handler WebViewNavigatingEventHandler) int
func (*WebViewNavigatingEvent) Detach ¶
func (e *WebViewNavigatingEvent) Detach(handle int)
type WebViewNavigatingEventData ¶
type WebViewNavigatingEventData struct {
	// contains filtered or unexported fields
}
func (*WebViewNavigatingEventData) Canceled ¶
func (eventData *WebViewNavigatingEventData) Canceled() bool
func (*WebViewNavigatingEventData) Flags ¶
func (eventData *WebViewNavigatingEventData) Flags() int32
func (*WebViewNavigatingEventData) Headers ¶
func (eventData *WebViewNavigatingEventData) Headers() string
func (*WebViewNavigatingEventData) PostData ¶
func (eventData *WebViewNavigatingEventData) PostData() string
func (*WebViewNavigatingEventData) SetCanceled ¶
func (eventData *WebViewNavigatingEventData) SetCanceled(value bool)
func (*WebViewNavigatingEventData) TargetFrameName ¶
func (eventData *WebViewNavigatingEventData) TargetFrameName() string
func (*WebViewNavigatingEventData) Url ¶
func (eventData *WebViewNavigatingEventData) Url() string
type WebViewNavigatingEventHandler ¶
type WebViewNavigatingEventHandler func(eventData *WebViewNavigatingEventData)
type WebViewNavigatingEventPublisher ¶
type WebViewNavigatingEventPublisher struct {
	// contains filtered or unexported fields
}
func (*WebViewNavigatingEventPublisher) Event ¶
func (p *WebViewNavigatingEventPublisher) Event() *WebViewNavigatingEvent
func (*WebViewNavigatingEventPublisher) Publish ¶
func (p *WebViewNavigatingEventPublisher) Publish(eventData *WebViewNavigatingEventData)
type WebViewNewWindowEvent ¶
type WebViewNewWindowEvent struct {
	// contains filtered or unexported fields
}
func (*WebViewNewWindowEvent) Attach ¶
func (e *WebViewNewWindowEvent) Attach(handler WebViewNewWindowEventHandler) int
func (*WebViewNewWindowEvent) Detach ¶
func (e *WebViewNewWindowEvent) Detach(handle int)
type WebViewNewWindowEventData ¶
type WebViewNewWindowEventData struct {
	// contains filtered or unexported fields
}
func (*WebViewNewWindowEventData) Canceled ¶
func (eventData *WebViewNewWindowEventData) Canceled() bool
func (*WebViewNewWindowEventData) Flags ¶
func (eventData *WebViewNewWindowEventData) Flags() uint32
func (*WebViewNewWindowEventData) SetCanceled ¶
func (eventData *WebViewNewWindowEventData) SetCanceled(value bool)
func (*WebViewNewWindowEventData) Url ¶
func (eventData *WebViewNewWindowEventData) Url() string
func (*WebViewNewWindowEventData) UrlContext ¶
func (eventData *WebViewNewWindowEventData) UrlContext() string
type WebViewNewWindowEventHandler ¶
type WebViewNewWindowEventHandler func(eventData *WebViewNewWindowEventData)
type WebViewNewWindowEventPublisher ¶
type WebViewNewWindowEventPublisher struct {
	// contains filtered or unexported fields
}
func (*WebViewNewWindowEventPublisher) Event ¶
func (p *WebViewNewWindowEventPublisher) Event() *WebViewNewWindowEvent
func (*WebViewNewWindowEventPublisher) Publish ¶
func (p *WebViewNewWindowEventPublisher) Publish(eventData *WebViewNewWindowEventData)
type WebViewWindowClosingEvent ¶
type WebViewWindowClosingEvent struct {
	// contains filtered or unexported fields
}
func (*WebViewWindowClosingEvent) Attach ¶
func (e *WebViewWindowClosingEvent) Attach(handler WebViewWindowClosingEventHandler) int
func (*WebViewWindowClosingEvent) Detach ¶
func (e *WebViewWindowClosingEvent) Detach(handle int)
type WebViewWindowClosingEventData ¶
type WebViewWindowClosingEventData struct {
	// contains filtered or unexported fields
}
func (*WebViewWindowClosingEventData) Canceled ¶
func (eventData *WebViewWindowClosingEventData) Canceled() bool
func (*WebViewWindowClosingEventData) IsChildWindow ¶
func (eventData *WebViewWindowClosingEventData) IsChildWindow() bool
func (*WebViewWindowClosingEventData) SetCanceled ¶
func (eventData *WebViewWindowClosingEventData) SetCanceled(value bool)
type WebViewWindowClosingEventHandler ¶
type WebViewWindowClosingEventHandler func(eventData *WebViewWindowClosingEventData)
type WebViewWindowClosingEventPublisher ¶
type WebViewWindowClosingEventPublisher struct {
	// contains filtered or unexported fields
}
func (*WebViewWindowClosingEventPublisher) Event ¶
func (p *WebViewWindowClosingEventPublisher) Event() *WebViewWindowClosingEvent
func (*WebViewWindowClosingEventPublisher) Publish ¶
func (p *WebViewWindowClosingEventPublisher) Publish(eventData *WebViewWindowClosingEventData)
type Widget ¶
type Widget interface {
	Window

	// Alignment returns the alignment of the Widget.
	Alignment() Alignment2D

	// AlwaysConsumeSpace returns if the Widget should consume space even if it
	// is not visible.
	AlwaysConsumeSpace() bool

	// AsWidgetBase returns a *WidgetBase that implements Widget.
	AsWidgetBase() *WidgetBase

	// CreateLayoutItem creates and returns a new LayoutItem specific to the
	// concrete Widget type, that carries all data and logic required to layout
	// the Widget.
	CreateLayoutItem(ctx *LayoutContext) LayoutItem

	// GraphicsEffects returns a list of WidgetGraphicsEffects that are applied to the Widget.
	GraphicsEffects() *WidgetGraphicsEffectList

	// LayoutFlags returns a combination of LayoutFlags that specify how the
	// Widget wants to be treated by Layout implementations.
	LayoutFlags() LayoutFlags

	// MinSizeHint returns the minimum outer size in native pixels, including decorations, that
	// makes sense for the respective type of Widget.
	MinSizeHint() Size

	// Parent returns the Container of the Widget.
	Parent() Container

	// SetAlignment sets the alignment of the widget.
	SetAlignment(alignment Alignment2D) error

	// SetAlwaysConsumeSpace sets if the Widget should consume space even if it
	// is not visible.
	SetAlwaysConsumeSpace(b bool) error

	// SetParent sets the parent of the Widget and adds the Widget to the
	// Children list of the Container.
	SetParent(value Container) error

	// SetToolTipText sets the tool tip text of the Widget.
	SetToolTipText(s string) error

	// SizeHint returns the preferred size in native pixels for the respective type of Widget.
	SizeHint() Size

	// ToolTipText returns the tool tip text of the Widget.
	ToolTipText() string
}
func DescendantByName ¶
func DescendantByName(container Container, name string) Widget
type WidgetBase ¶
type WidgetBase struct {
	WindowBase
	// contains filtered or unexported fields
}
func (*WidgetBase) Alignment ¶
func (wb *WidgetBase) Alignment() Alignment2D
Alignment return the alignment ot the *WidgetBase.

func (*WidgetBase) AlwaysConsumeSpace ¶
func (wb *WidgetBase) AlwaysConsumeSpace() bool
AlwaysConsumeSpace returns if the Widget should consume space even if it is not visible.

func (*WidgetBase) AsWidgetBase ¶
func (wb *WidgetBase) AsWidgetBase() *WidgetBase
AsWidgetBase just returns the receiver.

func (*WidgetBase) Bounds ¶
func (wb *WidgetBase) Bounds() Rectangle
Bounds returns the outer bounding box rectangle of the WidgetBase, including decorations.

The coordinates are relative to the parent of the Widget.

func (*WidgetBase) BoundsPixels ¶
func (wb *WidgetBase) BoundsPixels() Rectangle
BoundsPixels returns the outer bounding box rectangle of the WidgetBase, including decorations.

The coordinates are relative to the parent of the Widget.

func (*WidgetBase) BringToTop ¶
func (wb *WidgetBase) BringToTop() error
BringToTop moves the WidgetBase to the top of the keyboard focus order.

func (*WidgetBase) Dispose ¶
func (wb *WidgetBase) Dispose()
func (*WidgetBase) Enabled ¶
func (wb *WidgetBase) Enabled() bool
Enabled returns if the WidgetBase is enabled for user interaction.

func (*WidgetBase) Font ¶
func (wb *WidgetBase) Font() *Font
Font returns the Font of the WidgetBase.

By default this is a MS Shell Dlg 2, 8 point font.

func (*WidgetBase) ForEachAncestor ¶
func (wb *WidgetBase) ForEachAncestor(f func(window Window) bool)
func (*WidgetBase) GraphicsEffects ¶
func (wb *WidgetBase) GraphicsEffects() *WidgetGraphicsEffectList
GraphicsEffects returns a list of WidgetGraphicsEffects that are applied to the WidgetBase.

func (*WidgetBase) LayoutFlags ¶
func (wb *WidgetBase) LayoutFlags() LayoutFlags
func (*WidgetBase) MinSizeHint ¶
func (wb *WidgetBase) MinSizeHint() Size
func (*WidgetBase) Parent ¶
func (wb *WidgetBase) Parent() Container
Parent returns the Container of the WidgetBase.

func (*WidgetBase) SetAlignment ¶
func (wb *WidgetBase) SetAlignment(alignment Alignment2D) error
SetAlignment sets the alignment of the *WidgetBase.

func (*WidgetBase) SetAlwaysConsumeSpace ¶
func (wb *WidgetBase) SetAlwaysConsumeSpace(b bool) error
SetAlwaysConsumeSpace sets if the Widget should consume space even if it is not visible.

func (*WidgetBase) SetMinMaxSize ¶
func (wb *WidgetBase) SetMinMaxSize(min, max Size) (err error)
SetMinMaxSize sets the minimum and maximum outer size of the *WidgetBase, including decorations.

Use walk.Size{} to make the respective limit be ignored.

func (*WidgetBase) SetParent ¶
func (wb *WidgetBase) SetParent(parent Container) (err error)
SetParent sets the parent of the WidgetBase and adds the WidgetBase to the Children list of the Container.

func (*WidgetBase) SetToolTipText ¶
func (wb *WidgetBase) SetToolTipText(s string) error
SetToolTipText sets the tool tip text of the WidgetBase.

func (*WidgetBase) SizeHint ¶
func (wb *WidgetBase) SizeHint() Size
func (*WidgetBase) ToolTipText ¶
func (wb *WidgetBase) ToolTipText() string
ToolTipText returns the tool tip text of the WidgetBase.

type WidgetGraphicsEffect ¶
type WidgetGraphicsEffect interface {
	Draw(widget Widget, canvas *Canvas) error
}
var (
	InteractionEffect WidgetGraphicsEffect
	FocusEffect       WidgetGraphicsEffect
)
var ValidationErrorEffect WidgetGraphicsEffect
type WidgetGraphicsEffectList ¶
type WidgetGraphicsEffectList struct {
	// contains filtered or unexported fields
}
func (*WidgetGraphicsEffectList) Add ¶
func (l *WidgetGraphicsEffectList) Add(effect WidgetGraphicsEffect) error
func (*WidgetGraphicsEffectList) At ¶
func (l *WidgetGraphicsEffectList) At(index int) WidgetGraphicsEffect
func (*WidgetGraphicsEffectList) Clear ¶
func (l *WidgetGraphicsEffectList) Clear() error
func (*WidgetGraphicsEffectList) Contains ¶
func (l *WidgetGraphicsEffectList) Contains(effect WidgetGraphicsEffect) bool
func (*WidgetGraphicsEffectList) Index ¶
func (l *WidgetGraphicsEffectList) Index(effect WidgetGraphicsEffect) int
func (*WidgetGraphicsEffectList) Insert ¶
func (l *WidgetGraphicsEffectList) Insert(index int, effect WidgetGraphicsEffect) error
func (*WidgetGraphicsEffectList) Len ¶
func (l *WidgetGraphicsEffectList) Len() int
func (*WidgetGraphicsEffectList) Remove ¶
func (l *WidgetGraphicsEffectList) Remove(effect WidgetGraphicsEffect) error
func (*WidgetGraphicsEffectList) RemoveAt ¶
func (l *WidgetGraphicsEffectList) RemoveAt(index int) error
type WidgetList ¶
type WidgetList struct {
	// contains filtered or unexported fields
}
func (*WidgetList) Add ¶
func (l *WidgetList) Add(item Widget) error
func (*WidgetList) At ¶
func (l *WidgetList) At(index int) Widget
func (*WidgetList) Clear ¶
func (l *WidgetList) Clear() error
func (*WidgetList) Contains ¶
func (l *WidgetList) Contains(item Widget) bool
func (*WidgetList) Index ¶
func (l *WidgetList) Index(item Widget) int
func (*WidgetList) Insert ¶
func (l *WidgetList) Insert(index int, item Widget) error
func (*WidgetList) Len ¶
func (l *WidgetList) Len() int
func (*WidgetList) Remove ¶
func (l *WidgetList) Remove(item Widget) error
func (*WidgetList) RemoveAt ¶
func (l *WidgetList) RemoveAt(index int) error
type Window ¶
type Window interface {
	// AddDisposable adds a Disposable resource that should be disposed of
	// together with this Window.
	AddDisposable(d Disposable)

	// AsWindowBase returns a *WindowBase, a pointer to an instance of the
	// struct that implements most operations common to all windows.
	AsWindowBase() *WindowBase

	// Accessibility returns the accessibility object used to set Dynamic Annotation properties of the
	// window.
	Accessibility() *Accessibility

	// Background returns the background Brush of the Window.
	//
	// By default this is nil.
	Background() Brush

	// Bounds returns the outer bounding box rectangle of the Window, including
	// decorations.
	//
	// For a Form, like *MainWindow or *Dialog, the rectangle is in screen
	// coordinates, for a child Window the coordinates are relative to its
	// parent.
	Bounds() Rectangle

	// BoundsPixels returns the outer bounding box rectangle of the Window, including
	// decorations.
	//
	// For a Form, like *MainWindow or *Dialog, the rectangle is in screen
	// coordinates, for a child Window the coordinates are relative to its
	// parent.
	BoundsPixels() Rectangle

	// BoundsChanged returns an *Event that you can attach to for handling bounds
	// changed events for the Window.
	BoundsChanged() *Event

	// BringToTop moves the Window to the top of the keyboard focus order.
	BringToTop() error

	// ClientBounds returns the inner bounding box rectangle of the Window,
	// excluding decorations.
	ClientBounds() Rectangle

	// ClientBoundsPixels returns the inner bounding box rectangle of the Window,
	// excluding decorations.
	ClientBoundsPixels() Rectangle

	// ContextMenu returns the context menu of the Window.
	//
	// By default this is nil.
	ContextMenu() *Menu

	// ContextMenuLocation returns the context menu suggested location in screen coordinates in
	// native pixels. This method is called when context menu is invoked using keyboard and mouse
	// coordinates are not available.
	ContextMenuLocation() Point

	// CreateCanvas creates and returns a *Canvas that can be used to draw
	// inside the ClientBoundsPixels of the Window.
	//
	// Remember to call the Dispose method on the canvas to release resources,
	// when you no longer need it.
	CreateCanvas() (*Canvas, error)

	// Cursor returns the Cursor of the Window.
	//
	// By default this is nil.
	Cursor() Cursor

	// Dispose releases the operating system resources, associated with the
	// Window.
	//
	// If a user closes a *MainWindow or *Dialog, it is automatically released.
	// Also, if a Container is disposed of, all its descendants will be released
	// as well.
	Dispose()

	// Disposing returns an Event that is published when the Window is disposed
	// of.
	Disposing() *Event

	// DoubleBuffering returns whether double buffering of the
	// drawing is enabled, which may help reduce flicker.
	DoubleBuffering() bool

	// DPI returns the current DPI value of the Window.
	DPI() int

	// Enabled returns if the Window is enabled for user interaction.
	Enabled() bool

	// Focused returns whether the Window has the keyboard input focus.
	Focused() bool

	// FocusedChanged returns an Event that you can attach to for handling focus
	// changed events for the Window.
	FocusedChanged() *Event

	// Font returns the *Font of the Window.
	//
	// By default this is a MS Shell Dlg 2, 8 point font.
	Font() *Font

	// Form returns the Form of the Window.
	Form() Form

	// Handle returns the window handle of the Window.
	Handle() win.HWND

	// Height returns the outer height of the Window, including decorations.
	Height() int

	// HeightPixels returns the outer height of the Window, including decorations.
	HeightPixels() int

	// Invalidate schedules a full repaint of the Window.
	Invalidate() error

	// IsDisposed returns if the Window has been disposed of.
	IsDisposed() bool

	// KeyDown returns a *KeyEvent that you can attach to for handling key down
	// events for the Window.
	KeyDown() *KeyEvent

	// KeyPress returns a *KeyEvent that you can attach to for handling key
	// press events for the Window.
	KeyPress() *KeyEvent

	// KeyUp returns a *KeyEvent that you can attach to for handling key up
	// events for the Window.
	KeyUp() *KeyEvent

	// MaxSize returns the maximum allowed outer size for the Window, including
	// decorations.
	//
	// For child windows, this is only relevant when the parent of the Window
	// has a Layout. RootWidgets, like *MainWindow and *Dialog, also honor this.
	MaxSize() Size

	// MaxSizePixels returns the maximum allowed outer size for the Window, including
	// decorations.
	//
	// For child windows, this is only relevant when the parent of the Window
	// has a Layout. RootWidgets, like *MainWindow and *Dialog, also honor this.
	MaxSizePixels() Size

	// MinSize returns the minimum allowed outer size for the Window, including
	// decorations.
	//
	// For child windows, this is only relevant when the parent of the Window
	// has a Layout. RootWidgets, like *MainWindow and *Dialog, also honor this.
	MinSize() Size

	// MinSizePixels returns the minimum allowed outer size for the Window, including
	// decorations.
	//
	// For child windows, this is only relevant when the parent of the Window
	// has a Layout. RootWidgets, like *MainWindow and *Dialog, also honor this.
	MinSizePixels() Size

	// MouseDown returns a *MouseEvent that you can attach to for handling
	// mouse down events for the Window.
	MouseDown() *MouseEvent

	// MouseMove returns a *MouseEvent that you can attach to for handling
	// mouse move events for the Window.
	MouseMove() *MouseEvent

	// MouseUp returns a *MouseEvent that you can attach to for handling
	// mouse up events for the Window.
	MouseUp() *MouseEvent

	// Name returns the name of the Window.
	Name() string

	// RequestLayout either schedules or immediately starts performing layout.
	RequestLayout()

	// RightToLeftReading returns whether the reading order of the Window
	// is from right to left.
	RightToLeftReading() bool

	// Screenshot returns an image of the window.
	Screenshot() (*image.RGBA, error)

	// SendMessage sends a message to the window and returns the result.
	SendMessage(msg uint32, wParam, lParam uintptr) uintptr

	// SetBackground sets the background Brush of the Window.
	SetBackground(value Brush)

	// SetBounds sets the outer bounding box rectangle of the Window, including
	// decorations.
	//
	// For a Form, like *MainWindow or *Dialog, the rectangle is in screen
	// coordinates, for a child Window the coordinates are relative to its
	// parent.
	SetBounds(value Rectangle) error

	// SetBoundsPixels sets the outer bounding box rectangle of the Window, including
	// decorations.
	//
	// For a Form, like *MainWindow or *Dialog, the rectangle is in screen
	// coordinates, for a child Window the coordinates are relative to its
	// parent.
	SetBoundsPixels(value Rectangle) error

	// SetClientSize sets the size of the inner bounding box of the Window,
	// excluding decorations.
	SetClientSize(value Size) error

	// SetClientSizePixels sets the size of the inner bounding box of the Window,
	// excluding decorations.
	SetClientSizePixels(value Size) error

	// SetContextMenu sets the context menu of the Window.
	SetContextMenu(value *Menu)

	// SetCursor sets the Cursor of the Window.
	SetCursor(value Cursor)

	// SetDoubleBuffering enables or disables double buffering of the
	// drawing, which may help reduce flicker.
	SetDoubleBuffering(value bool) error

	// SetEnabled sets if the Window is enabled for user interaction.
	SetEnabled(value bool)

	// SetFocus sets the keyboard input focus to the Window.
	SetFocus() error

	// SetFont sets the *Font of the Window.
	SetFont(value *Font)

	// SetHeight sets the outer height of the Window, including decorations.
	SetHeight(value int) error

	// SetHeightPixels sets the outer height of the Window, including decorations.
	SetHeightPixels(value int) error

	// SetMinMaxSize sets the minimum and maximum outer size of the Window,
	// including decorations.
	//
	// Use walk.Size{} to make the respective limit be ignored.
	SetMinMaxSize(min, max Size) error

	// SetMinMaxSizePixels sets the minimum and maximum outer size of the Window,
	// including decorations.
	//
	// Use walk.Size{} to make the respective limit be ignored.
	SetMinMaxSizePixels(min, max Size) error

	// SetName sets the name of the Window.
	//
	// This is important if you want to make use of the built-in UI persistence.
	// Some windows support automatic state persistence. See Settings for
	// details.
	SetName(name string)

	// SetRightToLeftReading sets whether the reading order of the Window
	// is from right to left.
	SetRightToLeftReading(rtl bool) error

	// SetSize sets the outer size of the Window, including decorations.
	SetSize(value Size) error

	// SetSizePixels sets the outer size of the Window, including decorations.
	SetSizePixels(value Size) error

	// SetSuspended sets if the Window is suspended for layout and repainting
	// purposes.
	//
	// You should call SetSuspended(true), before doing a batch of modifications
	// that would cause multiple layout or drawing updates. Remember to call
	// SetSuspended(false) afterwards, which will update the Window accordingly.
	SetSuspended(suspend bool)

	// SetVisible sets if the Window is visible.
	SetVisible(value bool)

	// SetWidth sets the outer width of the Window, including decorations.
	SetWidth(value int) error

	// SetWidthPixels sets the outer width of the Window, including decorations.
	SetWidthPixels(value int) error

	// SetX sets the x coordinate of the Window, relative to the screen for
	// RootWidgets like *MainWindow or *Dialog and relative to the parent for
	// child Windows.
	SetX(value int) error

	// SetXPixels sets the x coordinate of the Window, relative to the screen for
	// RootWidgets like *MainWindow or *Dialog and relative to the parent for
	// child Windows.
	SetXPixels(value int) error

	// SetY sets the y coordinate of the Window, relative to the screen for
	// RootWidgets like *MainWindow or *Dialog and relative to the parent for
	// child Windows.
	SetY(value int) error

	// SetYPixels sets the y coordinate of the Window, relative to the screen for
	// RootWidgets like *MainWindow or *Dialog and relative to the parent for
	// child Windows.
	SetYPixels(value int) error

	// Size returns the outer size of the Window, including decorations.
	Size() Size

	// SizePixels returns the outer size of the Window, including decorations.
	SizePixels() Size

	// SizeChanged returns an *Event that you can attach to for handling size
	// changed events for the Window.
	SizeChanged() *Event

	// Suspended returns if the Window is suspended for layout and repainting
	// purposes.
	Suspended() bool

	// Synchronize enqueues func f to be called some time later by the main
	// goroutine from inside a message loop.
	Synchronize(f func())

	// Visible returns if the Window is visible.
	Visible() bool

	// VisibleChanged returns an Event that you can attach to for handling
	// visible changed events for the Window.
	VisibleChanged() *Event

	// Width returns the outer width of the Window, including decorations.
	Width() int

	// WidthPixels returns the outer width of the Window, including decorations.
	WidthPixels() int

	// WndProc is the window procedure of the window.
	//
	// When implementing your own WndProc to add or modify behavior, call the
	// WndProc of the embedded window for messages you don't handle yourself.
	WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr

	// X returns the x coordinate of the Window, relative to the screen for
	// RootWidgets like *MainWindow or *Dialog and relative to the parent for
	// child Windows.
	X() int

	// XPixels returns the x coordinate of the Window, relative to the screen for
	// RootWidgets like *MainWindow or *Dialog and relative to the parent for
	// child Windows.
	XPixels() int

	// Y returns the y coordinate of the Window, relative to the screen for
	// RootWidgets like *MainWindow or *Dialog and relative to the parent for
	// child Windows.
	Y() int

	// YPixels returns the y coordinate of the Window, relative to the screen for
	// RootWidgets like *MainWindow or *Dialog and relative to the parent for
	// child Windows.
	YPixels() int
}
Window is an interface that provides operations common to all windows.

func FocusedWindow ¶
func FocusedWindow() Window
FocusedWindow returns the Window that has the keyboard input focus.

type WindowBase ¶
type WindowBase struct {
	// contains filtered or unexported fields
}
WindowBase implements many operations common to all Windows.

func (*WindowBase) Accessibility ¶
func (wb *WindowBase) Accessibility() *Accessibility
Accessibility returns the accessibility object used to set Dynamic Annotation properties of the window.

func (*WindowBase) AddDisposable ¶
func (wb *WindowBase) AddDisposable(d Disposable)
AddDisposable adds a Disposable resource that should be disposed of together with this Window.

func (*WindowBase) ApplyDPI ¶
func (wb *WindowBase) ApplyDPI(dpi int)
func (*WindowBase) ApplySysColors ¶
func (wb *WindowBase) ApplySysColors()
func (*WindowBase) AsWindowBase ¶
func (wb *WindowBase) AsWindowBase() *WindowBase
WindowBase simply returns the receiver.

func (*WindowBase) Background ¶
func (wb *WindowBase) Background() Brush
Background returns the background Brush of the *WindowBase.

By default this is nil.

func (*WindowBase) Bounds ¶
func (wb *WindowBase) Bounds() Rectangle
Bounds returns the outer bounding box rectangle of the *WindowBase, including decorations.

The coordinates are relative to the screen.

func (*WindowBase) BoundsChanged ¶
func (wb *WindowBase) BoundsChanged() *Event
BoundsChanged returns an *Event that you can attach to for handling bounds changed events for the *WindowBase.

func (*WindowBase) BoundsPixels ¶
func (wb *WindowBase) BoundsPixels() Rectangle
BoundsPixels returns the outer bounding box rectangle of the *WindowBase, including decorations.

The coordinates are relative to the screen.

func (*WindowBase) BringToTop ¶
func (wb *WindowBase) BringToTop() error
BringToTop moves the *WindowBase to the top of the keyboard focus order.

func (*WindowBase) ClientBounds ¶
func (wb *WindowBase) ClientBounds() Rectangle
ClientBounds returns the inner bounding box rectangle of the *WindowBase, excluding decorations.

func (*WindowBase) ClientBoundsPixels ¶
func (wb *WindowBase) ClientBoundsPixels() Rectangle
ClientBoundsPixels returns the inner bounding box rectangle of the *WindowBase, excluding decorations.

func (*WindowBase) ContextMenu ¶
func (wb *WindowBase) ContextMenu() *Menu
ContextMenu returns the context menu of the *WindowBase.

By default this is nil.

func (*WindowBase) ContextMenuLocation ¶
func (wb *WindowBase) ContextMenuLocation() Point
ContextMenuLocation returns the the *WindowBase center in screen coordinates in native pixels.

func (*WindowBase) CreateCanvas ¶
func (wb *WindowBase) CreateCanvas() (*Canvas, error)
CreateCanvas creates and returns a *Canvas that can be used to draw inside the ClientBoundsPixels of the *WindowBase.

Remember to call the Dispose method on the canvas to release resources, when you no longer need it.

func (*WindowBase) Cursor ¶
func (wb *WindowBase) Cursor() Cursor
Cursor returns the Cursor of the *WindowBase.

By default this is nil.

func (*WindowBase) DPI ¶
func (wb *WindowBase) DPI() int
DPI returns the current DPI value of the WindowBase.

func (*WindowBase) Dispose ¶
func (wb *WindowBase) Dispose()
Dispose releases the operating system resources, associated with the *WindowBase.

If a user closes a *MainWindow or *Dialog, it is automatically released. Also, if a Container is disposed of, all its descendants will be released as well.

func (*WindowBase) Disposing ¶
func (wb *WindowBase) Disposing() *Event
Disposing returns an Event that is published when the Window is disposed of.

func (*WindowBase) DoubleBuffering ¶
func (wb *WindowBase) DoubleBuffering() bool
DoubleBuffering returns whether double buffering of the drawing is enabled, which may help reduce flicker.

func (*WindowBase) DropFiles ¶
func (wb *WindowBase) DropFiles() *DropFilesEvent
DropFiles returns a *DropFilesEvent that you can attach to for handling drop file events for the *WindowBase.

func (*WindowBase) Enabled ¶
func (wb *WindowBase) Enabled() bool
Enabled returns if the *WindowBase is enabled for user interaction.

func (*WindowBase) Focused ¶
func (wb *WindowBase) Focused() bool
Focused returns whether the Window has the keyboard input focus.

func (*WindowBase) FocusedChanged ¶
func (wb *WindowBase) FocusedChanged() *Event
FocusedChanged returns an Event that you can attach to for handling focus change events for the WindowBase.

func (*WindowBase) Font ¶
func (wb *WindowBase) Font() *Font
Font returns the *Font of the *WindowBase.

By default this is a MS Shell Dlg 2, 8 point font.

func (*WindowBase) ForEachDescendant ¶
func (wb *WindowBase) ForEachDescendant(f func(widget Widget) bool)
func (*WindowBase) Form ¶
func (wb *WindowBase) Form() Form
Form returns the Form of the Window.

func (*WindowBase) Handle ¶
func (wb *WindowBase) Handle() win.HWND
Handle returns the window handle of the Window.

func (*WindowBase) Height ¶
func (wb *WindowBase) Height() int
Height returns the outer height of the *WindowBase, including decorations.

func (*WindowBase) HeightPixels ¶
func (wb *WindowBase) HeightPixels() int
HeightPixels returns the outer height of the *WindowBase, including decorations.

func (*WindowBase) IntFrom96DPI ¶
func (wb *WindowBase) IntFrom96DPI(value int) int
IntFrom96DPI converts from 1/96" units to native pixels.

func (*WindowBase) IntTo96DPI ¶
func (wb *WindowBase) IntTo96DPI(value int) int
IntTo96DPI converts from native pixels to 1/96" units.

func (*WindowBase) Invalidate ¶
func (wb *WindowBase) Invalidate() error
Invalidate schedules a full repaint of the *WindowBase.

func (*WindowBase) IsDisposed ¶
func (wb *WindowBase) IsDisposed() bool
IsDisposed returns if the *WindowBase has been disposed of.

func (*WindowBase) KeyDown ¶
func (wb *WindowBase) KeyDown() *KeyEvent
KeyDown returns a *KeyEvent that you can attach to for handling key down events for the *WindowBase.

func (*WindowBase) KeyPress ¶
func (wb *WindowBase) KeyPress() *KeyEvent
KeyPress returns a *KeyEvent that you can attach to for handling key press events for the *WindowBase.

func (*WindowBase) KeyUp ¶
func (wb *WindowBase) KeyUp() *KeyEvent
KeyUp returns a *KeyEvent that you can attach to for handling key up events for the *WindowBase.

func (*WindowBase) MarginsFrom96DPI ¶
func (wb *WindowBase) MarginsFrom96DPI(value Margins) Margins
MarginsFrom96DPI converts from 1/96" units to native pixels.

func (*WindowBase) MarginsTo96DPI ¶
func (wb *WindowBase) MarginsTo96DPI(value Margins) Margins
MarginsTo96DPI converts from native pixels to 1/96" units.

func (*WindowBase) MaxSize ¶
func (wb *WindowBase) MaxSize() Size
MaxSize returns the maximum allowed outer size for the *WindowBase, including decorations.

For child windows, this is only relevant when the parent of the *WindowBase has a Layout. RootWidgets, like *MainWindow and *Dialog, also honor this.

func (*WindowBase) MaxSizePixels ¶
func (wb *WindowBase) MaxSizePixels() Size
MaxSizePixels returns the maximum allowed outer size for the *WindowBase, including decorations.

For child windows, this is only relevant when the parent of the *WindowBase has a Layout. RootWidgets, like *MainWindow and *Dialog, also honor this.

func (*WindowBase) MinSize ¶
func (wb *WindowBase) MinSize() Size
MinSize returns the minimum allowed outer size for the *WindowBase, including decorations.

For child windows, this is only relevant when the parent of the *WindowBase has a Layout. RootWidgets, like *MainWindow and *Dialog, also honor this.

func (*WindowBase) MinSizePixels ¶
func (wb *WindowBase) MinSizePixels() Size
MinSizePixels returns the minimum allowed outer size for the *WindowBase, including decorations.

For child windows, this is only relevant when the parent of the *WindowBase has a Layout. RootWidgets, like *MainWindow and *Dialog, also honor this.

func (*WindowBase) MouseDown ¶
func (wb *WindowBase) MouseDown() *MouseEvent
MouseDown returns a *MouseEvent that you can attach to for handling mouse down events for the *WindowBase.

func (*WindowBase) MouseMove ¶
func (wb *WindowBase) MouseMove() *MouseEvent
MouseMove returns a *MouseEvent that you can attach to for handling mouse move events for the *WindowBase.

func (*WindowBase) MouseUp ¶
func (wb *WindowBase) MouseUp() *MouseEvent
MouseUp returns a *MouseEvent that you can attach to for handling mouse up events for the *WindowBase.

func (*WindowBase) MouseWheel ¶
func (wb *WindowBase) MouseWheel() *MouseEvent
func (*WindowBase) MustRegisterProperty ¶
func (wb *WindowBase) MustRegisterProperty(name string, property Property)
func (*WindowBase) Name ¶
func (wb *WindowBase) Name() string
Name returns the name of the *WindowBase.

func (*WindowBase) PointFrom96DPI ¶
func (wb *WindowBase) PointFrom96DPI(value Point) Point
PointFrom96DPI converts from 1/96" units to native pixels.

func (*WindowBase) PointTo96DPI ¶
func (wb *WindowBase) PointTo96DPI(value Point) Point
PointTo96DPI converts from native pixels to 1/96" units.

func (*WindowBase) Property ¶
func (wb *WindowBase) Property(name string) Property
func (*WindowBase) ReadState ¶
func (wb *WindowBase) ReadState() (string, error)
func (*WindowBase) RectangleFrom96DPI ¶
func (wb *WindowBase) RectangleFrom96DPI(value Rectangle) Rectangle
RectangleFrom96DPI converts from 1/96" units to native pixels.

func (*WindowBase) RectangleTo96DPI ¶
func (wb *WindowBase) RectangleTo96DPI(value Rectangle) Rectangle
RectangleTo96DPI converts from native pixels to 1/96" units.

func (*WindowBase) RequestLayout ¶
func (wb *WindowBase) RequestLayout()
RequestLayout either schedules or immediately starts performing layout.

func (*WindowBase) RestoreState ¶
func (wb *WindowBase) RestoreState() (err error)
func (*WindowBase) RightToLeftReading ¶
func (wb *WindowBase) RightToLeftReading() bool
RightToLeftReading returns whether the reading order of the Window is from right to left.

func (*WindowBase) SaveState ¶
func (wb *WindowBase) SaveState() (err error)
func (*WindowBase) Screenshot ¶
func (wb *WindowBase) Screenshot() (*image.RGBA, error)
Screenshot returns an image of the window.

func (*WindowBase) SendMessage ¶
func (wb *WindowBase) SendMessage(msg uint32, wParam, lParam uintptr) uintptr
SendMessage sends a message to the window and returns the result.

func (*WindowBase) SetBackground ¶
func (wb *WindowBase) SetBackground(background Brush)
SetBackground sets the background Brush of the *WindowBase.

func (*WindowBase) SetBounds ¶
func (wb *WindowBase) SetBounds(bounds Rectangle) error
SetBounds sets the outer bounding box rectangle of the *WindowBase, including decorations.

For a Form, like *MainWindow or *Dialog, the rectangle is in screen coordinates, for a child Window the coordinates are relative to its parent.

func (*WindowBase) SetBoundsPixels ¶
func (wb *WindowBase) SetBoundsPixels(bounds Rectangle) error
SetBoundsPixels sets the outer bounding box rectangle of the *WindowBase, including decorations.

For a Form, like *MainWindow or *Dialog, the rectangle is in screen coordinates, for a child Window the coordinates are relative to its parent.

func (*WindowBase) SetClientSize ¶
func (wb *WindowBase) SetClientSize(value Size) error
SetClientSize sets the size of the inner bounding box of the *WindowBase, excluding decorations.

func (*WindowBase) SetClientSizePixels ¶
func (wb *WindowBase) SetClientSizePixels(value Size) error
SetClientSizePixels sets the size of the inner bounding box of the *WindowBase, excluding decorations.

func (*WindowBase) SetContextMenu ¶
func (wb *WindowBase) SetContextMenu(value *Menu)
SetContextMenu sets the context menu of the *WindowBase.

func (*WindowBase) SetCursor ¶
func (wb *WindowBase) SetCursor(value Cursor)
SetCursor sets the Cursor of the *WindowBase.

func (*WindowBase) SetDoubleBuffering ¶
func (wb *WindowBase) SetDoubleBuffering(enabled bool) error
SetDoubleBuffering enables or disables double buffering of the drawing, which may help reduce flicker.

func (*WindowBase) SetEnabled ¶
func (wb *WindowBase) SetEnabled(enabled bool)
SetEnabled sets if the *WindowBase is enabled for user interaction.

func (*WindowBase) SetFocus ¶
func (wb *WindowBase) SetFocus() error
SetFocus sets the keyboard input focus to the *WindowBase.

func (*WindowBase) SetFont ¶
func (wb *WindowBase) SetFont(font *Font)
SetFont sets the *Font of the *WindowBase.

func (*WindowBase) SetHeight ¶
func (wb *WindowBase) SetHeight(value int) error
SetHeight sets the outer height of the *WindowBase, including decorations.

func (*WindowBase) SetHeightPixels ¶
func (wb *WindowBase) SetHeightPixels(value int) error
SetHeightPixels sets the outer height of the *WindowBase, including decorations.

func (*WindowBase) SetMinMaxSize ¶
func (wb *WindowBase) SetMinMaxSize(min, max Size) error
SetMinMaxSize sets the minimum and maximum outer size of the *WindowBase, including decorations.

Use walk.Size{} to make the respective limit be ignored.

func (*WindowBase) SetMinMaxSizePixels ¶
func (wb *WindowBase) SetMinMaxSizePixels(min, max Size) error
SetMinMaxSizePixels sets the minimum and maximum outer size of the *WindowBase, including decorations.

Use walk.Size{} to make the respective limit be ignored.

func (*WindowBase) SetName ¶
func (wb *WindowBase) SetName(name string)
SetName sets the name of the *WindowBase.

func (*WindowBase) SetRightToLeftReading ¶
func (wb *WindowBase) SetRightToLeftReading(rtl bool) error
SetRightToLeftReading sets whether the reading order of the Window is from right to left.

func (*WindowBase) SetSize ¶
func (wb *WindowBase) SetSize(size Size) error
SetSize sets the outer size of the *WindowBase, including decorations.

func (*WindowBase) SetSizePixels ¶
func (wb *WindowBase) SetSizePixels(size Size) error
SetSizePixels sets the outer size of the *WindowBase, including decorations.

func (*WindowBase) SetSuspended ¶
func (wb *WindowBase) SetSuspended(suspend bool)
SetSuspended sets if the *WindowBase is suspended for layout and repainting purposes.

You should call SetSuspended(true), before doing a batch of modifications that would cause multiple layout or drawing updates. Remember to call SetSuspended(false) afterwards, which will update the *WindowBase accordingly.

func (*WindowBase) SetVisible ¶
func (wb *WindowBase) SetVisible(visible bool)
SetVisible sets if the *WindowBase is visible.

func (*WindowBase) SetWidth ¶
func (wb *WindowBase) SetWidth(value int) error
SetWidth sets the outer width of the *WindowBase, including decorations.

func (*WindowBase) SetWidthPixels ¶
func (wb *WindowBase) SetWidthPixels(value int) error
SetWidthPixels sets the outer width of the *WindowBase, including decorations.

func (*WindowBase) SetX ¶
func (wb *WindowBase) SetX(value int) error
SetX sets the x coordinate of the *WindowBase, relative to the screen for RootWidgets like *MainWindow or *Dialog and relative to the parent for child Windows.

func (*WindowBase) SetXPixels ¶
func (wb *WindowBase) SetXPixels(value int) error
SetXPixels sets the x coordinate of the *WindowBase, relative to the screen for RootWidgets like *MainWindow or *Dialog and relative to the parent for child Windows.

func (*WindowBase) SetY ¶
func (wb *WindowBase) SetY(value int) error
SetY sets the y coordinate of the *WindowBase, relative to the screen for RootWidgets like *MainWindow or *Dialog and relative to the parent for child Windows.

func (*WindowBase) SetYPixels ¶
func (wb *WindowBase) SetYPixels(value int) error
SetYPixels sets the y coordinate of the *WindowBase, relative to the screen for RootWidgets like *MainWindow or *Dialog and relative to the parent for child Windows.

func (*WindowBase) ShortcutActions ¶
func (wb *WindowBase) ShortcutActions() *ActionList
ShortcutActions returns the list of actions that will be triggered if their shortcut is pressed when this window or one of its descendants has the keyboard focus.

func (*WindowBase) Size ¶
func (wb *WindowBase) Size() Size
Size returns the outer size of the *WindowBase, including decorations.

func (*WindowBase) SizeChanged ¶
func (wb *WindowBase) SizeChanged() *Event
SizeChanged returns an *Event that you can attach to for handling size changed events for the *WindowBase.

func (*WindowBase) SizeFrom96DPI ¶
func (wb *WindowBase) SizeFrom96DPI(value Size) Size
SizeFrom96DPI converts from 1/96" units to native pixels.

func (*WindowBase) SizePixels ¶
func (wb *WindowBase) SizePixels() Size
SizePixels returns the outer size of the *WindowBase, including decorations.

func (*WindowBase) SizeTo96DPI ¶
func (wb *WindowBase) SizeTo96DPI(value Size) Size
SizeTo96DPI converts from native pixels to 1/96" units.

func (*WindowBase) Suspended ¶
func (wb *WindowBase) Suspended() bool
Suspended returns if the *WindowBase is suspended for layout and repainting purposes.

func (*WindowBase) Synchronize ¶
func (wb *WindowBase) Synchronize(f func())
Synchronize enqueues func f to be called some time later by the main goroutine from inside a message loop.

func (*WindowBase) Visible ¶
func (wb *WindowBase) Visible() bool
Visible returns if the *WindowBase is visible.

func (*WindowBase) VisibleChanged ¶
func (wb *WindowBase) VisibleChanged() *Event
VisibleChanged returns an Event that you can attach to for handling visible changed events for the Window.

func (*WindowBase) Width ¶
func (wb *WindowBase) Width() int
Width returns the outer width of the *WindowBase, including decorations.

func (*WindowBase) WidthPixels ¶
func (wb *WindowBase) WidthPixels() int
WidthPixels returns the outer width of the *WindowBase, including decorations.

func (*WindowBase) WndProc ¶
func (wb *WindowBase) WndProc(hwnd win.HWND, msg uint32, wParam, lParam uintptr) uintptr
WndProc is the window procedure of the window.

When implementing your own WndProc to add or modify behavior, call the WndProc of the embedded window for messages you don't handle yourself.

func (*WindowBase) WriteState ¶
func (wb *WindowBase) WriteState(state string) error
func (*WindowBase) X ¶
func (wb *WindowBase) X() int
X returns the x coordinate of the *WindowBase, relative to the screen for RootWidgets like *MainWindow or *Dialog and relative to the parent for child Windows.

func (*WindowBase) XPixels ¶
func (wb *WindowBase) XPixels() int
XPixels returns the x coordinate of the *WindowBase, relative to the screen for RootWidgets like *MainWindow or *Dialog and relative to the parent for child Windows.

func (*WindowBase) Y ¶
func (wb *WindowBase) Y() int
Y returns the y coordinate of the *WindowBase, relative to the screen for RootWidgets like *MainWindow or *Dialog and relative to the parent for child Windows.

func (*WindowBase) YPixels ¶
func (wb *WindowBase) YPixels() int
YPixels returns the y coordinate of the *WindowBase, relative to the screen for RootWidgets like *MainWindow or *Dialog and relative to the parent for child Windows.

type WindowGroup ¶
type WindowGroup struct {
	// contains filtered or unexported fields
}
WindowGroup holds data common to windows that share a thread.

Each WindowGroup keeps track of the number of references to the group. When the number of references reaches zero, the group is disposed of.

func (*WindowGroup) ActiveForm ¶
func (g *WindowGroup) ActiveForm() Form
ActiveForm returns the currently active form for the group. If no form is active it returns nil.

func (*WindowGroup) Add ¶
func (g *WindowGroup) Add(delta int)
Add changes the group's reference counter by delta, which may be negative.

If the reference counter becomes zero the group will be disposed of.

If the reference counter goes negative Add will panic.

func (*WindowGroup) CreateToolTip ¶
func (g *WindowGroup) CreateToolTip() (*ToolTip, error)
CreateToolTip returns a tool tip control for the group.

If a control has not already been prepared for the group one will be created.

func (*WindowGroup) Done ¶
func (g *WindowGroup) Done()
Done decrements the group's reference counter by one.

func (*WindowGroup) Refs ¶
func (g *WindowGroup) Refs() int
Refs returns the current number of references to the group.

func (*WindowGroup) RunSynchronized ¶
func (g *WindowGroup) RunSynchronized()
RunSynchronized runs all of the function calls queued by Synchronize and applies any layout changes queued by synchronizeLayout.

RunSynchronized must be called by the group's thread.

func (*WindowGroup) SetActiveForm ¶
func (g *WindowGroup) SetActiveForm(form Form)
SetActiveForm updates the currently active form for the group.

func (*WindowGroup) Synchronize ¶
func (g *WindowGroup) Synchronize(f func())
Synchronize adds f to the group's function queue, to be executed by the message loop running on the the group's thread.

Synchronize can be called from any thread.

func (*WindowGroup) ThreadID ¶
func (g *WindowGroup) ThreadID() uint32
ThreadID identifies the thread that the group is affiliated with.

func (*WindowGroup) ToolTip ¶
func (g *WindowGroup) ToolTip() *ToolTip
ToolTip returns the tool tip control for the group, if one exists.

 Source Files ¶
accessibility.go
action.go
actionlist.go
application.go
bitmap.go
boxlayout.go
brush.go
button.go
cancelevent.go
canvas.go
checkbox.go
clipboard.go
closeevent.go
color.go
combobox.go
commondialogs.go
composite.go
condition.go
container.go
cursor.go
customwidget.go
databinding.go
dateedit.go
datelabel.go
dialog.go
dropfilesevent.go
error.go
errorevent.go
event.go
expression.go
flowlayout.go
font.go
fontresource.go
form.go
gradientcomposite.go
graphicseffects.go
gridlayout.go
groupbox.go
icon.go
iconcache.go
image.go
imagelist.go
imageview.go
inifilesettings.go
intevent.go
intrangeevent.go
keyboard.go
keyevent.go
label.go
layout.go
lineedit.go
linklabel.go
listbox.go
mainloop_default.go
mainwindow.go
maptablemodel.go
menu.go
messagebox.go
metafile.go
models.go
mouseevent.go
notifyicon.go
numberedit.go
numberlabel.go
path.go
pen.go
point.go
progressbar.go
progressindicator.go
property.go
pushbutton.go
radiobutton.go
rectangle.go
reflectmodels.go
registry.go
resourcemanager.go
scrollview.go
separator.go
simpletypes.go
size.go
slider.go
spacer.go
splitbutton.go
splitter.go
splitterhandle.go
splitterlayout.go
static.go
statusbar.go
stopwatch.go
stringevent.go
tableview.go
tableviewcolumn.go
tableviewcolumnlist.go
tabpage.go
tabpagelist.go
tabwidget.go
textedit.go
textlabel.go
toolbar.go
toolbutton.go
tooltip.go
tooltiperrorpresenter.go
treeitemevent.go
treeview.go
util.go
validators.go
walk.go
webview.go
webview_dwebbrowserevents2.go
webview_events.go
webview_idochostuihandler.go
webview_ioleclientsite.go
webview_ioleinplaceframe.go
webview_ioleinplacesite.go
widget.go
widgetlist.go
window.go
windowgroup.go
 Directories ¶
Expand all
declarative
examples
tools
Why Go
Use Cases
Case Studies
Get Started
Playground
Tour
Stack Overflow
Help
Packages
Standard Library
Sub-repositories
About Go Packages
About
Download
Blog
Issue Tracker
Release Notes
Brand Guidelines
Code of Conduct
Connect
Twitter
GitHub
Slack
r/golang
Meetup
Golang Weekly
Gopher in flight goggles
Copyright
Terms of Service
Privacy Policy
Report an Issue
System theme
Theme Toggle


Shortcuts Modal

Google logo
go.dev uses cookies from Google to deliver and enhance the quality of its services and to analyze traffic. Learn more.
Okay