using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FinancialCalculator.Engine;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models.Facade;
using FinancialCalculator.WinUI3.ViewModels;

namespace FinancialCalculator.WinUI3.Services;

public class CampaignCalculationService
{
    private readonly FinancialFacade _financialFacade;
    private readonly IStandardRateService _standardRateService;

    public CampaignCalculationService(FinancialFacade financialFacade, IStandardRateService standardRateService)
    {
        _financialFacade = financialFacade;
        _standardRateService = standardRateService;
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
            // 1) Gather inputs from vm
            decimal vehiclePrice = baseRequest.VehiclePrice;
            double cashDiscount = Math.Max(0, vm.CashDiscountAmount);
            double requestedSubdown = Math.Max(0, vm.FSSubDownAmount);
            double fsFreeInsurance = Math.Max(0, vm.FSSubInterestAmount);
            double fsFreeMbsp = Math.Max(0, vm.FSFreeMBSPAmount);
            double? targetRatePct = vm.TargetRatePct;

            // Apply Cash Discount to Vehicle Price
            decimal transactionPrice = vehiclePrice - (decimal)cashDiscount;
            if (transactionPrice < 0) transactionPrice = 0;

            // 2) Allocate subsidy to SubDown first (no double counting)
            // Total subsidy available for allocation after cash discount
            decimal totalBudgetAfterCash = (decimal)Math.Max(0, subsidyBudget - cashDiscount);

            var allocInput = new CampaignAllocation.Input(
                TransactionPrice: transactionPrice,
                DownIsPercent: baseRequest.DownIsPercent,
                DownValue: baseRequest.DownValue,
                TotalSubsidyBudget: totalBudgetAfterCash,
                RequestedSubdownTHB: (decimal)requestedSubdown
            );
            var alloc = CampaignAllocation.Allocate(allocInput);

            double actualSubdownUsed = (double)alloc.SubsidyUsedForSubdown;
            double customerDownpayment = (double)alloc.CustomerDownpayment;
            double subsidyRemaining = (double)alloc.SubsidyRemaining;

            // 3) Compute Rate Buydown first, then allocate IDC (Insurance/MBSP) from remaining budget
            // Always apply IDC costs in the scenario (engine treats them as costs) but only the budget-funded part reduces net via subsidies.
            decimal upfrontCostsDelta = (decimal)(fsFreeInsurance + fsFreeMbsp); // IDC (costs always applied)
            double usedForRate = 0.0;
            decimal subinterestSubsidy = 0m;
            decimal usedInsuranceBudget = 0m;
            decimal usedMbspBudget = 0m;

            if (targetRatePct.HasValue)
            {
                // Base rate from baseRequest
                double baseRate = (double)baseRequest.CustomerRatePercent;

                // Required upfront subsidy equivalent to achieve target rate (interest delta proxy)
                double required = ComputeRequiredSubsidyForRateBuydown(
                    baseRequest,
                    dealInput,
                    transactionPrice,
                    false,
                    alloc.SubsidyUsedForSubdown, // pass actual SubDown used
                    upfrontCostsDelta,
                    baseRate,
                    targetRatePct.Value);

                // Clamp to remaining budget (after SubDown and IDC allocations)
                double availableForRate = Math.Max(0, subsidyRemaining);
                // Do NOT alter the user's selected target rate.
                // Always compute RoRAC at the target rate and plug in only the available subsidy.
                usedForRate = Math.Min(required, availableForRate);
                subinterestSubsidy = (decimal)usedForRate;
                // Reduce remaining budget after rate allocation
                subsidyRemaining = Math.Max(0, subsidyRemaining - usedForRate);
            }

            // 4) Allocate remaining budget to IDC (Insurance/MBSP) up to their costs, then optionally consume remainder
            decimal decRemaining = (decimal)Math.Max(0, subsidyRemaining);

            // Offset IDC via subsidies (only up to remaining budget)
            usedInsuranceBudget = Math.Min((decimal)fsFreeInsurance, decRemaining);
            decRemaining -= usedInsuranceBudget;

            usedMbspBudget = Math.Min((decimal)fsFreeMbsp, decRemaining);
            decRemaining -= usedMbspBudget;

            // Add IDC-funded portion to UpfrontSubsidies (positive)
            subinterestSubsidy += usedInsuranceBudget + usedMbspBudget;

            // Update remaining wallet
            subsidyRemaining = (double)decRemaining;

            // Optionally consume any leftover budget to further improve RoRAC
            if (vm.ConsumeAllSubsidy && decRemaining > 0)
            {
                subinterestSubsidy += decRemaining;
                subsidyRemaining = 0; // fully consumed
            }

            // 5) Compute full scenario (UpfrontSubsidies includes rate subsidy plus any consumed remainder; SubDown = actual used)
            var (res, commPct, commAmt) = ComputeScenarioWithCommission(
                baseRequest,
                dealInput,
                transactionPrice,
                false,
                alloc.SubsidyUsedForSubdown,
                upfrontCostsDelta,
                subinterestSubsidy,
                targetRatePct
            );

            // 5) Update VM: display Customer Downpayment and actual SubDown used
            vm.Monthly = ((double)res.MonthlyInstallment).ToString("N0", CultureInfo.InvariantCulture);
            vm.CustomerFlatRate = ((double)res.FlatRatePercent / 100.0).ToString("0.00%", CultureInfo.InvariantCulture);
            vm.TransactionPrice = transactionPrice.ToString("N0", CultureInfo.InvariantCulture);

            // Replace displayed downpayment with Customer Downpayment (base down - subdown used, floored at 0)
            vm.Downpayment = customerDownpayment.ToString("N0", CultureInfo.InvariantCulture);

            // Show actual used SubDown (may be lower than requested due to allocation clamps)
            // Set numeric amounts to actual utilized values for downstream visuals/exports
            vm.FSSubDownAmount = actualSubdownUsed;
            vm.FSSubDown = actualSubdownUsed.ToString("N0", CultureInfo.InvariantCulture);
            vm.CashDiscount = cashDiscount.ToString("N0", CultureInfo.InvariantCulture);
            vm.FSSubInterest = fsFreeInsurance.ToString("N0", CultureInfo.InvariantCulture);
            vm.FSFreeMBSP = fsFreeMbsp.ToString("N0", CultureInfo.InvariantCulture);
            vm.SubinterestSubsidyAmount = usedForRate;
            vm.SubinterestSubsidy = usedForRate.ToString("N0", CultureInfo.InvariantCulture);

            // Totals for display (clamped to budget allocations actually used)
            double subsidyUsed = cashDiscount + actualSubdownUsed + (double)usedInsuranceBudget + (double)usedMbspBudget + usedForRate;
            vm.SubsidyUsed = subsidyUsed.ToString("N0", CultureInfo.InvariantCulture);

            double idcsTotal = commAmt + fsFreeInsurance + fsFreeMbsp + dealInput.IdcOther;
            vm.IDCsTotal = idcsTotal.ToString("N0", CultureInfo.InvariantCulture);

            vm.DealerCommission = $"{commPct.ToString("0.00%", CultureInfo.InvariantCulture)} ({commAmt.ToString("N0", CultureInfo.InvariantCulture)} THB)";
            vm.RoRAC = ((double)res.AcquisitionRoRacPercent).ToString("0.00%");

            // --- Populate per-term breakdown and compute aggregated avg RoRAC ---
            try
            {
                var termSvc = new CampaignTermBreakdownService(_financialFacade, _standardRateService);
                var breakdown = await termSvc.CalculateTermBreakdownAsync(vm, baseRequest, dealInput);

                // Populate VM collection (CampaignSummaryViewModel.TermBreakdown)
                vm.TermBreakdown.Clear();
                foreach (var tb in breakdown)
                {
                    vm.TermBreakdown.Add(tb);
                }

                // Compute aggregated average RoRAC across terms using distribution weights
                double agg = 0.0;
                foreach (var tb in vm.TermBreakdown)
                {
                    var s = (tb.RoRAC ?? string.Empty).Trim();
                    if (s.EndsWith("%")) s = s[..^1];
                    if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var pctVal))
                    {
                        double r = pctVal / 100.0;
                        agg += r * (tb.DistributionPct / 100.0);
                    }
                }
                vm.AvgRoRAC = agg.ToString("0.00%", CultureInfo.InvariantCulture);
            }
            catch
            {
                // Non-fatal: fallback to single-term RoRAC if breakdown fails
                vm.AvgRoRAC = vm.RoRAC;
            }

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