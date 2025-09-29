//go:build windows

package main

import (
	"strings"

	"github.com/lxn/walk"
	. "github.com/lxn/walk/declarative"
)

// promptCampaignName shows a small modal dialog to rename a campaign.
// Returns (newName, true) when accepted; ("", false) on cancel.
func promptCampaignName(initial string) (string, bool) {
	var dlg *walk.Dialog
	var ed *walk.LineEdit
	accepted := false

	initText := strings.TrimSpace(initial)
	_, _ = (Dialog{
		AssignTo: &dlg,
		Title:    "Rename Campaign",
		MinSize:  Size{Width: 380, Height: 140},
		Layout:   Grid{Columns: 2, Spacing: 6},
		Children: []Widget{
			Label{Text: "New name:"},
			LineEdit{AssignTo: &ed, Text: initText, MinSize: Size{Width: 260}},
			Composite{
				ColumnSpan: 2,
				Layout:     HBox{Spacing: 6},
				Children: []Widget{
					HSpacer{},
					PushButton{
						Text:      "Cancel",
						OnClicked: func() { dlg.Cancel() },
					},
					PushButton{
						Text: "OK",
						OnClicked: func() {
							accepted = true
							dlg.Accept()
						},
					},
				},
			},
		},
	}).Run(walk.App().ActiveForm())

	if !accepted || ed == nil {
		return "", false
	}
	return strings.TrimSpace(ed.Text()), true
}
