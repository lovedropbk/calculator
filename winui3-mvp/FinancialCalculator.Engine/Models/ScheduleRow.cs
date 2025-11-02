using System;

namespace FinancialCalculator.Engine.Models;

public sealed class ScheduleRow
{
    public int Period { get; init; }
    public decimal Principal { get; init; }
    public decimal Interest { get; init; }
    public decimal Balance { get; init; }
    public decimal Cashflow { get; init; }

    // MARK: Holiday annotations
    public PaymentKind Kind { get; init; } = PaymentKind.Regular;
    public decimal CapitalizedInterest { get; init; } // interest accrued and added to balance during holiday
    public string? RuleId { get; init; }
}
