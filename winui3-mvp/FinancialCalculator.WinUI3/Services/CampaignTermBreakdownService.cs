
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FinancialCalculator.Engine;
using FinancialCalculator.Engine.Models.Facade;
using FinancialCalculator.WinUI3.ViewModels;

namespace FinancialCalculator.WinUI3.Services;

public class CampaignTermBreakdownService
{
    private readonly FinancialFacade _financialFacade;
    private readonly IStandardRateService _standardRateService;

    public CampaignTermBreakdownService(FinancialFacade financialFacade, IStandardRateService standardRateService)
    {
        _financialFacade = financialFacade ?? throw new ArgumentNullException(nameof(financialFacade));
        _standardRateService = standardRateService ?? throw new ArgumentNullException(nameof(standardRateService));
    }

    /// <summary>
    /// Calculate a term-by-term breakdown for the provided campaign summary and scenario request.
    /// Returns a list of TermBreakdownItemViewModel (one per evaluation term).
    /// The method does not mutate the provided CampaignSummaryViewModel.
    /// </summary>
    public async Task<List<TermBreakdownItemViewModel>> CalculateTermBreakdownAsync(
        CampaignSummaryViewModel vm,
        ScenarioRequest baseRequest,
        DealInputViewModel dealInput)
    {
        if (vm == null) throw new ArgumentNullException(nameof(vm));
        if (baseRequest == null) throw new ArgumentNullException(nameof(baseRequest));
        if (dealInput == null) throw new ArgumentNullException(nameof(dealInput));

        // Fixed evaluation terms for the first pass
        var terms = new[] { 12, 24, 36, 48, 60 };
        var list = new List<TermBreakdownItemViewModel>();

        decimal vehiclePrice = baseRequest.VehiclePrice;
        double baseCustomerRate = (double)baseRequest.CustomerRatePercent;

        foreach (var term in terms)
        {
            // Resolve customer rate for this term:
            // 1) If campaign has explicit TargetRatePct use that
            // 2) Else ask the standard-rate service for a suggested rate for (product, term, downPaymentPct, paymentMode)
            // 3) Fallback to baseRequest.CustomerRatePercent
            double customerRate = baseCustomerRate;
            if (vm.TargetRatePct.HasValue)
            {
                customerRate = vm.TargetRatePct.Value;
            }
            else
            {
                var std = _standardRateService.GetStandardRate((string)(baseRequest?.Product ?? string.Empty), term, GetDownPaymentPct(baseRequest), GetPaymentMode(baseRequest));
                if (std.HasValue) customerRate = std.Value;
            }

            // Build a request for this term and run the engine (replicates ComputeScenarioWithCommission logic)
            var req1 = baseRequest with
            {
                TermMonths = term,
                VehiclePrice = vehiclePrice,
                CustomerRatePercent = (decimal)customerRate
            };

            var res1 = _financialFacade.Calculate(req1);

            // Resolve commission for financed amount (same as existing flow)
            var (pct, amt) = dealInput.ResolveCommissionForFinanced((double)res1.FinancedAmount);

            // Re-run with commission added to upfront costs
            var req2 = req1 with { UpfrontCosts = req1.UpfrontCosts + (decimal)amt };
            var res2 = _financialFacade.Calculate(req2);

            // Format RoRAC the same way other views do (fraction -> percent string)
            var roracFormatted = ((double)res2.AcquisitionRoRacPercent).ToString("0.00%", CultureInfo.InvariantCulture);

            // Default distribution by product (UI will allow user to edit later)
            double dist = GetDefaultDistribution((string)(baseRequest?.Product ?? string.Empty), term);

            list.Add(new TermBreakdownItemViewModel
            {
                Term = term,
                CustomerRatePct = customerRate,
                RoRAC = roracFormatted,
                DistributionPct = dist
            });
        }

        // Ensure the distribution sums to 100%:
        var sum = list.Sum(x => x.DistributionPct);
        if (Math.Abs(sum - 100.0) > 0.0001)
        {
            if (sum == 0.0)
            {
                // If no defaults applied, allocate all to the base request term
                var baseItem = list.FirstOrDefault(t => t.Term == (int)baseRequest.TermMonths);
                if (baseItem != null) baseItem.DistributionPct = 100.0;
            }
            else
            {
                var scale = 100.0 / sum;
                foreach (var it in list) it.DistributionPct *= scale;
            }
        }

        return await Task.FromResult(list);
    }

    private double GetDefaultDistribution(string product, int term)
    {
        var p = (product ?? string.Empty).Trim().ToUpperInvariant();
        if (p.StartsWith("HP") || p.Contains("HP"))
        {
            if (term == 36) return 10.0;
            if (term == 48) return 40.0;
            if (term == 60) return 50.0;
            return 0.0;
        }

        if (p.Contains("MYSTAR"))
        {
            if (term == 48) return 10.0;
            if (term == 60) return 90.0;
            return 0.0;
        }

        // Default: no pre-filled distribution
        return 0.0;
    }

    private double GetDownPaymentPct(ScenarioRequest req)
    {
        try
        {
            if (req.DownIsPercent) return (double)req.DownValue;
            if (req.VehiclePrice > 0) return (double)(req.DownValue / req.VehiclePrice * 100m);
        }
        catch { /* best-effort, fall through */ }

        return 0.0;
    }

    private string GetPaymentMode(ScenarioRequest req)
    {
        try
        {
            // The ScenarioRequest type may expose either a 'Timing' (string) or 'PaymentMode' property.
            // Use reflection for compatibility with different facade/versions, falling back to "arrears".
            var t = req.GetType();

            var timingProp = t.GetProperty("Timing");
            if (timingProp != null)
            {
                var timingVal = timingProp.GetValue(req) as string;
                if (!string.IsNullOrWhiteSpace(timingVal))
                    return timingVal;
            }

            var paymentModeProp = t.GetProperty("PaymentMode");
            if (paymentModeProp != null)
            {
                var pm = paymentModeProp.GetValue(req);
                if (pm != null)
                    return pm.ToString() ?? "arrears";
            }

            // Default legacy value
            return "arrears";
        }
        catch
        {
            return "arrears";
        }
    }
// End of CampaignTermBreakdownService
}