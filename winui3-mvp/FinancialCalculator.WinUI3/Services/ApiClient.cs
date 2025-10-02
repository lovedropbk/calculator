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

            // Basic numbers
            result.Quote.MonthlyInstallment = quoteElem.GetProperty("monthly_installment").GetDouble();
            result.Quote.CustomerRateNominal = quoteElem.GetProperty("customer_rate_nominal").GetDouble();
            result.Quote.CustomerRateEffective = quoteElem.GetProperty("customer_rate_effective").GetDouble();
            if (quoteElem.TryGetProperty("profitability", out var prof))
            {
                result.Quote.Profitability = new ProfitabilityDto
                {
                    AcquisitionRoRAC = prof.GetProperty("acquisition_rorac").GetDouble()
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
                    double GetOr0(string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0.0;
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
    }
}
