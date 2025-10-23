using System;
using System.Collections.Generic;
using System.Linq;

namespace FinancialCalculator.Engine.Core;

/// <summary>
/// Implements yield curve interpolation matching legacy VBA logic (Linear/Exponential).
/// </summary>
public class YieldCurve
{
    private readonly SortedDictionary<double, double> _points;

    /// <summary>
    /// Initializes a new instance of the <see cref="YieldCurve"/> class.
    /// </summary>
    /// <param name="points">Pairs of (Term in Years, Annual Rate as decimal, e.g., 0.05 for 5%).</param>
    public YieldCurve(IEnumerable<(double TermInYears, double AnnualRate)> points)
    {
        _points = new SortedDictionary<double, double>(points.ToDictionary(p => p.TermInYears, p => p.AnnualRate));
        if (_points.Count == 0)
            throw new ArgumentException("Yield curve must have at least one point.", nameof(points));
    }

    /// <summary>
    /// Gets the interpolated annual rate for a specific term.
    /// </summary>
    /// <param name="termInYears">Term in years.</param>
    /// <returns>Annual rate (decimal).</returns>
    public double GetRate(double termInYears)
    {
        if (_points.Count == 1) return _points.Values.First();

        // Extrapolation: simplified to constant for now to avoid wild linear swings if not intended.
        // Legacy might have used linear, but constant is safer without more data.
        if (termInYears <= _points.Keys.First()) return _points.Values.First();
        if (termInYears >= _points.Keys.Last()) return _points.Values.Last();

        // Find bracketing points
        // LastOrDefault with predicate works because it's a SortedDictionary keys are ordered, 
        // but standard LINQ LastOrDefault might be slow. sorting is guaranteed by SortedDictionary definition.
        // Efficient lookup would be binary search, but for small curves LINQ is okay.
        
        double x1 = 0, y1 = 0, x2 = 0, y2 = 0;
        
        foreach (var point in _points)
        {
            if (point.Key <= termInYears)
            {
                x1 = point.Key;
                y1 = point.Value;
            }
            else
            {
                x2 = point.Key;
                y2 = point.Value;
                break;
            }
        }

        return MathUtils.InterpolateExponential(termInYears, x1, y1, x2, y2);
    }

    /// <summary>
    /// Calculates the Discount Factor for a specific term using the interpolated rate.
    /// DF = 1 / (1 + r)^t
    /// </summary>
    /// <param name="termInYears">Term in years.</param>
    /// <returns>Discount Factor.</returns>
    public double GetDiscountFactor(double termInYears)
    {
        if (termInYears == 0) return 1.0;
        double rate = GetRate(termInYears);
        return 1.0 / Math.Pow(1.0 + rate, termInYears);
    }
}