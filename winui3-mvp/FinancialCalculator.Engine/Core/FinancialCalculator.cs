using System;
using System.Collections.Generic;
using System.Linq;
using FinancialCalculator.Engine.Models;
using MathNet.Numerics.RootFinding;

namespace FinancialCalculator.Engine.Core;

public sealed class FinancialCalculator
{
    public CalculatorOutputs Calculate(CalculatorInputs input)
    {
        var financed = ComputeFinancedAmount(input);
        var schedule = BuildSchedule(input, financed);
        var monthly = schedule.Count > 0 ? schedule[0].Cashflow : 0m;

        var dealIrr = ComputeIrrAnnualPercent(input, financed, schedule);
        var dealIrrNoUpfrontIncomes = ComputeIrrAnnualPercent(input with { UpfrontSubsidies = 0 }, financed, schedule);
        var dealIrrNoUpfrontCosts = ComputeIrrAnnualPercent(input with { UpfrontCosts = 0 }, financed, schedule);
        var dealIrrBaseline = ComputeIrrAnnualPercent(input with { UpfrontSubsidies = 0, UpfrontCosts = 0 }, financed, schedule);

        var flatRate = ComputeFlatRatePercent(financed, schedule);

        return new CalculatorOutputs
        {
            Inputs = input,
            FinancedAmount = Decimal.Round(financed, 2),
            MonthlyRate = Decimal.Round(monthly, 2),
            FlatRatePercentPerAnnum = Decimal.Round(flatRate, 6),
            DealIrrAnnualPercent = Decimal.Round(dealIrr, 6),
            DealIrrAnnualPercentWithoutUpfrontIncomes = Decimal.Round(dealIrrNoUpfrontIncomes, 6),
            DealIrrAnnualPercentWithoutUpfrontCosts = Decimal.Round(dealIrrNoUpfrontCosts, 6),
            DealIrrAnnualPercentBaseline = Decimal.Round(dealIrrBaseline, 6),
            Schedule = schedule,
            T0Disbursement = financed,
            T0UpfrontSubsidies = input.UpfrontSubsidies,
            T0UpfrontCosts = input.UpfrontCosts
        };
    }

    // Rate conversions moved to RateConverter class

    private static decimal ComputeFinancedAmount(CalculatorInputs input)
    {
        var dp = input.DownpaymentIsPercent
            ? input.VehicleSalesPrice * input.DownpaymentValue / 100m
            : input.DownpaymentValue;

        var baseFinanced = input.VehicleSalesPrice + input.AdditionalFinancedItems - dp;
        if (baseFinanced < 0) baseFinanced = 0m;

        // Apply subdown: reduces financed amount, not booked as t0 income
        decimal subdown = 0m;
        if (input.SubdownIsPercent)
            subdown = input.VehicleSalesPrice * input.SubdownPercent / 100m;
        else
            subdown = input.SubdownTHB;
        subdown = Math.Clamp(subdown, 0m, baseFinanced);

        var financed = baseFinanced - subdown;
        return financed;
    }

