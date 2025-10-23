using System;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.Engine.Core;

public static class RateConverter
{
    public static decimal ConvertFlatToNominal(decimal flatRatePercent, int termMonths, PaymentMode paymentMode)
    {
        if (termMonths <= 0) return 0m;
        if (flatRatePercent == 0m) return 0m;

        // Assume financed = 100 for simplified calculation (percentage based)
        double financed = 100.0;
        double totalInterest = (double)flatRatePercent / 100.0 * financed * (termMonths / 12.0);
        double totalPayments = financed + totalInterest;
        double pmt = totalPayments / termMonths;

        // Solve for nominal rate that gives this PMT
        // PV = financed, FV = 0 (assuming no balloon for standard flat rate conversion), N = termMonths
        double rMonthly = Rate(termMonths, pmt, -financed, 0, paymentMode);
        return (decimal)(rMonthly * 12.0 * 100.0);
    }

    public static decimal ConvertNominalToFlat(decimal nominalRatePercent, int termMonths, PaymentMode paymentMode)
    {
        if (termMonths <= 0) return 0m;
        decimal financed = 100m; // Basis
        decimal i_m = nominalRatePercent / 100m / 12m;
        
        // PMT calculation
        decimal pmt;
        if (Math.Abs(i_m) < 1e-12m)
        {
            pmt = financed / termMonths;
        }
        else
        {
            double r = (double)i_m;
            double pow = Math.Pow(1 + r, termMonths);
            pmt = (decimal)((double)financed * pow * r / (pow - 1));
        }

        if (paymentMode == PaymentMode.InAdvance)
        {
            pmt /= (1 + i_m);
        }

        decimal totalPayments = pmt * termMonths;
        decimal totalInterest = totalPayments - financed;
        decimal years = termMonths / 12m;
        
        return (totalInterest / financed) / years * 100m;
    }

    // Duplicated Rate function from FinancialCalculator for stateless helper
    private static double Rate(int nper, double pmt, double pv, double fv, PaymentMode paymentMode)
    {
        if (nper <= 0) return 0;

        double F(double r)
        {
            if (Math.Abs(r) < 1e-12) return pv + pmt * nper + fv;
            double pow = Math.Pow(1 + r, nper);
            double type = paymentMode == PaymentMode.InAdvance ? (1 + r) : 1.0;
            return pv * pow + pmt * type * (pow - 1) / r + fv;
        }

        double DF(double r)
        {
            if (Math.Abs(r) < 1e-12) return pmt * nper * (paymentMode == PaymentMode.InAdvance ? 1 : 0);
            double h = 1e-5;
            return (F(r + h) - F(r - h)) / (2 * h);
        }

        double r_guess = 0.01;
        for (int i = 0; i < 50; i++)
        {
            double y = F(r_guess);
            if (Math.Abs(y) < 1e-9) return r_guess;
            double dy = DF(r_guess);
            if (Math.Abs(dy) < 1e-12) break;
            r_guess = r_guess - y / dy;
        }
        return r_guess;
    }
}