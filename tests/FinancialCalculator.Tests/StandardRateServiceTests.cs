using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using FinancialCalculator.WinUI3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FinancialCalculator.Tests
{
    [TestClass]
    public class StandardRateServiceTests
    {
        private const double EPS = 1e-6;

        private sealed class RateCsvRow
        {
            public string Product { get; set; } = "";
            public int Term { get; set; }
            public double DPMin { get; set; }
            public double DPMax { get; set; }
            public string PaymentMode { get; set; } = "";
            public double StandardRate { get; set; }
        }

        private static string LocateCsvPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var filename = Path.Combine("parameters", "standard_rates.csv");

            var direct = Path.Combine(baseDir, "docs", filename);
            if (File.Exists(direct)) return direct;

            var current = new DirectoryInfo(baseDir);
            int maxDepth = 12;
            while (current != null && maxDepth-- > 0)
            {
                var check = Path.Combine(current.FullName, "winui3-mvp", "docs", filename);
                if (File.Exists(check)) return check;

                check = Path.Combine(current.FullName, "docs", filename);
                if (File.Exists(check)) return check;

                current = current.Parent!;
            }

            throw new FileNotFoundException("Could not locate winui3-mvp/docs/parameters/standard_rates.csv");
        }

        private static List<RateCsvRow> ReadCsvRows(string path)
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim
            });

            return csv.GetRecords<RateCsvRow>().ToList();
        }

        [TestMethod]
        public async Task CsvRoundtrip_AllRowsMatch()
        {
            // Arrange
            var svc = new StandardRateService();
            await svc.LoadAsync();

            var rows = ReadCsvRows(LocateCsvPath());

            // Act + Assert for each row using a midpoint within the range
            foreach (var r in rows)
            {
                var dpMid = Math.Round((r.DPMin + r.DPMax) / 2.0, 4, MidpointRounding.AwayFromZero);

                if (string.Equals(r.PaymentMode, "Any", StringComparison.OrdinalIgnoreCase))
                {
                    // Verify fallback to 'Any' works for both modes and normalization of casing
                    var adv = svc.GetStandardRate(r.Product, r.Term, dpMid, "advance");
                    var arr = svc.GetStandardRate(r.Product, r.Term, dpMid, "ARREARS");
                    Assert.IsTrue(adv.HasValue, $"No rate for {r.Product}/{r.Term}/dp={dpMid} (advance)");
                    Assert.IsTrue(arr.HasValue, $"No rate for {r.Product}/{r.Term}/dp={dpMid} (arrears)");
                    Assert.AreEqual(r.StandardRate, adv.Value, EPS, $"Mismatch (advance) for {r.Product}/{r.Term}/dp={dpMid}");
                    Assert.AreEqual(r.StandardRate, arr.Value, EPS, $"Mismatch (arrears) for {r.Product}/{r.Term}/dp={dpMid}");
                }
                else
                {
                    var mode = r.PaymentMode;
                    var got = svc.GetStandardRate(r.Product, r.Term, dpMid, mode);
                    Assert.IsTrue(got.HasValue, $"No rate for {r.Product}/{r.Term}/dp={dpMid}/{mode}");
                    Assert.AreEqual(r.StandardRate, got.Value, EPS, $"Mismatch for {r.Product}/{r.Term}/dp={dpMid}/{mode}");
                }
            }
        }

        [TestMethod]
        public async Task Normalization_WhitespaceCaseAndSynonyms_Work()
        {
            var svc = new StandardRateService();
            await svc.LoadAsync();

            // mySTAR 'Any' → should match for any mode; also test whitespace/case
            var r1 = svc.GetStandardRate("  mySTAR  ", 36, 0.10, "in advance");
            Assert.IsTrue(r1.HasValue, "Expected rate for mySTAR/36/10%/advance");
            Assert.AreEqual(12.25, r1.Value, EPS);

            // Finance Lease synonyms → 'FL' rows with PaymentMode=Any
            var r2 = svc.GetStandardRate("Finance Lease", 60, 0.25, "ArReArS");
            Assert.IsTrue(r2.HasValue, "Expected rate for Finance Lease/60/25%/arrears");
            Assert.AreEqual(11.00, r2.Value, EPS);
        }

        [TestMethod]
        public async Task UnknownCombination_ReturnsNull()
        {
            var svc = new StandardRateService();
            await svc.LoadAsync();

            // HP 12 months does not exist in CSV
            var r = svc.GetStandardRate("HP", 12, 0.10, "advance");
            Assert.IsFalse(r.HasValue, "Expected null (no match) for HP/12/10%/advance");
        }

        [TestMethod]
        public async Task Rounding_Edges_MapToExpectedBucket()
        {
            var svc = new StandardRateService();
            await svc.LoadAsync();

            var rows = ReadCsvRows(LocateCsvPath());
            // Find bucket that contains 0.15 for HP/36/Advance
            var target = rows.FirstOrDefault(x =>
                string.Equals(x.Product, "HP", StringComparison.OrdinalIgnoreCase) &&
                x.Term == 36 &&
                string.Equals(x.PaymentMode, "Advance", StringComparison.OrdinalIgnoreCase) &&
                0.15 >= x.DPMin && 0.15 <= x.DPMax);

            Assert.IsNotNull(target, "Could not find HP/36/Advance bucket containing 0.15");

            // 14.995% should round to 0.1500 → match target range
            var r = svc.GetStandardRate("HP", 36, 0.14995, "advance");
            Assert.IsTrue(r.HasValue, "Expected rate at 14.995% (rounded to 0.1500)");
            Assert.AreEqual(target!.StandardRate, r.Value, EPS);
        }

        [TestMethod]
        public async Task DuplicateOverlap_RaisesDeterministicError()
        {
            // Create overlapping ranges for same product/term/mode
            var tmp = Path.Combine(Path.GetTempPath(), $"std_rates_overlap_{Guid.NewGuid():N}.csv");
            await File.WriteAllTextAsync(tmp,
@"Product,Term,DPMin,DPMax,PaymentMode,StandardRate
HP,36,0.10,0.20,Advance,5.0
HP,36,0.15,0.25,Advance,6.0
");

            try
            {
                var svc = new StandardRateService();
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await svc.LoadAsync(tmp));
            }
            finally
            {
                try { File.Delete(tmp); } catch { /* ignore */ }
            }
        }

        [TestMethod]
        public async Task RepeatedLookups_AreThreadSafeAndConsistent()
        {
            var svc = new StandardRateService();
            await svc.LoadAsync();

            var expected = svc.GetStandardRate("HP", 36, 0.10, "advance");
            Assert.IsTrue(expected.HasValue, "Baseline expected rate missing");

            var tasks = Enumerable.Range(0, 32)
                .Select(async _ => svc.GetStandardRate("HP", 36, 0.10, "advance"))
                .ToArray();

            await Task.WhenAll(tasks);
            foreach (var t in tasks)
            {
                Assert.IsTrue(t.Result.HasValue);
                Assert.AreEqual(expected.Value, t.Result.Value, EPS);
            }
        }

        [TestMethod]
        public async Task GetAvailableTerms_UsesCsvDistinctTerms()
        {
            var svc = new StandardRateService();
            await svc.LoadAsync();

            // For HP/advance/10% the CSV includes 24,36,48,60,72
            var terms = svc.GetAvailableTerms("HP", 0.10, "advance");
            Assert.IsTrue(terms.Count >= 5);
            CollectionAssert.IsSubsetOf(new[] { 24, 36, 48, 60, 72 }, terms.ToArray());
            Assert.IsTrue(terms.SequenceEqual(terms.OrderBy(x => x)), "Terms should be sorted ascending");
        }
    }
}