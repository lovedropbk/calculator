namespace FinancialCalculator.Engine.Models;

public sealed class ScheduleRow
{
    public int Period { get; init; }
    public decimal Principal { get; init; }
    public decimal Interest { get; init; }
    public decimal Balance { get; init; }
    public decimal Cashflow { get; init; }
}
