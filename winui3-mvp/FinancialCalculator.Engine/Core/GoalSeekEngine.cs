using System;

namespace FinancialCalculator.Engine.Core;

public class GoalSeekEngine
{
    private readonly DealEngine _dealEngine;

    public GoalSeekEngine(DealEngine dealEngine)
    {
        _dealEngine = dealEngine ?? throw new ArgumentNullException(nameof(dealEngine));
    }

    public enum GoalVariable
    {
        CustomerNominalRate,
        DownPaymentAmount,
        VehiclePrice,
        UpfrontSubsidy
    }

    public enum TargetMetric
    {
        MonthlyInstallment,
        RoRAC
    }

    public double Seek(DealEngine.DealInput baseInput, GoalVariable variable, TargetMetric targetMetric, double targetValue)
    {
        // Simple bisection search
        double low, high;

        switch (variable)
        {
            case GoalVariable.CustomerNominalRate:
                low = 0.0; high = 20.0; // 0% to 20%
                break;
            case GoalVariable.DownPaymentAmount:
                low = 0.0; high = (double)baseInput.VehiclePrice;
                break;
             case GoalVariable.VehiclePrice:
                 low = 0.0; high = 10_000_000.0;
                 break;
             case GoalVariable.UpfrontSubsidy:
                 low = 0.0; high = 5_000_000.0; // Reasonable upper bound for subsidy
                 break;
             default:
                 throw new ArgumentOutOfRangeException(nameof(variable));
         }

        for (int i = 0; i < 50; i++) // Max 50 iterations
        {
            double mid = (low + high) / 2.0;
            var currentInput = CloneWithVariable(baseInput, variable, mid);
            var output = _dealEngine.Calculate(currentInput);

            double currentValue = targetMetric switch
            {
                TargetMetric.MonthlyInstallment => (double)output.Deal.MonthlyRate,
                TargetMetric.RoRAC => (double)output.Profit.AcquisitionRoRac,
                _ => 0
            };

            if (Math.Abs(currentValue - targetValue) < 1e-5) return mid;

            // Determine direction. Needs careful thought on correlation.
            // Rate UP -> Monthly UP, RoRAC UP
            // DownPayment UP -> Monthly DOWN, RoRAC UP (usually, less risk/funding)
            // Subsidy UP -> RoRAC UP (direct), Monthly NO CHANGE (usually)

            bool isDirectCorrelation = variable == GoalVariable.CustomerNominalRate ||
                                       variable == GoalVariable.UpfrontSubsidy ||
                                       (variable == GoalVariable.DownPaymentAmount && targetMetric == TargetMetric.RoRAC);
            
            if (variable == GoalVariable.DownPaymentAmount && targetMetric == TargetMetric.MonthlyInstallment)
            {
                 isDirectCorrelation = false;
            }
            
            // Subsidy UP -> RoRAC UP (direct)
            // Subsidy UP -> Monthly (no change if pure lender subsidy, so maybe can't goal seek monthly with it efficiently if slope is 0)
            
            if (currentValue < targetValue)
            {
                if (isDirectCorrelation) low = mid; else high = mid;
            }
            else
            {
                if (isDirectCorrelation) high = mid; else low = mid;
            }
        }

        return (low + high) / 2.0;
    }

    private DealEngine.DealInput CloneWithVariable(DealEngine.DealInput input, GoalVariable variable, double value)
    {
        return variable switch
        {
            GoalVariable.CustomerNominalRate => input with { CustomerRatePercent = (decimal)value },
            GoalVariable.DownPaymentAmount => input with { DownValue = (decimal)value, DownIsPercent = false },
            GoalVariable.VehiclePrice => input with { VehiclePrice = (decimal)value },
            GoalVariable.UpfrontSubsidy => input with { UpfrontSubsidies = (decimal)value },
            _ => input
        };
    }
}