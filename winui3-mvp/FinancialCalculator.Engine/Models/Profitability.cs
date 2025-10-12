namespace FinancialCalculator.Engine.Models;

public sealed class Profitability
{
    public decimal DealIrrEffective { get; init; } // use annual nominal as proxy here
    public decimal MatchedFundingRate { get; init; } // MFR
    public decimal MatchedFundingSpread { get; init; } // MFS
    public decimal OpexPct { get; init; }
    public decimal NetEbitMargin { get; init; }
    public decimal AcquisitionRoRac { get; init; }
}
