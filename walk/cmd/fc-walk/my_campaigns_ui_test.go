//go:build windows

package main

import "testing"

func TestSetMonthlyInstallmentByID_UpdatesRowAndValue(t *testing.T) {
	m := NewCampaignsModel()
	drafts := []CampaignDraft{
		{ID: "id-1", Name: "Row 1"},
		{ID: "id-2", Name: "Row 2"},
	}
	m.ReplaceFromDrafts(drafts)

	if !m.SetMonthlyInstallmentByID("id-2", "12,345.67") {
		t.Fatalf("SetMonthlyInstallmentByID returned false")
	}
	if m.items[1].MonthlyInstallmentStr != "12,345.67" {
		t.Fatalf("MonthlyInstallmentStr = %q; want %q", m.items[1].MonthlyInstallmentStr, "12,345.67")
	}
	if m.items[0].MonthlyInstallmentStr != "" {
		t.Fatalf("MonthlyInstallmentStr = %q; want %q", m.items[0].MonthlyInstallmentStr, "")
	}
}

func TestReplaceFromDrafts_BuildsRows(t *testing.T) {
	m := NewCampaignsModel()
	drafts := []CampaignDraft{
		{ID: "a", Name: "Alpha"},
		{ID: "b", Name: ""},
		{ID: "c", Name: "Charlie"},
	}
	m.ReplaceFromDrafts(drafts)

	if len(m.items) != 3 {
		t.Fatalf("RowCount = %d; want 3", len(m.items))
	}
	if m.items[0].Name != "Alpha" {
		t.Fatalf("row0 name = %v; want %q", m.items[0].Name, "Alpha")
	}
	if m.items[1].Name != "(unnamed)" {
		t.Fatalf("row1 name = %v; want %q", m.items[1].Name, "(unnamed)")
	}
	if m.items[2].Name != "Charlie" {
		t.Fatalf("row2 name = %v; want %q", m.items[2].Name, "Charlie")
	}
}

func TestRemoveByID_RemovesRow(t *testing.T) {
	m := NewCampaignsModel()
	drafts := []CampaignDraft{
		{ID: "to-remove", Name: "Remove Me"},
		{ID: "keep", Name: "Keep Me"},
	}
	m.ReplaceFromDrafts(drafts)

	if !m.RemoveByID("to-remove") {
		t.Fatalf("RemoveByID(to-remove) returned false")
	}
	if len(m.items) != 1 {
		t.Fatalf("RowCount = %d; want 1", len(m.items))
	}
	if idx := m.IndexByID("to-remove"); idx != -1 {
		t.Fatalf("IndexByID(to-remove) = %d; want -1", idx)
	}
	// Ensure remaining row intact
	if m.items[0].Name != "Keep Me" {
		t.Fatalf("remaining row name = %v; want %q", m.items[0].Name, "Keep Me")
	}
}
