using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.WinUI3.Services;

public sealed class LocalEngineService
{
    private readonly FinancialCalculator.Engine.Core.FinancialCalculator _calc = new();

    public CalculatorOutputs Calculate(
        string timing,
        string product,
        decimal vehicleSalesPrice,
        decimal additionalFinancedItems,
        bool downIsPercent,
        decimal downValue,
        int termMonths,
        decimal customerRatePercent,
        bool balloonIsPercent,
        decimal balloonValue,
        decimal periodicFeeAnnualPercent,
        decimal upfrontSubsidies,
        decimal upfrontCosts,
        bool subdownIsPercent,
        decimal subdownValue,
        string customerType = "RETAIL PRIVATE",
        string assetState = "N",
        string avc = "MBPC",
        string rating = "4.0")
    {
        var input = new CalculatorInputs
        {
            VehicleSalesPrice = vehicleSalesPrice,
            AdditionalFinancedItems = additionalFinancedItems,
            DownpaymentIsPercent = downIsPercent,
            DownpaymentValue = downValue,
            TermMonths = termMonths,
            PaymentMode = ParseTiming(timing),
            Product = ParseProduct(product),
            CustomerRatePercent = customerRatePercent,
            BalloonIsPercent = balloonIsPercent,
            BalloonPercent = balloonIsPercent ? balloonValue : 0,
            BalloonTHB = balloonIsPercent ? 0 : balloonValue,
            PeriodicFeeAnnualPercent = periodicFeeAnnualPercent,
            UpfrontSubsidies = upfrontSubsidies,
            UpfrontCosts = upfrontCosts,
            SubdownIsPercent = subdownIsPercent,
            SubdownPercent = subdownIsPercent ? subdownValue : 0,
            SubdownTHB = subdownIsPercent ? 0 : subdownValue,
            CustomerType = customerType,
            AssetState = assetState,
            AssetValuationCurve = avc,
            Rating = rating
        };
        return _calc.Calculate(input);
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
