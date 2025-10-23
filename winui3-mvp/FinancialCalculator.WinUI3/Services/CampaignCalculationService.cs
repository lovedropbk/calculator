using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FinancialCalculator.Engine;
using FinancialCalculator.Engine.Models.Facade;
using FinancialCalculator.WinUI3.ViewModels;

namespace FinancialCalculator.WinUI3.Services;

public class CampaignCalculationService
{
    private readonly FinancialFacade _financialFacade;

    public CampaignCalculationService(FinancialFacade financialFacade)
    {
        _financialFacade = financialFacade;
    }

    public async Task<(ScenarioResult? Result, double CommPct, double CommAmt)> CalculateCampaignAsync(
        CampaignSummaryViewModel vm,
        ScenarioRequest baseRequest,
        double subsidyBudget,
        DealInputViewModel dealInput, // Needed for Commission Policy access if not refactored yet
        bool autoClampToBudget = false)
    {
        if (!autoClampToBudget) await Task.Delay(1);

        try
        {
            // 1. Gather inputs from vm
            decimal vehiclePrice = baseRequest.VehiclePrice;
            double cashDiscount = vm.CashDiscountAmount;
            double fsSubDown = vm.FSSubDownAmount;
            double fsFreeInsurance = vm.FSSubInterestAmount;
            double fsFreeMbsp = vm.FSFreeMBSPAmount;
            double? targetRatePct = vm.TargetRatePct;

            // Apply Cash Discount to Vehicle Price
            decimal transactionPrice = vehiclePrice - (decimal)cashDiscount;
            if (transactionPrice < 0) transactionPrice = 0;

            // 2. Calculate Subinterest Subsidy if Target Rate is set
            decimal subinterestSubsidy = 0m;
            if (targetRatePct.HasValue)
            {
                decimal upfrontCostsDelta = (decimal)(fsFreeInsurance + fsFreeMbsp);
                // We need base customer rate from baseRequest, not DealInput directly if we want to be pure.
                // But baseRequest has it.
                double baseRate = (double)baseRequest.CustomerRatePercent;
                
                double required = ComputeRequiredSubsidyForRateBuydown(baseRequest, dealInput, transactionPrice, false, (decimal)fsSubDown, upfrontCostsDelta, baseRate, targetRatePct.Value);
                
                // Auto-clamp for standard campaigns if over budget
                double leftoverBudget = subsidyBudget - (cashDiscount + fsSubDown + fsFreeInsurance + fsFreeMbsp);
                if (autoClampToBudget && required > leftoverBudget && leftoverBudget >= 0)
                {
                     targetRatePct = CalculateLowestAchievableRate(baseRequest, dealInput, transactionPrice, false, (decimal)fsSubDown, upfrontCostsDelta, baseRate, leftoverBudget);
                     required = leftoverBudget;
                     vm.TargetRatePct = targetRatePct;
                }
                subinterestSubsidy = (decimal)required;
            }

            // 4. Compute full scenario
            var (res, commPct, commAmt) = ComputeScenarioWithCommission(
                baseRequest,
                dealInput,
                transactionPrice,
                false,
                (decimal)fsSubDown,
                (decimal)(fsFreeInsurance + fsFreeMbsp),
                (decimal)(subsidyBudget - cashDiscount),
                targetRatePct
            );

            // Update VM (or return data to update VM)
            vm.Monthly = ((double)res.MonthlyInstallment).ToString("N0", CultureInfo.InvariantCulture);
            vm.CustomerFlatRate = ((double)res.FlatRatePercent / 100.0).ToString("0.00%", CultureInfo.InvariantCulture);
            vm.TransactionPrice = transactionPrice.ToString("N0", CultureInfo.InvariantCulture);
            // vm.Downpayment = ... (needs ComputeDownpaymentDisplay, might need to be passed in or calculated here if simple)
            // Let's assume simple calculation for now or pass it.
            // MainViewModel used: return (double)(vehiclePrice * (decimal)DealInput.DownPaymentValueEntry / 100m); if %
            decimal downPayment = baseRequest.DownIsPercent ? transactionPrice * baseRequest.DownValue / 100m : baseRequest.DownValue;
            vm.Downpayment = downPayment.ToString("N0", CultureInfo.InvariantCulture);

            vm.CashDiscount = cashDiscount.ToString("N0", CultureInfo.InvariantCulture);
            vm.FSSubDown = fsSubDown.ToString("N0", CultureInfo.InvariantCulture);
            vm.FSSubInterest = fsFreeInsurance.ToString("N0", CultureInfo.InvariantCulture);
            vm.FSFreeMBSP = fsFreeMbsp.ToString("N0", CultureInfo.InvariantCulture);
            vm.SubinterestSubsidy = subinterestSubsidy.ToString("N0", CultureInfo.InvariantCulture);
            
            double subsidyUsed = cashDiscount + fsSubDown + fsFreeInsurance + fsFreeMbsp + (double)subinterestSubsidy;
            vm.SubsidyUsed = subsidyUsed.ToString("N0", CultureInfo.InvariantCulture);
            
            double idcsTotal = commAmt + fsFreeInsurance + fsFreeMbsp + (double)baseRequest.UpfrontCosts; // Wait, baseRequest.UpfrontCosts might already include some things?
            // MainVM used DealInput.IdcOther.
            // Let's use what's passed in baseRequest if it only contains IdcOther initially.
            // Actually baseRequest.UpfrontCosts in MainVM was (Decimal)(DealInput.DealerCommissionResolvedAmt + DealInput.IdcOther)
            // If we use baseRequest from DealInput, it might have commission already.
            // We should probably pass raw IdcOther if we want to be precise.
            // Or rely on `ComputeScenarioWithCommission` to handle it.
            // `ComputeScenarioWithCommission` in MainVM used `Math.Max(0, DealInput.IdcOther)`.
            // Let's assume we can get it from `dealInput.IdcOther` for now as we pass `dealInput`.
            idcsTotal = commAmt + fsFreeInsurance + fsFreeMbsp + dealInput.IdcOther;

            vm.IDCsTotal = idcsTotal.ToString("N0", CultureInfo.InvariantCulture);
            
            vm.DealerCommission = $"{commPct.ToString("0.00%", CultureInfo.InvariantCulture)} ({commAmt.ToString("N0", CultureInfo.InvariantCulture)} THB)";
            vm.RoRAC = ((double)res.AcquisitionRoRacPercent).ToString("0.00%");

            return (res, commPct, commAmt);
        }
        catch
        {
            return (null, 0, 0);
        }
    }

