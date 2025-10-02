using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FinancialCalculator.WinUI3.Models
{
    // Transport DTOs for API calls (align with Go engines/types JSON contracts)

    public class CalculationRequestDto
    {
        [JsonPropertyName("deal")] public DealDto Deal { get; set; } = new();
        [JsonPropertyName("campaigns")] public List<CampaignDto> Campaigns { get; set; } = new();
        [JsonPropertyName("idc_items")] public List<IdcItemDto> IdcItems { get; set; } = new();
        [JsonPropertyName("parameter_set")] public Dictionary<string, object>? ParameterSet { get; set; }
        [JsonPropertyName("options")] public Dictionary<string, object> Options { get; set; } = new();
    }

    public class DealDto
    {
        [JsonPropertyName("market")] public string Market { get; set; } = "TH";
        [JsonPropertyName("currency")] public string Currency { get; set; } = "THB";
        [JsonPropertyName("product")] public string Product { get; set; } = "HP";
        [JsonPropertyName("price_ex_tax")] public double PriceExTax { get; set; }
        [JsonPropertyName("down_payment_amount")] public double DownPaymentAmount { get; set; }
        [JsonPropertyName("down_payment_percent")] public double DownPaymentPercent { get; set; }
        [JsonPropertyName("down_payment_locked")] public string DownPaymentLocked { get; set; } = "amount";
        [JsonPropertyName("financed_amount")] public double FinancedAmount { get; set; }
        [JsonPropertyName("term_months")] public int TermMonths { get; set; }
        [JsonPropertyName("balloon_percent")] public double BalloonPercent { get; set; }
        [JsonPropertyName("balloon_amount")] public double BalloonAmount { get; set; }
        [JsonPropertyName("timing")] public string Timing { get; set; } = "arrears";
        [JsonPropertyName("rate_mode")] public string RateMode { get; set; } = "fixed_rate";
        [JsonPropertyName("customer_nominal_rate")] public double CustomerNominalRate { get; set; }
        [JsonPropertyName("target_installment")] public double TargetInstallment { get; set; }
    }

    public class CampaignDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
        [JsonPropertyName("funder")] public string? Funder { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("parameters")] public Dictionary<string, object> Parameters { get; set; } = new();
        [JsonPropertyName("eligibility")] public Dictionary<string, object>? Eligibility { get; set; }
        [JsonPropertyName("stacking")] public int? Stacking { get; set; }
    }

    public class IdcItemDto
    {
        [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;
        [JsonPropertyName("amount")] public double Amount { get; set; }
        [JsonPropertyName("financed")] public bool Financed { get; set; }
        [JsonPropertyName("timing")] public string Timing { get; set; } = "upfront";
        [JsonPropertyName("is_revenue")] public bool IsRevenue { get; set; }
        [JsonPropertyName("is_cost")] public bool IsCost { get; set; } = true;
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    }

    public class CampaignSummariesRequestDto
    {
        [JsonPropertyName("deal")] public DealDto deal { get; set; } = new();
        [JsonPropertyName("state")] public DealStateDto state { get; set; } = new();
        [JsonPropertyName("campaigns")] public List<CampaignDto> campaigns { get; set; } = new();
    }

    public class DealStateDto
    {
        [JsonPropertyName("dealerCommission")] public DealerCommissionDto dealerCommission { get; set; } = new();
        [JsonPropertyName("idcOther")] public IDCOtherDto idcOther { get; set; } = new();
    }

    public class DealerCommissionDto
    {
        [JsonPropertyName("mode")] public string mode { get; set; } = "auto"; // auto|override
        [JsonPropertyName("pct")] public double? pct { get; set; }
        [JsonPropertyName("amt")] public double? amt { get; set; }
        [JsonPropertyName("resolvedAmt")] public double resolvedAmt { get; set; }
    }

    public class IDCOtherDto
    {
        [JsonPropertyName("value")] public double value { get; set; }
        [JsonPropertyName("userEdited")] public bool userEdited { get; set; }
    }

    public class CampaignSummaryDto
    {
        [JsonPropertyName("campaign_id")] public string CampaignId { get; set; } = "";
        [JsonPropertyName("campaign_type")] public string CampaignType { get; set; } = "";
        [JsonPropertyName("dealerCommissionAmt")] public double DealerCommissionAmt { get; set; }
        [JsonPropertyName("dealerCommissionPct")] public double DealerCommissionPct { get; set; }
    }

    public class CalculationResponseDto
    {
        public QuoteDto Quote { get; set; } = new();
        public List<CashflowRowDto> Schedule { get; set; } = new();
    }

    public class QuoteDto
    {
        public double MonthlyInstallment { get; set; }
        public double CustomerRateNominal { get; set; }
        public double CustomerRateEffective { get; set; }
        public double FinancedAmount { get; set; }
        public ProfitabilityDto Profitability { get; set; } = new();
        public List<CampaignAuditEntryDto> CampaignAudit { get; set; } = new();
    }

    public class CampaignAuditEntryDto
    {
        public string CampaignId { get; set; } = string.Empty;
        public string CampaignType { get; set; } = string.Empty;
        public bool Applied { get; set; }
        public double Impact { get; set; }
        public double T0Flow { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class ProfitabilityDto
    {
        public double AcquisitionRoRAC { get; set; }
    }

    public class CashflowRowDto
    {
        public int Period { get; set; }
        public double Principal { get; set; }
        public double Interest { get; set; }
        public double Fees { get; set; }
        public double Balance { get; set; }
        public double Cashflow { get; set; }
    }

    public class CampaignCatalogItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Funder { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class CommissionAutoResponseDto
    {
        public string Product { get; set; } = string.Empty;
        public double Percent { get; set; }
        public string PolicyVersion { get; set; } = string.Empty;
    }
}
