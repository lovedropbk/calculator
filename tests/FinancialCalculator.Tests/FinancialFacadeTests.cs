using System;
using FinancialCalculator.Engine;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models.Facade;
using Moq;
using Xunit;

namespace FinancialCalculator.Tests;

public class FinancialFacadeTests
{
    [Fact]
    public void Calculate_SimpleScenario_ReturnsValidResult()
    {
        // Arrange
        var mockRisk = new Mock<IRiskParameterRepository>();
        mockRisk.Setup(r => r.GetPd(It.IsAny<string>(), It.IsAny<string>())).Returns(0.01);
        mockRisk.Setup(r => r.GetLgd(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns((0.45, 0.45));
        mockRisk.Setup(r => r.GetEcTotal()).Returns(0.088);

        var facade = new FinancialFacade(mockRisk.Object);
        var request = new ScenarioRequest
        {
            VehiclePrice = 1_000_000,
            DownValue = 200_000,
            TermMonths = 48,
            CustomerRatePercent = 5.0m
        };

        // Act
        var result = facade.Calculate(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.MonthlyInstallment > 0);
        Assert.Equal(800_000, result.FinancedAmount);
    }
}