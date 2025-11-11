using System;
using MathNet.Numerics.RootFinding;

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
        RoRAC,
        FlatRate
    }

    public double Seek(DealEngine.DealInput baseInput, GoalVariable variable, TargetMetric targetMetric, double targetValue)
    {
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
                 low = 0.0; high = 5_000_000.0;
                 break;
             default:
                 throw new ArgumentOutOfRangeException(nameof(variable));
         }

        try
        {
            return Brent.FindRoot(x =>
            {
                var currentInput = CloneWithVariable(baseInput, variable, x);
                var output = _dealEngine.Calculate(currentInput);
                double currentValue = targetMetric switch
                {
                    TargetMetric.MonthlyInstallment => (double)output.Deal.MonthlyRate,
                    TargetMetric.RoRAC => (double)output.Profit.AcquisitionRoRac,
                    TargetMetric.FlatRate => (double)output.Deal.FlatRatePercentPerAnnum,
                    _ => 0
                };
                return currentValue - targetValue;
            }, low, high, accuracy: 1e-5);
        }
        catch (Exception)
        {
             // Fallback if root not bracketed or other issue, return current best guess or initial
             // Maybe return 0 or throw, let's return low to indicate failure to find in range
             return 0;
        }
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