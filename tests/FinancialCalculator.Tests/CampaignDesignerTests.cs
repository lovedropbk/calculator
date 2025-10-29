using FinancialCalculator.Engine;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.WinUI3.Models;
using FinancialCalculator.WinUI3.Services;
using FinancialCalculator.WinUI3.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialCalculator.Tests
{
    [TestClass]
    public class CampaignDesignerTests
    {
        private static FinancialFacade _facade;
        private static IStandardRateService _standardRateService;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            var riskRepo = new RiskParameterRepository(new FileService());
            await riskRepo.LoadAsync("dev-parameters.json");
            _facade = new FinancialFacade(riskRepo);

            _standardRateService = new StandardRateService();
            await _standardRateService.LoadAsync();
        }

        [TestMethod]
        public void TestCampaignTileViewModel_RecalculateAggregates()
        {
            // Arrange
            var tile = new CampaignTileViewModel();
            tile.TermBreakdown.Add(new TermBreakdownItemViewModel { RoRAC = "10.00%", DistributionPct = 50 });
            tile.TermBreakdown.Add(new TermBreakdownItemViewModel { RoRAC = "5.00%", DistributionPct = 50 });

            // Act
            tile.RecalculateAggregates();

            // Assert
            Assert.AreEqual("7.50%", tile.AvgRoRAC);
        }

        [TestMethod]
        public void TestComparisonViewModel_RecalculateOverall()
        {
            // Arrange
            var vm = new ComparisonViewModel();
            vm.DesignerCampaigns.Add(new CampaignTileViewModel { AvgRoRAC = "10.00%", CampaignVolumePct = 50 });
            vm.DesignerCampaigns.Add(new CampaignTileViewModel { AvgRoRAC = "5.00%", CampaignVolumePct = 50 });

            // Act
            // RecalculateOverall is called automatically on collection change
            
            // Assert
            Assert.AreEqual("7.50%", vm.OverallAvgRoRAC);
        }

        [TestMethod]
        public async Task TestCampaignTermBreakdownService_CalculatesBreakdown()
        {
            // Arrange
            var service = new CampaignTermBreakdownService(_facade, _standardRateService);
            var dealInput = new DealInputViewModel(_standardRateService, new CommissionService());
            var campaign = new CampaignSummaryViewModel();
            var baseRequest = new Engine.Models.Facade.ScenarioRequest();

            // Act
            var breakdown = await service.CalculateTermBreakdownAsync(campaign, baseRequest, dealInput);

            // Assert
            Assert.IsNotNull(breakdown);
            Assert.AreEqual(5, breakdown.Count); // 12, 24, 36, 48, 60
            Assert.IsTrue(breakdown.All(b => !string.IsNullOrEmpty(b.RoRAC)));
        }
    }
}