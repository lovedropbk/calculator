package main

import (
	"fmt"
	"time"

	"github.com/lxn/walk"
)

// MARK: My Campaigns (Editable) table model and helpers
// This model is intentionally minimal for MVP. Rows are derived from []CampaignDraft.
// Later phases can add live-calculated fields (MonthlyInstallmentStr, etc.) based on engine results.

// MyCampaignRow represents one row in the "My Campaigns" editable table.
type MyCampaignRow struct {
	Selected bool

	// Identity
	ID   string
	Name string

	// Display metrics (MVP placeholders; to be wired to live calc later)
	MonthlyInstallmentStr string // "22,198.61" (without THB prefix)
	Notes                 string
}

// MyCampaignsTableModel implements walk.TableModel for My Campaigns.
type MyCampaignsTableModel struct {
	walk.TableModelBase
	rows []MyCampaignRow
}

// NewMyCampaignsTableModel creates an empty table model.
func NewMyCampaignsTableModel() *MyCampaignsTableModel {
	return &MyCampaignsTableModel{
		rows: []MyCampaignRow{},
	}
}

// ReplaceFromDrafts rebuilds rows based on the given drafts and publishes a reset.
func (m *MyCampaignsTableModel) ReplaceFromDrafts(drafts []CampaignDraft) {
	rows := make([]MyCampaignRow, 0, len(drafts))
	for _, d := range drafts {
		name := d.Name
		if name == "" {
			name = "(unnamed)"
		}
		rows = append(rows, MyCampaignRow{
			Selected:              false,
			ID:                    d.ID,
			Name:                  name,
			MonthlyInstallmentStr: "", // computed later; keep blank for MVP
			Notes:                 "",
		})
	}
	m.rows = rows
	m.PublishRowsReset()
}

// ReplaceRows replaces the internal rows directly and publishes a reset (utility).
func (m *MyCampaignsTableModel) ReplaceRows(rows []MyCampaignRow) {
	m.rows = rows
	m.PublishRowsReset()
}

// SetCampaigns replaces the rows with a copy and publishes a reset (API helper).
func (m *MyCampaignsTableModel) SetCampaigns(rows []MyCampaignRow) {
	if rows == nil {
		m.rows = nil
	} else {
		m.rows = append([]MyCampaignRow(nil), rows...)
	}
	m.PublishRowsReset()
}

// AppendDraft appends a single draft as a row (Selected flag left false by default).
func (m *MyCampaignsTableModel) AppendDraft(d CampaignDraft) {
	name := d.Name
	if name == "" {
		name = "(unnamed)"
	}
	m.rows = append(m.rows, MyCampaignRow{
		Selected:              false,
		ID:                    d.ID,
		Name:                  name,
		MonthlyInstallmentStr: "",
		Notes:                 "",
	})
	m.PublishRowsReset()
}

// AddDraft maps to AppendDraft (parity with milestone spec).
func (m *MyCampaignsTableModel) AddDraft(d CampaignDraft) {
	m.AppendDraft(d)
}

// ToDrafts maps the current rows back to minimal drafts. This is a lossy mapping,
// intended only for Save/Load scaffolding in MVP where rows carry limited fields.
func (m *MyCampaignsTableModel) ToDrafts() []CampaignDraft {
	out := make([]CampaignDraft, 0, len(m.rows))
	now := nowRFC3339()
	for _, r := range m.rows {
		out = append(out, CampaignDraft{
			ID:      r.ID,
			Name:    r.Name,
			Product: "", // product unknown at this layer; left blank for MVP
			Inputs: CampaignInputs{
				PriceExTaxTHB:        0,
				DownpaymentPercent:   0,
				DownpaymentTHB:       0,
				TermMonths:           0,
				BalloonPercent:       0,
				RateMode:             "fixed_rate",
				CustomerRateAPR:      0,
				TargetInstallmentTHB: 0,
			},
			Adjustments: CampaignAdjustments{
				CashDiscountTHB:     0,
				SubdownTHB:          0,
				IDCFreeInsuranceTHB: 0,
				IDCFreeMBSPTHB:      0,
			},
			Metadata: CampaignMetadata{
				CreatedAt: now,
				UpdatedAt: now,
				Version:   1,
			},
		})
	}
	return out
}

