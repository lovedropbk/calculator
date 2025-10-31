using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using FinancialCalculator.WinUI3.Models;

namespace FinancialCalculator.WinUI3.Services
{
    public interface IInsuranceCatalogService
    {
        Task LoadAsync();
        double? TryGetInsuranceCost(Vehicle vehicle);
        string? LastMatchInfo { get; }
    }

    public sealed class InsuranceCatalogService : IInsuranceCatalogService
    {
        private readonly List<InsuranceEntry> _entries = new();
        private bool _loaded = false;
        public string? LastMatchInfo { get; private set; }

        private sealed class InsuranceEntry
        {
            public string ModelDisplay { get; init; } = "";
            public double MSRP { get; init; }
            public double? CC { get; init; }
            public double InsuranceInclVat { get; init; }
            public double InsuranceExclVat { get; init; }
            public string NormalizedKey { get; init; } = "";
        }

        public async Task LoadAsync()
        {
            if (_loaded) return;

            try
            {
                // Prefer cleaned catalog if present
                var cleanedPath = GetDocsPath("insurance_catalog.csv");
                if (File.Exists(cleanedPath))
                {
                    await LoadCleanedAsync(cleanedPath);
                    _loaded = true;
                    return;
                }

                var rawPath = GetDocsPath("insurance_raw_input.csv");
                if (!File.Exists(rawPath))
                {
                    Logger.Warn($"Insurance raw input not found at {rawPath}");
                    _loaded = true; // Avoid repeated attempts
                    return;
                }

                await LoadFromRawAsync(rawPath);

                // Persist cleaned output for auditability (optional but helpful)
                try
                {
                    await WriteCleanedAsync(cleanedPath);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to write cleaned insurance catalog: {ex.Message}");
                }

                _loaded = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading insurance catalog", ex);
                _loaded = true;
            }
        }

