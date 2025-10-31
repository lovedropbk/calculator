using System.Collections.Generic;

namespace FinancialCalculator.Engine.Core
{
    /// <summary>
    /// Centralized provider for Cost of Funds (CoF) inputs used by DCF/RoRAC:
    /// - MFR term curve (Matched Funding Rate) keyed by termMonths
    /// - MFS (Matched Funding Spread), scalar
    /// - OPEX by product (positive scalar; engine applies sign as needed)
    /// Implementations should load once (e.g., from config.yaml) and then serve immutable snapshots.
    /// </summary>
    public interface ICostOfFundsService
    {
        /// <summary>Full MFR curve keyed by term months.</summary>
        IReadOnlyDictionary<int, decimal> GetCurve();

        /// <summary>Nearest MFR for a given term (months).</summary>
        decimal GetNearestMfrRate(int termMonths);

        /// <summary>Matched funding spread (MFS), scalar (e.g., 0.0025).</summary>
        decimal GetMatchedFundingSpread();

        /// <summary>OPEX percentage for a given product key (e.g., HP, mySTAR, FinanceLease, OperatingLease).</summary>
        decimal GetOpexPctForProduct(string product);
    }
}