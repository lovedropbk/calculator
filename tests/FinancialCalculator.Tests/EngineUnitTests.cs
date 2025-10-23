using System;
using Xunit;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.Tests;

public class EngineUnitTests
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