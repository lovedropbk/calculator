using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FinancialCalculator.Engine.Core
{
    /// <summary>
    /// Default implementation of ICostOfFundsService that loads once from config.yaml
    /// and serves term-specific MFR, matched funding spread, and OPEX by product.
    /// Parsing is intentionally simple and robust to our current config.yaml format.
    /// </summary>
    public sealed class CostOfFundsService : ICostOfFundsService
    {
        private readonly object _sync = new();
        private bool _loaded;

        private Dictionary<int, decimal> _curve = new();
        private decimal _spread = 0.0025m;
        private Dictionary<string, decimal> _opexByProduct = new(StringComparer.OrdinalIgnoreCase);

        // Defaults (used if config.yaml is missing or malformed)
        private static readonly Dictionary<int, decimal> DefaultCurve = new()
        {
            {12, 0.0148m}, {24, 0.0165m}, {36, 0.0175m}, {48, 0.0185m}, {60, 0.0195m},
        };
        private const decimal DefaultSpread = 0.0025m;
        private static readonly Dictionary<string, decimal> DefaultOpex = new(StringComparer.OrdinalIgnoreCase)
        {
            { "HP", 0.0095m },
            { "mySTAR", 0.0095m },
            { "FinanceLease", 0.0065m },
            { "OperatingLease", 0.0070m }
        };

        public IReadOnlyDictionary<int, decimal> GetCurve()
        {
            EnsureLoaded();
            return _curve;
        }

        public decimal GetNearestMfrRate(int termMonths)
        {
            EnsureLoaded();
            if (_curve.TryGetValue(termMonths, out var v)) return v;

            // Nearest neighbor
            var bestKey = -1;
            var bestDiff = int.MaxValue;
            foreach (var kv in _curve)
            {
                var d = Math.Abs(kv.Key - termMonths);
                if (d < bestDiff) { bestKey = kv.Key; bestDiff = d; }
            }
            return bestKey >= 0 ? _curve[bestKey] : 0m;
        }

        public decimal GetMatchedFundingSpread()
        {
            EnsureLoaded();
            return _spread;
        }

        public decimal GetOpexPctForProduct(string product)
        {
            EnsureLoaded();
            var key = NormalizeProductKey(product);
            return _opexByProduct.TryGetValue(key, out var v) ? v : 0m;
        }

        // MARK: Load-once

        private void EnsureLoaded()
        {
            if (_loaded) return;
            lock (_sync)
            {
                if (_loaded) return;

                // Defaults first (used if config not found)
                _curve = new Dictionary<int, decimal>(DefaultCurve);
                _spread = DefaultSpread;
                _opexByProduct = new Dictionary<string, decimal>(DefaultOpex, StringComparer.OrdinalIgnoreCase);

                try
                {
                    var path = FindConfigPath("config.yaml");
                    if (path != null && File.Exists(path))
                    {
                        var lines = File.ReadAllLines(path);
                        var parsedCurve = ParseCostOfFundsCurve(lines);
                        if (parsedCurve.Count > 0)
                            _curve = parsedCurve;

                        var maybeSpread = ParseSingleDecimal(lines, "matchedFundedSpread");
                        if (maybeSpread.HasValue) _spread = maybeSpread.Value;

                        var opexByProduct = ParseOpexByProduct(lines);
                        if (opexByProduct.Count > 0)
                            _opexByProduct = opexByProduct;
                    }
                }
                catch
                {
                    // Keep defaults
                }

                _loaded = true;
            }
        }

        // MARK: Parsing helpers (lightweight YAML scanning)

        private static Dictionary<int, decimal> ParseCostOfFundsCurve(string[] lines)
        {
            var result = new Dictionary<int, decimal>();
            bool inCurve = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = (lines[i] ?? string.Empty).Trim();
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

                        // Attempt inline parse like "- termMonths: 24" or "- rate: 0.0100"
                        var inline = line.TrimStart('-').Trim();
                        if (!string.IsNullOrEmpty(inline))
                        {
                            if (inline.StartsWith("termMonths", StringComparison.OrdinalIgnoreCase))
                            {
                                var parts = inline.Split(':');
                                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var t)) term = t;
                            }
                            else if (inline.StartsWith("rate", StringComparison.OrdinalIgnoreCase))
                            {
                                var parts = inline.Split(':');
                                if (parts.Length == 2 && decimal.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r)) rate = r;
                            }
                        }

                        // Look ahead for termMonths: and rate: on following lines (YAML block style)
                        for (int j = i + 1; j < Math.Min(lines.Length, i + 6); j++)
                        {
                            var l = (lines[j] ?? string.Empty).Trim();
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
                var line = (raw ?? string.Empty).Trim();
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
                var line = (lines[i] ?? string.Empty).Trim();
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

        private static string NormalizeProductKey(string product)
        {
            product = (product ?? string.Empty).Trim();
            if (product.StartsWith("HP", StringComparison.OrdinalIgnoreCase)) return "HP";
            if (product.Contains("mySTAR", StringComparison.OrdinalIgnoreCase)) return "mySTAR";
            if (product.Contains("F-Lease", StringComparison.OrdinalIgnoreCase) || product.Contains("Finance", StringComparison.OrdinalIgnoreCase)) return "FinanceLease";
            if (product.Contains("Op-Lease", StringComparison.OrdinalIgnoreCase) || product.Contains("Operating", StringComparison.OrdinalIgnoreCase)) return "OperatingLease";
            return product;
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
    }
}