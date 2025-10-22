using System;
using System.Collections.Generic;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.WinUI3.Services;

public sealed class LocalScenarioService
{
    private readonly LocalEngineService _engine = new();
    private readonly RiskParameterRepository _riskRepo;

    public LocalScenarioService(string? parametersPath = null)
    {
        _riskRepo = new RiskParameterRepository();
        // Use relative path from app base, or override for testing
        string defaultPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Parameters");
        
        // Fallback for dev environment if not copied (e.g. during some test runs if not fully built)
        if (!System.IO.Directory.Exists(defaultPath))
        {
             // Try to find it relative to project root if we are in dev
             string devPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\winui3-mvp\docs\parameters"));
             if (System.IO.Directory.Exists(devPath)) defaultPath = devPath;
        }

        _riskRepo.Load(parametersPath ?? defaultPath);
    }

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
        // Risk Inputs
        public string CustomerType { get; init; } = "RETAIL PRIVATE";
        public string AssetState { get; init; } = "N";
        public string AssetValuationCurve { get; init; } = "MBPC";
        public string Rating { get; init; } = "4.0";
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
            subdownValue: i.SubdownValue,
            customerType: i.CustomerType,
            assetState: i.AssetState,
            avc: i.AssetValuationCurve,
            rating: i.Rating
        );

        // Calculate Risk Parameters
        double pd = _riskRepo.GetPd(i.CustomerType, i.Rating);
        var (dcfLgd, downturnLgd) = _riskRepo.GetLgd(i.CustomerType, i.AssetState, i.AssetValuationCurve);
        
        double corAnnual = BaselIIEngine.CalculateEL(pd, dcfLgd);
        
        // Use EC_TOTAL from parameters as the pragmatic total Economic Capital ratio
        double ecTotal = _riskRepo.GetEcTotal();

        var cof = BuildCofParams(i.Market, (decimal)corAnnual, (decimal)ecTotal);
        var profit = DcfModel.Compute(outputs, cof);
        return new ScenarioOutput { Deal = outputs, Profit = profit };
    }

    private static CofParams BuildCofParams(string market, decimal cor, decimal ecRatio)
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
        return new CofParams
        {
            Curve = curve,
            Spread = 0.0025m,
            OpexPct = opex,
            EconCapRatio = ecRatio,
            CostOfRisk = cor
        };
    }
}
