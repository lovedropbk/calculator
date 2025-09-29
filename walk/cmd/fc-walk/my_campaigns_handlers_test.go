//go:build windows

package main

import "testing"

func TestHandleMyCampaignNewBlank_AddsRowAndDirty(t *testing.T) {
	m := NewMyCampaignsTableModel()

	var selectedIDs []string
	var dirtyCalls []bool
	newDraft := CampaignDraft{ID: "new-blank-1", Name: "New Blank"}

	deps := MyCampaignsDeps{
		Model: m,
		SeedBlank: func() CampaignDraft {
			return newDraft
		},
		SelectMyCampaign: func(id string) { selectedIDs = append(selectedIDs, id) },
		SetDirty:         func(b bool) { dirtyCalls = append(dirtyCalls, b) },
	}

	if err := HandleMyCampaignNewBlank(deps); err != nil {
		t.Fatalf("HandleMyCampaignNewBlank error: %v", err)
	}

	if m.RowCount() != 1 {
		t.Fatalf("RowCount = %d; want 1", m.RowCount())
	}
	if m.IndexByID("new-blank-1") != 0 {
		t.Fatalf("expected new draft to be present with index 0")
	}
	if len(selectedIDs) != 1 || selectedIDs[0] != "new-blank-1" {
		t.Fatalf("SelectMyCampaign calls = %v; want [new-blank-1]", selectedIDs)
	}
	if len(dirtyCalls) != 1 || dirtyCalls[0] != true {
		t.Fatalf("SetDirty calls = %v; want [true]", dirtyCalls)
	}
}

func TestHandleMyCampaignCopySelected_AddsRowAndDirty(t *testing.T) {
	m := NewMyCampaignsTableModel()
	// Seed one to simulate existing selection context (not used directly by handler)
	m.ReplaceFromDrafts([]CampaignDraft{{ID: "base-1", Name: "Base"}})

	var selectedIDs []string
	var dirtyCalls []bool
	copied := CampaignDraft{ID: "copy-1", Name: "Copy of Base"}

	deps := MyCampaignsDeps{
		Model: m,
		SeedCopy: func() (CampaignDraft, error) {
			return copied, nil
		},
		SelectMyCampaign: func(id string) { selectedIDs = append(selectedIDs, id) },
		SetDirty:         func(b bool) { dirtyCalls = append(dirtyCalls, b) },
	}

	if err := HandleMyCampaignCopySelected(deps); err != nil {
		t.Fatalf("HandleMyCampaignCopySelected error: %v", err)
	}

	if m.RowCount() != 2 {
		t.Fatalf("RowCount = %d; want 2", m.RowCount())
	}
	if idx := m.IndexByID("copy-1"); idx == -1 {
		t.Fatalf("expected copied draft to be present")
	}
	if len(selectedIDs) != 1 || selectedIDs[0] != "copy-1" {
		t.Fatalf("SelectMyCampaign calls = %v; want [copy-1]", selectedIDs)
	}
	if len(dirtyCalls) != 1 || dirtyCalls[0] != true {
		t.Fatalf("SetDirty calls = %v; want [true]", dirtyCalls)
	}
}

func TestHandleMyCampaignDelete_RemovesRow_ExitEditMode_Dirty(t *testing.T) {
	m := NewMyCampaignsTableModel()
	m.ReplaceFromDrafts([]CampaignDraft{
		{ID: "del-1", Name: "Delete Me"},
		{ID: "keep-1", Name: "Keep Me"},
	})

	exitCalled := 0
	var dirtyCalls []bool

	deps := MyCampaignsDeps{
		Model: m,
		SelectedID: func() string {
			return "del-1"
		},
		ExitEditMode: func() { exitCalled++ },
		SetDirty:     func(b bool) { dirtyCalls = append(dirtyCalls, b) },
	}

	if err := HandleMyCampaignDelete(deps); err != nil {
		t.Fatalf("HandleMyCampaignDelete error: %v", err)
	}

	if m.RowCount() != 1 {
		t.Fatalf("RowCount = %d; want 1", m.RowCount())
	}
	if idx := m.IndexByID("del-1"); idx != -1 {
		t.Fatalf("deleted ID still present, IndexByID = %d", idx)
	}
	if exitCalled != 1 {
		t.Fatalf("ExitEditMode called %d times; want 1", exitCalled)
	}
	if len(dirtyCalls) != 1 || dirtyCalls[0] != true {
		t.Fatalf("SetDirty calls = %v; want [true]", dirtyCalls)
	}
}

