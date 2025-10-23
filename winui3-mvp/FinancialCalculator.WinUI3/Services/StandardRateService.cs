using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using FinancialCalculator.WinUI3.Models;

namespace FinancialCalculator.WinUI3.Services;

public class StandardRateService
{
    private List<StandardRate> _rates = new();
    private bool _isLoaded = false;

    public async Task LoadAsync()
    {
        if (_isLoaded) return;

        try
        {
            var path = GetPath("parameters", "standard_rates.csv");
            if (!File.Exists(path))
            {
                Logger.Warn($"Standard rates not found at {path}");
                return;
            }

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                // parts -> Product
                // parts -> Term
                // parts -> DP Min
                // parts -> DP Max
                // parts -> Payment Mode
                // parts -> Rate

                if (!csv.TryGetField<string>(0, out var product)) continue;
                if (!csv.TryGetField<int>(1, out var term)) continue;
                if (!csv.TryGetField<double>(2, out var dpMin)) continue;
                if (!csv.TryGetField<double>(3, out var dpMax)) continue;
                if (!csv.TryGetField<string>(4, out var paymentMode)) continue;
                if (!csv.TryGetField<double>(5, out var rate)) continue;

                _rates.Add(new StandardRate
                {
                    Product = product.Trim(),
                    Term = term,
                    DPMin = dpMin,
                    DPMax = dpMax,
                    PaymentMode = paymentMode.Trim(),
                    Rate = rate
                });
            }
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            Logger.Error("Error loading standard rates", ex);
        }
    }

    private string GetPath(params string[] pathParts)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var filename = Path.Combine(pathParts);

        // 1. Try relative to baseDir (deployment)
        var path = Path.Combine(baseDir, "docs", filename);
        if (File.Exists(path)) return path;

        // 2. Walk up to find 'winui3-mvp' folder or 'docs' folder
        var current = new DirectoryInfo(baseDir);
        int maxDepth = 10;
        while (current != null && maxDepth-- > 0)
        {
             var check = Path.Combine(current.FullName, "winui3-mvp", "docs", filename);
             if (File.Exists(check)) return check;
             
             check = Path.Combine(current.FullName, "docs", filename);
             if (File.Exists(check)) return check;

             current = current.Parent;
        }

        return Path.Combine(baseDir, filename);
    }

    public double? GetStandardRate(string product, int term, double downPaymentPct, string paymentMode)
    {
        var match = _rates.FirstOrDefault(r => r.Matches(product, term, downPaymentPct, paymentMode));
        return match?.Rate;
    }
}