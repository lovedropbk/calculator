using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FinancialCalculator.Engine.Models.Facade;
using FinancialCalculator.WinUI3.ViewModels;

namespace FinancialCalculator.WinUI3.Services;

public class ExportService
{
    public async Task<string> ExportScenarioAsync(CampaignSummaryViewModel campaign, ScenarioResult result, double customerNominalRate, double dealerCommissionResolvedAmt, double idcOther)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Deal Summary");
        sb.AppendLine("Key,Value");
        sb.AppendLine($"Selected Campaign,{campaign.Title}");
        sb.AppendLine($"Monthly Installment (THB),{result.MonthlyInstallment.ToString("N0", CultureInfo.InvariantCulture)}");
        
        var nominalRate = campaign.TargetRatePct ?? customerNominalRate;
        sb.AppendLine($"Nominal Rate,{(nominalRate / 100.0).ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Flat Rate,{((double)result.FlatRatePercent / 100.0).ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Financed Amount (THB),{result.FinancedAmount.ToString("N0", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Acq. RoRAC,{((double)result.AcquisitionRoRacPercent).ToString("0.00%", CultureInfo.InvariantCulture)}");
        
        sb.AppendLine($"Dealer Commission (THB),{dealerCommissionResolvedAmt.ToString("N0", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"IDC - Other (THB),{idcOther.ToString("N0", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"IDC Total (THB),{(dealerCommissionResolvedAmt + idcOther).ToString("N0", CultureInfo.InvariantCulture)}");
        sb.AppendLine();

        // Profitability Details
        sb.AppendLine("Profitability Details");
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Deal IRR,{result.Profitability.DealIrrEffectivePercent.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Deal IRR Nominal,{result.Profitability.DealIrrNominalPercent.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Cost of Debt Matched,{result.Profitability.CostOfDebtMatchedPercent.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Matched Funded Spread,{result.Profitability.MatchedFundingSpreadPercent.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Gross Interest Margin,{result.Profitability.GrossInterestMarginPercent.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Capital Advantage,{result.Profitability.CapitalAdvantagePercent.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Net Interest Margin,{result.Profitability.NetInterestMarginPercent.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Cost of Credit Risk,{result.Profitability.CostOfCreditRiskPercent.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"OPEX,{result.Profitability.OpexPercent.ToString("0.00%", CultureInfo.InvariantCulture)}");
        
        double netIdcUpfront = (double)(result.Profitability.IdcUpfrontAnnualizedPercent - result.Profitability.SubsidyUpfrontAnnualizedPercent);
        double netIdcPeriodic = (double)(result.Profitability.IdcPeriodicPercent - result.Profitability.SubsidyPeriodicPercent);
        
        sb.AppendLine($"Net IDC+Subsidies Upfront,{netIdcUpfront.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Net IDC+Subsidies Periodic,{netIdcPeriodic.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Net EBIT Margin,{result.Profitability.NetEbitMarginPercent.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Economic Capital,{result.Profitability.EconomicCapitalPercent.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine();

        // Cashflow Schedule
        sb.AppendLine("Cashflow Schedule");
        sb.AppendLine("Period,Principal,Interest,Balance,Cashflow");
        foreach (var r in result.Schedule)
        {
            sb.AppendLine($"{r.Period},{r.Principal.ToString("0.00", CultureInfo.InvariantCulture)},{r.Interest.ToString("0.00", CultureInfo.InvariantCulture)},{r.Balance.ToString("0.00", CultureInfo.InvariantCulture)},{r.Cashflow.ToString("0.00", CultureInfo.InvariantCulture)}");
        }

        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FinancialCalculatorExports");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"deal_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"); // Changed to .csv as it is CSV content, though originally .xlsx
        // Actually original code used .xlsx but wrote CSV content. I'll keep .xlsx if desired but .csv is more honest.
        // Keeping .xlsx to match original behavior if it matters for user expectation, but correcting it is better.
        // Let's stick to .xlsx for now to minimize change in behavior, but it's really CSV.
        file = Path.ChangeExtension(file, ".xlsx"); 
        
        await File.WriteAllTextAsync(file, sb.ToString(), Encoding.UTF8);
        return file;
    }
}