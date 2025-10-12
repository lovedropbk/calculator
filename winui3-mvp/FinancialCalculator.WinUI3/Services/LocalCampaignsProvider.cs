using System.Collections.Generic;

namespace FinancialCalculator.WinUI3.Services;

public sealed class LocalCampaignsProvider
{
    public sealed class Campaign
    {
        public string Id { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty; // subdown | cash_discount | free_insurance | free_mbsp | subinterest
        public double? SubsidyPercent { get; init; }
        public double? SubsidyAmount { get; init; }
        public double? DiscountPercent { get; init; }
        public double? DiscountAmount { get; init; }
        public double? InsuranceCost { get; init; }
        public double? MbspCost { get; init; }
        public double? TargetRate { get; init; }
    }

    public IReadOnlyList<Campaign> GetStandard()
    {
        // Default set mirrors catalog.json
        return new List<Campaign>
        {
            new() { Id="SUBDOWN-5", Type="subdown", SubsidyPercent=0.05 },
            new() { Id="SUBINT-299", Type="subinterest", TargetRate=0.0299 },
            new() { Id="FREE-INS", Type="free_insurance", InsuranceCost=15000 },
            new() { Id="FREE-MBSP", Type="free_mbsp", MbspCost=5000 },
            new() { Id="CASH-DISC-2", Type="cash_discount", DiscountPercent=0.02 },
        };
    }
}
