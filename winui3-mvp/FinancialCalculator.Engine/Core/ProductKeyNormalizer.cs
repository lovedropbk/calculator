using System;

namespace FinancialCalculator.Engine.Core
{
    internal static class ProductKeyNormalizer
    {
        // Normalize a variety of user-facing product strings into canonical keys
        // Canonical forms used across engine/config:
        //  - "HP"
        //  - "mySTAR"
        //  - "FinanceLease"
        //  - "OperatingLease"
        internal static string Normalize(string product)
        {
            var p = (product ?? string.Empty).Trim();

            // Fast canonical checks first
            if (p.StartsWith("HP", StringComparison.OrdinalIgnoreCase)) return "HP";

            // Case-insensitive contains to catch multiple variants (input may be mixed case)
            if (p.Contains("MYSTAR", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("mySTAR", StringComparison.OrdinalIgnoreCase))
                return "mySTAR";

            // Finance Lease variants (abbrev and words)
            if (p.Equals("FL", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("F-LEAS", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("F-LEASE", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("FINANCE", StringComparison.OrdinalIgnoreCase))
                return "FinanceLease";

            // Operating Lease variants (abbrev and words)
            if (p.Equals("OL", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("OP-LEAS", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("OP-LEASE", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("OPERAT", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("Operating", StringComparison.OrdinalIgnoreCase))
                return "OperatingLease";

            // Default: return as-is to preserve unknown/custom key usage
            return p;
        }
    }
}