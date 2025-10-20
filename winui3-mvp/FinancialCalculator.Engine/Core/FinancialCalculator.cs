using FinancialCalculator.Engine.Models;

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
            Schedule = schedule
        };
    }

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
        if (input.Product != FinancialProduct.HirePurchase)
        {
            balloon = input.BalloonIsPercent ? financed * input.BalloonPercent / 100m : input.BalloonTHB;
            balloon = Math.Clamp(balloon, 0m, financed);
        }

        var i_m = input.CustomerRatePercent / 100m / 12m;


        // Compute annuity payment (arrears), then adjust for advance if needed
        decimal Payment(decimal pv, decimal fv, int periods, decimal rate)
        {
            if (periods <= 0) return 0m;
            if (Math.Abs(rate) < 1e-12m)
            {
                return (pv - fv) / periods;
            }
            var r = rate;
            var pow = (decimal)Math.Pow((double)(1 + r), periods);
            var pmt = (pv * pow + fv) * r / (pow - 1);
            return pmt;
        }

        var pmtArrears = Payment(financed, -balloon, n, i_m);
        var pmt = input.PaymentMode == PaymentMode.InAdvance ? pmtArrears / (1 + i_m) : pmtArrears;

        var bal = financed;
        var rows = new List<ScheduleRow>(n);

        // t=0 payment for advance mode (period 0) booked into first row cashflow aggregation by adding to t=1
        if (input.PaymentMode == PaymentMode.InAdvance)
        {
            // First payment at t=0 reduces principal immediately by pmt
        }

        for (int k = 1; k <= n; k++)
        {
            var interest = Decimal.Round(bal * i_m, 10);
            var fee = 0m;
            var totalDue = pmt;

            decimal principal;
            if (k == n && balloon > 0)
            {
                // On final period, balloon due in addition to regular installment
                principal = totalDue - interest - fee;
                var newBal = bal - principal;
                var cf = totalDue + balloon;
                rows.Add(new ScheduleRow
                {
                    Period = k,
                    Principal = Decimal.Round(principal, 2),
                    Interest = Decimal.Round(interest, 2),
                    Balance = Decimal.Round(newBal, 2),
                    Cashflow = Decimal.Round(cf, 2)
                });
                bal = newBal - balloon;
                if (bal < 0) bal = 0;
                break;
            }
            else
            {
                principal = totalDue - interest - fee;
                if (principal > bal) principal = bal;
                var newBal = bal - principal;
                rows.Add(new ScheduleRow
                {
                    Period = k,
                    Principal = Decimal.Round(principal, 2),
                    Interest = Decimal.Round(interest, 2),
                    Balance = Decimal.Round(newBal, 2),
                    Cashflow = Decimal.Round(totalDue, 2)
                });
                bal = newBal;
            }
        }

        // Adjust for annuity-due (advance): recompute with t=0 payment applied as principal reduction
        if (input.PaymentMode == PaymentMode.InAdvance)
        {
            var pmt0 = pmt; // first payment at t=0
            var bal0 = financed - pmt0; if (bal0 < 0) bal0 = 0;
            rows.Clear();
            bal = bal0;
            for (int k = 1; k <= n; k++)
            {
                var interest = Decimal.Round(bal * i_m, 10);
                var fee = 0m;
                var totalDue = pmt;
                decimal principal;
                if (k == n && balloon > 0)
                {
                    principal = totalDue - interest - fee;
                    var newBal = bal - principal;
                    var cf = totalDue + balloon;
                    rows.Add(new ScheduleRow
                    {
                        Period = k,
                        Principal = Decimal.Round(principal, 2),
                        Interest = Decimal.Round(interest, 2),
                        Balance = Decimal.Round(newBal, 2),
                        Cashflow = Decimal.Round(cf, 2)
                    });
                    bal = newBal - balloon;
                    if (bal < 0) bal = 0;
                    break;
                }
                else
                {
                    principal = totalDue - interest - fee;
                    if (principal > bal) principal = bal;
                    var newBal = bal - principal;
                    rows.Add(new ScheduleRow
                    {
                        Period = k,
                        Principal = Decimal.Round(principal, 2),
                        Interest = Decimal.Round(interest, 2),
                        Balance = Decimal.Round(newBal, 2),
                        Cashflow = Decimal.Round(totalDue, 2)
                    });
                    bal = newBal;
                }
            }
            // Note: For IRR, we will include the t=0 payment explicitly in cashflows below.
        }

        return rows;
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
        var cf = new List<double>(schedule.Count + 2);
        // t0: lender disburses financed amount (negative), applies upfront incomes and costs
        var t0 = -(double)financed + (double)input.UpfrontSubsidies - (double)input.UpfrontCosts;
        cf.Add(t0);

        if (input.PaymentMode == PaymentMode.InAdvance && schedule.Count > 0)
        {
            // Add first payment at t=0; remaining payments occur at t=1..n-1
            var pmt0 = (double)(schedule[0].Cashflow);
            cf[0] += pmt0;
            for (int i = 1; i < schedule.Count; i++) cf.Add((double)schedule[i].Cashflow);
        }
        else
        {
            foreach (var r in schedule) cf.Add((double)r.Cashflow);
        }

        // Solve monthly nominal IRR, annualize by *12 and convert to percent
        var rMonthly = Irr(cf);
        var rAnnual = rMonthly * 12.0 * 100.0;
        return (decimal)rAnnual;
    }

    // Newton-Raphson IRR with bisection fallback
    private static double Irr(IReadOnlyList<double> cashflows)
    {
        double Npv(double rate)
        {
            double npv = 0;
            double df = 1.0;
            for (int t = 0; t < cashflows.Count; t++)
            {
                if (t > 0) df *= (1.0 + rate);
                npv += cashflows[t] / df;
            }
            return npv;
        }

        double Dnpv(double rate)
        {
            double d = 0;
            for (int t = 1; t < cashflows.Count; t++)
            {
                d -= t * cashflows[t] / Math.Pow(1.0 + rate, t + 1);
            }
            return d;
        }

        // Initial guess: 1% monthly
        double r = 0.01;
        for (int iter = 0; iter < 50; iter++)
        {
            double f = Npv(r);
            double df = Dnpv(r);
            if (Math.Abs(df) < 1e-12) break;
            double rNext = r - f / df;
            if (double.IsNaN(rNext) || double.IsInfinity(rNext)) break;
            if (Math.Abs(rNext - r) < 1e-10) return rNext;
            r = rNext;
        }

        // Bisection fallback
        double lo = -0.99, hi = 1.0; // -99% to 100% per month
        double flo = Npv(lo), fhi = Npv(hi);
        if (double.IsNaN(flo) || double.IsNaN(fhi) || flo * fhi > 0) return r;
        for (int i = 0; i < 200; i++)
        {
            double mid = 0.5 * (lo + hi);
            double fm = Npv(mid);
            if (Math.Abs(fm) < 1e-10) return mid;
            if (flo * fm <= 0) { hi = mid; fhi = fm; }
            else { lo = mid; flo = fm; }
        }
        return r;
    }
}
