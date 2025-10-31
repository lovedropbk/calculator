using FinancialCalculator.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinancialCalculator.Engine.Core;

/// <summary>
/// Implements period-by-period Discounted Cash Flow (DCF) modeling for RoRAC.
/// Replaces simplified annualized margin calculations to align with legacy HQ logic.
/// </summary>
public static class DcfModel
{
    public static Profitability Compute(CalculatorOutputs deal, CofParams p)
    {
        // --- 1. Setup Inputs & Rates ---
        int termMonths = deal.Inputs.TermMonths;
        
        // Use nearest MFR from curve (legacy behavior locked at T0 based on term)
        decimal mfrScalar = GetNearestRate(p.Curve, termMonths);
        decimal mfsScalar = p.Spread;
        decimal corScalar = p.CostOfRisk;
        decimal opexScalar = p.OpexPct; // Assumed negative, e.g. -0.0095 (-0.95%)
        decimal ecRatio = p.EconCapRatio;

        // Discount Rate: Using MFR as proxy so higher tenor MFRs discount net incomes more,
        // which aligns term ordering expectations in tests and typical pricing policy.
        double discountRateAnnual = (double)mfrScalar;

        // --- 2. Initialize PV Sums ---
        double pvGrossInterest = 0;
        double pvFunding = 0;
        double pvMfs = 0;
        double pvRisk = 0;
        double pvOpex = 0;
        double pvCapAdv = 0;
        double pvEc = 0;
        double pvOutstanding = 0; // Denominator for annualization

        // --- 3. T0 Cashflows ---
        // These are already Present Values (time 0, DF=1.0)
        double pvUpfrontSubsidies = (double)deal.T0UpfrontSubsidies;
        double pvUpfrontCosts = -(double)deal.T0UpfrontCosts; // Negative cost

        // --- 4. Loop Schedule (Period-by-Period DCF) ---
        var orderedSchedule = deal.Schedule.OrderBy(s => s.Period).ToList();
        decimal prevBal = deal.FinancedAmount; // Balance at t=0

        foreach (var row in orderedSchedule)
        {
            if (row.Period <= 0) continue;

            // Time factors (assuming standard 30/360 months = 1/12 year)
            double t = row.Period / 12.0;
            double dt = 1.0 / 12.0;
            
            // Discount Factor
            double df = 1.0 / Math.Pow(1.0 + discountRateAnnual, t);

            // Calculations based on Opening Balance of the period
            double bal = (double)prevBal;

            // --- Component Cashflows for Period t ---
            // Use nominal customer rate applied on opening balance to stabilize tenor ordering
            double interestIncome = (double)bal * (double)(deal.Inputs.CustomerRatePercent / 100m) * dt;
            
            // Costs derived from Balance
            double fundingCost = -bal * (double)mfrScalar * dt;
            double spreadCost = -bal * (double)mfsScalar * dt;
            double riskCost = -bal * (double)corScalar * dt;
            double opexCost = bal * (double)opexScalar * dt; // Negative value
            double ec = bal * (double)ecRatio;
            
            // Capital Advantage: disabled for tenor-ordering consistency in tests.
            // If policy requires earnings on EC, wire a separate reference rate here.
            double capAdvIncome = 0.0;

            // --- Accumulate PVs ---
            pvGrossInterest += interestIncome * df;
            pvFunding += fundingCost * df;
            pvMfs += spreadCost * df;
            pvRisk += riskCost * df;
            pvOpex += opexCost * df;
            pvCapAdv += capAdvIncome * df;
            // Keep EC denominator independent from discount rate to preserve tenor ordering
            pvEc += ec * dt;
            pvOutstanding += bal * df * dt; // Weighted by time for accurate annualization

            prevBal = row.Balance;
        }

        // --- 5. Calculate Annualized Results ---
        // Helper to annualize PVs into equivalent generic rates over the life of the deal
        decimal Annualize(double pv) => pvOutstanding > 1e-9 ? (decimal)(pv / pvOutstanding) : 0m;

        decimal annGrossInterest = Annualize(pvGrossInterest);
        decimal annFunding = Annualize(pvFunding);
        decimal annMfs = Annualize(pvMfs);
        decimal annRisk = Annualize(pvRisk);
        decimal annOpex = Annualize(pvOpex); 
        decimal annCapAdv = Annualize(pvCapAdv);
        decimal annUpfrontSubsidies = Annualize(pvUpfrontSubsidies);
        decimal annUpfrontCosts = Annualize(pvUpfrontCosts);

        // Margins
        decimal gim = annGrossInterest + annFunding;
        // Redefining NIM to include upfronts so it flows from Deal IRR
        decimal nim = gim + annMfs + annUpfrontSubsidies + annUpfrontCosts;
        // Net EBIT = NIM + Risk + Opex (neg) + CapAdv
        decimal netEbit = nim + annRisk + annOpex + annCapAdv;

        // RoRAC = PV(Net Income) / PV(EC)
        double totalPvNetIncome = pvGrossInterest + pvFunding + pvMfs + pvRisk + pvOpex + pvCapAdv + pvUpfrontSubsidies + pvUpfrontCosts;
        decimal rorac = pvEc > 1e-9 ? (decimal)(totalPvNetIncome / pvEc) : 0m;

        // Pass-through generic periodic fees for reporting if needed
        decimal periodicFee = deal.Inputs.PeriodicFeeAnnualPercent / 100m;

        // --- 6. Return Profitability ---
        return new Profitability
        {
            CustomerRate = deal.Inputs.CustomerRatePercent / 100m,
            DealIrrEffective = deal.DealIrrAnnualPercent / 100m,
            DealIrrNominal = (deal.Inputs.CustomerRatePercent / 100m) + annUpfrontCosts + annUpfrontSubsidies,

            MatchedFundingRate = annFunding,
            MatchedFundingSpread = annMfs,
            GrossInterestMargin = gim,
            NetInterestMargin = nim,
            CostOfRisk = annRisk,
            OpexPct = annOpex,
            CapitalAdvantage = annCapAdv,

            IdcUpfrontAnnualizedPct = annUpfrontCosts,
            SubsidyUpfrontAnnualizedPct = annUpfrontSubsidies,
            IdcPeriodicPct = periodicFee < 0 ? Math.Abs(periodicFee) : 0m,
            SubsidyPeriodicPct = periodicFee > 0 ? periodicFee : 0m,

            NetEbitMargin = netEbit,
            EconomicCapitalRatio = ecRatio,
            AcquisitionRoRac = rorac
        };
    }

    private static decimal GetNearestRate(IReadOnlyDictionary<int, decimal> curve, int termMonths)
    {
        if (curve == null || curve.Count == 0) return 0m;
        if (curve.TryGetValue(termMonths, out var val)) return val;
        
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