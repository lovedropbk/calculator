using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.WinUI3.Models;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // NOTE: Use Services.DebounceDispatcher to capture UI SynchronizationContext.
    // Do not use ViewModels.DebounceDispatcher (deprecated) to avoid background-thread UI updates.
    private readonly FinancialCalculator.WinUI3.Services.DebounceDispatcher _debounce = new();
    private readonly LocalEngineService _local = new();
    private readonly LocalCampaignsProvider _campaigns = new();
    private readonly LocalScenarioService _scenarios = new();

    // MARK: Parameter Set Caching (legacy no-op)

    // MARK: Deal Inputs
    private string _product = "HP";
    public string Product { get => _product; set { if (SetProperty(ref _product, value)) OnProductChanged(value); } }
    private double _priceExTax = 1_000_000;
    public double PriceExTax { get => _priceExTax; set { if (SetProperty(ref _priceExTax, value)) OnPriceExTaxChanged(value); } }
    private double _additionalFinancedItems = 0;
    public double AdditionalFinancedItems { get => _additionalFinancedItems; set => SetProperty(ref _additionalFinancedItems, value); }
    private double _downPaymentAmount = 200_000;
    public double DownPaymentAmount { get => _downPaymentAmount; set { if (SetProperty(ref _downPaymentAmount, value)) OnDownPaymentAmountChanged(value); } }
    // Unified entry + unit for Down Payment and Balloon
    private string _downPaymentUnit = "THB"; // THB | %
    public string DownPaymentUnit { get => _downPaymentUnit; set { if (SetProperty(ref _downPaymentUnit, value)) OnDownPaymentUnitChanged(value); } }
    private double _downPaymentValueEntry = 200_000;
    public double DownPaymentValueEntry { get => _downPaymentValueEntry; set { if (SetProperty(ref _downPaymentValueEntry, value)) OnDownPaymentValueEntryChanged(value); } }
    private string _balloonUnit = "%"; // THB | %
    public string BalloonUnit { get => _balloonUnit; set { if (SetProperty(ref _balloonUnit, value)) OnBalloonUnitChanged(value); } }
    private double _balloonValueEntry = 0;
    public double BalloonValueEntry { get => _balloonValueEntry; set { if (SetProperty(ref _balloonValueEntry, value)) OnBalloonValueEntryChanged(value); } }
    private int _termMonths = 36;
    public int TermMonths { get => _termMonths; set { if (SetProperty(ref _termMonths, value)) OnTermMonthsChanged(value); } }
    private string _timing = "arrears"; // arrears|advance
    public string Timing { get => _timing; set { if (SetProperty(ref _timing, value)) OnTimingChanged(value); } }
    private double _balloonPercent = 0;
    public double BalloonPercent { get => _balloonPercent; set { if (SetProperty(ref _balloonPercent, value)) OnBalloonPercentChanged(value); } }
    private string _lockMode = "amount"; // amount|percent
    public string LockMode { get => _lockMode; set { if (SetProperty(ref _lockMode, value)) OnLockModeChanged(value); } }

    // MARK: Rate Mode
    private string _rateMode = "fixed_rate"; // fixed_rate|target_installment
    public string RateMode { get => _rateMode; set { if (SetProperty(ref _rateMode, value)) OnRateModeChanged(value); } }
    private int _rateModeIndex = 0; // 0=fixed_rate, 1=target_installment
    public int RateModeIndex { get => _rateModeIndex; set { if (SetProperty(ref _rateModeIndex, value)) OnRateModeIndexChanged(value); } }
    public bool IsFixedRateMode => string.Equals(RateMode, "fixed_rate", StringComparison.OrdinalIgnoreCase);
    public bool IsTargetInstallmentMode => string.Equals(RateMode, "target_installment", StringComparison.OrdinalIgnoreCase);
    private double _customerRatePct = 3.99;
    public double CustomerRatePct { get => _customerRatePct; set { if (SetProperty(ref _customerRatePct, value)) OnCustomerRatePctChanged(value); } }
    private double _targetInstallment = 0;
    public double TargetInstallment { get => _targetInstallment; set { if (SetProperty(ref _targetInstallment, value)) OnTargetInstallmentChanged(value); } }

    // MARK: Subsidy & IDC
    private double _subsidyBudget = 100_000;
    public double SubsidyBudget { get => _subsidyBudget; set { if (SetProperty(ref _subsidyBudget, value)) OnSubsidyBudgetChanged(value); } }
    private bool _subsidyBudgetIsEnabled = true; // Always editable
    public bool SubsidyBudgetIsEnabled { get => _subsidyBudgetIsEnabled; set => SetProperty(ref _subsidyBudgetIsEnabled, value); }
    private string _dealerCommissionMode = "auto"; // auto|override
    public string DealerCommissionMode { get => _dealerCommissionMode; set { if (SetProperty(ref _dealerCommissionMode, value)) OnDealerCommissionModeChanged(value); } }
    private double? _dealerCommissionPct;
    public double? DealerCommissionPct { get => _dealerCommissionPct; set { if (SetProperty(ref _dealerCommissionPct, value)) OnDealerCommissionPctChanged(value); } }
    private double? _dealerCommissionAmt;
    public double? DealerCommissionAmt { get => _dealerCommissionAmt; set { if (SetProperty(ref _dealerCommissionAmt, value)) OnDealerCommissionAmtChanged(value); } }
    private double _dealerCommissionResolvedAmt;
    public double DealerCommissionResolvedAmt { get => _dealerCommissionResolvedAmt; set { if (SetProperty(ref _dealerCommissionResolvedAmt, value)) OnDealerCommissionResolvedAmtChanged(value); } }

    // Unified commission entry (auto | % | THB)
    private string _commissionEntryUnit = "auto"; // auto | % | THB
    public string CommissionEntryUnit { get => _commissionEntryUnit; set { if (SetProperty(ref _commissionEntryUnit, value)) OnCommissionEntryUnitChanged(value); } }
    private double _commissionEntryValue = 0;
    public double CommissionEntryValue { get => _commissionEntryValue; set { if (SetProperty(ref _commissionEntryValue, value)) OnCommissionEntryValueChanged(value); } }

    // Auto policy (local)
    private double _autoCommissionPct; // fraction (e.g., 0.03)
    public double AutoCommissionPct { get => _autoCommissionPct; set => SetProperty(ref _autoCommissionPct, value); }
    private string _commissionPolicyVersion = string.Empty;
    public string CommissionPolicyVersion { get => _commissionPolicyVersion; set => SetProperty(ref _commissionPolicyVersion, value); }

    private double _idcOther = 0;
    public double IdcOther { get => _idcOther; set { if (SetProperty(ref _idcOther, value)) OnIdcOtherChanged(value); } }
    private double _upfrontSubsidies = 0;
    public double UpfrontSubsidies { get => _upfrontSubsidies; set => SetProperty(ref _upfrontSubsidies, value); }
    private bool _idcOtherUserEdited = false;
    public bool IdcOtherUserEdited { get => _idcOtherUserEdited; set { if (SetProperty(ref _idcOtherUserEdited, value)) OnIdcOtherUserEditedChanged(value); } }

    public string DealerCommissionPctText => ((DealerCommissionMode == "override" ? (DealerCommissionPct ?? AutoCommissionPct) : AutoCommissionPct) * 100.0).ToString("0.00", CultureInfo.InvariantCulture);
    public string DealerCommissionResolvedAmtText => DealerCommissionResolvedAmt.ToString("N0", CultureInfo.InvariantCulture);

    // UI helpers for placeholders and unit tokens
    public string PricePlaceholder => "THB";
    public string PriceUnitSuffix => "THB";
    public string DownPaymentPlaceholder => DownPaymentUnit;
    public string DownPaymentUnitSuffix => DownPaymentUnit;
    public string BalloonPlaceholder => BalloonUnit;
    public string BalloonUnitSuffix => BalloonUnit;
    public bool IsBalloonEnabled => !string.Equals(Product, "HP", StringComparison.OrdinalIgnoreCase);
    public bool IsCommissionEntryEditable => !string.Equals(CommissionEntryUnit, "auto", StringComparison.OrdinalIgnoreCase);

    // Campaign Details panel
    private bool _isCampaignDetailsCollapsed = true;
    public bool IsCampaignDetailsCollapsed { get => _isCampaignDetailsCollapsed; set { if (SetProperty(ref _isCampaignDetailsCollapsed, value)) OnIsCampaignDetailsCollapsedChanged(value); } }
    private string _campaignDetailsColumnWidth = "Auto";
    public string CampaignDetailsColumnWidth { get => _campaignDetailsColumnWidth; set => SetProperty(ref _campaignDetailsColumnWidth, value); }
    [RelayCommand]
    private void ToggleCampaignDetailsCollapsed()
    {
        IsCampaignDetailsCollapsed = !IsCampaignDetailsCollapsed;
        // Listen for property changes in CampaignSummaryViewModel to trigger recalcs for My Campaigns
        MyCampaigns.CollectionChanged += MyCampaigns_CollectionChanged;
    }

    private void MyCampaigns_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (CampaignSummaryViewModel item in e.NewItems)
            {
                item.PropertyChanged += MyCampaign_PropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (CampaignSummaryViewModel item in e.OldItems)
            {
                item.PropertyChanged -= MyCampaign_PropertyChanged;
            }
        }
    }

    private async void MyCampaign_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // If the changed campaign is currently selected, and the changed property is one of the editable inputs
        if (sender is CampaignSummaryViewModel vm && vm == SelectedMyCampaign && IsCampaignInputProperty(e.PropertyName))
        {
            // Trigger recalculation for this specific campaign
            await RecalculateMyCampaignAsync(vm);
        }
    }

    private bool IsCampaignInputProperty(string? propertyName)
    {
        return propertyName == nameof(CampaignSummaryViewModel.CashDiscountAmount) ||
               propertyName == nameof(CampaignSummaryViewModel.FSSubDownAmount) ||
               propertyName == nameof(CampaignSummaryViewModel.FSSubInterestAmount) ||
               propertyName == nameof(CampaignSummaryViewModel.FSFreeMBSPAmount) ||
               propertyName == nameof(CampaignSummaryViewModel.TargetRatePct);
    }

    // MARK: Collections & Selection
    public ObservableCollection<CampaignSummaryViewModel> StandardCampaigns { get; } = new();
    public ObservableCollection<CampaignSummaryViewModel> CampaignSummaries { get; } = new(); // back-compat alias
    public ObservableCollection<CampaignSummaryViewModel> MyCampaigns { get; } = new();

    // Selections
    private CampaignSummaryViewModel? _selectedCampaign; // Standard selection
    public CampaignSummaryViewModel? SelectedCampaign { get => _selectedCampaign; set { if (SetProperty(ref _selectedCampaign, value)) OnSelectedCampaignChanged(value); } }
    private CampaignSummaryViewModel? _selectedMyCampaign;
    public CampaignSummaryViewModel? SelectedMyCampaign { get => _selectedMyCampaign; set { if (SetProperty(ref _selectedMyCampaign, value)) { OnSelectedMyCampaignChanged(value); OnPropertyChanged(nameof(IsMyCampaignSelected)); } } }

    // Cashflows grid for active selection
    public ObservableCollection<CashflowRowViewModel> Cashflows { get; } = new();

    // Cashflow summary properties
    private string _cashflowCampaignName = "";
    public string CashflowCampaignName { get => _cashflowCampaignName; set => SetProperty(ref _cashflowCampaignName, value); }
    private string _totalPrincipalPaid = "0";
    public string TotalPrincipalPaid { get => _totalPrincipalPaid; set => SetProperty(ref _totalPrincipalPaid, value); }
    private string _totalInterestPaid = "0";
    public string TotalInterestPaid { get => _totalInterestPaid; set => SetProperty(ref _totalInterestPaid, value); }
    private string _totalFeesPaid = "0";
    public string TotalFeesPaid { get => _totalFeesPaid; set => SetProperty(ref _totalFeesPaid, value); }
    private string _netAmountFinanced = "0";
    public string NetAmountFinanced { get => _netAmountFinanced; set => SetProperty(ref _netAmountFinanced, value); }

    // Active selection prefers MyCampaigns, else Standard
    public CampaignSummaryViewModel? ActiveCampaign => SelectedMyCampaign ?? SelectedCampaign;

    public bool IsMyCampaignSelected => SelectedMyCampaign != null;

    // MARK: Metrics & Status
    private MetricsViewModel _metrics = new();
    public MetricsViewModel Metrics { get => _metrics; set => SetProperty(ref _metrics, value); }
    private string _status = "Ready";
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    private bool _isCalculating = false;
    public bool IsCalculating { get => _isCalculating; set => SetProperty(ref _isCalculating, value); }
    private bool _isDealInputsCollapsed = false;
    public bool IsDealInputsCollapsed { get => _isDealInputsCollapsed; set { if (SetProperty(ref _isDealInputsCollapsed, value)) OnIsDealInputsCollapsedChanged(value); } }
    // Column width of the left Deal Inputs panel; bound to ColumnDefinition.Width
    private string _dealInputsColumnWidth = "420";
    public string DealInputsColumnWidth { get => _dealInputsColumnWidth; set => SetProperty(ref _dealInputsColumnWidth, value); }

    public IRelayCommand RecalculateCommand { get; }

    public MainViewModel()
    {
        RecalculateCommand = new AsyncRelayCommand(RecalculateAsync);
        
        IdcOther = 0; // Default to 0, SubsidyBudget is separate now

        // Initialize data on UI thread with proper error handling
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            Status = "Initializing...";

            await Task.Delay(200);

            await InitializeParameterSetAsync();

            // Commission policy: compute locally
            RefreshCommissionPolicyLocal();

            // Load campaign summaries with local engine
            await LoadSummariesLocalAsync();
            
            // Trigger initial refresh if there's a default selection
            if (ActiveCampaign != null)
            {
                await RefreshActiveSelectionAsync();
            }
        }
        catch (Exception ex)
        {
            Status = $"Initialization error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"MainViewModel initialization error: {ex}");
        }
    }

    // MARK: Parameter Set Initialization
    private async Task InitializeParameterSetAsync()
    {
        // Legacy no-op - parameter set caching removed
        await Task.CompletedTask;
    }

    // Legacy API param extraction (unused)
    private Dictionary<string, object>? GetEngineParameterSetOrNull() => null;

    // MARK: Commands - Dealer Commission
    [RelayCommand]
    private void ResetDealerCommissionAuto()
    {
        DealerCommissionMode = "auto";
        DealerCommissionPct = null;
        DealerCommissionAmt = null;
        DealerCommissionResolvedAmt = 0;
        ScheduleSummariesRefresh();
    }

    [RelayCommand]
    private void EnableDealerCommissionOverride()
    {
        DealerCommissionMode = "override";
        ScheduleSummariesRefresh();
    }

    [RelayCommand]
    private void ToggleDealInputsCollapsed()
    {
        IsDealInputsCollapsed = !IsDealInputsCollapsed;
    }

    // Copy a standard campaign to My Campaigns
    [RelayCommand(CanExecute = nameof(CanCopyToMyCampaigns))]
    private void CopyToMyCampaigns(CampaignSummaryViewModel? item)
    {
        if (item is null) item = SelectedCampaign;
        if (item is null) return;
        var clone = item.Clone();
        // Tag as custom for clarity
        if (!clone.Title.StartsWith("Custom:", StringComparison.OrdinalIgnoreCase))
            clone.Title = $"Custom: {clone.Title}";
        clone.CampaignId = Guid.NewGuid().ToString();
        MyCampaigns.Add(clone);
        SelectedMyCampaign = clone;
        Logger.Info($"MyCampaigns: copied from standard '{clone.Title}' (ID={clone.CampaignId})");
        
        // Clear standard selection since we're now selecting the copied campaign
        SelectedCampaign = null;
        
        // Trigger refresh to update metrics with the new campaign
        OnPropertyChanged(nameof(ActiveCampaign));
        ScheduleSummariesRefresh();
    }

    private bool CanCopyToMyCampaigns(CampaignSummaryViewModel? item) => item != null || SelectedCampaign != null;


    // MARK: My Campaigns persistence
    private static string MyCampaignsPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FinancialCalculator", "my_campaigns.json");

    [RelayCommand]
    private void NewBankCampaign()
    {
        var vm = new CampaignSummaryViewModel { Title = "Custom: Bank Campaign", Notes = "", CashDiscountAmount = 0, FSSubDownAmount = 0, FSSubInterestAmount = 0, IDC_MBSP_CostAmount = 0, FSFreeMBSPAmount = 0 };
        MyCampaigns.Add(vm);
        SelectedMyCampaign = vm;
    }

    [RelayCommand]
    private async Task SaveAllCampaignsAsync()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(MyCampaignsPath)!;
            System.IO.Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(MyCampaigns, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(MyCampaignsPath, json);
            Status = $"Saved {MyCampaigns.Count} campaigns";
        }
        catch (Exception ex)
        {
            Status = $"Save error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadCampaignsAsync()
    {
        try
        {
            if (!System.IO.File.Exists(MyCampaignsPath)) { Status = "No saved campaigns"; return; }
            var json = await System.IO.File.ReadAllTextAsync(MyCampaignsPath);
            var list = System.Text.Json.JsonSerializer.Deserialize<List<CampaignSummaryViewModel>>(json) ?? new();
            MyCampaigns.Clear();
            foreach (var c in list) MyCampaigns.Add(c);
            Status = $"Loaded {MyCampaigns.Count} campaigns";
        }
        catch (Exception ex)
        {
            Status = $"Load error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearCampaigns()
    {
        MyCampaigns.Clear();
        SelectedMyCampaign = null;
    }

    // MARK: Data Loading (Local)
    private async Task LoadSummariesLocalAsync()
    {
        try
        {
            // Preserve selection
            var selectedId = ActiveCampaign?.CampaignId;

            // Ensure commission policy is set
            RefreshCommissionPolicyLocal();

            var temp = new List<(CampaignSummaryViewModel vm, double monthly, double eff)>();
            StandardCampaigns.Clear();
            CampaignSummaries.Clear();

            // Baseline (no campaign) - but should still apply leftover subsidy budget for consistent calculation!
            var leftoverBudgetForBaseline = Math.Max(0, SubsidyBudget);  // All budget is available for baseline
            var baseline = ComputeScenarioWithCommission(
                vehiclePrice: (decimal)PriceExTax,
                subdownIsPercent: false,
                subdownValue: 0,
                upfrontCostsDelta: 0m,
                upfrontSubsidiesDelta: (decimal)leftoverBudgetForBaseline,  // Apply leftover budget for consistency
                customerRateOverride: null
            );

            var baselineDp = ComputeDownpaymentDisplay((decimal)PriceExTax);
            
            // Calculate IDCs Total for baseline (dealer commission + IDC Other)
            var baselineIdcsTotal = baseline.commissionAmt + IdcOther;
            
            var baselineVm = new CampaignSummaryViewModel
            {
                CampaignId = "baseline",
                CampaignType = "No Campaign (Baseline)",
                Title = "No Campaign (Baseline)",
                DealerCommission = $"{baseline.commissionPct.ToString("0.00%", CultureInfo.InvariantCulture)} ({baseline.commissionAmt.ToString("N0", CultureInfo.InvariantCulture)} THB)",
                Monthly = ((double)baseline.outputs.MonthlyRate).ToString("N0", CultureInfo.InvariantCulture),
                // Show the customer's NOMINAL rate, not the effective/flat rate!
                Effective = (CustomerRatePct / 100.0).ToString("0.00%"),
                Downpayment = baselineDp.ToString("N0", CultureInfo.InvariantCulture),
                TransactionPrice = ((decimal)PriceExTax).ToString("N0", CultureInfo.InvariantCulture),
                SubsidyUsed = "0",
                FSSubDown = "0",
                FSSubInterest = "0",
                FSFreeMBSP = "0",
                CashDiscount = "0",
                IDCsTotal = baselineIdcsTotal.ToString("N0", CultureInfo.InvariantCulture),
                RoRAC = ((double)baseline.profit.AcquisitionRoRac).ToString("0.00%"),
                Notes = "Baseline scenario without campaigns",
                FSSubDownAmount = 0,
                FSSubInterestAmount = 0,
                FSFreeMBSPAmount = 0,
                CashDiscountAmount = 0,
            };
            StandardCampaigns.Add(baselineVm);
            CampaignSummaries.Add(baselineVm);

            // Standard campaigns
            foreach (var c in _campaigns.GetStandard())
            {
                try
                {
                    decimal vehiclePrice = (decimal)PriceExTax;
                    bool subIsPct = false;
                    decimal subVal = 0;
                    decimal upCostDelta = 0m;
                    decimal upSubDelta = 0m;
                    double? rateOverride = null;

                    double fsSubDownThb = 0;
                    double freeInsuranceThb = 0;
                    double freeMbspThb = 0;
                    double cashDiscountThb = 0;
                    double subinterestSubsidyThb = 0;  // Separate variable for subinterest subsidy

                    switch (c.Type)
                    {
                        case "subdown":
                            if (c.SubsidyPercent.HasValue)
                            {
                                subIsPct = true;
                                var pct = (decimal)(c.SubsidyPercent.Value * 100.0); // engine expects percent units
                                subVal = pct;
                                fsSubDownThb = (double)(vehiclePrice * pct / 100m);
                            }
                            else if (c.SubsidyAmount.HasValue)
                            {
                                subIsPct = false;
                                subVal = (decimal)c.SubsidyAmount.Value;
                                fsSubDownThb = c.SubsidyAmount.Value;
                            }
                            break;
                        case "free_insurance":
                            if (c.InsuranceCost.HasValue)
                            {
                                upCostDelta += (decimal)c.InsuranceCost.Value;
                                freeInsuranceThb = c.InsuranceCost.Value;
                            }
                            break;
                        case "free_mbsp":
                            if (c.MbspCost.HasValue)
                            {
                                upCostDelta += (decimal)c.MbspCost.Value;
                                freeMbspThb = c.MbspCost.Value;
                            }
                            break;
                        case "cash_discount":
                            if (c.DiscountPercent.HasValue)
                            {
                                var disc = (decimal)c.DiscountPercent.Value;
                                cashDiscountThb = (double)(vehiclePrice * (decimal)disc);
                                vehiclePrice = vehiclePrice * (1m - (decimal)disc);
                            }
                            else if (c.DiscountAmount.HasValue)
                            {
                                var disc = (decimal)c.DiscountAmount.Value;
                                cashDiscountThb = (double)disc;
                                vehiclePrice = Math.Max(0m, vehiclePrice - disc);
                            }
                            break;
                        case "subinterest":
                            if (c.TargetRate.HasValue)
                            {
                                rateOverride = c.TargetRate.Value * 100.0; // 0.0299 => 2.99
                                // For subinterest, the customer pays at the target rate
                                // So we need to calculate with the target rate to get the correct effective rate
                            }
                            break;
                    }

                    // Leftover budget after explicit consumers (subdown + cash discount)
                    var leftoverBudget = Math.Max(0, SubsidyBudget - (fsSubDownThb + cashDiscountThb));
                    
                    // For subinterest, compute required subsidy (interest shortfall from base to target)
                    if (c.Type == "subinterest" && rateOverride.HasValue)
                    {
                        var requiredSubsidy = ComputeRequiredSubsidyForRateBuydown(vehiclePrice, subIsPct, subVal, upCostDelta, CustomerRatePct, rateOverride.Value);
                        // Use the required subsidy amount (capped by available budget) for rate buydown
                        upSubDelta = (decimal)Math.Min(requiredSubsidy, leftoverBudget);
                        // Track the subsidy used for subinterest rate buydown
                        subinterestSubsidyThb = Math.Min(requiredSubsidy, leftoverBudget);
                    }
                    else
                    {
                        // For other campaigns, apply leftover budget as upfront subsidy income (unallocated subsidy)
                        upSubDelta = (decimal)leftoverBudget;
                    }

                    var outc = ComputeScenarioWithCommission(vehiclePrice, subIsPct, subVal, upCostDelta, upSubDelta, rateOverride);
                    var dp = ComputeDownpaymentDisplay(vehiclePrice);

                    // Display Subsidy Utilized = All subsidies used (Subdown + Cash Discount + Subinterest)
                    var subsidyUsed = fsSubDownThb + cashDiscountThb + subinterestSubsidyThb;
                    
                    // Calculate IDCs Total = Dealer Commission + all actual IDCs
                    var idcsTotal = outc.commissionAmt + freeInsuranceThb + freeMbspThb + IdcOther;
                    
                    var vm = new CampaignSummaryViewModel
                    {
                        CampaignId = c.Id,
                        CampaignType = c.Type,
                        Title = c.Type,
                        DealerCommission = $"{outc.commissionPct.ToString("0.00%", CultureInfo.InvariantCulture)} ({outc.commissionAmt.ToString("N0", CultureInfo.InvariantCulture)} THB)",
                        Monthly = ((double)outc.outputs.MonthlyRate).ToString("N0", CultureInfo.InvariantCulture),
                        // Show the customer's NOMINAL rate that they pay (may be adjusted by campaign)
                        // For subinterest campaigns with rate override, show the target rate; otherwise show the base customer rate
                        Effective = ((rateOverride ?? CustomerRatePct) / 100.0).ToString("0.00%"),
                        Downpayment = dp.ToString("N0", CultureInfo.InvariantCulture),
                        TransactionPrice = (vehiclePrice).ToString("N0", CultureInfo.InvariantCulture),
                        SubsidyUsed = subsidyUsed.ToString("N0", CultureInfo.InvariantCulture),
                        FSSubDown = fsSubDownThb.ToString("N0", CultureInfo.InvariantCulture),
                        FSSubInterest = freeInsuranceThb.ToString("N0", CultureInfo.InvariantCulture),  // Always shows free insurance IDC
                        SubinterestSubsidy = subinterestSubsidyThb.ToString("N0", CultureInfo.InvariantCulture),  // Shows subsidy for subinterest
                        FSFreeMBSP = freeMbspThb.ToString("N0", CultureInfo.InvariantCulture),
                        CashDiscount = cashDiscountThb.ToString("N0", CultureInfo.InvariantCulture),
                        IDCsTotal = idcsTotal.ToString("N0", CultureInfo.InvariantCulture),
                        // Store the target rate in percent units (e.g., 2.99 for 2.99%)
                        TargetRatePct = (c.Type == "subinterest" && rateOverride.HasValue ? rateOverride.Value : (double?)null),
                        RoRAC = ((double)outc.profit.AcquisitionRoRac).ToString("0.00%"),
                        Notes = string.Empty,
                        FSSubDownAmount = fsSubDownThb,
                        FSSubInterestAmount = freeInsuranceThb,  // Always shows free insurance IDC
                        SubinterestSubsidyAmount = subinterestSubsidyThb,  // Shows subsidy for subinterest
                        FSFreeMBSPAmount = freeMbspThb,
                        CashDiscountAmount = cashDiscountThb,
                        IDC_MBSP_CostAmount = freeMbspThb, // Set this for consistency
                    };
                    temp.Add((vm, (double)outc.outputs.MonthlyRate, (double)outc.outputs.FlatRatePercentPerAnnum));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error computing campaign '{c.Id}': {ex.Message}");
                }
            }

            foreach (var (vm, _, _) in temp.OrderBy(t => t.monthly).ThenBy(t => t.eff))
            {
                StandardCampaigns.Add(vm);
                CampaignSummaries.Add(vm);
            }

            // Restore selection
            if (selectedId != null)
            {
                var toRestore = StandardCampaigns.FirstOrDefault(c => c.CampaignId == selectedId);
                if (toRestore != null)
                {
                    SelectedCampaign = toRestore;
                }
            }

            // Set default selection if none
            if (SelectedCampaign == null && CampaignSummaries.Count > 0)
                SelectedCampaign = CampaignSummaries.FirstOrDefault(c => c.CampaignId == "baseline");

            Status = $"Loaded {CampaignSummaries.Count} options";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        await Task.CompletedTask;
    }

    // MARK: Actions
    private async Task RecalculateAsync()
    {
        try
        {
            IsCalculating = true;
            Status = "Calculating...";

            // Always calculate with local C# engine for high-fidelity cashflows and IRR per spec
            var scenario = _scenarios.Compute(new LocalScenarioService.ScenarioInput
            {
                Market = "TH",
                Product = Product,
                Timing = Timing,
                TermMonths = TermMonths,
                VehiclePrice = (decimal)PriceExTax,
                AdditionalFinancedItems = (decimal)AdditionalFinancedItems,
                DownIsPercent = string.Equals(DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase),
                DownValue = (decimal)DownPaymentValueEntry,
                BalloonIsPercent = string.Equals(BalloonUnit, "%", StringComparison.OrdinalIgnoreCase),
                BalloonValue = (decimal)BalloonValueEntry,
                CustomerRatePercent = (decimal)CustomerRatePct,
                // For the main calculator tab (manual scenario), we use the full subsidy budget as upfront subsidy
                // if no specific campaign logic is applied here yet.
                // The requirement says "assume we utilize all subsidy available".
                // In manual mode, if user doesn't explicitly allocate it, we treat it as unallocated subsidy (upfront income).
                UpfrontSubsidies = (decimal)SubsidyBudget,
                UpfrontCosts = (decimal)(DealerCommissionResolvedAmt + IdcOther),
                SubdownIsPercent = false,
                SubdownValue = 0
            });

            // Update key metrics from local engine (monthly, flat rate, financed amount)
            Metrics = new MetricsViewModel
            {
                MonthlyInstallment = ((double)scenario.Deal.MonthlyRate).ToString("N0", CultureInfo.InvariantCulture),
                NominalRate = (CustomerRatePct / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
                EffectiveRate = ((double)scenario.Deal.FlatRatePercentPerAnnum / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
                FinancedAmount = ((double)scenario.Deal.FinancedAmount).ToString("N0", CultureInfo.InvariantCulture),
                RoRAC = ((double)scenario.Profit.AcquisitionRoRac).ToString("0.00%"),
            };

            // Populate cashflows from local engine schedule
            PopulateCashflows(LocalScheduleToDto(scenario.Deal.Schedule));

            // Populate profitability detail waterfall
            RefreshProfitabilityDetailsLocal(scenario.Profit);

            Status = "Done";

            // Also refresh the standard campaigns grid with the new inputs
            await LoadSummariesLocalAsync();
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            IsCalculating = false;
        }
    }

    private static List<CashflowRowDto> LocalScheduleToDto(IReadOnlyList<FinancialCalculator.Engine.Models.ScheduleRow> rows)
    {
        var list = new List<CashflowRowDto>(rows?.Count ?? 0);
        if (rows == null) return list;
        foreach (var r in rows)
        {
            list.Add(new CashflowRowDto
            {
                Period = r.Period,
                Principal = (double)r.Principal,
                Interest = (double)r.Interest,
                Balance = (double)r.Balance,
                Cashflow = (double)r.Cashflow,
            });
        }
        return list;
    }

    private async Task RefreshActiveSelectionAsync()
    {
        try
        {
            var active = ActiveCampaign;
            if (active == null)
            {
                CashflowCampaignName = "No Campaign Selected";
                Cashflows.Clear();
                return;
            }

            // If it's a MyCampaign, we might need to ensure it's up-to-date.
            // Trigger re-computation to be safe and get full waterfall details.
            if (IsMyCampaign(active))
            {
                // Already handled by property change listeners, but if first load/select, ensure it's calculated.
                await RecalculateMyCampaignAsync(active);
            }
            else
            {
                // Standard campaign: we can re-compute here similarly to get full details.
                // Since they are not editable, we could trust the VM values, but we need 'profit' object for waterfall.
                // Let's re-compute using the same logic.
                
                decimal vehiclePrice = (decimal)PriceExTax;
                double cashDiscount = active.CashDiscountAmount;
                double fsSubDown = active.FSSubDownAmount;
                double fsFreeInsurance = active.FSSubInterestAmount;
                double fsFreeMbsp = active.FSFreeMBSPAmount;
                
                // For standard campaigns, Target Rate comes from catalog if not explicitly set in VM yet.
                // Actually LoadSummariesLocalAsync should have set it in VM.TargetRatePct if applicable.
                double? targetRatePct = active.TargetRatePct;

                 // Apply Cash Discount
                decimal transactionPrice = vehiclePrice - (decimal)cashDiscount;
                if (transactionPrice < 0) transactionPrice = 0;

                // Calculate Subinterest Subsidy if Target Rate is set
                decimal subinterestSubsidy = 0m;
                if (targetRatePct.HasValue)
                {
                     decimal upfrontCostsDelta = (decimal)(fsFreeInsurance + fsFreeMbsp);
                     double required = ComputeRequiredSubsidyForRateBuydown(transactionPrice, false, (decimal)fsSubDown, upfrontCostsDelta, CustomerRatePct, targetRatePct.Value);
                     subinterestSubsidy = (decimal)required;
                }
                // Update VM with calculated subinterest subsidy for display
                active.SubinterestSubsidy = subinterestSubsidy.ToString("N0", CultureInfo.InvariantCulture);

                // Calculate Unallocated Subsidy & Total Upfront Subsidy for engine
                // Total Upfront Subsidy to lender = Total Budget - (CashDisc + SubDown + FreeIns + FreeMbsp)
                // The buydown cost is covered within this amount.
                double totalUpfrontSubsidyForEngine = SubsidyBudget - cashDiscount - fsSubDown - fsFreeInsurance - fsFreeMbsp;

                var (outc, profit, commPct, commAmt) = ComputeScenarioWithCommission(
                    transactionPrice,
                    false,
                    (decimal)fsSubDown,
                    (decimal)(fsFreeInsurance + fsFreeMbsp),
                    (decimal)totalUpfrontSubsidyForEngine - (decimal)UpfrontSubsidies, // Delta
                    targetRatePct
                );

                double subsidyUsed = cashDiscount + fsSubDown + fsFreeInsurance + fsFreeMbsp + (double)subinterestSubsidy;

                UpdateMetricsFromCampaign(active, outc, profit, commAmt, subsidyUsed, fsFreeInsurance, fsFreeMbsp, (double)subinterestSubsidy);
                PopulateCashflows(LocalScheduleToDto(outc.Schedule));
                
                var campaignTypeStr = IsMyCampaign(active) ? "My Campaign" : "Standard Campaign";
                CashflowCampaignName = $"{campaignTypeStr}: {active.CampaignId}";
            }
            
            OnPropertyChanged(nameof(ActiveCampaign));
            OnPropertyChanged(nameof(ActiveFsInsuranceText));
            OnPropertyChanged(nameof(ActiveFsMbspText));
            OnPropertyChanged(nameof(ActiveSubsidyUtilizedText));
            OnPropertyChanged(nameof(SubsidyRemainingText));
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        await Task.CompletedTask;
    }

    // MARK: Helper - Check if campaign is from My Campaigns
    private bool IsMyCampaign(CampaignSummaryViewModel campaign)
    {
        return MyCampaigns.Contains(campaign);
    }

    // MARK: Profitability details (local)
    private void RefreshProfitabilityDetailsLocal(FinancialCalculator.Engine.Models.Profitability p)
    {
        _wfCustomerRate = (double)p.CustomerRate;
        _wfDealIRREffective = (double)p.DealIrrEffective;
        _wfDealIRRNominal = (double)p.DealIrrNominal;
        _wfIDCUpfrontAnnualized = (double)p.IdcUpfrontAnnualizedPct;
        _wfSubsidyUpfrontAnnualized = (double)p.SubsidyUpfrontAnnualizedPct;
        _wfCostOfDebtMatched = (double)p.MatchedFundingRate;
        _wfMatchedFundedSpread = (double)p.MatchedFundingSpread;
        _wfGrossInterestMargin = (double)p.GrossInterestMargin;
        _wfNetInterestMargin = (double)p.NetInterestMargin;
        _wfCostOfCreditRisk = (double)p.CostOfRisk;
        _wfOPEX = (double)p.OpexPct;
        _wfCapitalAdvantage = (double)p.CapitalAdvantage;
        _wfNetEBITMargin = (double)p.NetEbitMargin;
        _wfEconomicCapital = 0.08; // fixed ratio used in local calc
        
        // Map separated IDC/Subsidy values
        _wfIDCUpfrontCostPct = (double)p.IdcUpfrontAnnualizedPct; // Same as annualized for now
        _wfIDCPeriodicCostPct = (double)p.IdcPeriodicPct;
        _wfSubsidyUpfrontPct = (double)p.SubsidyUpfrontAnnualizedPct; // Same as annualized for now
        _wfSubsidyPeriodicPct = (double)p.SubsidyPeriodicPct;
        
        // Combined net values (IDC - Subsidy)
        _wfIDCUpfront = (double)(p.IdcUpfrontAnnualizedPct - p.SubsidyUpfrontAnnualizedPct);
        _wfIDCPeriodic = (double)(p.IdcPeriodicPct - p.SubsidyPeriodicPct);

        // Active campaign allocations for bottom summary (use VM numeric fields)
        if (ActiveCampaign != null)
        {
            _activeFsInsurance = Math.Max(0, ActiveCampaign.FSSubInterestAmount);
            _activeFsMbsp = Math.Max(0, ActiveCampaign.FSFreeMBSPAmount);
            _activeCashDiscount = Math.Max(0, ActiveCampaign.CashDiscountAmount);
        }
        else
        {
            _activeFsInsurance = _activeFsMbsp = _activeCashDiscount = 0;
        }

        // Notify UI
        OnPropertyChanged(nameof(ActiveFsInsuranceText));
        OnPropertyChanged(nameof(ActiveFsMbspText));
        OnPropertyChanged(nameof(ActiveSubsidyUtilizedText));
        OnPropertyChanged(nameof(SubsidyRemainingText));
        OnPropertyChanged(nameof(IdcOtherText));
        OnPropertyChanged(nameof(IdcTotalText));

        OnPropertyChanged(nameof(WfCustomerRateText));
        OnPropertyChanged(nameof(WfDealIRREffectiveText));
        OnPropertyChanged(nameof(WfIDCUpfrontAnnualizedText));
        OnPropertyChanged(nameof(WfSubsidyUpfrontAnnualizedText));
        OnPropertyChanged(nameof(WfCostOfDebtMatchedText));
        OnPropertyChanged(nameof(WfMatchedFundedSpreadText));
        OnPropertyChanged(nameof(WfGrossInterestMarginText));
        OnPropertyChanged(nameof(WfNetInterestMarginText));
        OnPropertyChanged(nameof(WfCostOfCreditRiskText));
        OnPropertyChanged(nameof(WfOPEXText));
        OnPropertyChanged(nameof(WfCapitalAdvantageText));
        OnPropertyChanged(nameof(WfNetEBITMarginText));
        OnPropertyChanged(nameof(WfEconomicCapitalText));
        
        // Notify for new separated IDC/Subsidy fields
        OnPropertyChanged(nameof(WfIDCUpfrontCostPctText));
        OnPropertyChanged(nameof(WfIDCPeriodicCostPctText));
        OnPropertyChanged(nameof(WfSubsidyUpfrontPctText));
        OnPropertyChanged(nameof(WfSubsidyPeriodicPctText));
    }

    // MARK: View Cashflows
    [RelayCommand]
    private async Task ViewCashflowsAsync()
    {
        try
        {
            if (ActiveCampaign == null)
            {
                Status = "No campaign selected";
                return;
            }

            await RefreshActiveSelectionAsync();
        }
        catch (Exception ex)
        {
            Status = $"Error loading cashflows: {ex.Message}";
        }
    }

    // MARK: Export - CSV (xlsx extension)
    [RelayCommand]
    private async Task ExportXlsxAsync()
    {
        try
        {
            Status = "Preparing export...";

            var active = ActiveCampaign;
            decimal vehiclePrice = (decimal)PriceExTax;
            bool subIsPct = false;
            decimal subVal = 0;
            decimal upCostDelta = 0m;
            decimal upSubDelta = 0m;
            double? rateOverride = null;

            if (active != null)
            {
                var type = active.CampaignType?.ToLowerInvariant() ?? string.Empty;
                if (type == "cash_discount" && active.CashDiscountAmount > 0)
                {
                    vehiclePrice = Math.Max(0m, vehiclePrice - (decimal)active.CashDiscountAmount);
                }
                if (active.FSSubDownAmount > 0) { subIsPct = false; subVal = (decimal)active.FSSubDownAmount; }
                if (active.FSSubInterestAmount > 0) upCostDelta += (decimal)active.FSSubInterestAmount;
                if (active.FSFreeMBSPAmount > 0) upCostDelta += (decimal)active.FSFreeMBSPAmount;
            }

            var res = ComputeScenarioWithCommission(vehiclePrice, subIsPct, subVal, upCostDelta, upSubDelta, rateOverride);
            RefreshProfitabilityDetailsLocal(res.profit);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Deal Summary");
            sb.AppendLine("Key,Value");
            sb.AppendLine($"Selected Campaign,{(active?.Title ?? "-")}");
            sb.AppendLine($"Monthly Installment (THB),{res.outputs.MonthlyRate.ToString("N0", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Nominal Rate,{(CustomerRatePct / 100.0).ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Effective Rate,{((double)res.outputs.FlatRatePercentPerAnnum / 100.0).ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Financed Amount (THB),{res.outputs.FinancedAmount.ToString("N0", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Acq. RoRAC,{((double)res.profit.AcquisitionRoRac).ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Dealer Commission (THB),{res.commissionAmt.ToString("N0", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"IDC - Other (THB),{IdcOther.ToString("N0", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"IDC Total (THB),{(res.commissionAmt + IdcOther).ToString("N0", CultureInfo.InvariantCulture)}");
            sb.AppendLine();

            // Profitability Details
            sb.AppendLine("Profitability Details");
            sb.AppendLine("Metric,Value");
            sb.AppendLine($"Deal IRR Effective,{_wfDealIRREffective.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Deal IRR Nominal,{_wfDealIRRNominal.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Cost of Debt Matched,{_wfCostOfDebtMatched.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Matched Funded Spread,{_wfMatchedFundedSpread.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Gross Interest Margin,{_wfGrossInterestMargin.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Capital Advantage,{_wfCapitalAdvantage.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Net Interest Margin,{_wfNetInterestMargin.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Cost of Credit Risk,{_wfCostOfCreditRisk.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"OPEX,{_wfOPEX.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Net IDC+Subsidies Upfront,{_wfIDCUpfront.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Net IDC+Subsidies Periodic,{_wfIDCPeriodic.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Net EBIT Margin,{_wfNetEBITMargin.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Economic Capital,{_wfEconomicCapital.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine();

            // Separated Values (placeholders 0 for now)
            sb.AppendLine("Separated Values:");
            sb.AppendLine($"IDC Upfront Cost %,{_wfIDCUpfrontCostPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"IDC Periodic Cost %,{_wfIDCPeriodicCostPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Subsidy Upfront %,{_wfSubsidyUpfrontPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Subsidy Periodic %,{_wfSubsidyPeriodicPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine();

            sb.AppendLine("Cashflow Schedule");
            sb.AppendLine("Period,Principal,Interest,Balance,Cashflow");
            foreach (var r in res.outputs.Schedule)
            {
                sb.AppendLine($"{r.Period},{r.Principal.ToString("0.00", CultureInfo.InvariantCulture)},{r.Interest.ToString("0.00", CultureInfo.InvariantCulture)},{r.Balance.ToString("0.00", CultureInfo.InvariantCulture)},{r.Cashflow.ToString("0.00", CultureInfo.InvariantCulture)}");
            }

            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FinancialCalculatorExports");
            System.IO.Directory.CreateDirectory(dir);
            var file = System.IO.Path.Combine(dir, $"deal_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            await System.IO.File.WriteAllTextAsync(file, sb.ToString(), System.Text.Encoding.UTF8);

            Status = $"Exported XLSX to {file}";
        }
        catch (Exception ex)
        {
            Status = $"Export failed: {ex.Message}";
        }
    }

    private void PopulateCashflows(IReadOnlyList<CashflowRowDto> schedule)
    {
        Cashflows.Clear();
        if (schedule == null) return;

        double cumulativePrincipal = 0;
        double cumulativeInterest = 0;
        double totalPrincipal = 0;
        double totalInterest = 0;

        foreach (var r in schedule)
        {
            cumulativePrincipal += r.Principal;
            cumulativeInterest += r.Interest;
            totalPrincipal += r.Principal;
            totalInterest += r.Interest;
            var totalPayment = r.Principal + r.Interest;

            string idcBreakdown = "";
            if (r.Period == 1)
            {
                var idcTotal = DealerCommissionResolvedAmt + IdcOther;
                if (idcTotal > 0)
                {
                    idcBreakdown = idcTotal.ToString("N0", CultureInfo.InvariantCulture);
                }
            }

            string subsidyAllocation = "";
            if (r.Period == 1 && ActiveCampaign != null)
            {
                double subsidyAmount = ActiveCampaign.FSSubInterestAmount + ActiveCampaign.FSFreeMBSPAmount;
                if (subsidyAmount > 0)
                {
                    subsidyAllocation = subsidyAmount.ToString("N0", CultureInfo.InvariantCulture);
                }
            }

            Cashflows.Add(new CashflowRowViewModel
            {
                Period = r.Period,
                Principal = r.Principal.ToString("N0", CultureInfo.InvariantCulture),
                Interest = r.Interest.ToString("N0", CultureInfo.InvariantCulture),

                Balance = r.Balance.ToString("N0", CultureInfo.InvariantCulture),
                Cashflow = r.Cashflow.ToString("N0", CultureInfo.InvariantCulture),
                PrincipalRunoff = cumulativePrincipal.ToString("N0", CultureInfo.InvariantCulture),
                InterestRunoff = cumulativeInterest.ToString("N0", CultureInfo.InvariantCulture),
                SubsidyAllocation = subsidyAllocation,
                IdcBreakdown = idcBreakdown,
                TotalPayment = totalPayment.ToString("N0", CultureInfo.InvariantCulture)
            });
        }

        // Update summary properties
        TotalPrincipalPaid = totalPrincipal.ToString("N0", CultureInfo.InvariantCulture);
        TotalInterestPaid = totalInterest.ToString("N0", CultureInfo.InvariantCulture);
        TotalFeesPaid = (DealerCommissionResolvedAmt + IdcOther).ToString("N0", CultureInfo.InvariantCulture);

        // Calculate net amount financed (baseline proxy)
        var netFinanced = Math.Max(0, PriceExTax - DownPaymentAmount);
        NetAmountFinanced = netFinanced.ToString("N0", CultureInfo.InvariantCulture);
    }

    private void UpdateDealerCommissionResolved()
    {
        try
        {
            var financed = Math.Max(0, PriceExTax - DownPaymentAmount);
            double pct = DealerCommissionMode == "override" ? (DealerCommissionPct ?? AutoCommissionPct) : AutoCommissionPct;
            if (pct < 0) pct = 0;
            double amt = DealerCommissionMode == "override" && DealerCommissionAmt.HasValue
                ? DealerCommissionAmt.Value
                : Math.Round(financed * pct);
            DealerCommissionResolvedAmt = Math.Max(0, amt);
        }
        catch
        {
            DealerCommissionResolvedAmt = 0;
        }
    }

    private void RefreshCommissionPolicyLocal()
    {
        try
        {
            var p = (Product ?? string.Empty).Trim().ToUpperInvariant();
            AutoCommissionPct = p == "HP" ? 0.03 : 0.07;
            CommissionPolicyVersion = "local-v1";
            UpdateDealerCommissionResolved();
            OnPropertyChanged(nameof(DealerCommissionPctText));
            Status = $"Policy {CommissionPolicyVersion}: auto dealer {AutoCommissionPct:P2}";
        }
        catch (Exception ex)
        {
            AutoCommissionPct = 0;
            CommissionPolicyVersion = string.Empty;
            Status = $"Commission policy error: {ex.Message}";
        }
    }

    // MARK: Helpers
    private DealDto BuildDealFromInputs()
    {
        // Map unified entry + unit to engine-facing fields (legacy DTO for minor display helpers)
        double dpAmt = 0, dpPct = 0; string dpLock = "amount";
        if (string.Equals(DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase))
        {
            dpPct = DownPaymentValueEntry / 100.0;
            dpLock = "percent";
        }
        else
        {
            dpAmt = DownPaymentValueEntry;
            dpLock = "amount";
        }

        double blAmt = 0, blPct = 0;
        if (string.Equals(BalloonUnit, "%", StringComparison.OrdinalIgnoreCase))
        {
            blPct = BalloonValueEntry / 100.0;
        }
        else
        {
            blAmt = BalloonValueEntry;
        }

        return new DealDto
        {
            Product = Product,
            PriceExTax = PriceExTax,
            DownPaymentAmount = dpAmt,
            DownPaymentPercent = dpPct,
            DownPaymentLocked = dpLock,
            TermMonths = TermMonths,
            BalloonPercent = blPct,
            BalloonAmount = blAmt,
            Timing = Timing,
            RateMode = RateMode,
            CustomerNominalRate = CustomerRatePct / 100.0,
            TargetInstallment = TargetInstallment
        };
    }

    private double ComputeDownpaymentDisplay(decimal vehiclePrice)
    {
        if (string.Equals(DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase))
        {
            return (double)(vehiclePrice * (decimal)DownPaymentValueEntry / 100m);
        }
        return DownPaymentValueEntry;
    }

    private async Task RecalculateMyCampaignAsync(CampaignSummaryViewModel vm)
    {
        // Small delay to let UI update if called frequently (pseudo-debounce) and make it truly async if needed
        await Task.Delay(1);
        if (IsCalculating) return; // Debounce/throttle if needed, though IsCalculating handles basic re-entrancy
        try
        {
            IsCalculating = true;
            Status = $"Recalculating {vm.Title}...";

            // 1. Gather inputs from vm
            decimal vehiclePrice = (decimal)PriceExTax;
            double cashDiscount = vm.CashDiscountAmount;
            double fsSubDown = vm.FSSubDownAmount;
            double fsFreeInsurance = vm.FSSubInterestAmount; // Mapped to free insurance field
            double fsFreeMbsp = vm.FSFreeMBSPAmount;
            double? targetRatePct = vm.TargetRatePct;

            // Apply Cash Discount to Vehicle Price
            decimal transactionPrice = vehiclePrice - (decimal)cashDiscount;
            if (transactionPrice < 0) transactionPrice = 0;

            // 2. Calculate Subinterest Subsidy if Target Rate is set
            decimal subinterestSubsidy = 0m;
            if (targetRatePct.HasValue)
            {
                // Calculate required subsidy to reach target rate.
                // Note: We need to be careful about circular dependencies here if subinterest subsidy affects financed amount (it shouldn't directly, but it uses budget).
                // Subinterest subsidy is an UPFRONT SUBSIDY to the lender (income) to offset lower interest.
                // It does NOT reduce financed amount for customer.
                
                // We need to calculate the deal WITHOUT subinterest subsidy first to see the base flows, 
                // but actually the function ComputeRequiredSubsidyForRateBuydown handles this by comparing two scenarios.
                
                // The cost base for buydown should include other campaign costs?
                // Actually, buydown is based on the financed amount.
                // Financed amount depends on Transaction Price and SubDown.
                
                // Let's use the helper, but we need to pass the correct transaction price.
                // And Upfront Costs Delta should include Free Insurance + Free MBSP.
                decimal upfrontCostsDelta = (decimal)(fsFreeInsurance + fsFreeMbsp);

                double required = ComputeRequiredSubsidyForRateBuydown(transactionPrice, false, (decimal)fsSubDown, upfrontCostsDelta, CustomerRatePct, targetRatePct.Value);
                subinterestSubsidy = (decimal)required;
            }

            // 3. Calculate Unallocated Subsidy
            // Total Budget - (Cash Discount + SubDown + Free Insurance + Free MBSP + Subinterest Subsidy)
            // Wait, are Free Insurance/MBSP paid from subsidy budget? Usually yes.
            // The requirement said: "Subsidy budget - cash discount - subdown campaign support - subinterest cost"
            // It didn't explicitly mention Free Insurance/MBSP in that formula in the prompt, but they are campaign costs.
            // Let's assume they ARE paid from budget for now to be safe and consistent with "total campaign cost".
            
            double totalCampaignCost = cashDiscount + fsSubDown + fsFreeInsurance + fsFreeMbsp + (double)subinterestSubsidy;
            double unallocated = SubsidyBudget - totalCampaignCost;
            // if unallocated < 0, it means we are over budget. The calculation should still proceed but maybe show warning?
            // For now, allow negative unallocated (effectively increases dealer contribution/reduces profit if not covered elsewhere, but engine handles negative subsidy as cost if passed correctly? actually UpfrontSubsidies should probably be clamped to 0 if we strictly follow "Subsidy *Budget*")
            // If we want to strictly enforce budget, we might clamp unallocated to 0.
            // BUT, if it's negative, it acts as an extra COST.
            
            decimal upfrontSubsidiesDelta = (decimal)unallocated;
            // If unallocated is positive, it's extra income (subsidy) to deal.
            // If negative, it's extra cost (overrun) to deal.
            // Our ComputeScenarioWithCommission adds this to UpfrontSubsidies.
            // If it's negative, it will reduce UpfrontSubsidies, which is correct (e.g. eating into standard dealer margin if we had any, or just showing up as lower RoRAC).

            // 4. Compute full scenario
            var (outc, profit, commPct, commAmt) = ComputeScenarioWithCommission(
                transactionPrice,
                false, // subdown is value
                (decimal)fsSubDown,
                (decimal)(fsFreeInsurance + fsFreeMbsp), // Upfront costs delta (campaign specific IDCs)
                (decimal)subinterestSubsidy + upfrontSubsidiesDelta, // Total upfront subsidy delta (specific rate subsidy + unallocated remnant)
                                                                     // Wait! If we add subinterestSubsidy AND unallocated,
                                                                     // Total = subinterestSubsidy + (SubsidyBudget - totalCampaignCost)
                                                                     // Total = subinterestSubsidy + SubsidyBudget - (cashDiscount + fsSubDown + fsFreeInsurance + fsFreeMbsp + subinterestSubsidy)
                                                                     // Total = SubsidyBudget - cashDiscount - fsSubDown - fsFreeInsurance - fsFreeMbsp
                                                                     // This looks correct! The specific 'subinterestSubsidy' cancels out in the *Total Upfront Subsidy* passed to engine,
                                                                     // because it's just one component of how the budget is used.
                                                                     // ACTUALLY, we should just pass SubsidyBudget minus non-upfront-subsidy components.
                                                                     // Cash Discount reduces vehicle price (not upfront subsidy to lender).
                                                                     // SubDown reduces financed amount (not upfront subsidy to lender).
                                                                     // Free Insurance/MBSP are Upfront Costs (paid to 3rd party).
                                                                     // SO: Upfront Subsidy to Lender = Total Budget - CashDisc - SubDown - FreeIns - FreeMbsp.
                                                                     // The Rate Buydown (subinterestSubsidy) is internally covered by this remaining amount.
                                                                     // IF we want to track it separately, we need to know how much of that remaining amount is NECESSARY for buydown.
                targetRatePct // Use target rate if set, else null (uses CustomerRatePct)
            );

            // RE-VERIFY THE UPFRONT SUBSIDY LOGIC ABOVE.
            // If Upfront Subsidy to Lender = Total Budget - CashDisc - SubDown - FreeIns - FreeMbsp
            // AND we have a Target Rate that requires 'subinterestSubsidy' amount to achieve same IRR.
            // The engine doesn't "know" about required subinterestSubsidy. It just takes UpfrontSubsidies.
            // If we pass the FULL remaining budget as UpfrontSubsidies, the Deal IRR will reflect it.
            // If that Deal IRR >= Target Rate IRR, then we are good.
            // The prompt said: "we utilize all subsidy available ... show the entire subsidy amount available ... as positive impact on deal irr"
            // So YES, we should pass the entire remaining budget as Upfront Subsidy.
            
            decimal totalUpfrontSubsidyForEngine = (decimal)(SubsidyBudget - cashDiscount - fsSubDown - fsFreeInsurance - fsFreeMbsp);
            
            // Recalculate with verified logic
             (outc, profit, commPct, commAmt) = ComputeScenarioWithCommission(
                transactionPrice,
                false,
                (decimal)fsSubDown,
                (decimal)(fsFreeInsurance + fsFreeMbsp),
                totalUpfrontSubsidyForEngine - (decimal)UpfrontSubsidies, // Delta to achieve desired
                targetRatePct
            );


            // 5. Update VM with results
            vm.Monthly = ((double)outc.MonthlyRate).ToString("N0", CultureInfo.InvariantCulture);
            vm.Effective = ((targetRatePct ?? CustomerRatePct) / 100.0).ToString("0.00%");
            vm.TransactionPrice = transactionPrice.ToString("N0", CultureInfo.InvariantCulture);
            vm.Downpayment = ComputeDownpaymentDisplay(transactionPrice).ToString("N0", CultureInfo.InvariantCulture);
            vm.CashDiscount = cashDiscount.ToString("N0", CultureInfo.InvariantCulture);
            vm.FSSubDown = fsSubDown.ToString("N0", CultureInfo.InvariantCulture);
            vm.FSSubInterest = fsFreeInsurance.ToString("N0", CultureInfo.InvariantCulture);
            vm.FSFreeMBSP = fsFreeMbsp.ToString("N0", CultureInfo.InvariantCulture);
            vm.SubinterestSubsidy = subinterestSubsidy.ToString("N0", CultureInfo.InvariantCulture);
            
            double subsidyUsed = cashDiscount + fsSubDown + fsFreeInsurance + fsFreeMbsp + (double)subinterestSubsidy;
            vm.SubsidyUsed = subsidyUsed.ToString("N0", CultureInfo.InvariantCulture);
            
            double idcsTotal = commAmt + fsFreeInsurance + fsFreeMbsp + IdcOther;
            vm.IDCsTotal = idcsTotal.ToString("N0", CultureInfo.InvariantCulture);
            
            vm.DealerCommission = $"{commPct.ToString("0.00%", CultureInfo.InvariantCulture)} ({commAmt.ToString("N0", CultureInfo.InvariantCulture)} THB)";
            vm.RoRAC = ((double)profit.AcquisitionRoRac).ToString("0.00%");

            // 6. If this is the active campaign, also update the main metrics area
            if (vm == ActiveCampaign)
            {
                UpdateMetricsFromCampaign(vm, outc, profit, commAmt, subsidyUsed, fsFreeInsurance, fsFreeMbsp, (double)subinterestSubsidy);
            }

            Status = "Done";
        }
        catch (Exception ex)
        {
            Status = $"Error calculating {vm.Title}: {ex.Message}";
        }
        finally
        {
            IsCalculating = false;
        }
    }

    private void UpdateMetricsFromCampaign(CampaignSummaryViewModel vm, FinancialCalculator.Engine.Models.CalculatorOutputs outc, FinancialCalculator.Engine.Models.Profitability profit, double commAmt, double subsidyUsed, double fsIns, double fsMbsp, double rateSubsidy = 0)
    {
        Metrics = new MetricsViewModel
        {
            MonthlyInstallment = ((double)outc.MonthlyRate).ToString("N0", CultureInfo.InvariantCulture),
            NominalRate = ((vm.TargetRatePct ?? CustomerRatePct) / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
            EffectiveRate = ((double)outc.FlatRatePercentPerAnnum / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
            FinancedAmount = ((double)outc.FinancedAmount).ToString("N0", CultureInfo.InvariantCulture),
            RoRAC = ((double)profit.AcquisitionRoRac).ToString("0.00%"),
        };

        _activeFsInsurance = fsIns;
        _activeFsMbsp = fsMbsp;
        _activeSubsidyUsed = subsidyUsed;
        // DealerCommissionResolvedAmt = commAmt; // Should we update the global state? Maybe just for display.
        // Better to keep it separate if it's campaign specific.
        // But the UI binds to DealerCommissionResolvedAmtText which uses DealerCommissionResolvedAmt.
        // Let's update it, but be aware it might affect 'Main Calculator' tab if we switch back?
        // Actually, Main Calculator tab recalculates when focused or inputs changed.
        DealerCommissionResolvedAmt = commAmt; 
        
        OnPropertyChanged(nameof(ActiveFsInsuranceText));
        OnPropertyChanged(nameof(ActiveFsMbspText));
        OnPropertyChanged(nameof(ActiveSubsidyUtilizedText));
        OnPropertyChanged(nameof(SubsidyRemainingText));
        OnPropertyChanged(nameof(DealerCommissionResolvedAmtText));
        OnPropertyChanged(nameof(IdcTotalText));

        UpdateBudgetUtilization(
            vm.CashDiscountAmount,
            vm.FSSubDownAmount,
             rateSubsidy,
             fsIns + fsMbsp,
             SubsidyBudget - subsidyUsed
        );

        RefreshProfitabilityDetailsLocal(profit);

        // Force main UI update if this is the selected campaign
        if (vm == SelectedMyCampaign)
        {
             // Trigger update of dependent properties that might not have fired
             OnPropertyChanged(nameof(SelectedMyCampaign));
        }
    }

    // Budget Visualization
    private BudgetUtilizationViewModel _budgetUtilization = new();
    public BudgetUtilizationViewModel BudgetUtilization { get => _budgetUtilization; set => SetProperty(ref _budgetUtilization, value); }

    private void UpdateBudgetUtilization(double cashDiscount, double subDown, double rateSubsidy, double idcs, double unallocated)
    {
        // Ensure we don't have negative widths for GridLength
        cashDiscount = Math.Max(0, cashDiscount);
        subDown = Math.Max(0, subDown);
        rateSubsidy = Math.Max(0, rateSubsidy);
        idcs = Math.Max(0, idcs);
        unallocated = Math.Max(0, unallocated);

        // Prevent all zeros which would collapse grid
        if (cashDiscount + subDown + rateSubsidy + idcs + unallocated <= 0)
        {
            unallocated = 1; // Default to full unallocated if everything is zero
        }

        BudgetUtilization = new BudgetUtilizationViewModel
        {
            CashDiscountPct = new Microsoft.UI.Xaml.GridLength(cashDiscount, Microsoft.UI.Xaml.GridUnitType.Star),
            SubDownPct = new Microsoft.UI.Xaml.GridLength(subDown, Microsoft.UI.Xaml.GridUnitType.Star),
            RateSubsidyPct = new Microsoft.UI.Xaml.GridLength(rateSubsidy, Microsoft.UI.Xaml.GridUnitType.Star),
            IdcPct = new Microsoft.UI.Xaml.GridLength(idcs, Microsoft.UI.Xaml.GridUnitType.Star),
            UnallocatedPct = new Microsoft.UI.Xaml.GridLength(unallocated, Microsoft.UI.Xaml.GridUnitType.Star)
        };
    }
    private (FinancialCalculator.Engine.Models.CalculatorOutputs outputs,
             FinancialCalculator.Engine.Models.Profitability profit,
             double commissionPct,
             double commissionAmt)
        ComputeScenarioWithCommission(decimal vehiclePrice, bool subdownIsPercent, decimal subdownValue, decimal upfrontCostsDelta, decimal upfrontSubsidiesDelta, double? customerRateOverride)
    {
        // First pass: without commission in upfront costs
        var input1 = new LocalScenarioService.ScenarioInput
        {
            Market = "TH",
            Product = Product,
            Timing = Timing,
            TermMonths = TermMonths,
            VehiclePrice = vehiclePrice,
            AdditionalFinancedItems = (decimal)AdditionalFinancedItems,
            DownIsPercent = string.Equals(DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase),
            DownValue = (decimal)DownPaymentValueEntry,
            BalloonIsPercent = string.Equals(BalloonUnit, "%", StringComparison.OrdinalIgnoreCase),
            BalloonValue = (decimal)BalloonValueEntry,
            CustomerRatePercent = (decimal)(customerRateOverride ?? CustomerRatePct),
            UpfrontSubsidies = upfrontSubsidiesDelta,
            UpfrontCosts = (decimal)Math.Max(0, IdcOther) + upfrontCostsDelta,
            SubdownIsPercent = subdownIsPercent,
            SubdownValue = subdownValue,
        };
        var out1 = _scenarios.Compute(input1);

        // Resolve commission based on financed amount
        var (pct, amt) = ResolveCommissionForFinanced(out1.Deal.FinancedAmount);

        // Second pass: include commission in upfront costs
        var input2 = input1 with { UpfrontCosts = input1.UpfrontCosts + (decimal)amt };
        var out2 = _scenarios.Compute(input2);

        return (out2.Deal, out2.Profit, pct, amt);
    }

    private (double pct, double amt) ResolveCommissionForFinanced(decimal financed)
    {
        double pct = DealerCommissionMode == "override" ? (DealerCommissionPct ?? AutoCommissionPct) : AutoCommissionPct;
        if (pct < 0) pct = 0;
        double amt = DealerCommissionMode == "override" && DealerCommissionAmt.HasValue
            ? DealerCommissionAmt.Value
            : Math.Round((double)financed * pct);
        return (pct, Math.Max(0, amt));
    }

    // Compute subsidy required to buy down from baseRatePct to targetRatePct by interest shortfall (approximation)
    private double ComputeRequiredSubsidyForRateBuydown(decimal vehiclePrice, bool subdownIsPercent, decimal subdownValue, decimal upfrontCostsDelta, double baseRatePct, double targetRatePct)
    {
        // Compute monthly at base rate
        var baseRes = ComputeScenarioWithCommission(vehiclePrice, subdownIsPercent, subdownValue, upfrontCostsDelta, 0m, baseRatePct);
        var targetRes = ComputeScenarioWithCommission(vehiclePrice, subdownIsPercent, subdownValue, upfrontCostsDelta, 0m, targetRatePct);
        // Interest shortfall over the term: approximate as sum of (base interest - target interest)
        var baseInt = baseRes.outputs.Schedule.Sum(r => (double)r.Interest);
        var tgtInt = targetRes.outputs.Schedule.Sum(r => (double)r.Interest);
        var shortfall = Math.Max(0, baseInt - tgtInt);
        return shortfall;
    }

    // MARK: Bottom Summary Bindings for Details/Key Metrics
    private double _activeFsInsurance;
    private double _activeFsMbsp;
    private double _activeCashDiscount;

    public string ActiveFsInsuranceText => _activeFsInsurance.ToString("N0", CultureInfo.InvariantCulture);
    public string ActiveFsMbspText => _activeFsMbsp.ToString("N0", CultureInfo.InvariantCulture);
    private double _activeSubsidyUsed;
    public string ActiveSubsidyUtilizedText => _activeSubsidyUsed.ToString("N0", CultureInfo.InvariantCulture);
    public string SubsidyRemainingText => Math.Max(0, SubsidyBudget - _activeSubsidyUsed).ToString("N0", CultureInfo.InvariantCulture);
    public string IdcOtherText => IdcOther.ToString("N0", CultureInfo.InvariantCulture);
    public string IdcTotalText => (DealerCommissionResolvedAmt + IdcOther).ToString("N0", CultureInfo.InvariantCulture);

    // MARK: Profitability Waterfall (for RoRAC details panel)
    private double _wfCustomerRate;
    private double _wfIDCUpfrontAnnualized;
    private double _wfSubsidyUpfrontAnnualized;
    private double _wfDealIRREffective;
    private double _wfCostOfDebtMatched;
    private double _wfMatchedFundedSpread;
    private double _wfGrossInterestMargin;
    private double _wfNetInterestMargin;
    private double _wfCostOfCreditRisk;
    private double _wfOPEX;
    private double _wfCapitalAdvantage;
    private double _wfNetEBITMargin;
    private double _wfEconomicCapital;
    
    // Additional waterfall fields for separated IDC/Subsidy values
    private double _wfDealIRRNominal;
    private double _wfIDCUpfront;  // Combined IDC+Subsidies upfront (net)
    private double _wfIDCPeriodic; // Combined IDC+Subsidies periodic (net)
    private double _wfIDCUpfrontCostPct;  // Separated IDC upfront cost %
    private double _wfIDCPeriodicCostPct; // Separated IDC periodic cost %
    private double _wfSubsidyUpfrontPct;  // Separated subsidy upfront %
    private double _wfSubsidyPeriodicPct; // Separated subsidy periodic %

    // Percent formatting helper
    private static string Pct(double v) => v.ToString("0.00%", CultureInfo.InvariantCulture);

    // Exposed formatted texts
    public string WfCustomerRateText => Pct(_wfCustomerRate);
    public string WfIDCUpfrontAnnualizedText => Pct(_wfIDCUpfrontAnnualized);
    public string WfSubsidyUpfrontAnnualizedText => Pct(_wfSubsidyUpfrontAnnualized);
    public string WfDealIRREffectiveText => Pct(_wfDealIRREffective);
    public string WfCostOfDebtMatchedText => Pct(_wfCostOfDebtMatched);
    public string WfMatchedFundedSpreadText => Pct(_wfMatchedFundedSpread);
    public string WfGrossInterestMarginText => Pct(_wfGrossInterestMargin);
    public string WfNetInterestMarginText => Pct(_wfNetInterestMargin);
    public string WfCostOfCreditRiskText => Pct(_wfCostOfCreditRisk);
    public string WfOPEXText => Pct(_wfOPEX);
    public string WfCapitalAdvantageText => Pct(_wfCapitalAdvantage);
    public string WfNetEBITMarginText => Pct(_wfNetEBITMargin);
    public string WfEconomicCapitalText => Pct(_wfEconomicCapital);
    
    // Additional separated IDC/Subsidy properties for UI display
    // These show the breakdown of upfront/periodic IDC and subsidies
    public string WfIDCUpfrontCostPctText => Pct(_wfIDCUpfrontCostPct);
    public string WfIDCPeriodicCostPctText => Pct(_wfIDCPeriodicCostPct);
    public string WfSubsidyUpfrontPctText => Pct(_wfSubsidyUpfrontPct);
    public string WfSubsidyPeriodicPctText => Pct(_wfSubsidyPeriodicPct);
}

public partial class MetricsViewModel : ObservableObject
{
    public string MonthlyInstallment { get; set; } = "";
    public string NominalRate { get; set; } = "";
    public string EffectiveRate { get; set; } = "";
    public string FinancedAmount { get; set; } = "";
    public string RoRAC { get; set; } = "";
}

public partial class CampaignSummaryViewModel : ObservableObject
{
    public string CampaignId { get; set; } = string.Empty;
    public string CampaignType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DealerCommission { get; set; } = string.Empty;
    public string Monthly { get; set; } = string.Empty;
    public string Effective { get; set; } = string.Empty;
    public string Downpayment { get; set; } = string.Empty;
    public string TransactionPrice { get; set; } = string.Empty;
    public string CashDiscount { get; set; } = string.Empty;
    public string FSSubDown { get; set; } = string.Empty;
    public string FSSubInterest { get; set; } = string.Empty;  // For free insurance IDC amount
    public string SubinterestSubsidy { get; set; } = string.Empty;  // For subinterest rate buydown subsidy
    public string FSFreeMBSP { get; set; } = string.Empty;
    public string SubsidyUsed { get; set; } = string.Empty;
    public string IDCsTotal { get; set; } = string.Empty;  // Total of all IDCs (commission + free insurance + free MBSP + other)
    public string RoRAC { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // Editable amounts for My Campaigns (impact calculators)
    private double _cashDiscountAmount;
    public double CashDiscountAmount { get => _cashDiscountAmount; set { if (_cashDiscountAmount != value) { _cashDiscountAmount = value; OnPropertyChanged(nameof(CashDiscountAmount)); } } }
    private double _fsSubDownAmount;
    public double FSSubDownAmount { get => _fsSubDownAmount; set { if (_fsSubDownAmount != value) { _fsSubDownAmount = value; OnPropertyChanged(nameof(FSSubDownAmount)); } } }
    private double _fsSubInterestAmount;
    public double FSSubInterestAmount { get => _fsSubInterestAmount; set { if (_fsSubInterestAmount != value) { _fsSubInterestAmount = value; OnPropertyChanged(nameof(FSSubInterestAmount)); } } }
    private double _subinterestSubsidyAmount;
    public double SubinterestSubsidyAmount { get => _subinterestSubsidyAmount; set { if (_subinterestSubsidyAmount != value) { _subinterestSubsidyAmount = value; OnPropertyChanged(nameof(SubinterestSubsidyAmount)); } } }
    private double _idcMbspCostAmount;
    public double IDC_MBSP_CostAmount { get => _idcMbspCostAmount; set { if (_idcMbspCostAmount != value) { _idcMbspCostAmount = value; OnPropertyChanged(nameof(IDC_MBSP_CostAmount)); } } }
    private double _fsFreeMbspAmount;
    public double FSFreeMBSPAmount { get => _fsFreeMbspAmount; set { if (_fsFreeMbspAmount != value) { _fsFreeMbspAmount = value; OnPropertyChanged(nameof(FSFreeMBSPAmount)); } } }

    // Editable Target Rate for subinterest campaigns (% p.a., e.g., 0.99, 2.99)
    private double? _targetRatePct;
    public double? TargetRatePct
    {
        get => _targetRatePct;
        set
        {
            if (_targetRatePct != value)
            {
                _targetRatePct = value;
                OnPropertyChanged(nameof(TargetRatePct));
            }
        }
    }

    public CampaignSummaryViewModel Clone() => new CampaignSummaryViewModel
    {
        CampaignId = this.CampaignId,
        CampaignType = this.CampaignType,
        Title = this.Title,
        DealerCommission = this.DealerCommission,
        Monthly = this.Monthly,
        Effective = this.Effective,
        Downpayment = this.Downpayment,
        TransactionPrice = this.TransactionPrice,
        CashDiscount = this.CashDiscount,
        FSSubDown = this.FSSubDown,
        FSSubInterest = this.FSSubInterest,
        SubinterestSubsidy = this.SubinterestSubsidy,
        FSFreeMBSP = this.FSFreeMBSP,
        SubsidyUsed = this.SubsidyUsed,
        IDCsTotal = this.IDCsTotal,
        RoRAC = this.RoRAC,
        Notes = this.Notes,
        CashDiscountAmount = this.CashDiscountAmount,
        FSSubDownAmount = this.FSSubDownAmount,
        FSSubInterestAmount = this.FSSubInterestAmount,
        SubinterestSubsidyAmount = this.SubinterestSubsidyAmount,
        IDC_MBSP_CostAmount = this.IDC_MBSP_CostAmount,
        FSFreeMBSPAmount = this.FSFreeMBSPAmount,
        TargetRatePct = this.TargetRatePct
    };
}

public partial class CashflowRowViewModel : ObservableObject
{
    public int Period { get; set; }
    public string Principal { get; set; } = "";
    public string Interest { get; set; } = "";

    public string Balance { get; set; } = "";
    public string Cashflow { get; set; } = "";

    // New detailed breakdown properties
    public string PrincipalRunoff { get; set; } = "";  // Cumulative principal paid
    public string InterestRunoff { get; set; } = "";   // Cumulative interest paid
    public string SubsidyAllocation { get; set; } = ""; // Subsidy amount if any
    public string IdcBreakdown { get; set; } = "";      // Commission and other IDCs per period
    public string TotalPayment { get; set; } = "";      // Principal + Interest + Fees
}

public partial class BudgetUtilizationViewModel : ObservableObject
{
    // Using GridLength to support proportional sizing in XAML
    public Microsoft.UI.Xaml.GridLength CashDiscountPct { get; set; } = new(0, Microsoft.UI.Xaml.GridUnitType.Star);
    public Microsoft.UI.Xaml.GridLength SubDownPct { get; set; } = new(0, Microsoft.UI.Xaml.GridUnitType.Star);
    public Microsoft.UI.Xaml.GridLength RateSubsidyPct { get; set; } = new(0, Microsoft.UI.Xaml.GridUnitType.Star);
    public Microsoft.UI.Xaml.GridLength IdcPct { get; set; } = new(0, Microsoft.UI.Xaml.GridUnitType.Star);
    public Microsoft.UI.Xaml.GridLength UnallocatedPct { get; set; } = new(1, Microsoft.UI.Xaml.GridUnitType.Star); // Default all unallocated
}