func TestHandleMyCampaignSaveAll_ResetsDirty_InvokesOnSaved(t *testing.T) {
	m := NewMyCampaignsTableModel()
	m.ReplaceFromDrafts([]CampaignDraft{
		{ID: "s1", Name: "Save 1"},
		{ID: "s2", Name: "Save 2"},
	})

	var savedDrafts []CampaignDraft
	onSavedCalled := 0
	var dirtyCalls []bool

	deps := MyCampaignsDeps{
		Model: m,
		Save: func(list []CampaignDraft) error {
			// Capture and return OK
			savedDrafts = append([]CampaignDraft(nil), list...)
			return nil
		},
		SetDirty: func(b bool) { dirtyCalls = append(dirtyCalls, b) },
		OnSaved: func(list []CampaignDraft) {
			onSavedCalled++
			// The handler passes through the same slice it saved.
			if len(list) != len(savedDrafts) {
				t.Fatalf("OnSaved drafts len mismatch: got %d want %d", len(list), len(savedDrafts))
			}
		},
	}

	if err := HandleMyCampaignSaveAll(deps); err != nil {
		t.Fatalf("HandleMyCampaignSaveAll error: %v", err)
	}

	// Basic shape checks (avoid timestamp equality)
	if len(savedDrafts) != 2 {
		t.Fatalf("saved drafts len = %d; want 2", len(savedDrafts))
	}
	if savedDrafts[0].ID != "s1" || savedDrafts[1].ID != "s2" {
		t.Fatalf("saved IDs = [%s, %s]; want [s1, s2]", savedDrafts[0].ID, savedDrafts[1].ID)
	}

	// Dirty reset and callback
	if len(dirtyCalls) != 1 || dirtyCalls[0] != false {
		t.Fatalf("SetDirty calls = %v; want [false]", dirtyCalls)
	}
	if onSavedCalled != 1 {
		t.Fatalf("OnSaved called %d times; want 1", onSavedCalled)
	}
}

func TestHandleMyCampaignLoad_ReplacesRows_ResetsDirty_InvokesOnLoaded(t *testing.T) {
	m := NewMyCampaignsTableModel()
	m.ReplaceFromDrafts([]CampaignDraft{{ID: "old-1", Name: "Old"}})

	loaded := []CampaignDraft{
		{ID: "n1", Name: "New 1"},
		{ID: "n2", Name: "New 2"},
	}
	loadedVer := 1

	exitCalled := 0
	var dirtyCalls []bool
	onLoadedCalled := 0
	var onLoadedDrafts []CampaignDraft
	gotVer := 0

	deps := MyCampaignsDeps{
		Model: m,
		Load: func() ([]CampaignDraft, int, error) {
			return loaded, loadedVer, nil
		},
		ExitEditMode: func() { exitCalled++ },
		SetDirty:     func(b bool) { dirtyCalls = append(dirtyCalls, b) },
		OnLoaded: func(list []CampaignDraft, ver int) {
			onLoadedCalled++
			onLoadedDrafts = append([]CampaignDraft(nil), list...)
			gotVer = ver
		},
	}

	if err := HandleMyCampaignLoad(deps); err != nil {
		t.Fatalf("HandleMyCampaignLoad error: %v", err)
	}

	if m.RowCount() != 2 || m.IndexByID("n1") == -1 || m.IndexByID("n2") == -1 {
		t.Fatalf("rows not replaced correctly; RowCount=%d", m.RowCount())
	}
	if exitCalled != 1 {
		t.Fatalf("ExitEditMode called %d times; want 1", exitCalled)
	}
	if len(dirtyCalls) != 1 || dirtyCalls[0] != false {
		t.Fatalf("SetDirty calls = %v; want [false]", dirtyCalls)
	}
	if onLoadedCalled != 1 || len(onLoadedDrafts) != 2 || gotVer != loadedVer {
		t.Fatalf("OnLoaded not invoked correctly: called=%d len=%d ver=%d", onLoadedCalled, len(onLoadedDrafts), gotVer)
	}
}

func TestHandleMyCampaignClear_EmptiesRows_ResetsDirty_InvokesOnCleared(t *testing.T) {
	m := NewMyCampaignsTableModel()
	m.ReplaceFromDrafts([]CampaignDraft{{ID: "x1", Name: "X"}, {ID: "x2", Name: "Y"}})

	exitCalled := 0
	clearedCalled := 0
	var dirtyCalls []bool

	deps := MyCampaignsDeps{
		Model:        m,
		Clear:        func() error { return nil },
		ExitEditMode: func() { exitCalled++ },
		SetDirty:     func(b bool) { dirtyCalls = append(dirtyCalls, b) },
		OnCleared:    func() { clearedCalled++ },
	}

	if err := HandleMyCampaignClear(deps); err != nil {
		t.Fatalf("HandleMyCampaignClear error: %v", err)
	}

	if m.RowCount() != 0 {
		t.Fatalf("RowCount = %d; want 0", m.RowCount())
	}
	if exitCalled != 1 {
		t.Fatalf("ExitEditMode called %d times; want 1", exitCalled)
	}
	if len(dirtyCalls) != 1 || dirtyCalls[0] != false {
		t.Fatalf("SetDirty calls = %v; want [false]", dirtyCalls)
	}
	if clearedCalled != 1 {
		t.Fatalf("OnCleared called %d times; want 1", clearedCalled)
	}
}
