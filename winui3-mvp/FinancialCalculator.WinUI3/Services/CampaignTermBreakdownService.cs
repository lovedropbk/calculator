
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using FinancialCalculator.Engine;
using FinancialCalculator.Engine.Models.Facade;
using FinancialCalculator.WinUI3.ViewModels;

namespace FinancialCalculator.WinUI3.Services;

public class CampaignTermBreakdownService
{
    private readonly FinancialFacade _financialFacade;
    private readonly IStandardRateService _standardRateService;
    private readonly IDistributionConfigProvider _distProvider = YamlDistributionConfigProvider.Instance;
    private static readonly object s_distLogGate = new();
    private static readonly HashSet<string> s_distLogKeys = new(StringComparer.OrdinalIgnoreCase);

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

        // Terms derived from CSV via StandardRateService (exactly what's available for product/mode and current downpayment)
        var br = baseRequest; // already validated non-null above
        var terms = _standardRateService.GetAvailableTerms(
            br.Product ?? string.Empty,
            GetDownPaymentPct(br),
            GetPaymentMode(br));
        var list = new List<TermBreakdownItemViewModel>();

        decimal vehiclePrice = br.VehiclePrice;

        foreach (var term in terms)
        {
            // Resolve customer rate for this term from the standard-rate table (no silent fallback)
            var std = _standardRateService.GetStandardRate(
                br.Product ?? string.Empty,
                term,
                GetDownPaymentPct(br),
                GetPaymentMode(br));
            if (!std.HasValue) { continue; }
            double customerRate = std.Value;

            // Build a request for this term and run the engine (replicates ComputeScenarioWithCommission logic)
            // Apply campaign adjustments:
            // - Cash discount reduces transaction price
            // - FS free insurance/MBSP are IDC (upfront costs)
            // - Subdown is passed as SubdownValue (THB)
            decimal txnPrice = vehiclePrice - (decimal)Math.Max(0, vm.CashDiscountAmount);
            if (txnPrice < 0) txnPrice = 0;
            double upfrontIdcDelta = Math.Max(0, vm.FSSubInterestAmount) + Math.Max(0, vm.FSFreeMBSPAmount);
            decimal subdownValue = (decimal)Math.Max(0, vm.FSSubDownAmount);

            var req1 = baseRequest with
            {
                TermMonths = term,
                VehiclePrice = txnPrice,
                CustomerRatePercent = (decimal)customerRate,
                UpfrontCosts = baseRequest.UpfrontCosts + (decimal)upfrontIdcDelta,
                SubdownIsPercent = false,
                SubdownValue = subdownValue
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
            double dist = GetDefaultDistribution(br.Product ?? string.Empty, term);

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
                var baseItem = list.FirstOrDefault(t => t.Term == (int)baseRequest!.TermMonths);
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

    /// <summary>
    /// Recalculate per-term Acquisition RoRAC string for a given term and customer rate,
    /// using the same flow as CalculateTermBreakdownAsync (including commission on financed amount).
    /// This mirrors the "deal input widget" behavior with the overridden term and rate.
    /// </summary>
    public async Task<string> CalculateTermRoRACAsync(
        ScenarioRequest baseRequest,
        DealInputViewModel dealInput,
        CampaignSummaryViewModel? campaign,
        int term,
        double customerRatePct)
    {
        if (baseRequest == null) throw new ArgumentNullException(nameof(baseRequest));
        if (dealInput == null) throw new ArgumentNullException(nameof(dealInput));

        // Apply campaign adjustments (same logic as main breakdown):
        // - Cash discount reduces transaction price
        // - Free insurance/MBSP are IDC (upfront costs)
        // - Subdown is passed as absolute THB
        decimal txnPrice = baseRequest.VehiclePrice;
        decimal upfrontCostsDelta = 0m;
        decimal subdownValue = 0m;

        if (campaign != null)
        {
            txnPrice = baseRequest.VehiclePrice - (decimal)Math.Max(0, campaign.CashDiscountAmount);
            if (txnPrice < 0) txnPrice = 0;
            upfrontCostsDelta = (decimal)(Math.Max(0, campaign.FSSubInterestAmount) + Math.Max(0, campaign.FSFreeMBSPAmount));
            subdownValue = (decimal)Math.Max(0, campaign.FSSubDownAmount);
        }

        // 1) Build request for this term with provided rate and deltas
        var req1 = baseRequest with
        {
            TermMonths = term,
            VehiclePrice = txnPrice,
            CustomerRatePercent = (decimal)customerRatePct,
            UpfrontCosts = baseRequest.UpfrontCosts + upfrontCostsDelta,
            SubdownIsPercent = false,
            SubdownValue = subdownValue
        };

        // 2) First pass to determine commission on financed amount
        var res1 = _financialFacade.Calculate(req1);
        var (_, amt) = dealInput.ResolveCommissionForFinanced((double)res1.FinancedAmount);

        // 3) Second pass with commission added to upfront costs
        var req2 = req1 with { UpfrontCosts = req1.UpfrontCosts + (decimal)amt };
        var res2 = _financialFacade.Calculate(req2);

        // 4) Format RoRAC consistently with the rest of the UI
        var roracFormatted = ((double)res2.AcquisitionRoRacPercent).ToString("0.00%", CultureInfo.InvariantCulture);
        return await Task.FromResult(roracFormatted);
    }

    // Back-compat wrapper
    public async Task<string> CalculateTermRoRACAsync(
        ScenarioRequest baseRequest,
        DealInputViewModel dealInput,
        int term,
        double customerRatePct)
        => await CalculateTermRoRACAsync(baseRequest, dealInput, null, term, customerRatePct);

    private static void LogConfiguredDefaultOnce(string product, string normalizedProduct, int term, double configured)
    {
        var key = $"{normalizedProduct}:{term}";
        lock (s_distLogGate)
        {
            if (!s_distLogKeys.Add(key))
                return;
        }
        Logger.Debug($"[GetDefaultDistribution] Found configured default for Product: '{product}' (Normalized: '{normalizedProduct}'), Term: {term} -> {configured}%");
    }

    private double GetDefaultDistribution(string product, int term)
    {
        // Normalize product key to ensure consistent lookups (e.g., "F-Lease", "FinanceLease" -> "FinanceLease")
        var normalizedProduct = NormalizeProductKey(product);

        // 1) Configurable defaults from config.yaml (designer.defaultDistribution)
        if (TryGetConfiguredDistribution(normalizedProduct, term, out var configured))
        {
            LogConfiguredDefaultOnce(product, normalizedProduct, term, configured);
            return configured;
        }
        Logger.Warn($"[GetDefaultDistribution] No configured default found for Product: '{product}' (Normalized: '{normalizedProduct}'), Term: {term}. Falling back to legacy defaults.");

        // 2) Fallback static defaults (legacy)
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

    // MARK: Config-driven default distribution (lightweight YAML scan; optional)
    private Dictionary<string, Dictionary<int, double>>? _distConfig;

    private bool TryGetConfiguredDistribution(string product, int term, out double value)
    {
        value = 0.0;
        try
        {
            // Delegate to provider with caching and timestamp checks
            if (_distProvider.TryGetConfiguredDistribution(product ?? string.Empty, term, out var v))
            {
                value = v;
                return true;
            }

            // Backward-compat: also attempt normalized product key
            var alt = NormalizeProductKey(product ?? string.Empty);
            if (!string.Equals(alt, product ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                _distProvider.TryGetConfiguredDistribution(alt, term, out var v2))
            {
                value = v2;
                return true;
            }
        }
        catch
        {
            // best-effort
        }
        return false;
    }

    private string? GetConfigPath(string filename)
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

                // repo-root fallback
                check = Path.Combine(current.FullName, "config.yaml");
                if (File.Exists(check)) return check;

                current = current.Parent;
            }
        }
        catch { }
        return null;
    }

    private static Dictionary<string, Dictionary<int, double>> ParseDistributionFromYaml(string[] lines)
    {
        // Expected structure:
        // designer:
        //   defaultDistribution:
        //     HP:
        //       12: 0
        //       24: 0
        //       36: 10
        //       48: 40
        //       60: 50
        var result = new Dictionary<string, Dictionary<int, double>>(StringComparer.OrdinalIgnoreCase);

        int i = 0;
        bool inDesigner = false, inDefaults = false;
        string currentProduct = "";
        while (i < lines.Length)
        {
            var raw = lines[i++];
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            if (line.StartsWith("designer:", StringComparison.OrdinalIgnoreCase))
            {
                inDesigner = true; inDefaults = false; currentProduct = "";
                continue;
            }
            if (inDesigner && line.StartsWith("defaultDistribution:", StringComparison.OrdinalIgnoreCase))
            {
                inDefaults = true; currentProduct = "";
                continue;
            }

            if (!inDesigner || !inDefaults) continue;

            // Product section, e.g., "HP:"
            if (line.EndsWith(":") && !line.StartsWith("-"))
            {
                currentProduct = line.TrimEnd(':').Trim();
                if (!result.ContainsKey(currentProduct))
                    result[currentProduct] = new Dictionary<int, double>();
                continue;
            }

            // Term mapping, e.g., "36: 10.0"
            var parts = line.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out var term))
            {
                if (double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                {
                    if (!string.IsNullOrEmpty(currentProduct))
                    {
                        result[currentProduct][term] = val;
                    }
                }
            }
        }

        return result;
    }

    private static string NormalizeProductKey(string product)
    {
        product = (product ?? string.Empty).Trim();
        if (product.StartsWith("HP", StringComparison.OrdinalIgnoreCase)) return "HP";
        if (product.Contains("mySTAR", StringComparison.OrdinalIgnoreCase)) return "mySTAR";
        if (product.Contains("F-Lease", StringComparison.OrdinalIgnoreCase) || product.Contains("Finance", StringComparison.OrdinalIgnoreCase)) return "FinanceLease";
        if (product.Contains("Op-Lease", StringComparison.OrdinalIgnoreCase) || product.Contains("Operating", StringComparison.OrdinalIgnoreCase)) return "OperatingLease";
        return product;
    }

    private double GetDownPaymentPct(ScenarioRequest req)
    {
        try
        {
            // StandardRateService expects fraction (0.00 - 1.00)
            if (req.DownIsPercent) return (double)req.DownValue / 100.0;
            if (req.VehiclePrice > 0) return (double)(req.DownValue / req.VehiclePrice);
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