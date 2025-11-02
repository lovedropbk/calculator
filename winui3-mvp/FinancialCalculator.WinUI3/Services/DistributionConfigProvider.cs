using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FinancialCalculator.WinUI3.Services
{
    public interface IDistributionConfigProvider
    {
        bool TryGetConfiguredDistribution(string product, int term, out double value);
    }

    /// <summary>
    /// YamlDistributionConfigProvider
    /// - Caches parsed distribution config (designer.defaultDistribution) from config.yaml in memory.
    /// - Reloads only if the file timestamp changes.
    /// - Logs "Loaded distribution from disk" on initial load or when changed.
    /// - Logs "Loaded distribution from cache" at most once per session after initial load to avoid spam.
    /// </summary>
    public sealed class YamlDistributionConfigProvider : IDistributionConfigProvider
    {
        private static readonly Lazy<YamlDistributionConfigProvider> _instance =
            new(() => new YamlDistributionConfigProvider());

        public static YamlDistributionConfigProvider Instance => _instance.Value;

        private readonly object _sync = new();
        private Dictionary<string, Dictionary<int, double>> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        private string? _path;
        private DateTime _lastWriteUtc = DateTime.MinValue;
        private bool _hasLoaded;
        private bool _loggedCacheOnce;

        private YamlDistributionConfigProvider() { }

        public bool TryGetConfiguredDistribution(string product, int term, out double value)
        {
            value = 0.0;
            try
            {
                EnsureLoaded();

                var key = (product ?? string.Empty).Trim();
                if (_cache.TryGetValue(key, out var byTerm) && byTerm.TryGetValue(term, out var v))
                {
                    value = v;
                    return true;
                }

                // Try normalized product key
                var alt = NormalizeProductKey(key);
                if (!string.Equals(alt, key, StringComparison.OrdinalIgnoreCase) &&
                    _cache.TryGetValue(alt, out byTerm) && byTerm.TryGetValue(term, out var v2))
                {
                    value = v2;
                    return true;
                }
            }
            catch
            {
                // best-effort, no throw
            }
            return false;
        }

        private void EnsureLoaded()
        {
            lock (_sync)
            {
                var path = LocateConfigPath();
                DateTime writeUtc = DateTime.MinValue;
                if (path != null && File.Exists(path))
                {
                    try { writeUtc = File.GetLastWriteTimeUtc(path); } catch { /* ignore */ }
                }

                if (!_hasLoaded)
                {
                    _cache = TryLoad(path);
                    _path = path;
                    _lastWriteUtc = writeUtc;
                    _hasLoaded = true;

                    if (path != null)
                    {
                        Logger.Info($"[DistributionConfigProvider] Loaded distribution from disk: {path}");
                    }
                }
                else
                {
                    bool changed = path != null &&
                                   File.Exists(path) &&
                                   (!string.Equals(path, _path, StringComparison.OrdinalIgnoreCase) ||
                                     writeUtc > _lastWriteUtc);

                    if (changed)
                    {
                        _cache = TryLoad(path);
                        _path = path;
                        _lastWriteUtc = writeUtc;
                        _loggedCacheOnce = false; // allow one cache log after reload
                        Logger.Info($"[DistributionConfigProvider] Loaded distribution from disk (changed): {path}");
                    }
                    else
                    {
                        if (!_loggedCacheOnce)
                        {
                            Logger.Info("[DistributionConfigProvider] Loaded distribution from cache");
                            _loggedCacheOnce = true;
                        }
                    }
                }
            }
        }

        private static Dictionary<string, Dictionary<int, double>> TryLoad(string? path)
        {
            if (path == null || !File.Exists(path))
                return new(StringComparer.OrdinalIgnoreCase);

            try
            {
                var lines = File.ReadAllLines(path);
                return ParseDistributionFromYaml(lines);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[DistributionConfigProvider] Failed to load config.yaml: {ex.Message}");
                return new(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static string? LocateConfigPath()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var check = Path.Combine(baseDir, "config.yaml");
                if (File.Exists(check)) return check;

                var current = new DirectoryInfo(baseDir);
                int depth = 8;
                while (current != null && depth-- > 0)
                {
                    check = Path.Combine(current.FullName, "config.yaml");
                    if (File.Exists(check)) return check;

                    current = current.Parent;
                }
            }
            catch { }
            return null;
        }

        // Reused parser from CampaignTermBreakdownService with small adjustments
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

        internal static string NormalizeProductKey(string product)
        {
            product = (product ?? string.Empty).Trim();
            if (product.StartsWith("HP", StringComparison.OrdinalIgnoreCase)) return "HP";
            if (product.Contains("mySTAR", StringComparison.OrdinalIgnoreCase)) return "mySTAR";
            if (product.Contains("F-Lease", StringComparison.OrdinalIgnoreCase) || product.Contains("Finance", StringComparison.OrdinalIgnoreCase)) return "FinanceLease";
            if (product.Contains("Op-Lease", StringComparison.OrdinalIgnoreCase) || product.Contains("Operating", StringComparison.OrdinalIgnoreCase)) return "OperatingLease";
            return product;
        }
    }
}