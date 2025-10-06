using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FinancialCalculator.WinUI3.Models;

namespace FinancialCalculator.WinUI3.Services
{
    public sealed class ApiClient
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public ApiClient()
        {
            var baseUrl = Environment.GetEnvironmentVariable("FC_API_BASE") ?? "http://localhost:8123/";
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<List<CampaignCatalogItemDto>> GetCampaignCatalogAsync()
        {
            var resp = await _http.GetAsync("api/v1/campaigns/catalog");
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync();
            var items = await JsonSerializer.DeserializeAsync<List<CampaignCatalogItemDto>>(stream, _json);
            return items ?? new();
        }

        public async Task<List<CampaignSummaryDto>> GetCampaignSummariesAsync(CampaignSummariesRequestDto req)
        {
            var json = JsonSerializer.Serialize(req);
            var resp = await _http.PostAsync("api/v1/campaigns/summaries", new StringContent(json, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync();
            var rows = await JsonSerializer.DeserializeAsync<List<CampaignSummaryDto>>(stream, _json);
            return rows ?? new();
        }

        public async Task<CalculationResponseDto> CalculateAsync(CalculationRequestDto req)
        {
            var json = JsonSerializer.Serialize(req);
            var resp = await _http.PostAsync("api/v1/calculate", new StringContent(json, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync();
            var node = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
            var quoteElem = node.GetProperty("quote");

            var result = new CalculationResponseDto
            {
                Quote = new QuoteDto(),
                Schedule = new List<CashflowRowDto>()
            };

            // Basic numbers - handle both string and number types from backend
            double GetDoubleOrParse(JsonElement elem, string propName)
            {
                if (!elem.TryGetProperty(propName, out var prop)) return 0.0;
                if (prop.ValueKind == JsonValueKind.Number) return prop.GetDouble();
                if (prop.ValueKind == JsonValueKind.String)
                {
                    var str = prop.GetString();
                    if (double.TryParse(str, out var val)) return val;
                }
                return 0.0;
            }
            
            result.Quote.MonthlyInstallment = GetDoubleOrParse(quoteElem, "monthly_installment");
            result.Quote.CustomerRateNominal = GetDoubleOrParse(quoteElem, "customer_rate_nominal");
            result.Quote.CustomerRateEffective = GetDoubleOrParse(quoteElem, "customer_rate_effective");
            if (quoteElem.TryGetProperty("profitability", out var prof))
            {
                double GetProfOrZero(string name) => prof.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0.0;

                result.Quote.Profitability = new ProfitabilityDto
                {
                    DealIRREffective       = GetProfOrZero("deal_irr_effective"),
                    DealIRRNominal         = GetProfOrZero("deal_irr_nominal"),
                    CostOfDebtMatched      = GetProfOrZero("cost_of_debt_matched"),
                    MatchedFundedSpread    = GetProfOrZero("matched_funded_spread"),
                    GrossInterestMargin    = GetProfOrZero("gross_interest_margin"),
                    CapitalAdvantage       = GetProfOrZero("capital_advantage"),
                    NetInterestMargin      = GetProfOrZero("net_interest_margin"),
                    CostOfCreditRisk       = GetProfOrZero("cost_of_credit_risk"),
                    OPEX                   = GetProfOrZero("opex"),
                    IDCSubsidiesFeesUpfront  = GetProfOrZero("idc_subsidies_fees_upfront"),
                    IDCSubsidiesFeesPeriodic = GetProfOrZero("idc_subsidies_fees_periodic"),
                    
                    // Safely extract the new separated IDC/Subsidy fields
                    // These fields may not exist in older responses, so we default to 0
                    IDCUpfrontCostPct      = prof.TryGetProperty("idc_upfront_cost_pct", out var iucPct) && iucPct.ValueKind == JsonValueKind.Number ? iucPct.GetDouble() : 0.0,
                    IDCPeriodicCostPct     = prof.TryGetProperty("idc_periodic_cost_pct", out var ipcPct) && ipcPct.ValueKind == JsonValueKind.Number ? ipcPct.GetDouble() : 0.0,
                    SubsidyUpfrontPct      = prof.TryGetProperty("subsidy_upfront_pct", out var suPct) && suPct.ValueKind == JsonValueKind.Number ? suPct.GetDouble() : 0.0,
                    SubsidyPeriodicPct     = prof.TryGetProperty("subsidy_periodic_pct", out var spPct) && spPct.ValueKind == JsonValueKind.Number ? spPct.GetDouble() : 0.0,
                    
                    NetEBITMargin          = GetProfOrZero("net_ebit_margin"),
                    EconomicCapital        = GetProfOrZero("economic_capital"),
                    AcquisitionRoRAC       = GetProfOrZero("acquisition_rorac"),
                };
            }

            // Campaign audit for detailed UI columns
            var audits = new List<CampaignAuditEntryDto>();
            if (quoteElem.TryGetProperty("campaign_audit", out var auditElem) && auditElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in auditElem.EnumerateArray())
                {
                    audits.Add(new CampaignAuditEntryDto
                    {
                        CampaignId = e.GetProperty("campaign_id").GetString() ?? string.Empty,
                        CampaignType = e.GetProperty("campaign_type").GetString() ?? string.Empty,
                        Applied = e.TryGetProperty("applied", out var ap) && ap.GetBoolean(),
                        Impact = e.TryGetProperty("impact", out var imp) && imp.ValueKind == JsonValueKind.Number ? imp.GetDouble() : 0.0,
                        T0Flow = e.TryGetProperty("t0_flow", out var t0) && t0.ValueKind == JsonValueKind.Number ? t0.GetDouble() : 0.0,
                        Description = e.TryGetProperty("description", out var d) ? d.GetString() ?? string.Empty : string.Empty
                    });
                }
            }
            result.Quote.CampaignAudit = audits;

            // Schedule (map flexible names)
            if (quoteElem.TryGetProperty("schedule", out var scheduleElem) && scheduleElem.ValueKind == JsonValueKind.Array)
            {
                int period = 0;
                foreach (var e in scheduleElem.EnumerateArray())
                {
                    period++;
                    double GetOr0(string name)
                    {
                        if (!e.TryGetProperty(name, out var v)) return 0.0;
                        if (v.ValueKind == JsonValueKind.Number) return v.GetDouble();
                        if (v.ValueKind == JsonValueKind.String)
                        {
                            var str = v.GetString();
                            if (double.TryParse(str, out var val)) return val;
                        }
                        return 0.0;
                    }
                    var total = GetOr0("amount");
                    if (Math.Abs(total) < 1e-12) total = GetOr0("cashflow");
                    var feesVal = GetOr0("fee");
                    if (Math.Abs(feesVal) < 1e-12) feesVal = GetOr0("fees");

                    result.Schedule.Add(new CashflowRowDto
                    {
                        Period = period,
                        Principal = GetOr0("principal"),
                        Interest = GetOr0("interest"),
                        Fees = feesVal,
                        Balance = GetOr0("balance"),
                        Cashflow = total,
                    });
                }
            }

            // Financed amount proxy: first schedule balance if present
            if (result.Schedule.Count > 0)
            {
                result.Quote.FinancedAmount = result.Schedule[0].Balance;
            }

            return result;
        }

        public async Task<CommissionAutoResponseDto> GetCommissionAutoAsync(string product)
        {
            var resp = await _http.GetAsync($"api/v1/commission/auto?product={Uri.EscapeDataString(product ?? string.Empty)}");
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            var node = JsonSerializer.Deserialize<JsonElement>(json, _json);
            return new CommissionAutoResponseDto
            {
                Product = node.GetProperty("product").GetString() ?? string.Empty,
                Percent = node.GetProperty("percent").GetDouble(),
                PolicyVersion = node.TryGetProperty("policyVersion", out var pv) ? pv.GetString() ?? string.Empty : string.Empty
            };
        }

        public async Task<Dictionary<string, object>> GetCurrentParametersAsync()
        {
            var resp = await _http.GetAsync("api/v1/parameters/current");
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync();
            
            // Parse as JsonElement first to handle flexible parameter structure
            var jsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(stream, _json);
            
            // Convert JsonElement to Dictionary<string, object>
            var parameters = new Dictionary<string, object>();
            
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in jsonElement.EnumerateObject())
                {
                    parameters[property.Name] = ParseJsonElement(property.Value);
                }
            }
            
            return parameters;
        }

        private object ParseJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                        return intValue;
                    if (element.TryGetInt64(out var longValue))
                        return longValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null!;
                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ParseJsonElement(item));
                    }
                    return list;
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = ParseJsonElement(prop.Value);
                    }
                    return dict;
                default:
                    return null!;
            }
        }
    }
}
