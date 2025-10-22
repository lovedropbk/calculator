using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FinancialCalculator.Engine.Core;

public class RiskParameterRepository
{
    private readonly Dictionary<(string, string), double> _pdTable = new();
    private readonly Dictionary<(string, string, string), (double DcfLgd, double DownturnLgd)> _lgdTable = new();
    private double _ecTotal = 0.088;

    public bool IsLoaded { get; private set; }

    public void Load(string parametersPath)
    {
        try
        {
            LoadPd(Path.Combine(parametersPath, "PD.csv"));
            LoadLgd(Path.Combine(parametersPath, "LGD_OneEC.csv"));
            LoadEcTotal(Path.Combine(parametersPath, "EC_TOTAL.csv"));
            IsLoaded = true;
        }
        catch (Exception ex) { Console.WriteLine($"Failed to load risk parameters: {ex.Message}"); }
    }

    public double GetPd(string customerType, string rating) => _pdTable.TryGetValue((customerType, rating), out var pd) ? pd : 0.0025;
    public (double DcfLgd, double DownturnLgd) GetLgd(string customerType, string assetState, string avc)
    {
        if (_lgdTable.TryGetValue((customerType, assetState, avc), out var lgd)) return lgd;
        if (_lgdTable.TryGetValue((customerType, assetState, "*"), out lgd)) return lgd;
        return (0.45, 0.45);
    }
    public double GetEcTotal() => _ecTotal;

    private void LoadPd(string path)
    {
        if (!File.Exists(path)) return;
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var parts = SplitCsvLine(line);
            if (parts.Length <= 15) continue;
            var custTypeRaw = parts.ElementAt(2);
            var rating = parts.ElementAt(13).Trim();
            if (double.TryParse(parts.ElementAt(15), NumberStyles.Any, CultureInfo.InvariantCulture, out var pd))
            {
                foreach (var ct in custTypeRaw.Split(',')) _pdTable[(ct.Trim(), rating)] = pd;
            }
        }
    }

    private void LoadLgd(string path)
    {
        if (!File.Exists(path)) return;
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var parts = SplitCsvLine(line);
            if (parts.Length <= 14) continue;
            var assetStatesRaw = parts.ElementAt(3);
            var custType = parts.ElementAt(4).Trim();
            var avcsRaw = parts.ElementAt(5);
            if (double.TryParse(parts.ElementAt(13), NumberStyles.Any, CultureInfo.InvariantCulture, out var dcfLgd) && double.TryParse(parts.ElementAt(14), NumberStyles.Any, CultureInfo.InvariantCulture, out var downturnLgd))
            {
                foreach (var aState in assetStatesRaw.Split(','))
                    foreach (var avc in avcsRaw.Split(','))
                        _lgdTable[(custType, aState.Trim(), string.IsNullOrEmpty(avc.Trim()) ? "*" : avc.Trim())] = (dcfLgd, downturnLgd);
            }
        }
    }

    private void LoadEcTotal(string path)
    {
        if (!File.Exists(path)) return;
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var parts = SplitCsvLine(line);
            if (parts.Length >= 2 && double.TryParse(parts.ElementAt(1), NumberStyles.Any, CultureInfo.InvariantCulture, out var ec)) { _ecTotal = ec; break; }
        }
    }

    private static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        int start = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') inQuotes = !inQuotes;
            else if (line[i] == ',' && !inQuotes) { result.Add(StripQuotes(line.Substring(start, i - start))); start = i + 1; }
        }
        result.Add(StripQuotes(line.Substring(start)));
        return result.ToArray();
    }
    private static string StripQuotes(string s) { s = s.Trim(); return (s.StartsWith("\"") && s.EndsWith("\"")) ? s.Substring(1, s.Length - 2).Replace("\"\"", "\"") : s; }
}