using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiClient _api = new ApiClient();

    [ObservableProperty] private string product = "HP";
    [ObservableProperty] private double priceExTax = 1000000;
    [ObservableProperty] private double downPaymentAmount = 200000;
    [ObservableProperty] private int termMonths = 36;
    [ObservableProperty] private double customerRatePct = 3.99;

    public ObservableCollection<CampaignSummaryViewModel> CampaignSummaries { get; } = new();

    [ObservableProperty] private CampaignSummaryViewModel? selectedCampaign;
    [ObservableProperty] private MetricsViewModel metrics = new();
    [ObservableProperty] private double subsidyBudget = 0;
    [ObservableProperty] private string status = "Ready";

    public IRelayCommand RecalculateCommand { get; }

    public MainViewModel()
    {
        RecalculateCommand = new AsyncRelayCommand(RecalculateAsync);
        _ = LoadSummariesAsync();
    }

    private readonly DebounceDispatcher _debounce = new();

    private async Task LoadSummariesAsync()
    {
        try
        {
            var deal = BuildDealFromInputs();
            var state = new DealStateDto
            {
                dealerCommission = new DealerCommissionDto { mode = "auto", resolvedAmt = 0 },
                idcOther = new IDCOtherDto { value = SubsidyBudget, userEdited = false }
            };
            var catalog = await _api.GetCampaignCatalogAsync();
            var req = new CampaignSummariesRequestDto { deal = deal, state = state, campaigns = catalog.Select(c => new CampaignDto { Id = c.Id, Type = c.Type, Funder = c.Funder, Description = c.Description, Parameters = c.Parameters }).ToList() };
            var rows = await _api.GetCampaignSummariesAsync(req);
            CampaignSummaries.Clear();
            foreach (var r in rows)
            {
                // For MVP, we can call calculate per row to show Monthly/Eff quickly. Optimize later with batch endpoint.
                var calcReq = new CalculationRequestDto { Deal = deal, Campaigns = new List<CampaignDto> { new CampaignDto { Id = r.CampaignId, Type = r.CampaignType } }, IdcItems = new(), Options = new() { ["derive_idc_from_cf"] = true } };
                var calcRes = await _api.CalculateAsync(calcReq);

                CampaignSummaries.Add(new CampaignSummaryViewModel
                {
                    CampaignId = r.CampaignId,
                    CampaignType = r.CampaignType,
                    Title = r.CampaignType,
                    Subtitle = $"Dealer Comm: {r.DealerCommissionPct:P2} ({r.DealerCommissionAmt:N0} THB)",
                    Monthly = calcRes.Quote.MonthlyInstallment.ToString("N0", CultureInfo.InvariantCulture),
                    Effective = (calcRes.Quote.CustomerRateEffective * 100).ToString("0.00%"),
                    Notes = "",
                });
            }
            Status = $"Loaded {CampaignSummaries.Count} options";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    private async Task RecalculateAsync()
    {
        try
        {
            Status = "Calculating...";
            var deal = BuildDealFromInputs();
            var req = new CalculationRequestDto
            {
                Deal = deal,
                Campaigns = new(), // keep MVP simple; we can later send selected campaign
                IdcItems = new(),
                Options = new() { ["derive_idc_from_cf"] = true },
            };
            var res = await _api.CalculateAsync(req);
            Metrics = new MetricsViewModel
            {
                MonthlyInstallment = res.Quote.MonthlyInstallment.ToString("N0", CultureInfo.InvariantCulture),
                NominalRate = (res.Quote.CustomerRateNominal * 100).ToString("0.00%"),
                EffectiveRate = (res.Quote.CustomerRateEffective * 100).ToString("0.00%"),
                FinancedAmount = res.Quote.FinancedAmount.ToString("N0", CultureInfo.InvariantCulture),
                RoRAC = (res.Quote.Profitability.AcquisitionRoRAC * 100).ToString("0.00%"),
            };
            Status = "Done";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }
    private DealDto BuildDealFromInputs()
    {
        return new DealDto
        {
            Product = Product,
            PriceExTax = PriceExTax,
            DownPaymentAmount = DownPaymentAmount,
            DownPaymentLocked = "amount",
            TermMonths = TermMonths,
            BalloonPercent = 0,
            BalloonAmount = 0,
            Timing = "arrears",
            RateMode = "fixed_rate",
            CustomerNominalRate = CustomerRatePct / 100.0,
        };
    }
}

public class MetricsViewModel : ObservableObject
{
    public string MonthlyInstallment { get; set; } = "";
    public string NominalRate { get; set; } = "";
    public string EffectiveRate { get; set; } = "";
    public string FinancedAmount { get; set; } = "";
    public string RoRAC { get; set; } = "";
}

public class CampaignSummaryViewModel : ObservableObject
{
    public string CampaignId { get; set; } = string.Empty;
    public string CampaignType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Monthly { get; set; } = string.Empty;
    public string Effective { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

// DTOs align with engines/types for JSON transport
public class CalculationRequestDto
{
    public DealDto Deal { get; set; } = new();
    public List<CampaignDto> Campaigns { get; set; } = new();
    public List<IdcItemDto> IdcItems { get; set; } = new();
    public Dictionary<string, object> Options { get; set; } = new();
}

public class DealDto
{
    public string Market { get; set; } = "TH";
    public string Currency { get; set; } = "THB";
    public string Product { get; set; } = "HP";
    public double PriceExTax { get; set; }
    public double DownPaymentAmount { get; set; }
    public double DownPaymentPercent { get; set; }
    public string DownPaymentLocked { get; set; } = "amount";
    public double FinancedAmount { get; set; }
    public int TermMonths { get; set; }
    public double BalloonPercent { get; set; }
    public double BalloonAmount { get; set; }
    public string Timing { get; set; } = "arrears";
    public string RateMode { get; set; } = "fixed_rate";
    public double CustomerNominalRate { get; set; }
    public double TargetInstallment { get; set; }
}

public class CampaignDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Funder { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class IdcItemDto
{
    public string Category { get; set; } = string.Empty;
    public double Amount { get; set; }
    public bool Financed { get; set; }
    public string Timing { get; set; } = "upfront";
    public bool IsRevenue { get; set; }
    public bool IsCost { get; set; } = true;
    public string Description { get; set; } = string.Empty;
}

public class CampaignSummariesRequestDto
{
    public DealDto deal { get; set; } = new();
    public DealStateDto state { get; set; } = new();
    public List<CampaignDto> campaigns { get; set; } = new();
}

public class DealStateDto
{
    public DealerCommissionDto dealerCommission { get; set; } = new();
    public IDCOtherDto idcOther { get; set; } = new();
}

public class DealerCommissionDto { public string mode { get; set; } = "auto"; public double? pct { get; set; } public double? amt { get; set; } public double resolvedAmt { get; set; } }
public class IDCOtherDto { public double value { get; set; } public bool userEdited { get; set; } }

public class CampaignSummaryDto { public string CampaignId { get; set; } = ""; public string CampaignType { get; set; } = ""; public double DealerCommissionAmt { get; set; } public double DealerCommissionPct { get; set; } }

public class CalculationResponseDto
{
    public QuoteDto Quote { get; set; } = new();
}

public class QuoteDto
{
    public double MonthlyInstallment { get; set; }
    public double CustomerRateNominal { get; set; }
    public double CustomerRateEffective { get; set; }
    public double FinancedAmount { get; set; }
    public ProfitabilityDto Profitability { get; set; } = new();
}

public class ProfitabilityDto
{
    public double AcquisitionRoRAC { get; set; }
}

public class CampaignCatalogItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Funder { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ApiClient
{
    private readonly HttpClient _http = new HttpClient { BaseAddress = new Uri(Environment.GetEnvironmentVariable("FC_API_BASE") ?? "http://localhost:8123/") };

    public async Task<List<CampaignCatalogItemDto>> GetCampaignCatalogAsync()
    {
        var resp = await _http.GetAsync("api/v1/campaigns/catalog");
        resp.EnsureSuccessStatusCode();
        var stream = await resp.Content.ReadAsStreamAsync();
        var items = await JsonSerializer.DeserializeAsync<List<CampaignCatalogItemDto>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return items ?? new();
    }

    public async Task<List<CampaignSummaryDto>> GetCampaignSummariesAsync(CampaignSummariesRequestDto req)
    {
        var json = JsonSerializer.Serialize(req);
        var resp = await _http.PostAsync("api/v1/campaigns/summaries", new StringContent(json, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var stream = await resp.Content.ReadAsStreamAsync();
        var rows = await JsonSerializer.DeserializeAsync<List<CampaignSummaryDto>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return rows ?? new();
    }

    public async Task<CalculationResponseDto> CalculateAsync(CalculationRequestDto req)
    {
        var json = JsonSerializer.Serialize(req);
        var resp = await _http.PostAsync("api/v1/calculate", new StringContent(json, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var stream = await resp.Content.ReadAsStreamAsync();
        var node = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
        var quote = node.GetProperty("quote");
        var result = new CalculationResponseDto
        {
            Quote = new QuoteDto
            {
                MonthlyInstallment = quote.GetProperty("monthly_installment").GetDouble(),
                CustomerRateNominal = quote.GetProperty("customer_rate_nominal").GetDouble(),
                CustomerRateEffective = quote.GetProperty("customer_rate_effective").GetDouble(),
                FinancedAmount = quote.GetProperty("schedule").EnumerateArray().FirstOrDefault().GetProperty("balance").GetDouble(),
                Profitability = new ProfitabilityDto
                {
                    AcquisitionRoRAC = quote.GetProperty("profitability").GetProperty("acquisition_rorac").GetDouble()
                }
            }
        };
        return result;
    }
}
