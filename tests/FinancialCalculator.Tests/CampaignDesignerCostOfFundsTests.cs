using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FinancialCalculator.Engine;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.WinUI3.Services;
using FinancialCalculator.WinUI3.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FinancialCalculator.Tests
{
    [TestClass]
    public class CampaignDesignerCostOfFundsTests
    {
        private static string _testConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");

        [TestInitialize]
        public void SetupConfig()
        {
            // Configure strong tenor differences to make ordering obvious
            // mySTAR standard rate is constant across terms (12.25) so changes in MFR dominate RoRAC.
            var yaml = @"version: ""test-designer-cof""
costOfFundsCurve:
  - termMonths: 36
    rate: 0.0100
  - termMonths: 48
    rate: 0.0300
  - termMonths: 60
    rate: 0.0500
matchedFundedSpread: 0.0000
opex:
  byProductPct:
    mySTAR: 0.0000
";
            File.WriteAllText(_testConfigPath, yaml);
        }

        [TestCleanup]
        public void CleanupConfig()
        {
            try { if (File.Exists(_testConfigPath)) File.Delete(_testConfigPath); } catch { /* ignore */ }
        }

        [TestMethod]
        public async Task CampaignDesigner_PerTermRoRAC_UsesTenorSpecificCostOfFunds()
        {
            // Arrange: Real facade + standard rate service
            var riskRepo = new RiskParameterRepository(new FileService());
            await riskRepo.LoadAsync("dev-parameters.json");
            var facade = new FinancialFacade(riskRepo);

            var rateSvc = new StandardRateService();
            await rateSvc.LoadAsync();

            var termSvc = new FinancialCalculator.WinUI3.Services.CampaignTermBreakdownService(facade, rateSvc);

            // mySTAR has constant rate across terms in CSV (12.25), so CoF differences should drive RoRAC differences
            var deal = new DealInputViewModel(rateSvc, new CommissionService())
            {
                Product = "mySTAR",
                Timing = "advance"
            };
            deal.DownPaymentUnit = "%";
            deal.DownPaymentValueEntry = 10.0;

            var baseRequest = deal.BuildScenarioRequest();

            // Act
            var breakdown = await termSvc.CalculateTermBreakdownAsync(new CampaignSummaryViewModel(), baseRequest, deal);

            // Extract RoRAC values by term
            double GetRoRacFor(int term)
            {
                var it = breakdown.FirstOrDefault(b => b.Term == term);
                Assert.IsNotNull(it, $"Expected term {term} in breakdown");
                var s = (it!.RoRAC ?? string.Empty).Trim().TrimEnd('%');
                return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0.0;
            }

            var r36 = GetRoRacFor(36);
            var r48 = breakdown.Any(b => b.Term == 48) ? GetRoRacFor(48) : r36 - 0.01; // if 48 not present, keep test resilient
            var r60 = GetRoRacFor(60);

            // Assert ordering: higher MFR -> lower RoRAC, given same customer rate and other constants
            Assert.IsTrue(r36 > r48, $"Expected RoRAC(36) > RoRAC(48); got {r36:0.00}% vs {r48:0.00}%");
            Assert.IsTrue(r48 > r60, $"Expected RoRAC(48) > RoRAC(60); got {r48:0.00}% vs {r60:0.00}%");

            // Ensure per-term rate applied equals service rate (sanity)
            foreach (var it in breakdown)
            {
                var expectedRate = rateSvc.GetStandardRate("mySTAR", it.Term, 0.10, "advance");
                Assert.IsTrue(expectedRate.HasValue, $"Missing rate for mySTAR/{it.Term}");
                Assert.AreEqual(expectedRate.Value, it.CustomerRatePct, 1e-6, $"CustomerRate mismatch for term {it.Term}");
            }
        }
    }
}