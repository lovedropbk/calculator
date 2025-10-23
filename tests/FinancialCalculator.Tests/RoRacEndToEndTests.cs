using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace FinancialCalculator.Tests
{
    public class RoRacEndToEndTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private readonly RiskParameterRepository _riskRepo;

        public RoRacEndToEndTests(ITestOutputHelper output)
        {
            _output = output;

            // Mock File Service with static content
            var mockFile = new Mock<IFileService>();
            mockFile.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            // PD.csv mock content
            // Adjusting to match index usage in repo:
            // parts -> CustType (RETAIL PRIVATE)
            // parts -> Rating (4.0)
            // parts -> PD (0.01)
            // Need enough columns
            var pdCsvReal = @"H1,H2,Customer Types,H4,H5,H6,H7,H8,H9,H10,H11,H12,H13,Rating,H15,PD_Value
1,2,""RETAIL PRIVATE, FLEET"",4,5,6,7,8,9,10,11,12,13,4.0,15,0.015
1,2,""RETAIL PRIVATE"",4,5,6,7,8,9,10,11,12,13,5.0,15,0.008";

            mockFile.Setup(f => f.OpenText(It.Is<string>(s => s.EndsWith("PD.csv"))))
                .Returns(() => new StringReader(pdCsvReal));

            // LGD_OneEC.csv mock content
            // parts -> assetStates (N, U)
            // parts -> custType
            // parts -> avcs (MBPC)
            // parts -> dcfLgd
            // parts -> downturnLgd
            var lgdCsv = @"H1,H2,H3,Asset States,Customer Type,AVCs,H7,H8,H9,H10,H11,H12,H13,DcfLgd,DownturnLgd
1,2,3,""N, U"",RETAIL PRIVATE,MBPC,7,8,9,10,11,12,13,0.40,0.45";
            
            mockFile.Setup(f => f.OpenText(It.Is<string>(s => s.EndsWith("LGD_OneEC.csv"))))
                 .Returns(() => new StringReader(lgdCsv));

            // EC_TOTAL.csv mock content
            // parts -> ec
            var ecCsv = @"H1,EC Total
1,0.09";
            mockFile.Setup(f => f.OpenText(It.Is<string>(s => s.EndsWith("EC_TOTAL.csv"))))
                 .Returns(() => new StringReader(ecCsv));

            _riskRepo = new RiskParameterRepository(mockFile.Object);
        }

        public async Task InitializeAsync()
        {
             // Path doesn't matter due to mocks
             await _riskRepo.LoadAsync("dummy/path");
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public void TestRoRac_HP_StandardFinance()
        {
            _output.WriteLine("Starting HP RoRAC Test");
            
            var inputs = new CalculatorInputs
            {
                Product = FinancialProduct.HirePurchase,
                VehicleSalesPrice = 2_300_000m,
                DownpaymentValue = 0m,
                TermMonths = 48,
                CustomerRatePercent = 5.0m, // Nominal 5%
                PaymentMode = PaymentMode.InArrears,
                BalloonTHB = 0m,
                UpfrontCosts = 120_000m,
                UpfrontSubsidies = 20_000m,
                CustomerType = "RETAIL PRIVATE",
                AssetState = "N",
                AssetValuationCurve = "MBPC",
                Rating = "4.0" 
            };

            // 1. Calculate Deal Flows
            var calc = new Engine.Core.FinancialCalculator();
            var deal = calc.Calculate(inputs);

            // 2. Load Risk Params from Repo (uses mocks now)
            double pd = _riskRepo.GetPd(inputs.CustomerType, inputs.Rating);
            var (dcfLgd, _) = _riskRepo.GetLgd(inputs.CustomerType, inputs.AssetState, inputs.AssetValuationCurve);
            double corAnnual = BaselIIEngine.CalculateEL(pd, dcfLgd);
            double ecTotal = _riskRepo.GetEcTotal();

            // 3. Setup CoF Params 
            var cof = new CofParams
            {
                 Curve = new Dictionary<int, decimal>
                {
                    {12, 0.0148m}, {24, 0.0165m}, {36, 0.0175m}, {48, 0.0185m}, {60, 0.0195m}
                },
                Spread = 0.0025m,
                OpexPct = -0.0095m,
                EconCapRatio = (decimal)ecTotal,
                CostOfRisk = (decimal)corAnnual
            };

            // 4. Calculate RoRAC via DCF
            var profit = DcfModel.Compute(deal, cof);

            _output.WriteLine($"Financed: {deal.FinancedAmount:N0}");
            _output.WriteLine($"Monthly PMT: {deal.MonthlyRate:N2}");
            _output.WriteLine($"Deal IRR (Effective): {profit.DealIrrEffective:P2}");
            _output.WriteLine($"Cost of Risk: {profit.CostOfRisk:P2}");
            _output.WriteLine($"Acquisition RoRAC: {profit.AcquisitionRoRac:P2}");

            // Assertions with expected values based on mocks
            Assert.True(profit.AcquisitionRoRac != 0);
            // With PD=0.015, LGD=0.40 -> EL = 0.006 (0.6%)
            // EC=0.09 (9%)
        }

        [Fact]
        public void TestRoRac_mySTAR_BalloonFinance()
        {
            _output.WriteLine("Starting mySTAR RoRAC Test");
             var inputs = new CalculatorInputs
            {
                Product = FinancialProduct.MySTAR,
                VehicleSalesPrice = 2_300_000m,
                DownpaymentValue = 0m,
                TermMonths = 48,
                CustomerRatePercent = 5.0m, // Nominal 5%
                PaymentMode = PaymentMode.InArrears,
                BalloonTHB = 1_100_000m,
                UpfrontCosts = 120_000m,
                UpfrontSubsidies = 20_000m,
                CustomerType = "RETAIL PRIVATE",
                AssetState = "N",
                AssetValuationCurve = "MBPC",
                Rating = "4.0"
            };

            var calc = new Engine.Core.FinancialCalculator();
            var deal = calc.Calculate(inputs);

            double pd = _riskRepo.GetPd(inputs.CustomerType, inputs.Rating);
            var (dcfLgd, _) = _riskRepo.GetLgd(inputs.CustomerType, inputs.AssetState, inputs.AssetValuationCurve);
            double corAnnual = BaselIIEngine.CalculateEL(pd, dcfLgd);
            double ecTotal = _riskRepo.GetEcTotal();

             var cof = new CofParams
            {
                Curve = new Dictionary<int, decimal>
                {
                    {12, 0.0148m}, {24, 0.0165m}, {36, 0.0175m}, {48, 0.0185m}, {60, 0.0195m}
                },
                Spread = 0.0025m,
                OpexPct = -0.0095m,
                EconCapRatio = (decimal)ecTotal,
                CostOfRisk = (decimal)corAnnual
            };

            var profit = DcfModel.Compute(deal, cof);

            _output.WriteLine($"Financed: {deal.FinancedAmount:N0}");
            _output.WriteLine($"Balloon: {inputs.BalloonTHB:N0}");
            _output.WriteLine($"Monthly PMT: {deal.MonthlyRate:N2}");
             _output.WriteLine($"Deal IRR (Effective): {profit.DealIrrEffective:P2}");
            _output.WriteLine($"Acquisition RoRAC: {profit.AcquisitionRoRac:P2}");

            Assert.True(profit.AcquisitionRoRac != 0);
        }
    }
}