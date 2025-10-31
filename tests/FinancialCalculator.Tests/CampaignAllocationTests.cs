using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using FinancialCalculator.Engine;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models.Facade;

namespace FinancialCalculator.Tests
{
    [TestClass]
    public class CampaignAllocationTests
    {
        private static FinancialFacade _facade = null!;

        [ClassInitialize]
        public static async Task Init(TestContext ctx)
        {
            var repo = new RiskParameterRepository(new FileService());
            await repo.LoadAsync("dev-parameters.json");
            _facade = new FinancialFacade(repo);
        }

        [TestMethod]
        public void Allocate_PartialAndEdgeCases_WorkAsExpected()
        {
            decimal price = 2_540_000m;

            // Full subdown allocation
            var r = CampaignAllocation.Allocate(new CampaignAllocation.Input(
                TransactionPrice: price,
                DownIsPercent: false,
                DownValue: 200_000m,
                TotalSubsidyBudget: 200_000m,
                RequestedSubdownTHB: 999_999m
            ));
            Assert.AreEqual(200_000m, r.BaseDownpayment);
            Assert.AreEqual(200_000m, r.SubsidyUsedForSubdown);
            Assert.AreEqual(0m, r.CustomerDownpayment);
            Assert.AreEqual(0m, r.SubsidyRemaining);

            // Subsidy exceeds base downpayment
            var r2 = CampaignAllocation.Allocate(new CampaignAllocation.Input(
                TransactionPrice: price,
                DownIsPercent: false,
                DownValue: 100_000m,
                TotalSubsidyBudget: 200_000m,
                RequestedSubdownTHB: 200_000m
            ));
            Assert.AreEqual(100_000m, r2.SubsidyUsedForSubdown);
            Assert.AreEqual(0m, r2.CustomerDownpayment);
            Assert.AreEqual(100_000m, r2.SubsidyRemaining);

            // Partial subdown
            var r3 = CampaignAllocation.Allocate(new CampaignAllocation.Input(
                TransactionPrice: price,
                DownIsPercent: false,
                DownValue: 300_000m,
                TotalSubsidyBudget: 200_000m,
                RequestedSubdownTHB: 150_000m
            ));
            Assert.AreEqual(150_000m, r3.SubsidyUsedForSubdown);
            Assert.AreEqual(150_000m, r3.CustomerDownpayment);
            Assert.AreEqual(50_000m, r3.SubsidyRemaining);
        }

        [TestMethod]
        public void FullSubdown_200k_ReducesDownpaymentAndZeroAnnualizedSubsidy()
        {
            decimal price = 2_540_000m;

            var baseReq = new ScenarioRequest
            {
                Market = "TH",
                Product = "HP",
                Timing = "arrears",
                TermMonths = 36,
                VehiclePrice = price,
                AdditionalFinancedItems = 0m,
                DownIsPercent = false,
                DownValue = 200_000m,
                BalloonIsPercent = false,
                BalloonValue = 0m,
                CustomerRatePercent = 6.13m,
                UpfrontSubsidies = 200_000m,
                UpfrontCosts = 0m,
                SubdownIsPercent = false,
                SubdownValue = 0m,
                CustomerType = "RETAIL PRIVATE",
                AssetState = "N",
                AssetValuationCurve = "MBPC",
                Rating = "4.0"
            };

            var alloc = CampaignAllocation.Allocate(new CampaignAllocation.Input(
                TransactionPrice: price,
                DownIsPercent: false,
                DownValue: 200_000m,
                TotalSubsidyBudget: 200_000m,
                RequestedSubdownTHB: 999_999m
            ));

            var reqAllocated = CampaignAllocation.ApplyToScenario(baseReq, alloc);

            var resNoCampaign = _facade.Calculate(baseReq);
            var resAllocated = _facade.Calculate(reqAllocated);

            // Compare against control (no subdown, no upfront subsidy)
            var controlReq = baseReq with { UpfrontSubsidies = 0m, SubdownValue = 0m };
            var resControl = _facade.Calculate(controlReq);

            // Financed amount reduced by full 200,000 THB when subdown is fully utilized
            Assert.AreEqual(resControl.FinancedAmount - 200_000m, resAllocated.FinancedAmount);

            // No double counting: annualized subsidy impact must be zero when all subsidy used for SubDown
            Assert.AreEqual(0m, resAllocated.Profitability.SubsidyUpfrontAnnualizedPercent);

            // Baseline recognizing entire subsidy as upfront should be positive
            Assert.IsTrue(resNoCampaign.Profitability.SubsidyUpfrontAnnualizedPercent > 0m);
        }
    }
}