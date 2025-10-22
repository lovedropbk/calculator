using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
                System.Diagnostics.Debug.WriteLine($"Standard rates not found at {path}");
                return;
            }

            var lines = await File.ReadAllLinesAsync(path);
            // Skip header
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = CsvParser.SplitCsvLine(line);
                if (parts.Length < 6) continue;

                _rates.Add(new StandardRate
                {
                    Product = parts[0].Trim(),
                    Term = int.Parse(parts[1].Trim()),
                    DPMin = ParseDouble(parts[2]),
                    DPMax = ParseDouble(parts[3]),
                    PaymentMode = parts[4].Trim(),
                    Rate = ParseDouble(parts[5])

                });
            }
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading standard rates: {ex.Message}");
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
    
    private double ParseDouble(string s)
    {
        if (double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            return val;
        return 0;
    }

    public double? GetStandardRate(string product, int term, double downPaymentPct, string paymentMode)
    {
        var match = _rates.FirstOrDefault(r => r.Matches(product, term, downPaymentPct, paymentMode));
        return match?.Rate;
    }
}