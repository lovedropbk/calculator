using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        var lines = await File.ReadAllLinesAsync(path);
        // Header is on line 3 (index 2)
        if (lines.Length < 4) return;

        for (int i = 3; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(",")) continue; // Skip empty rows
            var parts = CsvParser.SplitCsvLine(line);
            if (parts.Length < 12) continue;

            var modelName = parts[1].Trim();
            if (string.IsNullOrEmpty(modelName) || modelName == "Models name") continue;

            var vehicle = new Vehicle
            {
                ModelName = modelName,
                Class = InferVehicleClass(modelName),
                MSRP = ParseCurrency(parts[4]),
                RV12 = ParseRV(parts[7]),
                RV24 = ParseRV(parts[8]),
                RV36 = ParseRV(parts[9]),
                RV48 = ParseRV(parts[10]),
                RV60 = ParseRV(parts[11])
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

        var lines = await File.ReadAllLinesAsync(path);
        if (lines.Length < 2) return;

        var headers = CsvParser.SplitCsvLine(lines[0]).Select(h => h.Trim()).ToArray();
        // Find indices for MBSP packages (starting from index 3 usually)
        var mbspIndices = new Dictionary<string, int>();
        for (int i = 3; i < headers.Length; i++)
        {
            if (!string.IsNullOrEmpty(headers[i]))
            {
                mbspIndices[headers[i]] = i;
                MbspPackages.Add(headers[i]);
            }
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(",")) continue;
            var parts = CsvParser.SplitCsvLine(line);
            
            // Ensure we have enough parts for the max index we need
            int maxIndexNeeded = mbspIndices.Values.Count > 0 ? mbspIndices.Values.Max() : 0;
            if (parts.Length <= maxIndexNeeded) continue;

            var modelName = parts[0].Trim();
            var vehicle = _vehicles.FirstOrDefault(v => ModelNamesMatch(v.ModelName, modelName));
            
            if (vehicle != null)
            {
                foreach (var kvp in mbspIndices)
                {
                    if (parts.Length > kvp.Value)
                    {
                        var cost = ParseCurrency(parts[kvp.Value]);
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

        // Fallback for dev environment (up from bin/Debug/net9.0...)
        path = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "winui3-mvp", "docs", filename));
        if (File.Exists(path)) return path;

        return Path.Combine(baseDir, filename);
    }

    private string InferVehicleClass(string modelName)
    {
        // Simple heuristic: first word after stripping sub-brands
        var cleaned = modelName.Replace("Mercedes-AMG ", "").Replace("Mercedes-Maybach ", "").Trim();
        var parts = cleaned.Split(' ');
        if (parts.Length > 0)
        {
            var prefix = parts[0];

            
            // Check if it starts with V and has digits (e.g. V250d)
            if (prefix.StartsWith("V") && prefix.Length > 1 && prefix.Skip(1).Any(char.IsDigit))
            {
                 return "V-Class";
            }

            if (prefix == "Sprinter" || prefix == "Vito") return prefix;
            
            // Generic fallback to append -Class for short prefixes
            if (prefix.Length <= 4) return prefix + "-Class";
        }
        return "Other";
    }

    private double ParseCurrency(string s)
    {
        // Remove quotes, spaces, commas, currency symbols if any
        var cleaned = s.Replace("\"", "").Replace(",", "").Replace(" ", "").Trim();
        if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            return val;
        return 0;
    }

    private double? ParseRV(string s)
    {
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