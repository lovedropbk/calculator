using System;
using System.Globalization;
using FinancialCalculator.WinUI3.ViewModels;

namespace FinancialCalculator.WinUI3.Services;

public class ComparisonService
{
    public DealComparisonItemViewModel CreateComparisonItem(
        int currentCount,
        string vehicleName,
        string product,
        double price,
        double downPaymentValue,
        string downPaymentUnit,
        int term,
        double nominalRate,
        double flatRate,
        double balloonValue,
        string balloonUnit,
        string monthlyInstallment,
        string financedAmount,
        string roracStr,
        string totalInterest,
        double wfCustomerRate,
        string wfCustomerRateText,
        double wfCostOfDebtMatched,
        string wfCostOfDebtMatchedText,
        double wfCostOfCreditRisk,
        string wfCostOfCreditRiskText,
        double wfOpex,
        string wfOpexText,
        double wfIdcUpfrontAnnualized,
        double wfSubsidyUpfrontAnnualized
    )
    {
        var deal = new DealComparisonItemViewModel
        {
            Title = $"Scenario {currentCount + 1}",
            VehicleName = vehicleName,
            Product = product,
            Price = price.ToString("N0", CultureInfo.InvariantCulture),
            DownPayment = $"{downPaymentValue.ToString("N0", CultureInfo.InvariantCulture)} {downPaymentUnit}",
            Term = term,
            NominalRate = (nominalRate / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
            FlatRate = flatRate.ToString("0.00%"),
            Balloon = $"{balloonValue.ToString("N0", CultureInfo.InvariantCulture)} {balloonUnit}",

            MonthlyInstallment = monthlyInstallment,
            FinancedAmount = financedAmount,
            RoRAC = roracStr,
            TotalInterest = totalInterest
        };

        // Add waterfall steps
        deal.WaterfallSteps.Add(new WaterfallStepViewModel { Label = "Cust. Rate", Value = wfCustomerRate, FormattedValue = wfCustomerRateText, ColorHex = "#FF0078D7", HeightFactor = Math.Abs(wfCustomerRate) * 50 });
        deal.WaterfallSteps.Add(new WaterfallStepViewModel { Label = "CoF", Value = -wfCostOfDebtMatched, FormattedValue = $"-{wfCostOfDebtMatchedText}", ColorHex = "#FFD13438", HeightFactor = Math.Abs(wfCostOfDebtMatched) * 50 });
        deal.WaterfallSteps.Add(new WaterfallStepViewModel { Label = "Risk", Value = -wfCostOfCreditRisk, FormattedValue = $"-{wfCostOfCreditRiskText}", ColorHex = "#FFD13438", HeightFactor = Math.Abs(wfCostOfCreditRisk) * 50 });
        deal.WaterfallSteps.Add(new WaterfallStepViewModel { Label = "OPEX", Value = -wfOpex, FormattedValue = $"-{wfOpexText}", ColorHex = "#FFAA00", HeightFactor = Math.Abs(wfOpex) * 50 });

        // Net IDC/Subsidy
        double netIdc = wfIdcUpfrontAnnualized - wfSubsidyUpfrontAnnualized;

        deal.WaterfallSteps.Add(new WaterfallStepViewModel
        {
            Label = "Net IDC",
            Value = -netIdc,
            FormattedValue = (netIdc >= 0 ? "-" : "+") + Math.Abs(netIdc).ToString("0.00%", CultureInfo.InvariantCulture),
            ColorHex = netIdc >= 0 ? "#FFD13438" : "#FF107C10",
            HeightFactor = Math.Abs(netIdc) * 50
        });

        // RoRAC needs careful parsing from string like "1.45%"
        double roracVal = 0;
        var roracClean = roracStr.TrimEnd('%');
        if (double.TryParse(roracClean, NumberStyles.Any, CultureInfo.InvariantCulture, out var rval))
        {
            roracVal = rval / 100.0;
        }

        deal.WaterfallSteps.Add(new WaterfallStepViewModel { Label = "RoRAC", Value = roracVal, FormattedValue = roracStr, IsTotal = true, ColorHex = "#FF005A9E", HeightFactor = Math.Abs(roracVal) * 50 });

        return deal;
    }
}