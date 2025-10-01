package main

import (
	"fmt"
	"strings"
	"time"

	"github.com/financial-calculator/engines/types"
	"github.com/lxn/walk"
)

// MARK: My Campaigns (Editable) table model and helpers
// This model is intentionally minimal for MVP. Rows are derived from []CampaignDraft.
// Later phases can add live-calculated fields (MonthlyInstallmentStr, etc.) based on engine results.

// MyCampaignRow represents one row in the "My Campaigns" editable table.
// Fields match CampaignRow for consistent display across both tables.
type MyCampaignRow struct {
	Selected bool

	// Identity
	ID   string
	Name string

	// Computed metrics (populated from live calculations)
	MonthlyInstallment    float64
	MonthlyInstallmentStr string // "22,198.61" (without THB prefix)
	DownpaymentStr        string // "THB 200,000 (20% DP)"
	SubdownTHBStr         string // "THB 50,000" or "—"
	CashDiscountStr       string // "THB 50,000" or "—"
	FreeInsuranceTHBStr   string // "THB 50,000" or "—" - actual insurance cost
	MBSPTHBStr            string // "THB 150,000" or "—" - actual MBSP cost
	SubsidyUsedTHBStr     string // "THB 200,000" or "—"
	AcqRoRac              float64
	AcqRoRacStr           string // "15.23%"
	DealerCommAmt         float64
	DealerCommPct         float64
	DealerComm            string // "THB 25,000 (2.5%)"
	Notes                 string

	// Profitability snapshot and cashflows for drill-down views
	NominalRate   float64
	EffectiveRate float64
	IDCDealerTHB  float64
	IDCOtherTHB   float64
	SubsidyValue  float64
	SubsidyRorac  string
	Profit        ProfitabilitySnapshot
	Cashflows     []types.Cashflow
}

// CampaignsModel is the data model for the editable campaigns table.
type CampaignsModel struct {
	walk.TableModelBase
	walk.SorterBase

	// Legacy-compatible backing slice of value rows (kept in sync with items).
	rows []MyCampaignRow
	// Pointer slice for any binding helpers (points into rows).
	items []*MyCampaignRow

	// Optional: notify backing store when a row name is edited in-place
	OnRowNameEdited func(id, name string)
}

// NewCampaignsModel creates a new, empty model.
func NewCampaignsModel() *CampaignsModel {
	return new(CampaignsModel)
}

// Items returns the underlying slice for data binding.
func (m *CampaignsModel) Items() interface{} {
	return m.items
}

// ReplaceFromDrafts rebuilds the model's items from campaign drafts.
// Note: This only sets up the row structure - metrics should be computed separately via computeMyCampaignRow.
func (m *CampaignsModel) ReplaceFromDrafts(drafts []CampaignDraft) {
	// Build legacy-compatible value slice and pointer slice.
	m.rows = make([]MyCampaignRow, len(drafts))
	m.items = make([]*MyCampaignRow, len(drafts))

	for i, d := range drafts {
		name := d.Name
		if name == "" {
			name = "(unnamed)"
		}
		m.rows[i] = MyCampaignRow{
			Selected:              false,
			ID:                    d.ID,
			Name:                  name,
			MonthlyInstallmentStr: "",
			Notes:                 "",
		}
		m.items[i] = &m.rows[i]
	}
	m.PublishRowsReset()
}

// UpdateRowWithMetrics updates an existing row with computed metrics.
func (m *CampaignsModel) UpdateRowWithMetrics(id string, computed MyCampaignRow) bool {
	if m == nil {
		return false
	}
	idx := m.IndexByID(id)
	if idx < 0 || idx >= len(m.rows) {
		return false
	}
	
	// Preserve identity, selection, and notes
	computed.ID = m.rows[idx].ID
	computed.Selected = m.rows[idx].Selected
	if computed.Notes == "" {
		computed.Notes = m.rows[idx].Notes
	}
	
	m.rows[idx] = computed
	m.items[idx] = &m.rows[idx]
	m.PublishRowChanged(idx)
	return true
}

// ReplaceRows replaces internal rows (legacy API) and rebuilds item pointers.
func (m *CampaignsModel) ReplaceRows(rows []MyCampaignRow) {
	m.rows = append([]MyCampaignRow(nil), rows...)
	m.items = make([]*MyCampaignRow, len(m.rows))
	for i := range m.rows {
		m.items[i] = &m.rows[i]
	}
	m.PublishRowsReset()
}

// RowCount implements walk.TableModel (legacy API compatibility).
func (m *CampaignsModel) RowCount() int {
	return len(m.rows)
}

// ColumnCount provides fixed columns for the table (legacy API compatibility).
func (m *CampaignsModel) ColumnCount() int {
	return 10
}

// ColumnName provides human-readable column headers (legacy API compatibility).
func (m *CampaignsModel) ColumnName(col int) string {
	switch col {
	case 0:
		return "Sel"
	case 1:
		return "Campaign Name"
	case 2:
		return "Monthly Installment"
	case 3:
		return "Downpayment"
	case 4:
		return "Cash Discount"
	case 5:
		return "Free MBSP THB"
	case 6:
		return "Subsidy utilized"
	case 7:
		return "Acq. RoRAC"
	case 8:
		return "Dealer Comm."
	case 9:
		return "Notes"
	default:
		return ""
	}
}

