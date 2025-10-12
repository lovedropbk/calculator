using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.Engine.Core;

public static class Mapping
{
    public static CalculatorOutputs WithT0(this CalculatorOutputs o)
        => o with
        {
            T0Disbursement = o.FinancedAmount,
            T0UpfrontSubsidies = o.Inputs.UpfrontSubsidies,
            T0UpfrontCosts = o.Inputs.UpfrontCosts
        };
}
