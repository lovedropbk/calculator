using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;

namespace FinancialCalculator.Engine.Core;

/// <summary>
/// Implements yield curve interpolation matching legacy logic (LogLinear).
/// </summary>
public class YieldCurve
{
    private readonly IInterpolation _interpolation;
    private readonly double _minTerm;
    private readonly double _maxTerm;

    /// <summary>
    /// Initializes a new instance of the <see cref="YieldCurve"/> class.
    /// </summary>
    /// <param name="points">Pairs of (Term in Years, Annual Rate as decimal, e.g., 0.05 for 5%).</param>
    public YieldCurve(IEnumerable<(double TermInYears, double AnnualRate)> points)
    {
        var sortedPoints = points.OrderBy(p => p.TermInYears).ToList();
        if (sortedPoints.Count == 0)
            throw new ArgumentException("Yield curve must have at least one point.", nameof(points));

        _minTerm = sortedPoints.First().TermInYears;
        _maxTerm = sortedPoints.Last().TermInYears;

        var terms = sortedPoints.Select(p => p.TermInYears).ToArray();
        var rates = sortedPoints.Select(p => p.AnnualRate).ToArray();

        // Use LogLinearSpline for exponential interpolation between points.
        // Fallback to LinearSpline if rates are zero or negative (LogLinear doesn't handle them well usually, 
        // though MathNet might handle it or throw. MathUtils.InterpolateExponential fell back to linear).
        if (rates.Any(r => r <= 1e-9))
        {
             _interpolation = LinearSpline.InterpolateSorted(terms, rates);
        }
        else
        {
             _interpolation = Interpolate.LogLinear(terms, rates);
        }
    }

    /// <summary>
    /// Gets the interpolated annual rate for a specific term.
    /// </summary>
    /// <param name="termInYears">Term in years.</param>
    /// <returns>Annual rate (decimal).</returns>
    public double GetRate(double termInYears)
    {
        // Extrapolation: constant
        if (termInYears <= _minTerm) return _interpolation.Interpolate(_minTerm);
        if (termInYears >= _maxTerm) return _interpolation.Interpolate(_maxTerm);

        return _interpolation.Interpolate(termInYears);
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