package main

import (
	"sort"
	"time"

	"github.com/financial-calculator/engines/types"
	"github.com/lxn/walk"
)

// CashflowRow represents one row in the Cashflow TableView
type CashflowRow struct {
	Period      int
	Date        time.Time
	Principal   float64
	Interest    float64
	IDCs        float64
	Subsidy     float64
	Installment float64
}

type CashflowTableModel struct {
	walk.TableModelBase
	rows []CashflowRow
}

func NewCashflowTableModel() *CashflowTableModel {
	return &CashflowTableModel{rows: []CashflowRow{}}
}

func (m *CashflowTableModel) ReplaceRows(rows []CashflowRow) {
	m.rows = rows
	m.PublishRowsReset()
}

func (m *CashflowTableModel) RowCount() int {
	return len(m.rows)
}

func (m *CashflowTableModel) Value(row, col int) interface{} {
	if row < 0 || row >= len(m.rows) {
		return ""
	}
	r := m.rows[row]
	switch col {
	case 0:
		return r.Period
	case 1:
		return r.Date.Format("2006-01-02")
	case 2:
		return "THB " + FormatTHB(r.Principal)
	case 3:
		return "THB " + FormatTHB(r.Interest)
	case 4:
		return "THB " + FormatTHB(r.IDCs)
	case 5:
		return "THB " + FormatTHB(r.Subsidy)
	case 6:
		return "THB " + FormatTHB(r.Installment)
	default:
		return ""
	}
}

// buildCashflowRows aggregates engine cashflows into table rows.
// Heuristics:
// - Periodic schedule entries are Type=principal (Amount=installment; Principal/Interest breakdown provided).
// - Balloon is Type=balloon with Amount=principal; add as a row on its date.
// - IDC flows (Type=idc) are summed into IDCs column on their dates (absolute THB).
// - Subsidy flows (Type=subsidy) are summed into Subsidy column on their dates (absolute THB).
// - A T0 row naturally appears if T0 flows exist on payout date.
func buildCashflowRows(flows []types.Cashflow) []CashflowRow {
	if len(flows) == 0 {
		// Always return a non-empty slice for display; pad with a zero row.
		return []CashflowRow{{
			Period:      1,
			Date:        time.Now(),
			Principal:   0,
			Interest:    0,
			IDCs:        0,
			Subsidy:     0,
			Installment: 0,
		}}
	}

	type agg struct {
		date        time.Time
		principal   float64
		interest    float64
		idcs        float64
		subsidy     float64
		installment float64
	}

	byDate := map[time.Time]*agg{}

	addForDate := func(dt time.Time) *agg {
		if a, ok := byDate[day(dt)]; ok {
			return a
		}
		a := &agg{date: day(dt)}
		byDate[a.date] = a
		return a
	}

	for _, cf := range flows {
		a := addForDate(cf.Date)
		switch cf.Type {
		case types.CashflowPrincipal:
			a.installment += cf.Amount.InexactFloat64()
			a.principal += cf.Principal.InexactFloat64()
			a.interest += cf.Interest.InexactFloat64()
		case types.CashflowBalloon:
			a.principal += cf.Principal.InexactFloat64()
			a.installment += cf.Amount.InexactFloat64()
		case types.CashflowIDC:
			amt := cf.Amount.InexactFloat64()
			if amt < 0 {
				amt = -amt
			}
			a.idcs += amt
		case types.CashflowSubsidy:
			amt := cf.Amount.InexactFloat64()
			if amt < 0 {
				amt = -amt
			}
			a.subsidy += amt
		default:
			// ignore other types for this table
		}
	}

	// Order dates ascending
	var dates []time.Time
	for d := range byDate {
		dates = append(dates, d)
	}
	sort.Slice(dates, func(i, j int) bool { return dates[i].Before(dates[j]) })

	rows := make([]CashflowRow, 0, len(dates))
	period := 0
	for _, d := range dates {
		period++
		a := byDate[d]
		rows = append(rows, CashflowRow{
			Period:      period,
			Date:        a.date,
			Principal:   a.principal,
			Interest:    a.interest,
			IDCs:        a.idcs,
			Subsidy:     a.subsidy,
			Installment: a.installment,
		})
	}
	return rows
}

// refreshCashflowTable ensures the TableView is bound with a model and rows.
func refreshCashflowTable(tv *walk.TableView, flows []types.Cashflow) {
	if tv == nil {
		return
	}
	rows := buildCashflowRows(flows)

	if model, ok := tv.Model().(*CashflowTableModel); ok && model != nil {
		model.ReplaceRows(rows)
		return
	}
	m := NewCashflowTableModel()
	m.ReplaceRows(rows)
	_ = tv.SetModel(m)
}

// day normalizes a time to yyyy-mm-dd at local midnight for grouping.
func day(t time.Time) time.Time {
	return time.Date(t.Year(), t.Month(), t.Day(), 0, 0, 0, 0, t.Location())
}
