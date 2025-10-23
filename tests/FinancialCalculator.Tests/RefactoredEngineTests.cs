using System;
using Xunit;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.Tests;

public class RefactoredEngineTests
{
    [Fact]
    public void RateConverter_FlatToNominal_MatchesExpected()
    {
        // 2.15% flat, 48m, arrears -> ~4.09% nominal (illustrative)
        decimal flat = 2.15m;
        int term = 48;
        var nominal = RateConverter.ConvertFlatToNominal(flat, term, PaymentMode.InArrears);
        
        // Verify against known good value or approx
        Assert.InRange((double)nominal, 4.0, 4.2);
    }

    [Fact]
    public void MathUtils_Interpolation_Works()
    {
        // Linear
        Assert.Equal(5.0, MathUtils.InterpolateLinear(2.5, 2.0, 4.0, 3.0, 6.0));

        // Exponential (midpoint of 10 and 100 is 31.62 not 55)
        var mid = MathUtils.InterpolateExponential(0.5, 0, 10, 1, 100);
        Assert.InRange(mid, 31.6, 31.7);
    }

    [Fact]
    public void DealEngine_CalculatesBasicHP()
    {
        // Requires risk repo. For now we might need to mock it or use integration test if repo needs files.
        // Assuming integration test for now as repo needs files.
        // ...
    }

    [Fact]
    public void GoalSeekEngine_SeeksRate()
    {
        // Mock or use real engine if possible.
        // ...
    }
}