using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinancialCalculator.Engine.Models;

public sealed record class CalculatorInputs
{
    [Range(0, double.MaxValue)]
    public decimal VehicleSalesPrice { get; init; }

    [Range(0, double.MaxValue)]
    public decimal AdditionalFinancedItems { get; init; }

    // Downpayment may be in THB or percentage of sales price
    public bool DownpaymentIsPercent { get; init; }

    [Range(0, 1_000_000)]
    public decimal DownpaymentValue { get; init; }

    [Range(1, 600)]
    public int TermMonths { get; init; }

    public PaymentMode PaymentMode { get; init; }

    public FinancialProduct Product { get; init; }

    // Customer nominal APR in percent (annual, nominal, comp monthly). 0-100
    [Range(0, 100)]
    public decimal CustomerRatePercent { get; init; }

    // Optional balloon; specify either absolute THB or percentage
    public bool BalloonIsPercent { get; init; }

    [Range(0, 100)]
    public decimal BalloonPercent { get; init; }

    [Range(0, double.MaxValue)]
    public decimal BalloonTHB { get; init; }

    // Periodic fees/subsidies as annual percent applied on outstanding principal
    // Positive values represent income to the lender (increase IRR)
    // Negative values represent cost to the lender (reduce IRR)
    public decimal PeriodicFeeAnnualPercent { get; init; }

    // Upfront amounts (t0, NOT financed): positive = income/subsidy; negative = cost
    public decimal UpfrontSubsidies { get; init; } // increases IRR
    public decimal UpfrontCosts { get; init; }     // reduces IRR

    // Subdown campaign: additional downpayment funded by the lender (reduces financed amount)
    // This subsidy is consumed here and is NOT counted again in UpfrontSubsidies for IRR.
    public bool SubdownIsPercent { get; init; }
    public decimal SubdownPercent { get; init; }
    public decimal SubdownTHB { get; init; }

    // Risk Parameters (for Basel II CoR/EC)
    public string CustomerType { get; init; } = "RETAIL PRIVATE";
    public string AssetState { get; init; } = "N"; // "N" = New, "U" = Used
    public string AssetValuationCurve { get; init; } = "MBPC"; // e.g., MBPC, MBVA
    public string Rating { get; init; } = "4.0";

    // Cashflow rules: Payment holidays with NO capitalization (no interest charged) and unchanged maturity
    public IReadOnlyList<PaymentHolidayRule> PaymentHolidays { get; init; } = Array.Empty<PaymentHolidayRule>();
}
