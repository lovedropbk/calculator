using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.Engine.Core;

public sealed class DealEngine
{
    private readonly FinancialCalculator _calc = new();
    private readonly IRiskParameterRepository _riskRepo;
    private readonly ICostOfFundsService _cof;

    public DealEngine(IRiskParameterRepository riskRepo)
        : this(riskRepo, new CostOfFundsService())
    {
    }

    public DealEngine(IRiskParameterRepository riskRepo, ICostOfFundsService cof)
    {
        _riskRepo = riskRepo ?? throw new ArgumentNullException(nameof(riskRepo));
        _cof = cof ?? throw new ArgumentNullException(nameof(cof));
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

        var cof = BuildCofParams(i.Market, (decimal)corAnnual, (decimal)ecTotal, i.Product);
        var profit = DcfModel.Compute(outputs, cof);
        return new DealOutput { Deal = outputs, Profit = profit };
    }

    private CofParams BuildCofParams(string market, decimal cor, decimal ecRatio, string product)
    {
        // Centralized via CostOfFundsService
        var curve = new Dictionary<int, decimal>(_cof.GetCurve());
        var spread = _cof.GetMatchedFundingSpread();
        // Engine treats OPEX as cost (negative), config stores positive percentage
        var opex = -_cof.GetOpexPctForProduct(product);

        return new CofParams
        {
            Curve = curve,
            Spread = spread,
            OpexPct = opex,
            EconCapRatio = ecRatio,
            CostOfRisk = cor
        };
    }

    private static string NormalizeProductKey(string product)
    {
        product = (product ?? string.Empty).Trim();
        if (product.StartsWith("HP", StringComparison.OrdinalIgnoreCase)) return "HP";
        if (product.Contains("MYSTAR", StringComparison.OrdinalIgnoreCase)) return "mySTAR";
        if (product.Contains("F-LEAS", StringComparison.OrdinalIgnoreCase) || product.Contains("FINANCE", StringComparison.OrdinalIgnoreCase)) return "FinanceLease";
        if (product.Contains("OP-LEAS", StringComparison.OrdinalIgnoreCase) || product.Contains("OPERAT", StringComparison.OrdinalIgnoreCase)) return "OperatingLease";
        return product;
    }

    private static Dictionary<int, decimal> ParseCostOfFundsCurve(string[] lines)
    {
        var result = new Dictionary<int, decimal>();
        bool inCurve = false;
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            if (line.StartsWith("costOfFundsCurve:", StringComparison.OrdinalIgnoreCase))
            {
                inCurve = true;
                continue;
            }

            if (inCurve)
            {
                if (line.StartsWith("-", StringComparison.Ordinal))
                {
                    int term = 0; decimal rate = 0m;

                    // Look ahead a few lines for termMonths and rate
                    for (int j = i + 1; j < Math.Min(lines.Length, i + 6); j++)
                    {
                        var l = lines[j].Trim();
                        if (string.IsNullOrWhiteSpace(l) || l.StartsWith("-", StringComparison.Ordinal)) break;

                        if (l.StartsWith("termMonths", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = l.Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var t)) term = t;
                        }
                        else if (l.StartsWith("rate", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = l.Split(':');
                            if (parts.Length == 2 && decimal.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r)) rate = r;
                        }
                    }

                    if (term > 0) result[term] = rate;
                }
                else if (line.Length > 0 && char.IsLetterOrDigit(line[0]) && !line.StartsWith("rate", StringComparison.OrdinalIgnoreCase))
                {
                    // Reached next top-level key
                    break;
                }
            }
        }
        return result;
    }

    private static decimal? ParseSingleDecimal(string[] lines, string key)
    {
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(':');
                if (parts.Length == 2 && decimal.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    return v;
            }
        }
        return null;
    }

    private static Dictionary<string, decimal> ParseOpexByProduct(string[] lines)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        bool inOpex = false, inMap = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            if (line.StartsWith("opex:", StringComparison.OrdinalIgnoreCase))
            {
                inOpex = true; inMap = false; continue;
            }
            if (inOpex && line.StartsWith("byProductPct:", StringComparison.OrdinalIgnoreCase))
            {
                inMap = true; continue;
            }

            if (inOpex && inMap)
            {
                if (!line.Contains(":")) break;
                var parts = line.Split(':');
                if (parts.Length == 2 && decimal.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                {
                    var k = parts[0].Trim();
                    result[k] = v;
                }
            }
        }

        return result;
    }

    private static string? FindConfigPath(string filename)
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var check = Path.Combine(baseDir, filename);
            if (File.Exists(check)) return check;

            var current = new DirectoryInfo(baseDir);
            int depth = 8;
            while (current != null && depth-- > 0)
            {
                check = Path.Combine(current.FullName, filename);
                if (File.Exists(check)) return check;

                check = Path.Combine(current.FullName, "config.yaml");
                if (File.Exists(check)) return check;

                current = current.Parent;
            }
        }
        catch { }
        return null;
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