        public double? TryGetInsuranceCost(Vehicle vehicle)
        {
            if (!_loaded) return null;
            if (vehicle == null) return null;

            string key = NormalizeModelName(vehicle.ModelName);

            var candidates = _entries.Where(e => string.Equals(e.NormalizedKey, key, StringComparison.OrdinalIgnoreCase)).ToList();
            if (candidates.Count == 0)
            {
                var relaxedKey = RelaxKey(key);
                if (!string.Equals(relaxedKey, key, StringComparison.Ordinal))
                {
                    candidates = _entries.Where(e => string.Equals(e.NormalizedKey, relaxedKey, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }

            if (candidates.Count == 0)
            {
                LastMatchInfo = $"No match for '{vehicle.ModelName}' (key='{key}')";
                return null;
            }

            // Choose closest MSRP
            var chosen = candidates
                .OrderBy(e => Math.Abs(e.MSRP - vehicle.MSRP))
                .First();

            // Accept if MSRP within 15% or <= 150k THB difference, else reject as unsafe
            var diff = Math.Abs(chosen.MSRP - vehicle.MSRP);
            var pct = vehicle.MSRP > 0 ? diff / vehicle.MSRP : 1.0;
            if (pct > 0.15 && diff > 150_000)
            {
                LastMatchInfo = $"Unsafe match for '{vehicle.ModelName}'. Closest insurance entry '{chosen.ModelDisplay}' MSRP mismatch: Δ={diff:N0} THB ({pct:P2})";
                return null;
            }

            LastMatchInfo = $"Matched '{vehicle.ModelName}' -> '{chosen.ModelDisplay}' (ΔMSRP={diff:N0} THB)";
            return chosen.InsuranceInclVat;
        }

        // MARK: Loaders

        private async Task LoadCleanedAsync(string path)
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var model = csv.GetField("ModelDisplay") ?? "";
                if (string.IsNullOrWhiteSpace(model)) continue;

                double Parse(string name)
                {
                    var s = csv.GetField(name) ?? "";
                    return ParseCurrency(s);
                }

                var msrp = Parse("MSRP");
                var cc = csv.TryGetField("CC", out string? ccStr) ? ParseCurrency(ccStr ?? "") : 0.0;
                var incl = Parse("InsurancePriceInclVat");
                var excl = csv.TryGetField("InsurancePriceExclVat", out string? exStr) ? ParseCurrency(exStr ?? "") : 0.0;

                var key = csv.GetField("NormalizedKey") ?? NormalizeModelName(model);

                _entries.Add(new InsuranceEntry
                {
                    ModelDisplay = model.Trim(),
                    MSRP = msrp,
                    CC = cc > 0 ? cc : null,
                    InsuranceInclVat = incl,
                    InsuranceExclVat = excl,
                    NormalizedKey = key
                });
            }
        }

        private async Task LoadFromRawAsync(string path)
        {
            using var reader = new StreamReader(path, Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                BadDataFound = null,
                MissingFieldFound = null,
                IgnoreBlankLines = true,
                Delimiter = ","
            });

            var temp = new List<InsuranceEntry>();

            while (await csv.ReadAsync())
            {
                // Columns (best-effort): 0=index, 1=model, 2=CC, 3=age, 4=MSRP, 5=Incl VAT, 6=Excl VAT
                if (!csv.TryGetField<string>(1, out var model) || string.IsNullOrWhiteSpace(model))
                    continue;

                // Skip header/preamble rows
                if (string.Equals(model.Trim(), "รุ่น", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Parse numbers; tolerate missing/garbage
                double cc = 0;
                if (csv.TryGetField<string>(2, out var ccStr))
                    cc = ParseCurrency(ccStr ?? "");

                double msrp = 0;
                if (csv.TryGetField<string>(4, out var msrpStr))
                    msrp = ParseCurrency(msrpStr ?? "");

                double incl = 0;
                if (csv.TryGetField<string>(5, out var inclStr))
                    incl = ParseCurrency(inclStr ?? "");

                double excl = 0;
                if (csv.TryGetField<string>(6, out var exclStr))
                    excl = ParseCurrency(exclStr ?? "");

                if (incl <= 0 && excl <= 0)
                    continue;

                var key = NormalizeModelName(model);

                temp.Add(new InsuranceEntry
                {
                    ModelDisplay = (model ?? "").Trim(),
                    MSRP = msrp,
                    CC = cc > 0 ? cc : null,
                    InsuranceInclVat = incl > 0 ? incl : excl,
                    InsuranceExclVat = excl > 0 ? excl : incl,
                    NormalizedKey = key
                });
            }

            // Deduplicate by NormalizedKey; keep the one with the highest MSRP (assume latest price)
            foreach (var grp in temp.GroupBy(e => e.NormalizedKey, StringComparer.OrdinalIgnoreCase))
            {
                var chosen = grp
                    .OrderByDescending(e => e.MSRP)
                    .ThenByDescending(e => e.InsuranceInclVat)
                    .First();

                _entries.Add(chosen);
            }
        }

        private async Task WriteCleanedAsync(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var writer = new StreamWriter(path, false, Encoding.UTF8);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
            // Header
            csv.WriteField("ModelDisplay");
            csv.WriteField("MSRP");
            csv.WriteField("CC");
            csv.WriteField("InsurancePriceInclVat");
            csv.WriteField("InsurancePriceExclVat");
            csv.WriteField("NormalizedKey");
            await csv.NextRecordAsync();

            foreach (var e in _entries.OrderBy(e => e.ModelDisplay, StringComparer.InvariantCultureIgnoreCase))
            {
                csv.WriteField(e.ModelDisplay);
                csv.WriteField(e.MSRP.ToString("0.##", CultureInfo.InvariantCulture));
                csv.WriteField(e.CC?.ToString("0.##", CultureInfo.InvariantCulture) ?? "");
                csv.WriteField(e.InsuranceInclVat.ToString("0.##", CultureInfo.InvariantCulture));
                csv.WriteField(e.InsuranceExclVat.ToString("0.##", CultureInfo.InvariantCulture));
                csv.WriteField(e.NormalizedKey);
                await csv.NextRecordAsync();
            }
        }

        // MARK: Helpers

        private static double ParseCurrency(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var cleaned = s.Replace("\"", "").Replace(",", "").Trim();
            // Remove leading non-digits (currency text) and spaces
            cleaned = Regex.Replace(cleaned, @"[^\d\.\-]+", "");
            if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                return v;
            return 0;
        }

        private static string NormalizeModelName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";

            s = s.Trim();

            // Normalize brand separators and accents
            s = Regex.Replace(s, @"Mercedes\s*-\s*Maybach", "Mercedes-Maybach", RegexOptions.IgnoreCase);
            s = s.Replace("Coupé", "Coupe", StringComparison.OrdinalIgnoreCase);

            // Remove parenthetical hints: (MY2024), (Night Edition), (CKD) etc.
            s = Regex.Replace(s, @"\([^)]*\)", "", RegexOptions.CultureInvariant);

            // Remove Thai/locale year markers like 'MY:2023' or 'MY 2023'
            s = Regex.Replace(s, @"MY:? ?\d{4}", "", RegexOptions.IgnoreCase);

            // Remove market tags (THA/THB/THx), program tags (PGx, SPx), build tags (CKD, MMC, EC)
            s = Regex.Replace(s, @"\bTH[A-Z0-9]+\b", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bPG\d+\b", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bSP\d+\b", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bCKD\b|\bMMC\b|\bEC\b", "", RegexOptions.IgnoreCase);

            // Remove trim/edition tokens that vary between sources
            string[] tokens =
            {
                "Exclusive","Avantgarde","Dynamic","AMG Line","AMG Dynamic","AMG Premium",
                "Sport","Premium","Special EDITION","Final EDITION","Edition","Night Edition",
                "Electric Art"
            };
            foreach (var t in tokens)
                s = Regex.Replace(s, $@"\b{Regex.Escape(t)}\b", "", RegexOptions.IgnoreCase);

            // Collapse whitespace/ punctuation
            s = s.Replace("-", " ");
            s = Regex.Replace(s, @"[^\w\s\+]", " ");
            s = Regex.Replace(s, @"\s+", " ").Trim();

            // Key: remove spaces and dashes to match catalog strategy
            var key = Regex.Replace(s, @"[\s\-]", "", RegexOptions.CultureInvariant).ToLowerInvariant();
            return key;
        }

        private static string RelaxKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return key;

            var s = key;
            // Remove drivetrain/body suffixes for relaxed match
            s = s.Replace("4matic", "");
            s = s.Replace("coupe", "");
            s = s.Replace("suv", "");
            s = s.Replace("cabriolet", "");
            s = s.Replace("sedan", "");

            // Optionally drop 'amg' suffix to match non-AMG baseline names if source omitted or added it
            s = s.Replace("amg", "");

            s = Regex.Replace(s, @"\s+", "");
            return s;
        }

        private static string GetDocsPath(string filename)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 1) Prefer deployment layout: <base>/docs/filename
            var p = Path.Combine(baseDir, "docs", filename);
            if (File.Exists(p) || Directory.Exists(Path.GetDirectoryName(p)!)) return p;

            // 2) Walk up to find 'winui3-mvp/docs'
            var current = new DirectoryInfo(baseDir);
            int maxDepth = 10;
            while (current != null && maxDepth-- > 0)
            {
                var check = Path.Combine(current.FullName, "winui3-mvp", "docs", filename);
                var dir = Path.GetDirectoryName(check)!;
                if (File.Exists(check) || Directory.Exists(dir)) return check;

                check = Path.Combine(current.FullName, "docs", filename);
                dir = Path.GetDirectoryName(check)!;
                if (File.Exists(check) || Directory.Exists(dir)) return check;

                current = current.Parent;
            }

            // Fallback: return under baseDir
            return Path.Combine(baseDir, filename);
        }
    }
}