// Value implements walk.TableModel (legacy API compatibility).
func (m *CampaignsModel) Value(row, col int) interface{} {
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
	case 3: // Downpayment
		if r.DownpaymentStr == "" {
			return "—"
		}
		return r.DownpaymentStr
	case 4: // Cash Discount
		if r.CashDiscountStr == "" {
			return "—"
		}
		return r.CashDiscountStr
	case 5: // Free MBSP THB
		if r.MBSPTHBStr == "" {
			return "—"
		}
		return r.MBSPTHBStr
	case 6: // Subsidy utilized
		if r.SubsidyUsedTHBStr == "" {
			return "—"
		}
		return r.SubsidyUsedTHBStr
	case 7: // Acq. RoRAC
		if r.AcqRoRacStr == "" {
			return "—"
		}
		return r.AcqRoRacStr
	case 8: // Dealer Comm.
		if r.DealerComm == "" {
			return "—"
		}
		return r.DealerComm
	case 9: // Notes
		return r.Notes
	default:
		return ""
	}
}

// SetCampaignNameByID finds a row by ID and sets Name. Publishes a reset when updated.
func (m *CampaignsModel) SetCampaignNameByID(id string, name string) bool {
	if m == nil {
		return false
	}
	idx := m.IndexByID(id)
	if idx < 0 {
		return false
	}
	name = strings.TrimSpace(name)
	if name == "" {
		name = "(unnamed)"
	}
	if m.rows[idx].Name != name {
		m.rows[idx].Name = name
		// items points into rows, so pointer slice reflects the change
		m.PublishRowChanged(idx)
		if m.OnRowNameEdited != nil {
			m.OnRowNameEdited(id, name)
		}
	}
	return true
}

// SetValue enables in-place editing for Campaign Name (col 1) and Notes (col 9).
// TableView will call this only for columns marked Editable: true (legacy API).
func (m *CampaignsModel) SetValue(row, col int, value interface{}) error {
	if m == nil {
		return nil
	}
	if row < 0 || row >= len(m.rows) {
		return nil
	}
	switch col {
	case 1: // Campaign Name
		s, _ := value.(string)
		s = strings.TrimSpace(s)
		if s == "" {
			s = "(unnamed)"
		}
		if m.rows[row].Name != s {
			id := m.rows[row].ID
			m.rows[row].Name = s
			m.PublishRowChanged(row)
			if m.OnRowNameEdited != nil {
				m.OnRowNameEdited(id, s)
			}
		}
	case 9: // Notes
		s, _ := value.(string)
		if m.rows[row].Notes != s {
			m.rows[row].Notes = s
			m.PublishRowChanged(row)
		}
	}
	return nil
}

// AddDraft appends a single draft as a row.
func (m *CampaignsModel) AddDraft(d CampaignDraft) {
	name := d.Name
	if name == "" {
		name = "(unnamed)"
	}
	row := MyCampaignRow{
		Selected:              false,
		ID:                    d.ID,
		Name:                  name,
		MonthlyInstallmentStr: "",
		Notes:                 "",
	}
	m.rows = append(m.rows, row)
	// CRITICAL: Rebuild items pointers after append, as append may reallocate the backing array
	m.items = make([]*MyCampaignRow, len(m.rows))
	for i := range m.rows {
		m.items[i] = &m.rows[i]
	}
	m.PublishRowsReset()
}

// ToDrafts maps the current model items back to minimal drafts.
func (m *CampaignsModel) ToDrafts() []CampaignDraft {
	out := make([]CampaignDraft, 0, len(m.items))
	now := nowRFC3339()
	for _, r := range m.items {
		out = append(out, CampaignDraft{
			ID:   r.ID,
			Name: r.Name,
			// Note: This is a lossy conversion, suitable for MVP persistence.
			Metadata: CampaignMetadata{
				UpdatedAt: now,
				Version:   1,
			},
		})
	}
	return out
}

// SetSelectedIndex sets the Selected flag for a single row and clears others.
func (m *CampaignsModel) SetSelectedIndex(idx int) {
	if idx < 0 || idx >= len(m.items) {
		return
	}
	for i := range m.items {
		m.items[i].Selected = (i == idx)
	}
	m.PublishRowsReset()
}

// SelectedIndex returns the first selected row index, or -1 if none.
func (m *CampaignsModel) SelectedIndex() int {
	for i, item := range m.items {
		if item.Selected {
			return i
		}
	}
	return -1
}

// IndexByID returns the index of the row with the given ID, or -1.
func (m *CampaignsModel) IndexByID(id string) int {
	for i, item := range m.items {
		if item.ID == id {
			return i
		}
	}
	return -1
}

// RemoveByID removes the first row with the given ID. Returns true if removed.
func (m *CampaignsModel) RemoveByID(id string) bool {
	idx := m.IndexByID(id)
	if idx < 0 {
		return false
	}
	m.items = append(m.items[:idx], m.items[idx+1:]...)
	m.PublishRowsReset()
	return true
}

// SetMonthlyInstallmentByID finds a row by ID and sets MonthlyInstallmentStr.
func (m *CampaignsModel) SetMonthlyInstallmentByID(id string, value string) bool {
	if m == nil {
		return false
	}
	idx := m.IndexByID(id)
	if idx < 0 {
		return false
	}
	m.items[idx].MonthlyInstallmentStr = value
	m.PublishRowChanged(idx)
	return true
}

// Rows returns a shallow copy of current rows for read-only consumption.
func (m *CampaignsModel) Rows() []MyCampaignRow {
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
