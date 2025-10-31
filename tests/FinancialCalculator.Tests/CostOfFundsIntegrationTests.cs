using System;
using System.IO;
using System.Threading.Tasks;
using FinancialCalculator.Engine;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models.Facade;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FinancialCalculator.Tests
{
    [TestClass]
    public class CostOfFundsIntegrationTests
    {
        private static string _testConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");

        [TestInitialize]
        public void SetupConfig()
        {
            // Write a minimal config.yaml into the test bin directory to control the curve precisely
            var yaml = @"version: ""test-cof""
costOfFundsCurve:
  - termMonths: 24
    rate: 0.0100
  - termMonths: 48
    rate: 0.0500
matchedFundedSpread: 0.0000
opex:
  byProductPct:
    HP: 0.0000
";
            File.WriteAllText(_testConfigPath, yaml);
        }

        [TestCleanup]
        public void CleanupConfig()
        {
            try { if (File.Exists(_testConfigPath)) File.Delete(_testConfigPath); } catch { /* ignore */ }
        }

        [TestMethod]
        public async Task PerTerm_CostOfFunds_AppliedFromConfig()
        {
            // Arrange: Use real engine with risk repo, but controlled CoF via local config.yaml
            var riskRepo = new RiskParameterRepository(new FileService());
            await riskRepo.LoadAsync("dev-parameters.json");
            var facade = new FinancialFacade(riskRepo);

            // Keep inputs constant except term; set customer rate modest to avoid edge artefacts
            var baseReq = new ScenarioRequest
            {
                Market = "TH",
                Product = "HP",
                Timing = "arrears",
                VehiclePrice = 1_000_000m,
                AdditionalFinancedItems = 0m,
                DownIsPercent = false,
                DownValue = 0m,
                BalloonIsPercent = false,
                BalloonValue = 0m,
                CustomerRatePercent = 5.0m,
                UpfrontSubsidies = 0m,
                UpfrontCosts = 0m,
                SubdownIsPercent = false,
                SubdownValue = 0m,
                CustomerType = "RETAIL PRIVATE",
                AssetState = "N",
                AssetValuationCurve = "MBPC",
                Rating = "5, 5.0"
            };

            // Act
            var r24 = facade.Calculate(baseReq with { TermMonths = 24 });
            var r48 = facade.Calculate(baseReq with { TermMonths = 48 });

            // Assert:
            // MatchedFundingRate is an annualized PV-based measure (negative cost). With our config,
            // it should be near -MFR for each term and clearly different between 24 vs 48.
            double mfr24Expected = -0.0100;
            double mfr48Expected = -0.0500;

            double mf24 = (double)r24.Profitability.CostOfDebtMatchedPercent;
            double mf48 = (double)r48.Profitability.CostOfDebtMatchedPercent;

            // Basic sanity: both negative and distinct magnitudes reflecting the curve
            Assert.IsTrue(mf24 < 0 && mf48 < 0, "Funding rates should be negative costs.");
            Assert.IsTrue(Math.Abs(mf48) > Math.Abs(mf24) * 2.5, "48m funding magnitude should be much larger than 24m with configured curve.");

            // Tolerance-based closeness to configured MFRs (PV annualization makes it approximate)
            Assert.AreEqual(mfr24Expected, mf24, 0.005, "24m matched funding not aligned with configured curve.");
            Assert.AreEqual(mfr48Expected, mf48, 0.015, "48m matched funding not aligned with configured curve.");
        }
    }
}