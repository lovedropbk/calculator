using System;
using System.Collections.Generic;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.Tests;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting Risk Engine Test...");

        var repo = new RiskParameterRepository();
        // HACK: Hardcoded path for immediate testing in this environment.
        string paramPath = @"C:\Users\PATKRAN\Python\07_controlling\financial_calculator\winui3-mvp\docs\parameters";
        
        Console.WriteLine($"Loading parameters from: {paramPath}");
        repo.Load(paramPath);

        // Test Case: DEALER, 5.0
        string customerType = "DEALER";
        string rating = "5, 5.0"; 
        string assetState = "N"; 
        string avc = "MBPC"; 

        double pd = repo.GetPd(customerType, rating);
        var lgd = repo.GetLgd(customerType, assetState, avc);
        double cor = BaselIIEngine.CalculateEL(pd, lgd.DcfLgd);
        double ecTotal = repo.GetEcTotal();

        Console.WriteLine($"PD: {pd:P4}, LGD(DCF): {lgd.DcfLgd:F4}, CoR: {cor:P4}, EC Total: {ecTotal:P4}");

        // --- DCF Test with Subsidies ---
        Console.WriteLine("\n--- DCF Test with Subsidies ---");
        var inputs = new CalculatorInputs
        {
            VehicleSalesPrice = 2_000_000,
            DownpaymentValue = 500_000,
            TermMonths = 48,
            CustomerRatePercent = 3.99m,
            UpfrontSubsidies = 50_000, // 50k subsidy
            UpfrontCosts = 20_000      // 20k IDC
        };
        
        var calc = new FinancialCalculator.Engine.Core.FinancialCalculator();
        var outputs = calc.Calculate(inputs);
        Console.WriteLine($"Financed: {outputs.FinancedAmount:N2}");
        Console.WriteLine($"T0 Subsidies: {outputs.T0UpfrontSubsidies:N2}");
        Console.WriteLine($"T0 Costs: {outputs.T0UpfrontCosts:N2}");

        var cofParams = new CofParams 
        { 
            Curve = new Dictionary<int, decimal> { { 48, 0.025m } }, // 2.5% MFR
            CostOfRisk = (decimal)cor,
            EconCapRatio = (decimal)ecTotal,
            OpexPct = -0.01m
        };

        var profit = DcfModel.Compute(outputs, cofParams);
        Console.WriteLine($"Net EBIT Margin: {profit.NetEbitMargin:P4}");
        Console.WriteLine($"RoRAC: {profit.AcquisitionRoRac:P4}");
        Console.WriteLine($"IDC Upfront (Annualized): {profit.IdcUpfrontAnnualizedPct:P4}");
        Console.WriteLine($"Subsidy Upfront (Annualized): {profit.SubsidyUpfrontAnnualizedPct:P4}");
    }
}
