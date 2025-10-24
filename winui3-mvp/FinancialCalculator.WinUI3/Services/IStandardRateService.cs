using System.Threading.Tasks;

namespace FinancialCalculator.WinUI3.Services;

public interface IStandardRateService
{
    Task LoadAsync();
    double? GetStandardRate(string product, int term, double downPaymentPct, string paymentMode);
}