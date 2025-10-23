using System;
using FinancialCalculator.Engine.Core;
using Xunit;
using Xunit.Abstractions;

namespace FinancialCalculator.Tests
{
    public class GoalSeekTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private readonly RiskParameterRepository _riskRepo;
        private readonly DealEngine _engine;
        private readonly GoalSeekEngine _goalSeek;

        public GoalSeekTests(ITestOutputHelper output)
        {
            _output = output;
            _riskRepo = new RiskParameterRepository(new FileService());
            _engine = new DealEngine(_riskRepo);
            _goalSeek = new GoalSeekEngine(_engine);
        }

        public async Task InitializeAsync()
        {
            string paramPath = System.IO.Path.GetFullPath("winui3-mvp/docs/parameters");
            if (!System.IO.Directory.Exists(paramPath))
            {
                 paramPath = System.IO.Path.GetFullPath("../winui3-mvp/docs/parameters");
            }
            await _riskRepo.LoadAsync(paramPath);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public void SolveForRate_TargetRoRAC_ReturnsCorrectRate()
        {
             // Setup base input
             var input = new DealEngine.DealInput
             {
                 Market = "TH",
                 Product = "HP",
                 Timing = "arrears",
                 VehiclePrice = 1_000_000m,
                 DownValue = 200_000m,
                 DownIsPercent = false,
                 TermMonths = 48,
                 CustomerRatePercent = 0m, // Start at 0
                 UpfrontCosts = 30_000m, // Commission + IDCs
                 UpfrontSubsidies = 0m,
                 BalloonValue = 0m,
                 CustomerType = "RETAIL PRIVATE",
                 AssetState = "N",
                 AssetValuationCurve = "MBPC",
                 Rating = "5, 5.0"
             };

             double targetRoRAC = 0.05; // 5%

             double solvedRate = _goalSeek.Seek(input, GoalSeekEngine.GoalVariable.CustomerNominalRate, GoalSeekEngine.TargetMetric.RoRAC, targetRoRAC);

             _output.WriteLine($"Solved Rate for 5% RoRAC: {solvedRate:N4}%");

             // Verify
             var resultInput = input with { CustomerRatePercent = (decimal)solvedRate };
             var result = _engine.Calculate(resultInput);
             
             Assert.InRange((double)result.Profit.AcquisitionRoRac, targetRoRAC - 0.001, targetRoRAC + 0.001);
        }

        [Fact]
        public void SolveForSubsidy_TargetRoRAC_ReturnsCorrectSubsidy()
        {
             // Setup base input with fixed rate, need subsidy to reach RoRAC
             var input = new DealEngine.DealInput
             {
                 Market = "TH",
                 Product = "HP",
                 Timing = "arrears",
                 VehiclePrice = 1_000_000m,
                 DownValue = 200_000m,
                 DownIsPercent = false,
                 TermMonths = 48,
                 CustomerRatePercent = 1.99m, // Low rate
                 UpfrontCosts = 30_000m,
                 UpfrontSubsidies = 0m, // Start at 0
                 BalloonValue = 0m,
                 CustomerType = "RETAIL PRIVATE",
                 AssetState = "N",
                 AssetValuationCurve = "MBPC",
                 Rating = "5, 5.0"
             };

             double targetRoRAC = 0.10; // 10% - might need subsidy

             double solvedSubsidy = _goalSeek.Seek(input, GoalSeekEngine.GoalVariable.UpfrontSubsidy, GoalSeekEngine.TargetMetric.RoRAC, targetRoRAC);

             _output.WriteLine($"Solved Subsidy for 10% RoRAC: {solvedSubsidy:N2}");

             // Verify
             var resultInput = input with { UpfrontSubsidies = (decimal)solvedSubsidy };
             var result = _engine.Calculate(resultInput);
             
             Assert.InRange((double)result.Profit.AcquisitionRoRac, targetRoRAC - 0.001, targetRoRAC + 0.001);
        }
    }
}