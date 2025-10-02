using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class MainViewModel
{
    partial void OnProductChanged(string value)
    {
        _ = RefreshCommissionPolicyAsync();
        OnPropertyChanged(nameof(IsBalloonEnabled));
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
    partial void OnDownPaymentUnitChanged(string value) { OnPropertyChanged(nameof(DownPaymentPlaceholder)); OnPropertyChanged(nameof(DownPaymentUnitSuffix)); ScheduleSummariesRefresh(); }
    partial void OnDownPaymentValueEntryChanged(double value) => ScheduleSummariesRefresh();
    partial void OnBalloonUnitChanged(string value) { OnPropertyChanged(nameof(BalloonPlaceholder)); OnPropertyChanged(nameof(BalloonUnitSuffix)); ScheduleSummariesRefresh(); }
    partial void OnBalloonValueEntryChanged(double value) => ScheduleSummariesRefresh();
    partial void OnLockModeChanged(string value) => ScheduleSummariesRefresh();

    partial void OnRateModeChanged(string value) { OnPropertyChanged(nameof(IsFixedRateMode)); OnPropertyChanged(nameof(IsTargetInstallmentMode)); RateModeIndex = string.Equals(RateMode, "fixed_rate", System.StringComparison.OrdinalIgnoreCase) ? 0 : 1; ScheduleSummariesRefresh(); }
    partial void OnRateModeIndexChanged(int value) { RateMode = value == 0 ? "fixed_rate" : "target_installment"; }
    partial void OnTargetInstallmentChanged(double value) => ScheduleSummariesRefresh();

    partial void OnDealerCommissionModeChanged(string value) => ScheduleSummariesRefresh();
    partial void OnCommissionEntryUnitChanged(string value)
    {
        if (string.Equals(value, "auto", System.StringComparison.OrdinalIgnoreCase))
        {
            DealerCommissionMode = "auto"; DealerCommissionPct = null; DealerCommissionAmt = null; CommissionEntryValue = 0;
        }
        else
        {
            DealerCommissionMode = "override";
        }
        ScheduleSummariesRefresh();
    }
    partial void OnCommissionEntryValueChanged(double value)
    {
        if (!string.Equals(CommissionEntryUnit, "auto", System.StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(CommissionEntryUnit, "%", System.StringComparison.OrdinalIgnoreCase)) { DealerCommissionPct = value / 100.0; DealerCommissionAmt = null; }
            else { DealerCommissionAmt = value; DealerCommissionPct = null; }
            UpdateDealerCommissionResolved();
            OnPropertyChanged(nameof(DealerCommissionPctText));
        }
        ScheduleSummariesRefresh();
    }
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

    private CampaignSummaryViewModel? _subscribedMyCampaign;

    partial void OnSelectedMyCampaignChanged(CampaignSummaryViewModel? value)
    {
        // Unhook prior
        if (_subscribedMyCampaign != null)
        {
            _subscribedMyCampaign.PropertyChanged -= OnMyCampaignPropertyChanged;
            _subscribedMyCampaign = null;
        }

        // Enable subsidy budget editing only when a My Campaign is selected AND allocations exceed initial budget
        SubsidyBudgetIsEnabled = value != null && ExceedsInitialSubsidy(value);

        if (value != null)
        {
            _subscribedMyCampaign = value;
            _subscribedMyCampaign.PropertyChanged += OnMyCampaignPropertyChanged;
        }

        _debounce.Debounce(0, async _ => { await RefreshActiveSelectionAsync(); });
    }

    private void OnMyCampaignPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is CampaignSummaryViewModel mc)
        {
            SubsidyBudgetIsEnabled = ExceedsInitialSubsidy(mc);
            // Auto-recalc when adjustments change
            _debounce.Debounce(200, async _ => { await RefreshActiveSelectionAsync(); });
        }
    }

    private bool ExceedsInitialSubsidy(CampaignSummaryViewModel mc)
    {
        // Sum allocations from editable fields (cash discount considered a reduction of price, not subsidy)
        var used = mc.FSSubDownAmount + mc.FSSubInterestAmount + mc.FSFreeMBSPAmount;
        return used > SubsidyBudget + 1e-9; // small epsilon
    }

    private void ScheduleSummariesRefresh()
    {
        _debounce.Debounce(300, async _ => { await LoadSummariesAsync(); await RecalculateAsync(); });
    }
}
