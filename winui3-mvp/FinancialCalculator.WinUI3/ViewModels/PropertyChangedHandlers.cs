using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class MainViewModel
{
    partial void OnProductChanged(string value) => ScheduleSummariesRefresh();
    partial void OnPriceExTaxChanged(double value) => ScheduleSummariesRefresh();
    partial void OnDownPaymentAmountChanged(double value) => ScheduleSummariesRefresh();
    partial void OnTermMonthsChanged(int value) => ScheduleSummariesRefresh();
    partial void OnCustomerRatePctChanged(double value) => ScheduleSummariesRefresh();
    partial void OnSubsidyBudgetChanged(double value) => ScheduleSummariesRefresh();

    private void ScheduleSummariesRefresh()
    {
        _debounce.Debounce(300, async _ => { await LoadSummariesAsync(); await RecalculateAsync(); });
    }
}
