using System;
using System.Collections.Generic;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.WinUI3.Services;

public sealed class LocalScenarioService
{
    private readonly LocalEngineService _engine = new();

    public sealed record class ScenarioInput
    {
        public string Market { get; init; } = "TH";
        public string Product { get; init; } = "HP";
        public string Timing { get; init; } = "arrears";
        public int TermMonths { get; init; }
        public decimal VehiclePrice { get; init; }
        public decimal AdditionalFinancedItems { get; init; }
        public bool DownIsPercent { get; init; }
        public decimal DownValue { get; init; }
        public bool BalloonIsPercent { get; init; }
        public decimal BalloonValue { get; init; }
        public decimal CustomerRatePercent { get; init; }
        public decimal UpfrontSubsidies { get; init; }
        public decimal UpfrontCosts { get; init; }
        public bool SubdownIsPercent { get; init; }
        public decimal SubdownValue { get; init; }
    }

    public sealed class ScenarioOutput
    {
        public CalculatorOutputs Deal { get; init; } = default!;
        public Profitability Profit { get; init; } = default!;
    }

    public ScenarioOutput Compute(ScenarioInput i)
    {
        var outputs = _engine.Calculate(
            timing: i.Timing,
            product: i.Product,
            vehicleSalesPrice: i.VehiclePrice,
            additionalFinancedItems: i.AdditionalFinancedItems,
            downIsPercent: i.DownIsPercent,
            downValue: i.DownValue,
            termMonths: i.TermMonths,
            customerRatePercent: i.CustomerRatePercent,
            balloonIsPercent: i.BalloonIsPercent,
            balloonValue: i.BalloonValue,
            periodicFeeAnnualPercent: 0m,
            upfrontSubsidies: i.UpfrontSubsidies,
            upfrontCosts: i.UpfrontCosts,
            subdownIsPercent: i.SubdownIsPercent,
            subdownValue: i.SubdownValue
        );

        var cof = BuildCofParams(i.Market);
        var profit = RoracCalculator.Compute(outputs, cof);
        return new ScenarioOutput { Deal = outputs, Profit = profit };
    }

    private static RoracCalculator.CofParams BuildCofParams(string market)
    {
        var curve = new Dictionary<int, decimal>
        {
            {12, 0.0148m},
            {24, 0.0165m},
            {36, 0.0175m},
            {48, 0.0185m},
            {60, 0.0195m},
        };
        var opex = string.Equals(market, "AT", StringComparison.OrdinalIgnoreCase) ? 0.088m : -0.0095m;
        return new RoracCalculator.CofParams
        {
            Curve = curve,
            Spread = 0.0025m,
            OpexPct = opex,
            EconCapRatio = 0.08m
        };
    }
}