    private static List<ScheduleRow> BuildSchedule(CalculatorInputs input, decimal financed)
    {
        var n = input.TermMonths;
        if (n <= 0 || financed <= 0) return new List<ScheduleRow>();

        decimal balloon = 0m;
        // Allow balloon for all products (including HP) as per new requirement
        balloon = input.BalloonIsPercent ? financed * input.BalloonPercent / 100m : input.BalloonTHB;
        balloon = Math.Clamp(balloon, 0m, financed);

        var i_m = input.CustomerRatePercent / 100m / 12m;

        // Local annuity helper
        decimal Payment(decimal pv, decimal fv, int periods, decimal rate)
        {
            if (periods <= 0) return 0m;
            if (Math.Abs(rate) < 1e-12m) return (pv - fv) / periods;
            var r = rate;
            var pow = (decimal)Math.Pow((double)(1 + r), periods);
            var pmtLocal = (pv * pow + fv) * r / (pow - 1);
            return pmtLocal;
        }

        // Resolve initial installment ignoring holidays; will be re-amortized after holiday blocks
        var pmtArrears0 = Payment(financed, -balloon, n, i_m);
        var currentPmt = input.PaymentMode == PaymentMode.InAdvance ? pmtArrears0 / (1 + i_m) : pmtArrears0;

        var rows = new List<ScheduleRow>(n);
        var bal = financed;

        // Build holiday map
        var holidays = new bool[n + 2];
        var ruleIds = new string?[n + 2];
        var rules = input.PaymentHolidays ?? Array.Empty<PaymentHolidayRule>();
        foreach (var r in rules)
        {
            int a = Math.Max(1, r.StartPeriod);
            int b = Math.Min(n, r.EndPeriod);
            for (int k = a; k <= b; k++)
            {
                holidays[k] = true;
                if (ruleIds[k] == null) ruleIds[k] = r.RuleId;
            }
        }

        if (input.PaymentMode == PaymentMode.InAdvance)
        {
            // Period 1 special handling
            if (n >= 1)
            {
                if (holidays[1])
                {
                    // No payment at t0; no interest
                    var newBalRaw = bal;
                    rows.Add(new ScheduleRow
                    {
                        Period = 1,
                        Principal = 0m,
                        Interest = 0m,
                        Cashflow = 0m,
                        Balance = Decimal.Round(newBalRaw, 2),
                        Kind = PaymentKind.Holiday,
                        CapitalizedInterest = 0m,
                        RuleId = ruleIds[1]
                    });
                    bal = newBalRaw;

                    // If holiday ends after period 1, re-amortize remaining periods
                    if ((n == 1) || !holidays[2])
                    {
                        var rem = n - 1;
                        if (rem > 0)
                        {
                            var pmtAr = Payment(bal, -balloon, rem, i_m);
                            currentPmt = pmtAr; // post t0 uses arrears-style
                        }
                    }
                }
                else
                {
                    // Regular t0 payment
                    var pmt0 = currentPmt;
                    var bal0 = bal - pmt0;
                    if (bal0 < 0) bal0 = 0;
                    rows.Add(new ScheduleRow
                    {
                        Period = 1,
                        Principal = Decimal.Round(pmt0, 2),
                        Interest = 0m,
                        Balance = Decimal.Round(bal0, 2),
                        Cashflow = Decimal.Round(pmt0, 2)
                    });
                    bal = bal0;
                }
            }

            // Periods 2..N
            for (int k = 2; k <= n; k++)
            {
                ProcessPeriod(rows, k, ref bal, i_m, ref currentPmt, balloon, n, holidays, ruleIds, Payment);
            }

            return rows;
        }

        // In Arrears: periods 1..N
        for (int k = 1; k <= n; k++)
        {
            ProcessPeriod(rows, k, ref bal, i_m, ref currentPmt, balloon, n, holidays, ruleIds, Payment);
        }

        return rows;
    }

    private static decimal AddScheduleRow(List<ScheduleRow> rows, int period, decimal bal, decimal i_m, decimal pmt, decimal balloon, bool isFinal)
    {
        var interest = Decimal.Round(bal * i_m, 10);
        var totalDue = pmt;
        decimal principal, cf, newBal;

        if (isFinal && balloon > 0)
        {
            // Final period with balloon
            cf = totalDue + balloon;
            var principalFromPmt = totalDue - interest;
            newBal = bal - principalFromPmt - balloon;
            if (newBal < 0) newBal = 0;

            // Total principal paid this period = principalFromPmt + balloon
            principal = principalFromPmt + balloon;

            rows.Add(new ScheduleRow
            {
                Period = period,
                Principal = Decimal.Round(principal, 2),
                Interest = Decimal.Round(interest, 2),
                Balance = Decimal.Round(newBal, 2),
                Cashflow = Decimal.Round(cf, 2)
            });
            return newBal;
        }
        else
        {
            // Regular period
            principal = totalDue - interest;
            if (principal > bal) principal = bal;
            newBal = bal - principal;

            rows.Add(new ScheduleRow
            {
                Period = period,
                Principal = Decimal.Round(principal, 2),
                Interest = Decimal.Round(interest, 2),
                Balance = Decimal.Round(newBal, 2),
                Cashflow = Decimal.Round(totalDue, 2)
            });
            return newBal;
        }
    }