// RowCount implements walk.TableModel.
func (m *MyCampaignsTableModel) RowCount() int {
	return len(m.rows)
}

// ColumnCount provides fixed columns for the table.
func (m *MyCampaignsTableModel) ColumnCount() int {
	return 4
}

// ColumnName provides human-readable column headers.
func (m *MyCampaignsTableModel) ColumnName(col int) string {
	switch col {
	case 0:
		return "Sel"
	case 1:
		return "Campaign Name"
	case 2:
		return "Monthly Installment"
	case 3:
		return "Notes"
	default:
		return ""
	}
}

// Value implements walk.TableModel.
func (m *MyCampaignsTableModel) Value(row, col int) interface{} {
	if row < 0 || row >= len(m.rows) {
		return ""
	}
	r := m.rows[row]
	switch col {
	case 0: // Select indicator (radio-like)
		if r.Selected {
			return "•"
		}
		return " "
	case 1: // Campaign Name
		return r.Name
	case 2: // Monthly Installment
		if r.MonthlyInstallmentStr == "" {
			return "—"
		}
		return "THB " + r.MonthlyInstallmentStr
	case 3: // Notes
		return r.Notes
	default:
		return ""
	}
}

// SetSelectedIndex sets the Selected flag for a single row and clears others, then publishes a reset.
func (m *MyCampaignsTableModel) SetSelectedIndex(idx int) {
	if idx < 0 || idx >= len(m.rows) {
		return
	}
	for i := range m.rows {
		m.rows[i].Selected = (i == idx)
	}
	m.PublishRowsReset()
}

// SelectedIndex returns the first selected row index, or -1 if none.
func (m *MyCampaignsTableModel) SelectedIndex() int {
	for i := range m.rows {
		if m.rows[i].Selected {
			return i
		}
	}
	return -1
}

// IndexByID returns the index of the row with the given ID, or -1.
func (m *MyCampaignsTableModel) IndexByID(id string) int {
	for i := range m.rows {
		if m.rows[i].ID == id {
			return i
		}
	}
	return -1
}

// RemoveByID removes the first row with the given ID and publishes a reset. Returns true if removed.
func (m *MyCampaignsTableModel) RemoveByID(id string) bool {
	idx := m.IndexByID(id)
	if idx < 0 {
		return false
	}
	m.rows = append(m.rows[:idx], m.rows[idx+1:]...)
	m.PublishRowsReset()
	return true
}

// SetMonthlyInstallmentByID finds a row by ID and sets MonthlyInstallmentStr (expects numeric string without "THB ").
// Publishes a reset when updated. Returns true if a row was updated.
func (m *MyCampaignsTableModel) SetMonthlyInstallmentByID(id string, value string) bool {
	if m == nil {
		return false
	}
	idx := m.IndexByID(id)
	if idx < 0 {
		return false
	}
	m.rows[idx].MonthlyInstallmentStr = value
	m.PublishRowsReset()
	return true
}

// Rows returns a shallow copy of current rows for read-only consumption.
func (m *MyCampaignsTableModel) Rows() []MyCampaignRow {
	out := make([]MyCampaignRow, len(m.rows))
	copy(out, m.rows)
	return out
}

// AddNewBlankDraft creates a basic CampaignDraft seeded with a unique ID and default fields.
// Callers can fill in product/inputs before saving.
func AddNewBlankDraft(name string) CampaignDraft {
	if stringsTrim := func(s string) string { // local safe trim
		return s
	}; false {
		_ = stringsTrim // hint for older linters; not used
	}
	id := fmt.Sprintf("mc-%d", time.Now().UTC().UnixNano())
	if name == "" {
		name = "New Campaign"
	}
	now := nowRFC3339()
	return CampaignDraft{
		ID:      id,
		Name:    name,
		Product: "",
		Inputs: CampaignInputs{
			RateMode: "fixed_rate",
		},
		Adjustments: CampaignAdjustments{},
		Metadata: CampaignMetadata{
			CreatedAt: now,
			UpdatedAt: now,
			Version:   1,
		},
	}
}
