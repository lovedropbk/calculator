using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class MainViewModel
{
    private void OnProductChanged(string value)
    {
        RefreshCommissionPolicyLocal();
        OnPropertyChanged(nameof(IsBalloonEnabled));
        ScheduleSummariesRefresh();
    }
    private void OnPriceExTaxChanged(double value) { UpdateDealerCommissionResolved(); OnPropertyChanged(nameof(DealerCommissionPctText)); ScheduleSummariesRefresh(); }
    private void OnDownPaymentAmountChanged(double value) { UpdateDealerCommissionResolved(); OnPropertyChanged(nameof(DealerCommissionPctText)); ScheduleSummariesRefresh(); }
    private void OnTermMonthsChanged(int value) => ScheduleSummariesRefresh();
    private void OnCustomerRatePctChanged(double value) => ScheduleSummariesRefresh();
    private void OnSubsidyBudgetChanged(double value)
    {
        // Update dependent computed text for bottom summary
        OnPropertyChanged(nameof(SubsidyRemainingText));
        ScheduleSummariesRefresh();
    }

    // Additional handlers to keep UI reactive to all inputs (per redesign specs)
    private void OnTimingChanged(string value) => ScheduleSummariesRefresh();
    private void OnBalloonPercentChanged(double value) => ScheduleSummariesRefresh();
    private void OnDownPaymentUnitChanged(string value) { OnPropertyChanged(nameof(DownPaymentPlaceholder)); OnPropertyChanged(nameof(DownPaymentUnitSuffix)); ScheduleSummariesRefresh(); }
    private void OnDownPaymentValueEntryChanged(double value) => ScheduleSummariesRefresh();
    private void OnBalloonUnitChanged(string value) { OnPropertyChanged(nameof(BalloonPlaceholder)); OnPropertyChanged(nameof(BalloonUnitSuffix)); ScheduleSummariesRefresh(); }
    private void OnBalloonValueEntryChanged(double value) => ScheduleSummariesRefresh();
    private void OnLockModeChanged(string value) => ScheduleSummariesRefresh();

    private void OnRateModeChanged(string value) { OnPropertyChanged(nameof(IsFixedRateMode)); OnPropertyChanged(nameof(IsTargetInstallmentMode)); RateModeIndex = string.Equals(RateMode, "fixed_rate", System.StringComparison.OrdinalIgnoreCase) ? 0 : 1; ScheduleSummariesRefresh(); }
    private void OnRateModeIndexChanged(int value) { RateMode = value == 0 ? "fixed_rate" : "target_installment"; }
    private void OnTargetInstallmentChanged(double value) => ScheduleSummariesRefresh();

    private void OnDealerCommissionModeChanged(string value) => ScheduleSummariesRefresh();
    private void OnCommissionEntryUnitChanged(string value)
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
    private void OnCommissionEntryValueChanged(double value)
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
    private void OnDealerCommissionPctChanged(double? value)
    {
        if (value.HasValue) DealerCommissionMode = "override";
        UpdateDealerCommissionResolved();
        OnPropertyChanged(nameof(DealerCommissionPctText));
        ScheduleSummariesRefresh();
    }
    private void OnDealerCommissionAmtChanged(double? value)
    {
        if (value.HasValue) DealerCommissionMode = "override";
        UpdateDealerCommissionResolved();
        OnPropertyChanged(nameof(DealerCommissionPctText));
        ScheduleSummariesRefresh();
    }
    private void OnDealerCommissionResolvedAmtChanged(double value)
    {
        // Reflect derived texts in bottom summary
        OnPropertyChanged(nameof(DealerCommissionResolvedAmtText));
        OnPropertyChanged(nameof(IdcTotalText));
        ScheduleSummariesRefresh();
    }

    private void OnIdcOtherChanged(double value)
    {
        // Mark as user-edited so campaign selection won't auto-overwrite
        IdcOtherUserEdited = true;
        // Update dependent computed texts
        OnPropertyChanged(nameof(IdcOtherText));
        OnPropertyChanged(nameof(IdcTotalText));
        OnPropertyChanged(nameof(SubsidyRemainingText));
        ScheduleSummariesRefresh();
    }
    private void OnIdcOtherUserEditedChanged(bool value)
    {
        OnPropertyChanged(nameof(IdcOtherText));
        ScheduleSummariesRefresh();
    }

    private void OnSelectedCampaignChanged(CampaignSummaryViewModel? value)
    {
        // When selecting a Standard campaign, clear MyCampaign selection so ActiveCampaign reflects this grid
        if (value != null && SelectedMyCampaign != null)
        {
            SelectedMyCampaign = null;
        }
        // Notify ActiveCampaign bindings (e.g., ActiveCampaign.Title)
        OnPropertyChanged(nameof(ActiveCampaign));
        
        // Update IDC Other if not user-edited and campaign has specific values
        if (value != null && !IdcOtherUserEdited)
        {
            // Map campaign-specific IDCs to IDC Other
            if (value.IDC_MBSP_CostAmount > 0)
            {
                IdcOther = value.IDC_MBSP_CostAmount;
            }
            else
            {
                IdcOther = SubsidyBudget; // default mapping per spec
            }
        }
        
        // Refresh details/metrics/cashflows for the active selection
        _debounce.DebounceAsync(0, () => RefreshActiveSelectionAsync());

        // Also refresh summaries so Standard grid metrics (incl. RoRAC) recompute under the same IDC/Subsidy mapping
        _debounce.DebounceAsync(200, async () =>
        {
            await LoadSummariesLocalAsync();
            // Force UI update after recalculation
            OnPropertyChanged(nameof(StandardCampaigns));
            OnPropertyChanged(nameof(CampaignSummaries));
        });
    }

    private CampaignSummaryViewModel? _subscribedMyCampaign;

    private void OnSelectedMyCampaignChanged(CampaignSummaryViewModel? value)
    {
        // When selecting a MyCampaign, clear Standard selection so ActiveCampaign reflects this grid
        if (value != null && SelectedCampaign != null)
        {
            SelectedCampaign = null;
        }

        // Unhook prior
        if (_subscribedMyCampaign != null)
        {
            _subscribedMyCampaign.PropertyChanged -= OnMyCampaignPropertyChanged;
            _subscribedMyCampaign = null;
        }

        // Enable subsidy budget editing only when a My Campaign is selected AND allocations exceed initial budget
        SubsidyBudgetIsEnabled = value != null && ExceedsInitialSubsidy(value);
        
        // Update IDC Other from campaign if not user-edited
        if (value != null && !IdcOtherUserEdited)
        {
            // Map campaign-specific IDCs to IDC Other for My Campaigns
            if (value.IDC_MBSP_CostAmount > 0)
            {
                IdcOther = value.IDC_MBSP_CostAmount;
            }
            else if (value.FSSubInterestAmount > 0 || value.FSFreeMBSPAmount > 0)
            {
                // If campaign has subsidies but no IDC, use subsidy budget as IDC
                IdcOther = SubsidyBudget;
            }
        }

        if (value != null)
        {
            _subscribedMyCampaign = value;
            _subscribedMyCampaign.PropertyChanged += OnMyCampaignPropertyChanged;
        }

        // Notify ActiveCampaign bindings (e.g., ActiveCampaign.Title)
        OnPropertyChanged(nameof(ActiveCampaign));
        
        // Refresh details/metrics/cashflows for the active selection
        _debounce.DebounceAsync(0, () => RefreshActiveSelectionAsync());
    }

    private void OnMyCampaignPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is CampaignSummaryViewModel mc)
        {
            // Subsidy budget is always editable now
            // SubsidyBudgetIsEnabled = ExceedsInitialSubsidy(mc);
            // Auto-recalc when adjustments change
            _debounce.DebounceAsync(200, () => RefreshActiveSelectionAsync());
            // Also refresh summaries to reflect changes in Standard grid metrics
            _debounce.DebounceAsync(400, () => LoadSummariesLocalAsync());
        }
    }

    private bool ExceedsInitialSubsidy(CampaignSummaryViewModel mc)
    {
        // Sum allocations from editable fields (cash discount considered a reduction of price, not subsidy)
        var used = mc.FSSubDownAmount + mc.FSSubInterestAmount + mc.FSFreeMBSPAmount;
        return used > SubsidyBudget + 1e-9; // small epsilon
    }


    // Collapse/expand left panel and let right tables auto-grow via Grid star sizing
    private void OnIsDealInputsCollapsedChanged(bool value)
    {
        // When collapsed show a slim rail with just the arrow; otherwise auto-size to content
        DealInputsColumnWidth = value ? "36" : "420";
    }
    private void OnIsCampaignDetailsCollapsedChanged(bool value)
    {
        CampaignDetailsColumnWidth = value ? "36" : "420";
    }

    private void ScheduleSummariesRefresh()
    {
        // Immediate refresh for the active selection to keep metrics/cashflows snappy
        _ = RefreshActiveSelectionAsync();
        // Debounce summaries grid refresh with shorter delay for better responsiveness (UI-thread safe)
        _debounce.DebounceAsync(100, async () => {
            await LoadSummariesLocalAsync();
            // Force UI update after recalculation
            OnPropertyChanged(nameof(StandardCampaigns));
            OnPropertyChanged(nameof(CampaignSummaries));
        });
        
        // Update dependent properties
        OnPropertyChanged(nameof(Metrics));
        OnPropertyChanged(nameof(ActiveCampaign));
    }
}