    private static void AddHolidayRow(List<ScheduleRow> rows, int period, ref decimal bal, decimal i_m, string? ruleId)
    {
        var interest = Decimal.Round(bal * i_m, 10);
        var newBalRaw = bal + interest;
        rows.Add(new ScheduleRow
        {
            Period = period,
            Principal = 0m,
            Interest = 0m,
            Balance = Decimal.Round(newBalRaw, 2),
            Cashflow = 0m,
            Kind = PaymentKind.Holiday,
            CapitalizedInterest = Decimal.Round(interest, 2),
            RuleId = ruleId
        });
        bal = newBalRaw;
    }

    private static decimal ReAmortizeIfEnd(bool isEnd, int n, int period, decimal bal, decimal balloon, decimal i_m, Func<decimal, decimal, int, decimal, decimal> payment, decimal currentPmt)
    {
        if (isEnd && period < n)
        {
            var rem = n - period;
            var pmtAr = payment(bal, -balloon, rem, i_m);
            return pmtAr;
        }
        return currentPmt;
    }

    private static void ProcessPeriod(
        List<ScheduleRow> rows,
        int k,
        ref decimal bal,
        decimal i_m,
        ref decimal currentPmt,
        decimal balloon,
        int n,
        bool[] holidays,
        string?[] ruleIds,
        Func<decimal, decimal, int, decimal, decimal> payment)
    {
        if (holidays[k])
        {
            AddHolidayRow(rows, k, ref bal, i_m, ruleIds[k]);

            bool isEnd = (k == n) || !holidays[k + 1];
            currentPmt = ReAmortizeIfEnd(isEnd, n, k, bal, balloon, i_m, payment, currentPmt);
        }
        else
        {
            bool isFinal = k == n;
            bal = AddScheduleRow(rows, k, bal, i_m, currentPmt, balloon, isFinal);
        }
    }

    private static decimal ComputeFlatRatePercent(decimal financed, IReadOnlyList<ScheduleRow> schedule)
    {
        if (financed <= 0 || schedule.Count == 0) return 0m;
        var n = schedule.Count;
        var totalInterestPlusFees = schedule.Sum(r => r.Interest);
        var years = n / 12m;
        if (years <= 0) return 0m;
        var flat = (totalInterestPlusFees / financed) / years * 100m;
        return flat;
    }

    private static decimal ComputeIrrAnnualPercent(CalculatorInputs input, decimal financed, IReadOnlyList<ScheduleRow> schedule)
    {
        var cf = new List<double>(schedule.Count + 1);
        // t0: lender disburses financed amount (negative), applies upfront incomes and costs
        var t0 = -(double)financed + (double)input.UpfrontSubsidies - (double)input.UpfrontCosts;

        int start = 0;
        if (input.PaymentMode == PaymentMode.InAdvance && schedule.Count > 0)
        {
            // InAdvance: Period 1 (index 0) is at t=0.
            t0 += (double)schedule[0].Cashflow;
            start = 1; // Remaining cashflows start from Period 2 (index 1) at t=1
        }

        cf.Add(t0);
        for (int i = start; i < schedule.Count; i++)
        {
            cf.Add((double)schedule[i].Cashflow);
        }

        // Solve monthly nominal IRR, annualize by *12 and convert to percent
        var rMonthly = Irr(cf);
        var rAnnual = rMonthly * 12.0 * 100.0;
        return (decimal)rAnnual;
    }

    // Use MathNet.Numerics RootFinding for IRR
    private static double Irr(IReadOnlyList<double> cashflows)
    {
        try
        {
             // IRR is the rate that makes NPV = 0
             Func<double, double> npv = rate =>
             {
                 double sum = 0;
                 for (int i = 0; i < cashflows.Count; i++)
                 {
                     sum += cashflows[i] / Math.Pow(1 + rate, i);
                 }
                 return sum;
             };

             // Search from -90% to 1000% per period (broad range)
             return Brent.FindRoot(npv, -0.9, 10.0, accuracy: 1e-8);
        }
        catch
        {
            return 0; // Fallback if fails
        }
    }

}
