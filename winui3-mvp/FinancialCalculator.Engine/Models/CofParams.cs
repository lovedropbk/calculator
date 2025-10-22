using System.Collections.Generic;

namespace FinancialCalculator.Engine.Models;

public sealed class CofParams
{
    public IReadOnlyDictionary<int, decimal> Curve { get; init; } = new Dictionary<int, decimal>();
    public decimal Spread { get; init; } = 0.0025m; // 25 bps
    public decimal OpexPct { get; init; } = -0.0095m; // -0.95%
    public decimal EconCapRatio { get; init; } = 0.088m; // 8.8%
    public decimal CostOfRisk { get; init; } = 0.0025m; // 0.25%
    public decimal CapitalAdvantage { get; init; } = 0m; // Can be set if needed
}