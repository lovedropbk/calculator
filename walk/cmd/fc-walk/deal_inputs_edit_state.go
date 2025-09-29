//go:build windows

package main

import (
	"strings"

	"github.com/lxn/walk"
	. "github.com/lxn/walk/declarative"
)

// EditModeUI holds the progressive disclosure UI for Deal Inputs edit state.
type EditModeUI struct {
	Root           *walk.Composite
	HeaderLbl      *walk.Label
	CashDiscountNE *walk.NumberEdit
	SubdownNE      *walk.NumberEdit
	IDCInsuranceNE *walk.NumberEdit
	IDCMBSPNE      *walk.NumberEdit
}

// NewEditModeUI builds the hidden composite under the given parent.
// It renders:
// - Header label: “Editing: [Campaign Name]”
// - GroupBox “Campaign-Specific Adjustments” with four NumberEdit fields
// Initial state: hidden
func NewEditModeUI(parent walk.Container) (*EditModeUI, error) {
	ui := &EditModeUI{}
	err := (Composite{
		AssignTo:   &ui.Root,
		ColumnSpan: 2,
		Visible:    false,
		Layout:     VBox{Spacing: 6},
		Children: []Widget{
			Label{
				AssignTo: &ui.HeaderLbl,
				Text:     "Editing: —",
				Font:     Font{Bold: true},
			},
			GroupBox{
				Title:  "Campaign-Specific Adjustments",
				Layout: Grid{Columns: 2, Spacing: 6},
				Children: []Widget{
					Label{Text: "Cash Discount (THB):"},
					NumberEdit{AssignTo: &ui.CashDiscountNE, Decimals: 0, MinValue: 0, Value: 0},

					Label{Text: "Subdown (THB):"},
					NumberEdit{AssignTo: &ui.SubdownNE, Decimals: 0, MinValue: 0, Value: 0},

					Label{Text: "IDC - Free Insurance (THB):"},
					NumberEdit{AssignTo: &ui.IDCInsuranceNE, Decimals: 0, MinValue: 0, Value: 0},

					Label{Text: "IDC - Free MBSP (THB):"},
					NumberEdit{AssignTo: &ui.IDCMBSPNE, Decimals: 0, MinValue: 0, Value: 0},
				},
			},
		},
	}).Create(NewBuilder(parent))
	if ui.Root != nil {
		ui.Root.SetVisible(false)
	}
	return ui, err
}

// ShowHighLevelState hides the progressive section.
func ShowHighLevelState(ui *EditModeUI) {
	if ui == nil || ui.Root == nil {
		return
	}
	ui.Root.SetVisible(false)
}

// ShowCampaignEditState sets the header text and shows the progressive section.
func ShowCampaignEditState(ui *EditModeUI, campaignName string) {
	if ui == nil || ui.Root == nil {
		return
	}
	name := strings.TrimSpace(campaignName)
	if name == "" {
		name = "Custom Campaign"
	}
	if ui.HeaderLbl != nil {
		ui.HeaderLbl.SetText("Editing: " + name)
	}
	ui.Root.SetVisible(true)
}
