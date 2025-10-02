using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class MainViewModel
{
    partial void OnProductChanged(string value)
    {
        _ = RefreshCommissionPolicyAsync();
        ScheduleSummariesRefresh();
    }
    partial void OnPriceExTaxChanged(double value) { UpdateDealerCommissionResolved(); OnPropertyChanged(nameof(DealerCommissionPctText)); ScheduleSummariesRefresh(); }
    partial void OnDownPaymentAmountChanged(double value) { UpdateDealerCommissionResolved(); OnPropertyChanged(nameof(DealerCommissionPctText)); ScheduleSummariesRefresh(); }
    partial void OnTermMonthsChanged(int value) => ScheduleSummariesRefresh();
    partial void OnCustomerRatePctChanged(double value) => ScheduleSummariesRefresh();
    partial void OnSubsidyBudgetChanged(double value) => ScheduleSummariesRefresh();

    // Additional handlers to keep UI reactive to all inputs (per redesign specs)
    partial void OnTimingChanged(string value) => ScheduleSummariesRefresh();
    partial void OnBalloonPercentChanged(double value) => ScheduleSummariesRefresh();
    partial void OnLockModeChanged(string value) => ScheduleSummariesRefresh();

    partial void OnRateModeChanged(string value) => ScheduleSummariesRefresh();
    partial void OnTargetInstallmentChanged(double value) => ScheduleSummariesRefresh();

    partial void OnDealerCommissionModeChanged(string value) => ScheduleSummariesRefresh();
    partial void OnDealerCommissionPctChanged(double? value)
    {
        if (value.HasValue) DealerCommissionMode = "override";
        UpdateDealerCommissionResolved();
        OnPropertyChanged(nameof(DealerCommissionPctText));
        ScheduleSummariesRefresh();
    }
    partial void OnDealerCommissionAmtChanged(double? value)
    {
        if (value.HasValue) DealerCommissionMode = "override";
        UpdateDealerCommissionResolved();
        OnPropertyChanged(nameof(DealerCommissionPctText));
        ScheduleSummariesRefresh();
    }
    partial void OnDealerCommissionResolvedAmtChanged(double value) => ScheduleSummariesRefresh();

    partial void OnIdcOtherChanged(double value)
    {
        // Mark as user-edited so campaign selection won't auto-overwrite
        IdcOtherUserEdited = true;
        ScheduleSummariesRefresh();
    }
    partial void OnIdcOtherUserEditedChanged(bool value) => ScheduleSummariesRefresh();

    partial void OnSelectedCampaignChanged(CampaignSummaryViewModel? value)
    {
        // Refresh details/metrics/cashflows for the selected Standard campaign without reloading lists
        _debounce.Debounce(0, async _ => { await RefreshActiveSelectionAsync(); });
    }

    partial void OnSelectedMyCampaignChanged(CampaignSummaryViewModel? value)
    {
        // Refresh details/metrics/cashflows for the selected My Campaign
        _debounce.Debounce(0, async _ => { await RefreshActiveSelectionAsync(); });
    }

    private void ScheduleSummariesRefresh()
    {
        _debounce.Debounce(300, async _ => { await LoadSummariesAsync(); await RecalculateAsync(); });
    }
}
