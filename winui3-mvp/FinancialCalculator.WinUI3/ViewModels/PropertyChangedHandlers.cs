using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class MainViewModel
{
    private void OnProductChanged(string value)
    {
        RefreshCommissionPolicyLocal();
        OnPropertyChanged(nameof(IsBalloonEnabled));
        UpdateStandardRate();
        // Re-check vehicle eligibility if product changes to/from mySTAR
        if (_selectedVehicle != null) OnSelectedVehicleChanged(_selectedVehicle);
        ScheduleSummariesRefresh();
    }
    private void OnPriceExTaxChanged(double value) { UpdateDealerCommissionResolved(); OnPropertyChanged(nameof(DealerCommissionPctText)); UpdateStandardRate(); ScheduleSummariesRefresh(); }
    private void OnDownPaymentAmountChanged(double value) { UpdateDealerCommissionResolved(); OnPropertyChanged(nameof(DealerCommissionPctText)); ScheduleSummariesRefresh(); }
    private void OnTermMonthsChanged(int value)
    {
        UpdateStandardRate();
        // Re-populate balloon if mySTAR and term changes
        if (string.Equals(Product, "mySTAR", System.StringComparison.OrdinalIgnoreCase) && _selectedVehicle != null)
        {
            OnSelectedVehicleChanged(_selectedVehicle);
        }
        // Term changed -> Flat/Nominal relationship changes. Update Flat based on current Nominal.
        if (!_isUpdatingRate)
        {
            _isUpdatingRate = true;
            CustomerRateFlat = (double)FinancialCalculator.Engine.Core.FinancialCalculator.ConvertNominalToFlat((decimal)CustomerRatePct, TermMonths, GetPaymentMode());
            _isUpdatingRate = false;
        }
        ScheduleSummariesRefresh();
    }
    private void OnCustomerRatePctChanged(double value)
    {
        if (!_isUpdatingRate)
        {
            _isUpdatingRate = true;
            CustomerRateFlat = (double)FinancialCalculator.Engine.Core.FinancialCalculator.ConvertNominalToFlat((decimal)value, TermMonths, GetPaymentMode());
            _isUpdatingRate = false;
        }
        CheckRateDeviation();
        ScheduleSummariesRefresh();
    }
    private void OnCustomerRateFlatChanged(double value)
    {
        if (!_isUpdatingRate)
        {
            _isUpdatingRate = true;
            CustomerRatePct = (double)FinancialCalculator.Engine.Core.FinancialCalculator.ConvertFlatToNominal((decimal)value, TermMonths, GetPaymentMode());
            _isUpdatingRate = false;
        }
        CheckRateDeviation();
        ScheduleSummariesRefresh();
    }

    private PaymentMode GetPaymentMode()
    {
        return string.Equals(Timing, "advance", StringComparison.OrdinalIgnoreCase) ? PaymentMode.InAdvance : PaymentMode.InArrears;
    }
    private void OnSubsidyBudgetChanged(double value)
    {
        // Update dependent computed text for bottom summary
        OnPropertyChanged(nameof(SubsidyRemainingText));
        ScheduleSummariesRefresh();
    }

    // Additional handlers to keep UI reactive to all inputs (per redesign specs)
    private void OnTimingChanged(string value)
    {
        UpdateStandardRate();
        // Timing changed -> Flat/Nominal relationship changes. Update Flat based on current Nominal.
        if (!_isUpdatingRate)
        {
            _isUpdatingRate = true;
            CustomerRateFlat = (double)FinancialCalculator.Engine.Core.FinancialCalculator.ConvertNominalToFlat((decimal)CustomerRatePct, TermMonths, GetPaymentMode());
            _isUpdatingRate = false;
        }
        ScheduleSummariesRefresh();
    }
    private void OnBalloonPercentChanged(double value) => ScheduleSummariesRefresh();
    private void OnDownPaymentUnitChanged(string value) { OnPropertyChanged(nameof(DownPaymentPlaceholder)); OnPropertyChanged(nameof(DownPaymentUnitSuffix)); UpdateStandardRate(); ScheduleSummariesRefresh(); }
    private void OnDownPaymentValueEntryChanged(double value) { UpdateStandardRate(); ScheduleSummariesRefresh(); }
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
        
        // Refresh details/metrics/cashflows for the active selection (immediate)
        _debounceActive.DebounceAsync(0, () => RefreshActiveSelectionAsync());

        // Also refresh summaries so Standard grid metrics (incl. RoRAC) recompute under the same IDC/Subsidy mapping
        _debounceFull.DebounceAsync(200, async () =>
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

        // Subsidy budget is always editable now
        SubsidyBudgetIsEnabled = true;
        
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
        _debounceActive.DebounceAsync(0, () => RefreshActiveSelectionAsync());
    }

    private void OnMyCampaignPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is CampaignSummaryViewModel mc)
        {
            if (e.PropertyName == nameof(CampaignSummaryViewModel.SelectedMbspPackage))
            {
                 UpdateMbspCost(mc);
            }

            // Subsidy budget is always editable now
            // SubsidyBudgetIsEnabled = ExceedsInitialSubsidy(mc);
            // Auto-recalc when adjustments change
            _debounceActive.DebounceAsync(50, () => RefreshActiveSelectionAsync());
            // Also refresh summaries to reflect changes in Standard grid metrics
            _debounceFull.DebounceAsync(300, () => LoadSummariesLocalAsync());
        }
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
        // Fast debounce for active selection (keeps typing snappy while updating main metrics)
        _debounceActive.DebounceAsync(50, async () => {
            await RefreshActiveSelectionAsync();
            OnPropertyChanged(nameof(Metrics));
            OnPropertyChanged(nameof(ActiveCampaign));
        });

        // Slower debounce for full grid refresh (heavy operation)
        _debounceFull.DebounceAsync(300, async () => {
            await LoadSummariesLocalAsync();
            OnPropertyChanged(nameof(StandardCampaigns));
            OnPropertyChanged(nameof(CampaignSummaries));
        });
    }
    // MARK: Vehicle Selection Handlers
    private void OnSelectedVehicleChanged(Models.Vehicle? value)
    {
        if (value != null)
        {
            // Only auto-populate price if it's different to avoid overriding user edits unnecessarily if they select same vehicle?
            // Requirement says "auto-populate the Price (MSRP) field". usually means overwrite.
            PriceExTax = value.MSRP;

            // Auto-populate balloon if mySTAR
             if (string.Equals(Product, "mySTAR", System.StringComparison.OrdinalIgnoreCase))
            {
                var rv = value.GetRVForTerm(TermMonths);
                if (rv.HasValue)
                {
                    BalloonUnit = "%";
                    BalloonValueEntry = rv.Value * 100; // Convert decimal to percent
                    Status = "Ready"; // Clear potential previous warning
                }
                else
                {
                     Status = $"Warning: {value.ModelName} is not eligible for mySTAR at {TermMonths} months term (RV not available).";
                }
            }

            // Auto-populate MBSP cost if applicable
            UpdateMbspCost();
        }
    }

    // MARK: Standard Rate & Deviation
    private bool _isRateDeviation = false;
    public bool IsRateDeviation { get => _isRateDeviation; set => SetProperty(ref _isRateDeviation, value); }
    private double? _standardRateForCurrentSelection;

    private void UpdateStandardRate()
    {
        // Calculate down payment %
        double downPaymentPct;
        if (string.Equals(DownPaymentUnit, "%", System.StringComparison.OrdinalIgnoreCase))
        {
            downPaymentPct = DownPaymentValueEntry / 100.0;
        }
        else
        {
            downPaymentPct = PriceExTax > 0 ? DownPaymentValueEntry / PriceExTax : 0;
        }

        _standardRateForCurrentSelection = _standardRates.GetStandardRate(Product, TermMonths, downPaymentPct, Timing);
        
        if (_standardRateForCurrentSelection.HasValue)
        {
            // Auto-populate if we are in a state where we should (maybe if user hasn't manually edited rate yet? hard to track. For now auto-populate always on dependent change per req)
            // "When users change Product, Term, Downpayment, or Payment Mode, auto-populate the Customer Rate"
            CustomerRatePct = _standardRateForCurrentSelection.Value;
        }
        
        CheckRateDeviation();
    }

    private void CheckRateDeviation()
    {
        if (_standardRateForCurrentSelection.HasValue)
        {
            // Allow small epsilon for floating point comparison if needed, but exact match for now
            IsRateDeviation = Math.Abs(CustomerRatePct - _standardRateForCurrentSelection.Value) > 0.001;
        }
        else
        {
            IsRateDeviation = false; // No standard rate found, so can't deviate
        }
    }

    private void UpdateMbspCost(CampaignSummaryViewModel? campaign = null)
    {
        // Only update for currently active MyCampaign or specifically passed one
        var target = campaign ?? (SelectedMyCampaign); // Use SelectedMyCampaign to be sure we only update editable ones
        if (target != null && _selectedVehicle != null)
        {
             if (_selectedVehicle.MbspCosts.TryGetValue(target.SelectedMbspPackage, out var cost))
             {
                 target.FSFreeMBSPAmount = cost;
                 target.FSFreeMBSP = cost.ToString("N0", CultureInfo.InvariantCulture);
             }
             else
             {
                 target.FSFreeMBSPAmount = 0;
                 target.FSFreeMBSP = "N/A";
             }
        }
    }
}
