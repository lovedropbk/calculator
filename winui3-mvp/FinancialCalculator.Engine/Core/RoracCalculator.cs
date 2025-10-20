using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.Engine.Core;

public static class RoracCalculator
{
    public sealed class CofParams
    {
        public IReadOnlyDictionary<int, decimal> Curve { get; init; } = new Dictionary<int, decimal>();
        public decimal Spread { get; init; } = 0.0025m; // 25 bps
        public decimal OpexPct { get; init; } = -0.0095m; // -0.95%
        public decimal EconCapRatio { get; init; } = 0.088m; // 8.8%
        public decimal CostOfRisk { get; init; } = 0.0025m; // 0.25%
        public decimal CapitalAdvantage { get; init; } = 0m; // Can be set if needed
    }

    public static Profitability Compute(CalculatorOutputs deal, CofParams p)
    {
        var term = deal.Inputs.TermMonths;
        var mfr = NearestCurveRate(p.Curve, term);
        var mfs = p.Spread;
        var opex = p.OpexPct;

        // Calculate IDC and Subsidy impacts using Deal IRR difference method
        var financedAmount = deal.FinancedAmount;
        var vehiclePrice = deal.Inputs.VehicleSalesPrice;
        
        // Deal IRRs are already calculated in FinancialCalculator:
        // - DealIrrAnnualPercent: IRR with all costs and subsidies
        // - DealIrrAnnualPercentWithoutUpfrontIncomes: IRR without subsidies
        // - DealIrrAnnualPercentWithoutUpfrontCosts: IRR without IDCs
        
        var dealIrrEffective = deal.DealIrrAnnualPercent / 100m; // IRR with everything
        var dealIrrBaseline = deal.DealIrrAnnualPercentBaseline / 100m; // IRR without subsidies AND without IDCs
        var dealIrrWithIdcOnly = deal.DealIrrAnnualPercentWithoutUpfrontIncomes / 100m; // IRR with IDCs, without subsidies
        var dealIrrWithSubsidyOnly = deal.DealIrrAnnualPercentWithoutUpfrontCosts / 100m; // IRR with subsidies, without IDCs
        
        // IDC impact = difference between Baseline and IRR with IDCs (only)
        // This represents the true annualized cost of IDCs on the deal, isolated from subsidies
        var idcUpfrontPct = dealIrrBaseline - dealIrrWithIdcOnly;
        
        // Subsidy impact = difference between IRR with subsidies (only) and Baseline
        // This represents the true annualized benefit of subsidies on the deal, isolated from IDCs
        var subsidyUpfrontPct = dealIrrWithSubsidyOnly - dealIrrBaseline;
        
        // Make sure impacts are non-negative
        if (idcUpfrontPct < 0) idcUpfrontPct = 0m;
        if (subsidyUpfrontPct < 0) subsidyUpfrontPct = 0m;
        
        // Periodic fees/subsidies (already as annual percentage)
        // Positive PeriodicFeeAnnualPercent = income to lender = reduce IDC
        // Negative PeriodicFeeAnnualPercent = cost to lender = increase IDC
        var periodicFee = deal.Inputs.PeriodicFeeAnnualPercent / 100m;
        var idcPeriodicPct = periodicFee < 0 ? Math.Abs(periodicFee) : 0m; // If negative, it's a cost (IDC)
        var subsidyPeriodicPct = periodicFee > 0 ? periodicFee : 0m; // If positive, it's income (subsidy)

        // Start with customer rate INPUT (as annual fraction) - this is what the customer pays
        var customerRate = deal.Inputs.CustomerRatePercent / 100m;
        
        // Deal IRR Nominal = Customer Rate (what customer pays, before lender's perspective adjustments)
        var dealIrrNominal = customerRate;
        
        // Gross Interest Margin = Deal IRR Effective - Cost of Debt (MFR)
        var grossInterestMargin = dealIrrEffective - mfr;
        
        // Net Interest Margin = Gross Interest Margin - Matched Funding Spread
        var netInterestMargin = grossInterestMargin - mfs;
        
        // Net EBIT Margin = Net Interest Margin - Cost of Risk - OPEX - Capital Advantage
        // Note: OPEX is already negative, so we add it
        var netEbit = netInterestMargin - p.CostOfRisk + opex - p.CapitalAdvantage;
        
        // Acquisition RoRAC = Net EBIT Margin / Economic Capital
        var acqRorac = p.EconCapRatio > 0 ? netEbit / p.EconCapRatio : 0m;

        return new Profitability
        {
            DealIrrEffective = dealIrrEffective,
            DealIrrNominal = dealIrrNominal,
            CustomerRate = customerRate,  // This is the customer's input rate (e.g., 3.99%)
            IdcUpfrontAnnualizedPct = idcUpfrontPct,
            SubsidyUpfrontAnnualizedPct = subsidyUpfrontPct,
            IdcPeriodicPct = idcPeriodicPct,
            SubsidyPeriodicPct = subsidyPeriodicPct,
            MatchedFundingRate = mfr,
            MatchedFundingSpread = mfs,
            GrossInterestMargin = grossInterestMargin,
            NetInterestMargin = netInterestMargin,
            CostOfRisk = p.CostOfRisk,
            OpexPct = opex,
            CapitalAdvantage = p.CapitalAdvantage,
            NetEbitMargin = netEbit,
            AcquisitionRoRac = acqRorac
        };
    }

    private static decimal NearestCurveRate(IReadOnlyDictionary<int, decimal> curve, int termMonths)
    {
        if (curve.Count == 0) return 0m;
        var best = curve.First();
        var bestDiff = Math.Abs(best.Key - termMonths);
        foreach (var kv in curve)
        {
            var d = Math.Abs(kv.Key - termMonths);
            if (d < bestDiff) { best = kv; bestDiff = d; }
        }
        return best.Value;
    }
}
