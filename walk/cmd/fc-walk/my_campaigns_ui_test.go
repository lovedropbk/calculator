//go:build windows

package main

import "testing"

func TestSetMonthlyInstallmentByID_UpdatesRowAndValue(t *testing.T) {
	m := NewMyCampaignsTableModel()
	drafts := []CampaignDraft{
		{ID: "id-1", Name: "Row 1"},
		{ID: "id-2", Name: "Row 2"},
	}
	m.ReplaceFromDrafts(drafts)

	if !m.SetMonthlyInstallmentByID("id-2", "12,345.67") {
		t.Fatalf("SetMonthlyInstallmentByID returned false")
	}
	if got := m.Value(1, 2); got != "THB 12,345.67" {
		t.Fatalf("Value(1,2) = %v; want %q", got, "THB 12,345.67")
	}
	if got := m.Value(0, 2); got != "—" {
		t.Fatalf("Value(0,2) = %v; want %q", got, "—")
	}
}

func TestReplaceFromDrafts_BuildsRows(t *testing.T) {
	m := NewMyCampaignsTableModel()
	drafts := []CampaignDraft{
		{ID: "a", Name: "Alpha"},
		{ID: "b", Name: ""},
		{ID: "c", Name: "Charlie"},
	}
	m.ReplaceFromDrafts(drafts)

	if m.RowCount() != 3 {
		t.Fatalf("RowCount = %d; want 3", m.RowCount())
	}
	if name := m.ColumnName(1); name != "Campaign Name" {
		t.Fatalf("ColumnName(1) = %q; want %q", name, "Campaign Name")
	}
	if got := m.Value(0, 1); got != "Alpha" {
		t.Fatalf("row0 name = %v; want %q", got, "Alpha")
	}
	if got := m.Value(1, 1); got != "(unnamed)" {
		t.Fatalf("row1 name = %v; want %q", got, "(unnamed)")
	}
	if got := m.Value(2, 1); got != "Charlie" {
		t.Fatalf("row2 name = %v; want %q", got, "Charlie")
	}
}

func TestRemoveByID_RemovesRow(t *testing.T) {
	m := NewMyCampaignsTableModel()
	drafts := []CampaignDraft{
		{ID: "to-remove", Name: "Remove Me"},
		{ID: "keep", Name: "Keep Me"},
	}
	m.ReplaceFromDrafts(drafts)

	if !m.RemoveByID("to-remove") {
		t.Fatalf("RemoveByID(to-remove) returned false")
	}
	if m.RowCount() != 1 {
		t.Fatalf("RowCount = %d; want 1", m.RowCount())
	}
	if idx := m.IndexByID("to-remove"); idx != -1 {
		t.Fatalf("IndexByID(to-remove) = %d; want -1", idx)
	}
	// Ensure remaining row intact
	if got := m.Value(0, 1); got != "Keep Me" {
		t.Fatalf("remaining row name = %v; want %q", got, "Keep Me")
	}
}
