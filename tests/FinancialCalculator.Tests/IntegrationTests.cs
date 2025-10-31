using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialCalculator.Engine;
using FinancialCalculator.WinUI3.Services;
using FinancialCalculator.WinUI3.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FinancialCalculator.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        private static FinancialFacade _facade = null!;
        private static IStandardRateService _rates = null!;

        [ClassInitialize]
        public static async Task Init(TestContext ctx)
        {
            var riskRepo = new RiskParameterRepository(new FileService());
            await riskRepo.LoadAsync("dev-parameters.json");
            _facade = new FinancialFacade(riskRepo);

            _rates = new StandardRateService();
            await _rates.LoadAsync();
        }

        [TestMethod]
        public void DealInput_UsesServiceRateFromCsv()
        {
            // Arrange: HP, 36 months, 20% dp, advance -> from CSV should be 4.7366
            var deal = new DealInputViewModel(_rates, new CommissionService());

            deal.Product = "HP";
            deal.Timing = "advance";
            deal.TermMonths = 36;

            deal.DownPaymentUnit = "%";
            deal.DownPaymentValueEntry = 20.0;

            // Act
            var expected = _rates.GetStandardRate("HP", 36, 0.20, "advance");

            // Assert
            Assert.IsTrue(expected.HasValue, "Expected standard rate not found from service");
            Assert.AreEqual(expected.Value, deal.CustomerNominalRate, 1e-6, "DealInput should reflect service standard rate exactly");
        }

        [TestMethod]
        public async Task CampaignDesigner_TermIterationMatchesService()
        {
            // Arrange: HP, advance, 10% dp
            var termSvc = new CampaignTermBreakdownService(_facade, _rates);
            var deal = new DealInputViewModel(_rates, new CommissionService())
            {
                Product = "HP",
                Timing = "advance"
            };
            deal.DownPaymentUnit = "%";
            deal.DownPaymentValueEntry = 10.0;

            var baseRequest = deal.BuildScenarioRequest();

            // Act
            var breakdown = await termSvc.CalculateTermBreakdownAsync(new CampaignSummaryViewModel(), baseRequest, deal);
            var actualTerms = breakdown.Select(b => b.Term).OrderBy(t => t).ToArray();

            var expectedTerms = _rates.GetAvailableTerms("HP", 0.10, "advance");
            var expected = expectedTerms.OrderBy(t => t).ToArray();

            // Assert
            CollectionAssert.AreEqual(expected, actualTerms, "Designer should iterate exactly the CSV-derived distinct terms for the current dp/mode.");
        }
    }
}