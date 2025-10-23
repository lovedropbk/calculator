namespace FinancialCalculator.Engine.Models;

public sealed class Profitability
{
    // Starting point
    public decimal CustomerRate { get; init; } // Customer interest rate
    
    // IDC and Subsidy adjustments (annualized)
    public decimal IdcUpfrontAnnualizedPct { get; init; } // Annualized upfront IDC as percentage
    public decimal SubsidyUpfrontAnnualizedPct { get; init; } // Annualized upfront subsidy as percentage
    
    // Additional periodic IDC and subsidies (for more granular breakdown)
    public decimal IdcPeriodicPct { get; init; } // Periodic IDC as percentage (ongoing)
    public decimal SubsidyPeriodicPct { get; init; } // Periodic subsidy as percentage (ongoing)
    
    // Deal IRR (Customer Rate - IDCs + Subsidies)
    public decimal DealIrrEffective { get; init; } // Effective IRR after all adjustments
    public decimal DealIrrNominal { get; init; } // Nominal IRR (before certain adjustments)
    
    // Cost of funds
    public decimal MatchedFundingRate { get; init; } // MFR
    public decimal MatchedFundingSpread { get; init; } // MFS
    
    // Margins
    public decimal GrossInterestMargin { get; init; } // Deal IRR - MFR
    public decimal NetInterestMargin { get; init; } // Gross - MFS
    
    // Other costs
    public decimal CostOfRisk { get; init; } // Credit risk cost
    public decimal OpexPct { get; init; } // Operating expenses
    public decimal CapitalAdvantage { get; init; } // Capital advantage/disadvantage
    
    // Final results
    public decimal NetEbitMargin { get; init; } // Net Interest Margin - CostOfRisk - OPEX - CapitalAdvantage
    public decimal EconomicCapitalRatio { get; init; } // Economic Capital as ratio of financed amount (approx)
    public decimal AcquisitionRoRac { get; init; } // NetEbitMargin / EconomicCapital
}
