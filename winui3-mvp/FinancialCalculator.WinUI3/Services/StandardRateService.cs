using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace FinancialCalculator.WinUI3.Services;

public class StandardRateService : IStandardRateService
{
    private readonly object _sync = new();
    private bool _isLoaded = false;

    // product -> term -> TermIndex
    private readonly Dictionary<string, Dictionary<int, TermIndex>> _index =
        new(StringComparer.OrdinalIgnoreCase);

    private sealed class TermIndex
    {
        // PaymentMode ("Advance" | "Arrears" | "Any") -> sorted list of ranges
        public Dictionary<string, List<RangeItem>> ByMode { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class RangeItem
    {
        public double Min;    // inclusive
        public double Max;    // inclusive
        public double Rate;
    }

    // MARK: Loaders

    public async Task LoadAsync() => await LoadAsync(null);

    // Optional override for tests or future alt sources
    public async Task LoadAsync(string? overridePath)
    {
        if (_isLoaded) return;

        await Task.Run(() =>
        {
            lock (_sync)
            {
                if (_isLoaded) return;

                try
            {
                var path = overridePath ?? GetPath("parameters", "standard_rates.csv");
                if (!File.Exists(path))
                {
                    Logger.Warn($"Standard rates not found at {path}");
                    return;
                }

                using var reader = new StreamReader(path);
                using var csv = FinancialCalculator.Engine.Core.SafeCsv.Create(reader);

                csv.Read();
                csv.ReadHeader();

                // Validate schema
                var headers = (csv.HeaderRecord ?? Array.Empty<string>()).Select(h => (h ?? string.Empty).Trim()).ToArray();
                var expected = new[] { "Product", "Term", "DPMin", "DPMax", "PaymentMode", "StandardRate" };
                foreach (var col in expected)
                {
                    if (!headers.Any(h => string.Equals(h, col, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidDataException($"Standard rates CSV missing required column '{col}'.");
                }

                var rows = new List<(string Product, int Term, double Min, double Max, string Mode, double Rate)>();
                while (csv.Read())
                {
                    var product = csv.GetField("Product") ?? string.Empty;
                    var termStr = csv.GetField("Term");
                    var dpMinStr = csv.GetField("DPMin");
                    var dpMaxStr = csv.GetField("DPMax");
                    var paymentMode = csv.GetField("PaymentMode") ?? string.Empty;
                    var rateStr = csv.GetField("StandardRate");

                    if (!int.TryParse(termStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var term)) continue;
                    if (!double.TryParse(dpMinStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var dpMin)) continue;
                    if (!double.TryParse(dpMaxStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var dpMax)) continue;
                    if (!double.TryParse(rateStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)) continue;

                    var p = NormalizeProduct(product);
                    var m = NormalizePaymentMode(paymentMode);

                    rows.Add((p, term, dpMin, dpMax, m, rate));
                }

                BuildIndex(rows);
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading standard rates", ex);
                throw;
                }
            }
        });
    }

    private void BuildIndex(IEnumerable<(string Product, int Term, double Min, double Max, string Mode, double Rate)> rows)
    {
        _index.Clear();

        foreach (var productGroup in rows.GroupBy(r => r.Product, StringComparer.OrdinalIgnoreCase))
        {
            var byTerm = new Dictionary<int, TermIndex>();
            foreach (var termGroup in productGroup.GroupBy(r => r.Term))
            {
                var termIdx = new TermIndex();

                foreach (var modeGroup in termGroup.GroupBy(r => r.Mode, StringComparer.OrdinalIgnoreCase))
                {
                    var list = modeGroup
                        .Select(r => new RangeItem { Min = r.Min, Max = r.Max, Rate = r.Rate })
                        .OrderBy(r => r.Min)
                        .ToList();

                    // Validate for overlaps or ambiguities
                    for (int i = 1; i < list.Count; i++)
                    {
                        var prev = list[i - 1];
                        var cur = list[i];

                        // Overlap/ambiguity if current min <= previous max (strict) with tiny epsilon
                        if (cur.Min <= prev.Max - 1e-9)
                        {
                            throw new InvalidOperationException(
                                $"Overlapping downpayment ranges in standard rates for Product='{productGroup.Key}', Term={termGroup.Key}, Mode='{modeGroup.Key}'");
                        }
                    }

                    termIdx.ByMode[modeGroup.Key] = list;
                }

                byTerm[termGroup.Key] = termIdx;
            }

            _index[productGroup.Key] = byTerm;
        }
    }

    // MARK: Public API

    public double? GetStandardRate(string product, int term, double downPaymentPct, string paymentMode)
    {
        if (!_isLoaded) return null;

        var p = NormalizeProduct(product);
        var mode = NormalizePaymentMode(paymentMode);
        var dp = NormalizeDownPayment(downPaymentPct);

        if (!_index.TryGetValue(p, out var byTerm)) return null;
        if (!byTerm.TryGetValue(term, out var termIdx)) return null;

        // Exact mode first
        if (TryFindInMode(termIdx, mode, dp, out var rate))
            return rate;

        // Fallback to 'Any' if present
        if (TryFindInMode(termIdx, "Any", dp, out rate))
            return rate;

        return null;
    }

    public IReadOnlyList<int> GetAvailableTerms(string product, double downPaymentPct, string paymentMode)
    {
        var result = new List<int>();
        if (!_isLoaded) return result;

        var p = NormalizeProduct(product);
        var mode = NormalizePaymentMode(paymentMode);
        var dp = NormalizeDownPayment(downPaymentPct);

        if (!_index.TryGetValue(p, out var byTerm)) return result;

        foreach (var kvp in byTerm)
        {
            var term = kvp.Key;
            var idx = kvp.Value;

            if (HasMatchInMode(idx, mode, dp) || HasMatchInMode(idx, "Any", dp))
            {
                result.Add(term);
            }
        }

        result.Sort();
        return result;
    }

    // MARK: Helpers

    private static double NormalizeDownPayment(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return 0.0;
        var clamped = Math.Max(0.0, Math.Min(1.0, v));
        // CSV precision is 4 decimals → align to avoid edge gaps (e.g., 0.14995)
        return Math.Round(clamped, 4, MidpointRounding.AwayFromZero);
    }

    private static bool TryFindInMode(TermIndex termIdx, string mode, double dp, out double? rate)
    {
        rate = null;
        if (!termIdx.ByMode.TryGetValue(mode, out var list)) return false;

        var matches = list.Where(r => dp >= r.Min - 1e-9 && dp <= r.Max + 1e-9).ToList();
        if (matches.Count > 1)
        {
            throw new InvalidOperationException($"Ambiguous standard rate: multiple ranges match (Mode='{mode}', DP={dp:0.####}).");
        }
        if (matches.Count == 1)
        {
            rate = matches[0].Rate;
            return true;
        }
        return false;
    }

    private static bool HasMatchInMode(TermIndex termIdx, string mode, double dp)
    {
        if (!termIdx.ByMode.TryGetValue(mode, out var list)) return false;
        return list.Any(r => dp >= r.Min - 1e-9 && dp <= r.Max + 1e-9);
    }

    private string GetPath(params string[] pathParts)
    {
        return PathResolver.GetDocsFilePath(pathParts);
    }

    private static string NormalizeProduct(string product)
    {
        if (string.IsNullOrWhiteSpace(product)) return "";
        var p = product.Trim();
        var u = p.ToUpperInvariant();
        if (u == "F-LEASE" || u.Contains("FINANCE")) return "FL";
        if (u.StartsWith("HP")) return "HP";
        if (u.Contains("MYSTAR")) return "mySTAR";
        if (u == "OP-LEASE" || u.Contains("OPERAT")) return "OL";
        return p;
    }

    private static string NormalizePaymentMode(string paymentMode)
    {
        var s = (paymentMode ?? "").Trim().ToLowerInvariant();
        if (s == "advance" || s == "in advance" || s == "inadvance") return "Advance";
        if (s == "arrears" || s == "in arrears" || s == "inarrears") return "Arrears";
        if (s == "any" || s == "auto") return "Any";
        // Preserve canonical if already properly cased
        return string.IsNullOrWhiteSpace(paymentMode) ? "Arrears" : paymentMode.Trim();
    }
}