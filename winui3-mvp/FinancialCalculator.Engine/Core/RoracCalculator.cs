using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.Engine.Core;

public static class RoracCalculator
{
    public sealed class CofParams
    {
        public IReadOnlyDictionary<int, decimal> Curve { get; init; } = new Dictionary<int, decimal>();
        public decimal Spread { get; init; } = 0.0025m; // 25 bps
        public decimal OpexPct { get; init; } = -0.0095m; // -0.95%
        public decimal EconCapRatio { get; init; } = 0.08m; // 8%
    }

    public static Profitability Compute(CalculatorOutputs deal, CofParams p)
    {
        var term = deal.Inputs.TermMonths;
        var mfr = NearestCurveRate(p.Curve, term);
        var mfs = p.Spread;
        var opex = p.OpexPct;

        // IRR proxy: use nominal APR from inputs (deal.Calculate() returns annual percent)
        var irr = deal.DealIrrAnnualPercent / 100m; // convert % to fraction

        var netEbit = irr - (mfr + mfs) + opex; // opex negative reduces margin
        var acqRorac = p.EconCapRatio > 0 ? netEbit / p.EconCapRatio : 0m;

        return new Profitability
        {
            DealIrrEffective = irr,
            MatchedFundingRate = mfr,
            MatchedFundingSpread = mfs,
            OpexPct = opex,
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
