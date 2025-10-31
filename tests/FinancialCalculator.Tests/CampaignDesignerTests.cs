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
            var commissionService = new CommissionService();
            var dealInput = new DealInputViewModel(_standardRateService, commissionService) { Product = "HP" };
            var campaign = new CampaignSummaryViewModel();
            var baseRequest = dealInput.BuildScenarioRequest();

            // Act
            var breakdown = await service.CalculateTermBreakdownAsync(campaign, baseRequest, dealInput);

            // Assert
            Assert.IsNotNull(breakdown);
            Assert.IsTrue(breakdown.Count >= 3); // Based on CSV terms for HP (e.g., 24,36,48,60,72)
            Assert.IsTrue(breakdown.All(b => b.Term > 0 && !string.IsNullOrEmpty(b.RoRAC)));
        }

        [TestMethod]
        public async Task TestCampaignTermBreakdown_UsesConfigDefaultsForHP()
        {
            // Arrange
            var service = new CampaignTermBreakdownService(_facade, _standardRateService);
            var commissionService = new CommissionService();
            // Important: Product must be set to trigger the correct config lookup
            var dealInput = new DealInputViewModel(_standardRateService, commissionService) { Product = "HP" };
            var campaign = new CampaignSummaryViewModel(); // Campaign type doesn't matter for this part of the logic
            // Move BuildScenarioRequest *after* setting the product to ensure it's included
            var baseRequest = dealInput.BuildScenarioRequest();

            // Act
            var breakdown = await service.CalculateTermBreakdownAsync(campaign, baseRequest, dealInput);

            // Assert
            Assert.IsNotNull(breakdown);
            var breakdownDict = breakdown.ToDictionary(b => b.Term, b => b.DistributionPct);

            // Verify distributions for present CSV terms per config defaults (HP)
            // 24:0, 36:10, 48:40, 60:50; other terms (e.g., 72) default to 0.
            Assert.IsTrue(breakdownDict.ContainsKey(24));
            Assert.AreEqual(0, breakdownDict[24], 0.001);
            Assert.AreEqual(10, breakdownDict[36], 0.001);
            Assert.AreEqual(40, breakdownDict[48], 0.001);
            Assert.AreEqual(50, breakdownDict[60], 0.001);
        }
    }
}