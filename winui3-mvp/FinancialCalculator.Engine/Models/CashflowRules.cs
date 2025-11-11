using System;

namespace FinancialCalculator.Engine.Models
{
    /// <summary>
    /// Identifies the nature of a schedule row for display and export.
    /// Regular rows are standard amortization payments.
    /// Holiday rows represent zero-payment periods where interest is not charged and not capitalized (waived).
    /// </summary>
    public enum PaymentKind
    {
        Regular = 0,
        Holiday = 1
    }

    /// <summary>
    /// Defines a payment holiday rule with monthly capitalization and unchanged maturity.
    /// StartPeriod and EndPeriod are 1-based inclusive indexes into the schedule periods.
    /// Example: StartPeriod=4, EndPeriod=6 applies to months 4-6 inclusive.
    /// </summary>
    public sealed record class PaymentHolidayRule
    {
        /// <summary>1-based inclusive start period of the holiday interval</summary>
        public int StartPeriod { get; init; }

        /// <summary>1-based inclusive end period of the holiday interval</summary>
        public int EndPeriod { get; init; }

        /// <summary>Optional identifier for audit and UI tagging</summary>
        public string? RuleId { get; init; }
    }
}