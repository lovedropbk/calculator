using System;
using System.Collections.Generic;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.Engine.Core;

public sealed class DealEngine
{
    private readonly FinancialCalculator _calc = new();
    private readonly IRiskParameterRepository _riskRepo;

    public DealEngine(IRiskParameterRepository riskRepo)
    {
        _riskRepo = riskRepo ?? throw new ArgumentNullException(nameof(riskRepo));
    }

    public sealed record class DealInput
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

    public sealed class DealOutput
    {
        public CalculatorOutputs Deal { get; init; } = default!;
        public Profitability Profit { get; init; } = default!;
    }

    public DealOutput Calculate(DealInput i)
    {
        var calcInput = new CalculatorInputs
        {
            VehicleSalesPrice = i.VehiclePrice,
            AdditionalFinancedItems = i.AdditionalFinancedItems,
            DownpaymentIsPercent = i.DownIsPercent,
            DownpaymentValue = i.DownValue,
            TermMonths = i.TermMonths,
            PaymentMode = ParseTiming(i.Timing),
            Product = ParseProduct(i.Product),
            CustomerRatePercent = i.CustomerRatePercent,
            BalloonIsPercent = i.BalloonIsPercent,
            BalloonPercent = i.BalloonIsPercent ? i.BalloonValue : 0,
            BalloonTHB = i.BalloonIsPercent ? 0 : i.BalloonValue,
            PeriodicFeeAnnualPercent = 0m,
            UpfrontSubsidies = i.UpfrontSubsidies,
            UpfrontCosts = i.UpfrontCosts,
            SubdownIsPercent = i.SubdownIsPercent,
            SubdownPercent = i.SubdownIsPercent ? i.SubdownValue : 0,
            SubdownTHB = i.SubdownIsPercent ? 0 : i.SubdownValue,
            CustomerType = i.CustomerType,
            AssetState = i.AssetState,
            AssetValuationCurve = i.AssetValuationCurve,
            Rating = i.Rating
        };

        var outputs = _calc.Calculate(calcInput);

        // Calculate Risk Parameters
        double pd = _riskRepo.GetPd(i.CustomerType, i.Rating);
        var (dcfLgd, downturnLgd) = _riskRepo.GetLgd(i.CustomerType, i.AssetState, i.AssetValuationCurve);
        
        double corAnnual = BaselIIEngine.CalculateEL(pd, dcfLgd);
        
        // Use EC_TOTAL from parameters as the pragmatic total Economic Capital ratio
        double ecTotal = _riskRepo.GetEcTotal();

        var cof = BuildCofParams(i.Market, (decimal)corAnnual, (decimal)ecTotal);
        var profit = DcfModel.Compute(outputs, cof);
        return new DealOutput { Deal = outputs, Profit = profit };
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

    private static PaymentMode ParseTiming(string s)
        => string.Equals(s, "advance", StringComparison.OrdinalIgnoreCase)
            ? PaymentMode.InAdvance
            : PaymentMode.InArrears;

    private static FinancialProduct ParseProduct(string s)
    {
        s = s?.Trim().ToUpperInvariant() ?? string.Empty;
        return s switch
        {
            "HP" or "HIRE PURCHASE" => FinancialProduct.HirePurchase,
            "FL" or "FINANCE LEASE" => FinancialProduct.FinanceLease,
            "MYSTAR" => FinancialProduct.MySTAR,
            "OL" or "OPERATING LEASE" => FinancialProduct.OperatingLease,
            _ => FinancialProduct.HirePurchase
        };
    }
}