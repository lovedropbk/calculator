namespace FinancialCalculator.Engine.Models;

public sealed record class CalculatorOutputs
{
    // Inputs echo for UI
    public CalculatorInputs Inputs { get; init; } = default!;

    // Key amounts
    public decimal FinancedAmount { get; init; }

    // Payment/Rate
    public decimal MonthlyRate { get; init; }
    public decimal FlatRatePercentPerAnnum { get; init; }

    // IRR metrics
    public decimal DealIrrAnnualPercent { get; init; }
    public decimal DealIrrAnnualPercentWithoutUpfrontIncomes { get; init; }
    public decimal DealIrrAnnualPercentWithoutUpfrontCosts { get; init; }
    public decimal DealIrrAnnualPercentBaseline { get; init; }

    // Impacts calculated against Baseline (no upfronts) to isolate effects
    public decimal UpfrontIncomeRateImpactBps => (DealIrrAnnualPercentWithoutUpfrontCosts - DealIrrAnnualPercentBaseline) * 100;
    public decimal UpfrontCostRateImpactBps => (DealIrrAnnualPercentBaseline - DealIrrAnnualPercentWithoutUpfrontIncomes) * 100;

    // Schedule
    public IReadOnlyList<ScheduleRow> Schedule { get; init; } = Array.Empty<ScheduleRow>();

    // T0 breakdown (engine perspective)
    public decimal T0Disbursement { get; init; }
    public decimal T0UpfrontSubsidies { get; init; }
    public decimal T0UpfrontCosts { get; init; }
    public decimal T0Net => T0UpfrontSubsidies - T0UpfrontCosts - T0Disbursement;
}
