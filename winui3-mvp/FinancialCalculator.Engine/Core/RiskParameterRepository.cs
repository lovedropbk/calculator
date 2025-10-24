using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace FinancialCalculator.Engine.Core;

public class RiskParameterRepository : IRiskParameterRepository
{
    private readonly IFileService _fileService;
    private string _parametersPath = string.Empty;

    private Dictionary<(string, string), double>? _pdTable;
    private Dictionary<(string, string, string), (double DcfLgd, double DownturnLgd)>? _lgdTable;
    private double? _ecTotal;

    public bool IsInitialized { get; private set; }

    public RiskParameterRepository(IFileService fileService)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    }

    public void Initialize(string parametersPath)
    {
        _parametersPath = parametersPath;
        IsInitialized = true;
    }

    // Kept for backward compatibility if needed, but now just sets the path.
    // Tables are loaded on-demand.
    public async Task LoadAsync(string parametersPath)
    {
        Initialize(parametersPath);
        await EnsurePdLoadedAsync();
        await EnsureLgdLoadedAsync();
        await EnsureEcTotalLoadedAsync();
    }

    public async Task<double> GetPdAsync(string customerType, string rating)
    {
        await EnsurePdLoadedAsync();
        return _pdTable!.TryGetValue((customerType, rating), out var pd) ? pd : 0.0025;
    }

    // Synchronous version for current Engine compatibility, blocks if not loaded.
    // Ideally Engine becomes async, but for now we might need this.
    public double GetPd(string customerType, string rating)
    {
        if (_pdTable == null) throw new InvalidOperationException("PD Table not initialized. Call LoadAsync first.");
        return _pdTable!.TryGetValue((customerType, rating), out var pd) ? pd : 0.0025;
    }

    public async Task<(double DcfLgd, double DownturnLgd)> GetLgdAsync(string customerType, string assetState, string avc)
    {
        await EnsureLgdLoadedAsync();
        return GetLgdInternal(customerType, assetState, avc);
    }

    public (double DcfLgd, double DownturnLgd) GetLgd(string customerType, string assetState, string avc)
    {
         if (_lgdTable == null) throw new InvalidOperationException("LGD Table not initialized. Call LoadAsync first.");
         return GetLgdInternal(customerType, assetState, avc);
    }

    private (double DcfLgd, double DownturnLgd) GetLgdInternal(string customerType, string assetState, string avc)
    {
        if (_lgdTable!.TryGetValue((customerType, assetState, avc), out var lgd)) return lgd;
        if (_lgdTable!.TryGetValue((customerType, assetState, "*"), out lgd)) return lgd;
        return (0.45, 0.45);
    }

    public async Task<double> GetEcTotalAsync()
    {
        await EnsureEcTotalLoadedAsync();
        return _ecTotal!.Value;
    }

    public double GetEcTotal()
    {
        if (_ecTotal == null) throw new InvalidOperationException("EC Total not initialized. Call LoadAsync first.");
        return _ecTotal!.Value;
    }

    private async Task EnsurePdLoadedAsync()
    {
        if (_pdTable != null) return;
        _pdTable = new Dictionary<(string, string), double>();
        var path = Path.Combine(_parametersPath, "PD.csv");
        if (!_fileService.Exists(path)) return;

        using var reader = _fileService.OpenText(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
        
        // Manual reading to handle the splitting of customer types and specific column indices
        // Assuming the old parser's index usage:
        // parts.ElementAt(2) -> CustTypeRaw
        // parts.ElementAt(13) -> Rating
        // parts.ElementAt(15) -> PD
        
        // We can try to read by header if we know them, or by index.
        // Let's stick to index for robustness against header name changes if we aren't sure.
        // Actually, CsvHelper is good with headers. Let's assume standard headers exist if skipping 1 line worked.
        // But for safety and exact replication of previous logic, let's use index.
        // CsvHelper doesn't easily support "split by comma then take element at index" without custom mapping,
        // but we can read it as `dynamic` or `string[]` if we want.
        // Reading as `string[]` gives us behavior closest to `SplitCsvLine`.

        // Reloading with No Header to use indices safely if we want full control, 
        // OR we use standard reading and assume the CSV is well-formed.
        // Previous code: `lines.Skip(1)` -> implies header.
        
        await csv.ReadAsync(); // Skip header manually if we don't use HasHeaderRecord=true and read matches.
        csv.ReadHeader(); // Read header row

        while (await csv.ReadAsync())
        {
            // Using TryGetField with index to be safe against weird rows
            if (!csv.TryGetField<string>(2, out var custTypeRaw) || string.IsNullOrWhiteSpace(custTypeRaw)) continue;
            if (!csv.TryGetField<string>(13, out var rating) || rating == null) continue;
            if (!csv.TryGetField<double>(15, out var pd)) continue;

            rating = rating.Trim();
            foreach (var ct in custTypeRaw.Split(','))
            {
                _pdTable[(ct.Trim(), rating)] = pd;
            }
        }
    }

    private async Task EnsureLgdLoadedAsync()
    {
        if (_lgdTable != null) return;
        _lgdTable = new Dictionary<(string, string, string), (double, double)>();
        var path = Path.Combine(_parametersPath, "LGD_OneEC.csv");
        if (!_fileService.Exists(path)) return;

        using var reader = _fileService.OpenText(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            // parts.ElementAt(3) -> assetStatesRaw
            // parts.ElementAt(4) -> custType
            // parts.ElementAt(5) -> avcsRaw
            // parts.ElementAt(13) -> dcfLgd
            // parts.ElementAt(14) -> downturnLgd
             if (!csv.TryGetField<string>(3, out var assetStatesRaw) || assetStatesRaw == null) continue;
             if (!csv.TryGetField<string>(4, out var custType) || custType == null) continue;
             if (!csv.TryGetField<string>(5, out var avcsRaw) || avcsRaw == null) continue;
             if (!csv.TryGetField<double>(13, out var dcfLgd)) continue;
             if (!csv.TryGetField<double>(14, out var downturnLgd)) continue;

             custType = custType.Trim();
             foreach (var aState in assetStatesRaw.Split(','))
             {
                 foreach (var avc in avcsRaw.Split(','))
                 {
                     _lgdTable[(custType, aState.Trim(), string.IsNullOrEmpty(avc.Trim()) ? "*" : avc.Trim())] = (dcfLgd, downturnLgd);
                 }
             }
        }
    }

    private async Task EnsureEcTotalLoadedAsync()
    {
        if (_ecTotal != null) return;
        _ecTotal = 0.088; // Default
        var path = Path.Combine(_parametersPath, "EC_TOTAL.csv");
        if (!_fileService.Exists(path)) return;

        // EC_TOTAL seems simple, just read first data line, col 1.
        using var reader = _fileService.OpenText(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
        
        if (await csv.ReadAsync())
        {
             // parts.ElementAt(1) -> ec
             if (csv.TryGetField<double>(1, out var ec))
             {
                 _ecTotal = ec;
             }
        }
    }
}