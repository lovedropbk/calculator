using System;

namespace FinancialCalculator.WinUI3.Services
{
    /// <summary>
    /// Safe, reusable conversions between THB and percent, with clamping and sensible fallbacks.
    /// Use this for Down Payment, Balloon, and IDC Commission unit switching.
    /// </summary>
    public static class UnitConversionHelper
    {
        /// <summary>
        /// Convert an absolute amount (THB) to percent of base (Transaction Price).
        /// If base is not available or invalid, returns the provided fallback percent (clamped 0-100).
        /// </summary>
        public static double MoneyToPercent(double amountThb, double baseThb, double fallbackPercent = 20.0)
        {
            if (double.IsNaN(amountThb) || double.IsInfinity(amountThb)) amountThb = 0;
            if (double.IsNaN(baseThb) || double.IsInfinity(baseThb) || baseThb <= 0)
            {
                return ClampPercent(fallbackPercent);
            }

            var pct = (amountThb / baseThb) * 100.0;
            return ClampPercent(Math.Round(pct, 2, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Convert a percent value to an absolute amount (THB) of base (Transaction Price).
        /// If base is not available or invalid, returns the provided fallback amount (sanitized).
        /// </summary>
        public static double PercentToMoney(double percent, double baseThb, double fallbackAmount = 0.0)
        {
            if (double.IsNaN(percent) || double.IsInfinity(percent)) percent = 0;
            if (double.IsNaN(baseThb) || double.IsInfinity(baseThb) || baseThb <= 0)
            {
                return SanitizeAmount(fallbackAmount);
            }

            var p = ClampPercent(percent);
            var amt = baseThb * p / 100.0;
            return SanitizeAmount(amt);
        }

        /// <summary>Clamp percent to 0-100 with 2dp rounding.</summary>
        public static double ClampPercent(double percent)
        {
            if (double.IsNaN(percent) || double.IsInfinity(percent)) return 0.0;
            if (percent < 0) return 0;
            if (percent > 100) return 100;
            return Math.Round(percent, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>Sanitize currency amounts (non-negative, integer rounding).</summary>
        public static double SanitizeAmount(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return 0;
            return Math.Max(0, Math.Round(v, 0, MidpointRounding.AwayFromZero));
        }
    }
}