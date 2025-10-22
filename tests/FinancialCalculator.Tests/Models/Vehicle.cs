using System.Collections.Generic;

namespace FinancialCalculator.Tests.Models;

public class Vehicle
{
    public string Class { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public double MSRP { get; set; }
    public double? RV12 { get; set; }
    public double? RV24 { get; set; }
    public double? RV36 { get; set; }
    public double? RV48 { get; set; }
    public double? RV60 { get; set; }

    public Dictionary<string, double> MbspCosts { get; set; } = new();

    public double? GetRVForTerm(int termMonths)
    {
        return termMonths switch
        {
            12 => RV12,
            24 => RV24,
            36 => RV36,
            48 => RV48,
            60 => RV60,
            _ => null
        };
    }
}