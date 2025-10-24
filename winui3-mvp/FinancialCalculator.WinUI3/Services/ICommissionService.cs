namespace FinancialCalculator.WinUI3.Services;

public interface ICommissionService
{
    string PolicyVersion { get; }
    double GetAutoCommissionPct(string product);
}