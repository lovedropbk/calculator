using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinancialCalculator.WinUI3.Services;

public interface IStandardRateService
{
    Task LoadAsync();
    // Optional override for tests or alternative data source (e.g., future HTTP updates)
    Task LoadAsync(string? overridePath);
    double? GetStandardRate(string product, int term, double downPaymentPct, string paymentMode);
    IReadOnlyList<int> GetAvailableTerms(string product, double downPaymentPct, string paymentMode);
}