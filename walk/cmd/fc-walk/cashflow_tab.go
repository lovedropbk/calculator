package main

import (
	"sort"
	"time"

	"github.com/financial-calculator/engines/types"
	"github.com/lxn/walk"
)

// CashflowRow represents one row in the Cashflow TableView
// Columns: Period, Date, Principal Outflow (t0), Downpayment Inflow (t0), Balloon Inflow (maturity),
//
//	Principal Amortization, Interest, IDCs, Subsidy, Installment
type CashflowRow struct {
	Period                int
	Date                  time.Time
	PrincipalOutflow      float64
	DownpaymentInflow     float64
	BalloonInflow         float64
	PrincipalAmortization float64
	Interest              float64
	IDCs                  float64
	Subsidy               float64
	Installment           float64
	IsTotal               bool
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

	// Totals row: show "Totals" label in Period column, blank date
	if r.IsTotal {
		switch col {
		case 0:
			return "Totals"
		case 1:
			return ""
		case 2:
			if r.PrincipalOutflow == 0 {
				return ""
			}
			return "THB " + FormatTHB(r.PrincipalOutflow)
		case 3:
			if r.DownpaymentInflow == 0 {
				return ""
			}
			return "THB " + FormatTHB(r.DownpaymentInflow)
		case 4:
			if r.BalloonInflow == 0 {
				return ""
			}
			return "THB " + FormatTHB(r.BalloonInflow)
		case 5:
			return "THB " + FormatTHB(r.PrincipalAmortization)
		case 6:
			return "THB " + FormatTHB(r.Interest)
		case 7:
			return "THB " + FormatTHB(r.IDCs)
		case 8:
			return "THB " + FormatTHB(r.Subsidy)
		case 9:
			return "THB " + FormatTHB(r.Installment)
		default:
			return ""
		}
	}

	switch col {
	case 0:
		return r.Period
	case 1:
		return r.Date.Format("2006-01-02")
	case 2:
		// Show only for t0; blank otherwise
		if r.PrincipalOutflow == 0 {
			return ""
		}
		return "THB " + FormatTHB(r.PrincipalOutflow)
	case 3:
		if r.DownpaymentInflow == 0 {
			return ""
		}
		return "THB " + FormatTHB(r.DownpaymentInflow)
	case 4:
		if r.BalloonInflow == 0 {
			return ""
		}
		return "THB " + FormatTHB(r.BalloonInflow)
	case 5:
		return "THB " + FormatTHB(r.PrincipalAmortization)
	case 6:
		return "THB " + FormatTHB(r.Interest)
	case 7:
		return "THB " + FormatTHB(r.IDCs)
	case 8:
		return "THB " + FormatTHB(r.Subsidy)
	case 9:
		return "THB " + FormatTHB(r.Installment)
	default:
		return ""
	}
}

// buildCashflowRows aggregates engine cashflows into table rows and appends a Totals footer row.
// Heuristics:
// - Principal Outflow: t0 disbursement outflow (negative), blank for other dates.
// - Downpayment Inflow: synthetic t0 inflow (UI-only) using CashflowDownPayment type.
// - Balloon Inflow: balloon principal inflow on maturity date.
// - Principal Amortization / Interest / Installment from periodic principal schedule rows.
// - IDC and Subsidy columns are absolute THB amounts summed by date.
// - Totals row sums all monetary columns.
func buildCashflowRows(flows []types.Cashflow) []CashflowRow {
	if len(flows) == 0 {
		// Always return a non-empty slice for display; pad with a zero row and totals.
		now := time.Now()
		rows := []CashflowRow{{
			Period:                1,
			Date:                  now,
			PrincipalOutflow:      0,
			DownpaymentInflow:     0,
			BalloonInflow:         0,
			PrincipalAmortization: 0,
			Interest:              0,
			IDCs:                  0,
			Subsidy:               0,
			Installment:           0,
		}}
		rows = append(rows, CashflowRow{
			IsTotal: true,
		})
		return rows
	}

	type agg struct {
		date                  time.Time
		principalOutflow      float64
		downpaymentInflow     float64
		balloonInflow         float64
		principalAmortization float64
		interest              float64
		idcs                  float64
		subsidy               float64
		installment           float64
	}

	byDate := map[time.Time]*agg{}

	addForDate := func(dt time.Time) *agg {
		key := day(dt)
		if a, ok := byDate[key]; ok {
			return a
		}
		a := &agg{date: key}
		byDate[key] = a
		return a
	}

	for _, cf := range flows {
		a := addForDate(cf.Date)
		switch cf.Type {
		case types.CashflowDisbursement:
			// t0 dealer disbursement outflow; display as negative
			amt := cf.Amount.InexactFloat64()
			if cf.Direction == "out" {
				amt = -amt
			}
			a.principalOutflow += amt
		case types.CashflowDownPayment:
			amt := cf.Amount.InexactFloat64()
			if amt < 0 {
				amt = -amt
			}
			a.downpaymentInflow += amt
		case types.CashflowPrincipal:
			a.installment += cf.Amount.InexactFloat64()
			a.principalAmortization += cf.Principal.InexactFloat64()
			a.interest += cf.Interest.InexactFloat64()
		case types.CashflowBalloon:
			// Treat balloon separately and also include in installment column
			a.balloonInflow += cf.Principal.InexactFloat64()
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

	rows := make([]CashflowRow, 0, len(dates)+1)
	period := 0

	// Totals accumulators
	var tPO, tDP, tBI, tPA, tInt, tIDC, tSub, tInst float64

	for _, d := range dates {
		period++
		a := byDate[d]
		rows = append(rows, CashflowRow{
			Period:                period,
			Date:                  a.date,
			PrincipalOutflow:      a.principalOutflow,
			DownpaymentInflow:     a.downpaymentInflow,
			BalloonInflow:         a.balloonInflow,
			PrincipalAmortization: a.principalAmortization,
			Interest:              a.interest,
			IDCs:                  a.idcs,
			Subsidy:               a.subsidy,
			Installment:           a.installment,
		})

		tPO += a.principalOutflow
		tDP += a.downpaymentInflow
		tBI += a.balloonInflow
		tPA += a.principalAmortization
		tInt += a.interest
		tIDC += a.idcs
		tSub += a.subsidy
		tInst += a.installment
	}

	// Append Totals row
	rows = append(rows, CashflowRow{
		IsTotal:               true,
		PrincipalOutflow:      tPO,
		DownpaymentInflow:     tDP,
		BalloonInflow:         tBI,
		PrincipalAmortization: tPA,
		Interest:              tInt,
		IDCs:                  tIDC,
		Subsidy:               tSub,
		Installment:           tInst,
	})

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
