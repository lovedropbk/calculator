using System.Collections.Generic;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.Engine.Models.Facade;

public record class ScenarioRequest
{
    public string Market { get; init; } = "TH";
    public string Product { get; init; } = "HP";
    public string Timing { get; init; } = "arrears";
    public int TermMonths { get; init; }
    public decimal VehiclePrice { get; init; }
    public decimal AdditionalFinancedItems { get; init; }
    public bool DownIsPercent { get; init; }
    public decimal DownValue { get; init; }
    public bool BalloonIsPercent { get; init; }
    public decimal BalloonValue { get; init; }
    public decimal CustomerRatePercent { get; init; }
    public decimal UpfrontSubsidies { get; init; }
    public decimal UpfrontCosts { get; init; }
    public bool SubdownIsPercent { get; init; }
    public decimal SubdownValue { get; init; }
    
    // Risk Inputs
    public string CustomerType { get; init; } = "RETAIL PRIVATE";
    public string AssetState { get; init; } = "N";
    public string AssetValuationCurve { get; init; } = "MBPC";
    public string Rating { get; init; } = "4.0";
    public IReadOnlyList<PaymentHolidayRule> PaymentHolidays { get; init; } = new List<PaymentHolidayRule>();
}

public record class ScenarioResult
{
    public decimal MonthlyInstallment { get; init; }
    public decimal FlatRatePercent { get; init; }
    public decimal FinancedAmount { get; init; }
    public decimal AcquisitionRoRacPercent { get; init; }
    public decimal DealIrrEffectivePercent { get; init; }
    public decimal TotalInterest { get; init; }
    public decimal TotalPrincipal { get; init; }

    public IReadOnlyList<CashflowRow> Schedule { get; init; } = new List<CashflowRow>();
    public ProfitabilityDetails Profitability { get; init; } = new();
}

public record class CashflowRow
{
    public int Period { get; init; }
    public decimal Principal { get; init; }
    public decimal Interest { get; init; }
    public decimal Balance { get; init; }
    public decimal Cashflow { get; init; }

    // Extended annotations for structured cashflows
    public FinancialCalculator.Engine.Models.PaymentKind PaymentKind { get; init; }
    public decimal CapitalizedInterest { get; init; }
    public string? RuleId { get; init; }
}

public record class ProfitabilityDetails
{
    public decimal CustomerRatePercent { get; init; }
    public decimal DealIrrEffectivePercent { get; init; }
    public decimal DealIrrNominalPercent { get; init; }
    public decimal CostOfDebtMatchedPercent { get; init; }
    public decimal MatchedFundingSpreadPercent { get; init; }
    public decimal GrossInterestMarginPercent { get; init; }
    public decimal NetInterestMarginPercent { get; init; }
    public decimal CostOfCreditRiskPercent { get; init; }
    public decimal OpexPercent { get; init; }
    public decimal CapitalAdvantagePercent { get; init; }
    public decimal NetEbitMarginPercent { get; init; }
    public decimal EconomicCapitalPercent { get; init; }
    
    public decimal IdcUpfrontAnnualizedPercent { get; init; }
    public decimal SubsidyUpfrontAnnualizedPercent { get; init; }
    
    public decimal IdcPeriodicPercent { get; init; }
    public decimal SubsidyPeriodicPercent { get; init; }
}

public enum GoalSeekVariable
{
    CustomerNominalRate,
    DownPaymentAmount,
    VehiclePrice,
    UpfrontSubsidy
}

public enum GoalSeekMetric
{
    MonthlyInstallment,
    RoRAC
}