    private (ScenarioResult result, double commissionPct, double commissionAmt)
        ComputeScenarioWithCommission(ScenarioRequest baseReq, DealInputViewModel dealInput, decimal vehiclePrice, bool subdownIsPercent, decimal subdownValue, decimal upfrontCostsDelta, decimal upfrontSubsidiesDelta, double? customerRateOverride)
    {
        var req1 = baseReq with
        {
            VehiclePrice = vehiclePrice,
            UpfrontCosts = (decimal)Math.Max(0, dealInput.IdcOther) + upfrontCostsDelta,
            UpfrontSubsidies = upfrontSubsidiesDelta,
            SubdownIsPercent = subdownIsPercent,
            SubdownValue = subdownValue,
            CustomerRatePercent = (decimal)(customerRateOverride ?? (double)baseReq.CustomerRatePercent)
        };

        var res1 = _financialFacade.Calculate(req1);
        var (pct, amt) = dealInput.ResolveCommissionForFinanced((double)res1.FinancedAmount); 

        var req2 = req1 with { UpfrontCosts = req1.UpfrontCosts + (decimal)amt };
        var res2 = _financialFacade.Calculate(req2);

        return (res2, pct, amt);
    }

    private double ComputeRequiredSubsidyForRateBuydown(ScenarioRequest baseReq, DealInputViewModel dealInput, decimal vehiclePrice, bool subdownIsPercent, decimal subdownValue, decimal upfrontCostsDelta, double baseRatePct, double targetRatePct)
    {
        var baseRes = ComputeScenarioWithCommission(baseReq, dealInput, vehiclePrice, subdownIsPercent, subdownValue, upfrontCostsDelta, 0m, baseRatePct);
        var targetRes = ComputeScenarioWithCommission(baseReq, dealInput, vehiclePrice, subdownIsPercent, subdownValue, upfrontCostsDelta, 0m, targetRatePct);
        
        var baseInt = (double)baseRes.result.TotalInterest;
        var tgtInt = (double)targetRes.result.TotalInterest;
        return Math.Max(0, baseInt - tgtInt);
    }

    private double CalculateLowestAchievableRate(ScenarioRequest baseReq, DealInputViewModel dealInput, decimal vehiclePrice, bool subdownIsPercent, decimal subdownValue, decimal upfrontCostsDelta, double baseRatePct, double availableBudget)
    {
        double low = 0;
        double high = baseRatePct;
        double bestRate = baseRatePct;

        for (int i = 0; i < 20; i++)
        {
            double mid = (low + high) / 2;
            double required = ComputeRequiredSubsidyForRateBuydown(baseReq, dealInput, vehiclePrice, subdownIsPercent, subdownValue, upfrontCostsDelta, baseRatePct, mid);
            
            if (required > availableBudget)
            {
                low = mid;
            }
            else
            {
                bestRate = mid;
                high = mid;
            }
        }
        return Math.Round(bestRate, 2);
    }
}