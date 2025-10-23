using System;
using System.Collections.Generic;
using System.Linq;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models.Facade;

namespace FinancialCalculator.Engine;

public class FinancialFacade
{
    private readonly DealEngine _dealEngine;
    private readonly GoalSeekEngine _goalSeekEngine;

    public FinancialFacade(IRiskParameterRepository riskRepo)
    {
        _dealEngine = new DealEngine(riskRepo);
        _goalSeekEngine = new GoalSeekEngine(_dealEngine);
    }

    // Alternative constructor if we want to inject DealEngine directly for testing,
    // though Facade is meant to hide it.
    internal FinancialFacade(DealEngine dealEngine)
    {
        _dealEngine = dealEngine;
        _goalSeekEngine = new GoalSeekEngine(_dealEngine);
    }

    public ScenarioResult Calculate(ScenarioRequest request)
    {
        var input = MapToDealInput(request);
        var output = _dealEngine.Calculate(input);
        return MapToScenarioResult(output);
    }

    public double GoalSeek(ScenarioRequest baseRequest, GoalSeekVariable variable, GoalSeekMetric metric, double targetValue)
    {
        var input = MapToDealInput(baseRequest);
        // Map facade enums to engine enums if needed, or just use engine enums in facade for now if they are public and simple.
        // GoalSeekEngine.GoalVariable and TargetMetric are public in Core.
        // Let's assume we want facade enums to decouple.
        return _goalSeekEngine.Seek(input, (GoalSeekEngine.GoalVariable)variable, (GoalSeekEngine.TargetMetric)metric, targetValue);
    }

    private static DealEngine.DealInput MapToDealInput(ScenarioRequest r)
    {
        return new DealEngine.DealInput
        {
            Market = r.Market,
            Product = r.Product,
            Timing = r.Timing,
            TermMonths = r.TermMonths,
            VehiclePrice = r.VehiclePrice,
            AdditionalFinancedItems = r.AdditionalFinancedItems,
            DownIsPercent = r.DownIsPercent,
            DownValue = r.DownValue,
            BalloonIsPercent = r.BalloonIsPercent,
            BalloonValue = r.BalloonValue,
            CustomerRatePercent = r.CustomerRatePercent,
            UpfrontSubsidies = r.UpfrontSubsidies,
            UpfrontCosts = r.UpfrontCosts,
            SubdownIsPercent = r.SubdownIsPercent,
            SubdownValue = r.SubdownValue,
            CustomerType = r.CustomerType,
            AssetState = r.AssetState,
            AssetValuationCurve = r.AssetValuationCurve,
            Rating = r.Rating
        };
    }

    private static ScenarioResult MapToScenarioResult(DealEngine.DealOutput o)
    {
        return new ScenarioResult
        {
            MonthlyInstallment = o.Deal.MonthlyRate,
            FlatRatePercent = o.Deal.FlatRatePercentPerAnnum,
            FinancedAmount = o.Deal.FinancedAmount,
            AcquisitionRoRacPercent = o.Profit.AcquisitionRoRac,
            DealIrrEffectivePercent = o.Profit.DealIrrEffective,
            TotalInterest = o.Deal.Schedule.Sum(r => r.Interest),
            TotalPrincipal = o.Deal.Schedule.Sum(r => r.Principal),
            Schedule = o.Deal.Schedule.Select(r => new CashflowRow
            {
                Period = r.Period,
                Principal = r.Principal,
                Interest = r.Interest,
                Balance = r.Balance,
                Cashflow = r.Cashflow
            }).ToList(),
            Profitability = new ProfitabilityDetails
            {
                CustomerRatePercent = o.Profit.CustomerRate,
                DealIrrEffectivePercent = o.Profit.DealIrrEffective,
                DealIrrNominalPercent = o.Profit.DealIrrNominal,
                CostOfDebtMatchedPercent = o.Profit.MatchedFundingRate,
                MatchedFundingSpreadPercent = o.Profit.MatchedFundingSpread,
                GrossInterestMarginPercent = o.Profit.GrossInterestMargin,
                NetInterestMarginPercent = o.Profit.NetInterestMargin,
                CostOfCreditRiskPercent = o.Profit.CostOfRisk,
                OpexPercent = o.Profit.OpexPct,
                CapitalAdvantagePercent = o.Profit.CapitalAdvantage,
                NetEbitMarginPercent = o.Profit.NetEbitMargin,
                EconomicCapitalPercent = o.Profit.EconomicCapitalRatio,
                
                IdcUpfrontAnnualizedPercent = o.Profit.IdcUpfrontAnnualizedPct,
                SubsidyUpfrontAnnualizedPercent = o.Profit.SubsidyUpfrontAnnualizedPct,
                IdcPeriodicPercent = o.Profit.IdcPeriodicPct,
                SubsidyPeriodicPercent = o.Profit.SubsidyPeriodicPct
            }
        };
    }
}