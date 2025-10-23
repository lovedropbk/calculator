using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using FinancialCalculator.Tests.Models;

namespace FinancialCalculator.Tests.Services;

public class VehicleCatalogService
{
    private List<Vehicle> _vehicles = new();
    public List<string> MbspPackages { get; private set; } = new();
    private bool _isLoaded = false;

    public async Task LoadAsync()
    {
        if (_isLoaded) return;

        try
        {
            _vehicles.Clear();
            await LoadVehiclesAndRVsAsync();
            await LoadMbspCostsAsync();
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading vehicle catalog: {ex.Message}");
        }
    }

    private async Task LoadVehiclesAndRVsAsync()
    {
        var path = GetPath("RVbymodel OCT2025.csv");
        if (!File.Exists(path))
        {
             Console.WriteLine($"RV catalog not found at {path}");
             return;
        }

        using var reader = new StreamReader(path);
        // Skip first 3 lines of header garbage if any to match original behavior perfectly, 
        // though CsvHelper might handle it if we just read until we find the real header.
        // Let's stick to explicit skip to be safe and match WinUI3 impl I just wrote.
        for (int i = 0; i < 2; i++) await reader.ReadLineAsync();

        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            if (!csv.TryGetField<string>(1, out var modelName) || string.IsNullOrWhiteSpace(modelName) || modelName == "Models name") continue;

            double ParseCurrencySafe(int index) => csv.TryGetField<string>(index, out var s) ? ParseCurrency(s ?? string.Empty) : 0;
            double? ParseRVSafe(int index) => csv.TryGetField<string>(index, out var s) ? ParseRV(s ?? string.Empty) : null;

            var vehicle = new Vehicle
            {
                ModelName = modelName.Trim(),
                Class = InferVehicleClass(modelName),
                MSRP = ParseCurrencySafe(4),
                RV12 = ParseRVSafe(7),
                RV24 = ParseRVSafe(8),
                RV36 = ParseRVSafe(9),
                RV48 = ParseRVSafe(10),
                RV60 = ParseRVSafe(11)
            };
            _vehicles.Add(vehicle);
        }
    }

    private async Task LoadMbspCostsAsync()
    {
        var path = GetPath("MBSP OCT2025.csv");
        if (!File.Exists(path))
        {
            Console.WriteLine($"MBSP catalog not found at {path}");
            return;
        }

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

        await csv.ReadAsync();
        csv.ReadHeader();
        var headerRecord = csv.HeaderRecord;

        var mbspIndices = new Dictionary<string, int>();
        if (headerRecord != null)
        {
            for (int i = 3; i < headerRecord.Length; i++)
            {
                if (!string.IsNullOrEmpty(headerRecord[i]))
                {
                    mbspIndices[headerRecord[i].Trim()] = i;
                    if (!MbspPackages.Contains(headerRecord[i].Trim()))
                    {
                        MbspPackages.Add(headerRecord[i].Trim());
                    }
                }
            }
        }

        while (await csv.ReadAsync())
        {
            if (!csv.TryGetField<string>(0, out var modelName) || modelName == null) continue;
            modelName = modelName.Trim();

            var vehicle = _vehicles.FirstOrDefault(v => ModelNamesMatch(v.ModelName, modelName));
            if (vehicle != null)
            {
                foreach (var kvp in mbspIndices)
                {
                    if (csv.TryGetField<string>(kvp.Value, out var costStr))
                    {
                        var cost = ParseCurrency(costStr ?? string.Empty);
                        if (cost > 0)
                        {
                            vehicle.MbspCosts[kvp.Key] = cost;
                        }
                    }
                }
            }
        }
    }

    private string GetPath(string filename)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        
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

        // Fallback for dev environment (up from bin/Debug/net8.0...)
        path = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "winui3-mvp", "docs", filename));
        if (File.Exists(path)) return path;

        return Path.Combine(baseDir, filename);
    }

    private string InferVehicleClass(string modelName)
    {
        var cleaned = modelName.Replace("Mercedes-AMG ", "").Replace("Mercedes-Maybach ", "").Trim();
        var parts = cleaned.Split(' ');
        if (parts.Length > 0)
        {
            var prefix = parts.First();
            if (prefix.StartsWith("V") && prefix.Length > 1 && prefix.Skip(1).Any(char.IsDigit))
            {
                 return "V-Class";
            }
            if (prefix == "Sprinter" || prefix == "Vito") return prefix;
            if (prefix.Length <= 4) return prefix + "-Class";
        }
        return "Other";
    }

    private double ParseCurrency(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        var cleaned = s.Replace("\"", "").Replace(",", "").Replace(" ", "").Trim();
        if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            return val;
        return 0;
    }

    private double? ParseRV(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        var cleaned = s.Replace("\"", "").Trim();
        if (cleaned.EndsWith("%"))
        {
            if (double.TryParse(cleaned.TrimEnd('%'), NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                return val / 100.0;
        }
        if (cleaned == "N/A" || cleaned == "#N/A" || cleaned == "-" || string.IsNullOrEmpty(cleaned)) return null;
        return null;
    }

    public IEnumerable<string> GetVehicleClasses()
    {
        return _vehicles.Select(v => v.Class).Distinct().OrderBy(c => c);
    }

    public IEnumerable<Vehicle> GetVehiclesByClass(string vehicleClass)
    {
        return _vehicles.Where(v => string.Equals(v.Class, vehicleClass, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(v => v.ModelName);
    }

    private bool ModelNamesMatch(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        return string.Equals(
            a.Replace(" ", "").Replace("-", ""), 
            b.Replace(" ", "").Replace("-", ""), 
            StringComparison.OrdinalIgnoreCase);
    }
}