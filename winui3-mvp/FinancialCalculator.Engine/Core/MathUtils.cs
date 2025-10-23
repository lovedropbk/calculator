using System;

namespace FinancialCalculator.Engine.Core;

public static class MathUtils
{
    public static double InterpolateLinear(double x, double x1, double y1, double x2, double y2)
    {
        if (x2 == x1) return y1;
        return y1 + (x - x1) * (y2 - y1) / (x2 - x1);
    }

    public static double InterpolateExponential(double x, double x1, double y1, double x2, double y2)
    {
        // Fallback to linear if rates are non-positive (log undefined/NaN)
        if (y1 <= 1e-9 || y2 <= 1e-9) return InterpolateLinear(x, x1, y1, x2, y2);
        if (x2 == x1) return y1;

        // Lambda weighting for x1 value. 
        // If x = x1, lambda = 1.
        // If x = x2, lambda = 0.
        double lambda = (x2 - x) / (x2 - x1);
        
        return Math.Exp(lambda * Math.Log(y1) + (1.0 - lambda) * Math.Log(y2));
    }
}