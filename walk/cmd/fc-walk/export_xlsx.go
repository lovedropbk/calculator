package main

import (
	"fmt"
	"path/filepath"

	"github.com/lxn/walk"
	"github.com/xuri/excelize/v2"
)

// doExportXLSX creates an XLSX file with DealSummary and CashflowSchedule sheets.
// summary: key-value pairs to write as two columns.
// cfRows: rows from buildCashflowRows for the schedule table.
func doExportXLSX(mw *walk.MainWindow, summary map[string]string, cfRows []CashflowRow) error {
	if mw == nil {
		return fmt.Errorf("main window is nil")
	}

	// Save dialog
	var dlg walk.FileDialog
	dlg.Title = "Export XLSX"
	dlg.Filter = "Excel Workbook (*.xlsx)|*.xlsx"
	dlg.FilePath = "deal.xlsx"
	if ok, err := dlg.ShowSave(mw); err != nil {
		return err
	} else if !ok {
		return nil // user canceled
	}

	fp := dlg.FilePath
	if filepath.Ext(fp) == "" {
		fp += ".xlsx"
	}

	// Build workbook
	f := excelize.NewFile()
	// Remove default sheet
	def := f.GetSheetName(0)
	if def != "" {
		_ = f.DeleteSheet(def)
	}

	// Sheet 1: DealSummary
	const shSummary = "DealSummary"
	f.NewSheet(shSummary)
	// headers
	f.SetCellValue(shSummary, "A1", "Parameter")
	f.SetCellValue(shSummary, "B1", "Value")

	// Write summary rows in deterministic order
	row := 2
	write := func(k, v string) {
		_ = f.SetCellValue(shSummary, fmt.Sprintf("A%d", row), k)
		_ = f.SetCellValue(shSummary, fmt.Sprintf("B%d", row), v)
		row++
	}

	// Preferred order
	order := []string{
		"Product",
		"Price ex tax (THB)",
		"Down payment",
		"Term (months)",
		"Timing",
		"Balloon",
		"Rate mode",
		"Customer rate (% p.a.)",
		"Target installment (THB)",
		"Subsidy budget (THB)",
		"IDCs - Other (THB)",
		"Selected Campaign",
		"Monthly Installment (THB)",
		"Nominal Rate",
		"Effective Rate",
		"Acq RoRAC",
		"Dealer Commission",
	}
	seen := map[string]bool{}
	for _, k := range order {
		if v, ok := summary[k]; ok {
			write(k, v)
			seen[k] = true
		}
	}
	// Any additional keys
	for k, v := range summary {
		if !seen[k] {
			write(k, v)
		}
	}

	// Sheet 2: CashflowSchedule
	const shCF = "CashflowSchedule"
	f.NewSheet(shCF)
	// headers
	headers := []string{
		"Period",
		"Date",
		"Principal Outflow",
		"Downpayment Inflow",
		"Balloon Inflow",
		"Principal Amortization",
		"Interest",
		"IDCs",
		"Subsidy",
		"Installment",
	}
	for i, h := range headers {
		col := fmt.Sprintf("%c", 'A'+i)
		cell := fmt.Sprintf("%s1", col)
		_ = f.SetCellValue(shCF, cell, h)
	}
	for i, r := range cfRows {
		rn := i + 2
		_ = f.SetCellValue(shCF, fmt.Sprintf("A%d", rn), r.Period)
		_ = f.SetCellValue(shCF, fmt.Sprintf("B%d", rn), r.Date.Format("2006-01-02"))
		_ = f.SetCellValue(shCF, fmt.Sprintf("C%d", rn), r.PrincipalOutflow)
		_ = f.SetCellValue(shCF, fmt.Sprintf("D%d", rn), r.DownpaymentInflow)
		_ = f.SetCellValue(shCF, fmt.Sprintf("E%d", rn), r.BalloonInflow)
		_ = f.SetCellValue(shCF, fmt.Sprintf("F%d", rn), r.PrincipalAmortization)
		_ = f.SetCellValue(shCF, fmt.Sprintf("G%d", rn), r.Interest)
		_ = f.SetCellValue(shCF, fmt.Sprintf("H%d", rn), r.IDCs)
		_ = f.SetCellValue(shCF, fmt.Sprintf("I%d", rn), r.Subsidy)
		_ = f.SetCellValue(shCF, fmt.Sprintf("J%d", rn), r.Installment)
	}

	// Make Summary the first active sheet
	if idx, err := f.GetSheetIndex(shSummary); err == nil {
		f.SetActiveSheet(idx)
	}

	// Save
	if err := f.SaveAs(fp); err != nil {
		return err
	}
	return nil
}
