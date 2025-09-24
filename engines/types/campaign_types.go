package types

// MARK: Campaign DTOs for Subinterest solvers
//
// This file defines input/output contracts for subsidy-first subinterest solvers.
// Keep this DTO file focused and under 500 lines per repository rules.

import (
	"github.com/shopspring/decimal"
)

// RateCaps defines minimum and maximum nominal annual rate caps (as fractions, e.g., 0.059 for 5.90%).
type RateCaps struct {
	MinNominal decimal.Decimal `json:"min_nominal"` // lower bound (inclusive)
	MaxNominal decimal.Decimal `json:"max_nominal"` // upper bound (inclusive)
}

// CampaignBudgetInput is the input for the budget-constrained subinterest solver.
//   - The solver finds the lowest possible customer nominal rate r_target ≤ r_base such that
//     PV_base − PV_target(r_target) ≈ BudgetTHB within 0.01 THB, respecting RateCaps.
type CampaignBudgetInput struct {
	Deal         Deal            `json:"deal"`
	ParameterSet ParameterSet    `json:"parameter_set"`
	BudgetTHB    decimal.Decimal `json:"budget_thb"`
	RateCaps     *RateCaps       `json:"rate_caps,omitempty"`
}

// CampaignRateInput is the input for the manual target solver.
// - Provide either TargetNominalRate or TargetInstallment (one is required).
// - BudgetTHB is optional; when present, result flags OverBudget and SubsidyUsedTHB is clipped to BudgetTHB.
type CampaignRateInput struct {
	Deal              Deal             `json:"deal"`
	ParameterSet      ParameterSet     `json:"parameter_set"`
	TargetNominalRate *decimal.Decimal `json:"target_nominal_rate,omitempty"` // desired customer nominal annual rate
	TargetInstallment *decimal.Decimal `json:"target_installment,omitempty"`  // desired monthly installment (THB)
	BudgetTHB         *decimal.Decimal `json:"budget_thb,omitempty"`          // optional budget for subsidy
	RateCaps          *RateCaps        `json:"rate_caps,omitempty"`
}

// CampaignError provides a structured, non-panicking error model.
type CampaignError struct {
	Code       string            `json:"code"` // e.g., "invalid_inputs", "convergence_failure"
	Summary    string            `json:"summary"`
	Detail     string            `json:"detail"`
	BestEffort map[string]string `json:"best_effort,omitempty"` // any partial diagnostics or suggestions
}

// CampaignMetrics aggregates the key numbers for a subinterest result.
type CampaignMetrics struct {
	// Headline customer terms
	CustomerNominalRate   decimal.Decimal `json:"customer_nominal_rate"`   // annual nominal (fraction, e.g., 0.0590)
	CustomerEffectiveRate decimal.Decimal `json:"customer_effective_rate"` // annual effective (fraction)
	MonthlyInstallment    decimal.Decimal `json:"monthly_installment"`     // THB per period

	// Subsidy accounting
	SubsidyUsedTHB     decimal.Decimal `json:"subsidy_used_thb"`     // actually applied subsidy (≤ budget when present)
	RequiredSubsidyTHB decimal.Decimal `json:"required_subsidy_thb"` // subsidy required to achieve manual target (if provided)
	ExceedTHB          decimal.Decimal `json:"exceed_thb"`           // positive residual: budget not used (budget mode) or shortfall (manual mode)
	OverBudget         bool            `json:"over_budget"`          // true when RequiredSubsidyTHB > BudgetTHB in manual target mode

	// Dealer commission resolution (if policy available), else zero with diagnostic
	DealerCommissionResolvedTHB decimal.Decimal `json:"dealer_commission_resolved_thb"`
	DealerCommissionPctResolved decimal.Decimal `json:"dealer_commission_pct_resolved"`

	// IDC totals (if modeled by caller; solvers set to zero)
	IDCTotalTHB decimal.Decimal `json:"idc_total_thb"`

	// Profitability KPIs (from profitability engine)
	AcquisitionRoRAC decimal.Decimal `json:"acquisition_rorac"`
	NetEBITMargin    decimal.Decimal `json:"net_ebit_margin"`
	EconomicCapital  decimal.Decimal `json:"economic_capital"`
}

// CampaignResult is the complete result envelope from the subinterest solvers.
// It includes the rounded periodic schedule and full cashflows augmented with a T0 “Subsidy” inflow.
type CampaignResult struct {
	Metrics               CampaignMetrics   `json:"metrics"`
	Schedule              []Cashflow        `json:"schedule"`                 // periodic schedule from pricing engine (rounded)
	Cashflows             []Cashflow        `json:"cashflows"`                // T0 flows + periodic schedule (+ balloon if any)
	ParameterSetVersionID string            `json:"parameter_set_version_id"` // pinned version id for determinism
	Diagnostics           map[string]string `json:"diagnostics,omitempty"`    // explicit diagnostic codes and notes
	Warnings              []string          `json:"warnings,omitempty"`
	Error                 *CampaignError    `json:"error,omitempty"`
}
