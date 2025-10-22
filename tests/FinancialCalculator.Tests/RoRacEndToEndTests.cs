using System;
using System.Collections.Generic;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models;
using Xunit;
using Xunit.Abstractions;

namespace FinancialCalculator.Tests
{
    public class RoRacEndToEndTests
    {
        private readonly ITestOutputHelper _output;
        private readonly RiskParameterRepository _riskRepo;

        public RoRacEndToEndTests(ITestOutputHelper output)
        {
            _output = output;
            _riskRepo = new RiskParameterRepository();
            // Point to the actual parameters directory relative to test execution
            // Assuming tests run from project root or similar, adjust if needed.
            // Path based on workspace root: winui3-mvp/docs/parameters
            _riskRepo.Load(System.IO.Path.GetFullPath("winui3-mvp/docs/parameters"));
        }

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
                Rating = "4.0" // Average rating
            };

            // 1. Calculate Deal Flows
            var calc = new Engine.Core.FinancialCalculator();
            var deal = calc.Calculate(inputs);

            // 2. Load Risk Params from Repo
            double pd = _riskRepo.GetPd(inputs.CustomerType, inputs.Rating);
            var (dcfLgd, _) = _riskRepo.GetLgd(inputs.CustomerType, inputs.AssetState, inputs.AssetValuationCurve);
            double corAnnual = BaselIIEngine.CalculateEL(pd, dcfLgd);
            double ecTotal = _riskRepo.GetEcTotal();

            // 3. Setup CoF Params (using hardcoded curve from LocalScenarioService for now as it's not in a file)
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

            // Assert.True(profit.AcquisitionRoRac != 0);
            System.IO.File.WriteAllText("HP_RoRAC_Result.txt", $"HP PMT: {deal.MonthlyRate:N2}, HP RoRAC: {profit.AcquisitionRoRac:P2}, Deal IRR (Eff): {profit.DealIrrEffective:P2}, CoR: {profit.CostOfRisk:P2}");
            // Assert.Fail($"HP RoRAC: {profit.AcquisitionRoRac:P2}, Deal IRR (Eff): {profit.DealIrrEffective:P2}, CoR: {profit.CostOfRisk:P2}");
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

            // Assert.True(profit.AcquisitionRoRac != 0);
            System.IO.File.WriteAllText("mySTAR_RoRAC_Result.txt", $"mySTAR PMT: {deal.MonthlyRate:N2}, mySTAR RoRAC: {profit.AcquisitionRoRac:P2}, Deal IRR (Eff): {profit.DealIrrEffective:P2}");
            // Assert.Fail($"mySTAR RoRAC: {profit.AcquisitionRoRac:P2}, Deal IRR (Eff): {profit.DealIrrEffective:P2}");
        }
    }
}