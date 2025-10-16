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
    private double _upfrontCosts = 0;
    public double UpfrontCosts { get => _upfrontCosts; set => SetProperty(ref _upfrontCosts, value); }
    private double _subdownAmount = 0;
    public double SubdownAmount { get => _subdownAmount; set => SetProperty(ref _subdownAmount, value); }
    private double _subdownPercent = 0;
    public double SubdownPercent { get => _subdownPercent; set => SetProperty(ref _subdownPercent, value); }
    private bool _subdownIsPercent = false;
    public bool SubdownIsPercent { get => _subdownIsPercent; set => SetProperty(ref _subdownIsPercent, value); }
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
    }
    // MARK: Collections & Selection
    public ObservableCollection<CampaignSummaryViewModel> StandardCampaigns { get; } = new();
    public ObservableCollection<CampaignSummaryViewModel> CampaignSummaries { get; } = new(); // back-compat alias
    public ObservableCollection<CampaignSummaryViewModel> MyCampaigns { get; } = new();

    // Selections
    private CampaignSummaryViewModel? _selectedCampaign; // Standard selection
    public CampaignSummaryViewModel? SelectedCampaign { get => _selectedCampaign; set { if (SetProperty(ref _selectedCampaign, value)) OnSelectedCampaignChanged(value); } }
    private CampaignSummaryViewModel? _selectedMyCampaign;
    public CampaignSummaryViewModel? SelectedMyCampaign { get => _selectedMyCampaign; set { if (SetProperty(ref _selectedMyCampaign, value)) OnSelectedMyCampaignChanged(value); } }

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
        
        IdcOther = SubsidyBudget; // initial mapping per spec

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
                subdownIsPercent: SubdownIsPercent,
                subdownValue: (decimal)(SubdownIsPercent ? SubdownPercent : SubdownAmount),
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
                    bool subIsPct = SubdownIsPercent;
                    decimal subVal = (decimal)(SubdownIsPercent ? SubdownPercent : SubdownAmount);
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
                        // For other campaigns, apply leftover budget as upfront subsidy income
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
                UpfrontSubsidies = (decimal)UpfrontSubsidies,
                UpfrontCosts = (decimal)(UpfrontCosts + DealerCommissionResolvedAmt + IdcOther),
                SubdownIsPercent = SubdownIsPercent,
                SubdownValue = (decimal)(SubdownIsPercent ? SubdownPercent : SubdownAmount)
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

            decimal vehiclePrice = (decimal)PriceExTax;
            bool subIsPct = SubdownIsPercent;
            decimal subVal = (decimal)(SubdownIsPercent ? SubdownPercent : SubdownAmount);
            decimal upCostDelta = 0m;
            decimal upSubDelta = 0m;
            double? rateOverride = null;

            _activeSubsidyUsed = 0;
            if (active != null)
            {
                var type = active.CampaignType?.ToLowerInvariant() ?? string.Empty;
                if (type == "cash_discount" && active.CashDiscountAmount > 0)
                {
                    var disc = (decimal)active.CashDiscountAmount;
                    vehiclePrice = Math.Max(0m, vehiclePrice - disc);
                    _activeSubsidyUsed += active.CashDiscountAmount;
                }
                if (active.FSSubDownAmount > 0)
                {
                    subIsPct = false; // treat MyCampaign input as THB by default
                    subVal = (decimal)active.FSSubDownAmount;
                    _activeSubsidyUsed += active.FSSubDownAmount;
                }
                // Treat Free Insurance/MBSP as IDC costs only (no subsidy netting here)
                if (active.FSSubInterestAmount > 0) { upCostDelta += (decimal)active.FSSubInterestAmount; }
                if (active.FSFreeMBSPAmount > 0) { upCostDelta += (decimal)active.FSFreeMBSPAmount; }
                if (active.IDC_MBSP_CostAmount > 0) { upCostDelta += (decimal)active.IDC_MBSP_CostAmount; }

                // Subinterest: use editable row TargetRate if present; else from catalog
                if (type == "subinterest")
                {
                    double? targetRatePct = active.TargetRatePct;
                    if (!targetRatePct.HasValue)
                    {
                        var cat = _campaigns.GetStandard().FirstOrDefault(c => c.Id == active.CampaignId)?.TargetRate;
                        if (cat.HasValue) targetRatePct = cat.Value * 100.0; // Convert from fraction to percent
                    }
                    if (targetRatePct.HasValue)
                    {
                        rateOverride = targetRatePct.Value; // Already in percent units (e.g., 2.99)
                    }
                }
            }

            // For subinterest: compute required subsidy for rate buydown (interest shortfall vs base) and apply, capped by leftover budget
            if (active != null && string.Equals(active.CampaignType, "subinterest", StringComparison.OrdinalIgnoreCase) && rateOverride.HasValue)
            {
                var required = ComputeRequiredSubsidyForRateBuydown(vehiclePrice, subIsPct, subVal, upCostDelta, CustomerRatePct, rateOverride.Value);
                var leftoverBudgetLocal = Math.Max(0, SubsidyBudget - _activeSubsidyUsed);
                upSubDelta = (decimal)Math.Min(required, leftoverBudgetLocal);
            }
            else
            {
                // Leftover budget (not used by subdown/cash discount) becomes subsidy income to improve IRR (simplified model)
                var leftoverBudget = Math.Max(0, SubsidyBudget - _activeSubsidyUsed);
                upSubDelta = (decimal)leftoverBudget;
            }

            var res = ComputeScenarioWithCommission(vehiclePrice, subIsPct, subVal, upCostDelta, upSubDelta, rateOverride);

            // Update metrics with current campaign's calculated values
            var nominalDisplayPct = rateOverride.HasValue ? rateOverride.Value / 100.0 : CustomerRatePct / 100.0;
            Metrics = new MetricsViewModel
            {
                MonthlyInstallment = ((double)res.outputs.MonthlyRate).ToString("N0", CultureInfo.InvariantCulture),
                NominalRate = nominalDisplayPct.ToString("0.00%", CultureInfo.InvariantCulture),
                EffectiveRate = ((double)res.outputs.FlatRatePercentPerAnnum / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
                FinancedAmount = ((double)res.outputs.FinancedAmount).ToString("N0", CultureInfo.InvariantCulture),
                RoRAC = ((double)res.profit.AcquisitionRoRac).ToString("0.00%"),
            };
            
            // Notify UI of metrics update
            OnPropertyChanged(nameof(Metrics));

            PopulateCashflows(LocalScheduleToDto(res.outputs.Schedule));
            RefreshProfitabilityDetailsLocal(res.profit);

            // Update campaign name display
            if (ActiveCampaign != null)
            {
                var campaignType = IsMyCampaign(ActiveCampaign) ? "My Campaign" : "Standard Campaign";
                CashflowCampaignName = $"{campaignType}: {ActiveCampaign.CampaignId}";
            }
            else
            {
                CashflowCampaignName = "No Campaign Selected";
            }
            
            // Update campaign-specific details for bottom summary
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
            bool subIsPct = SubdownIsPercent;
            decimal subVal = (decimal)(SubdownIsPercent ? SubdownPercent : SubdownAmount);
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
            UpfrontSubsidies = (decimal)UpfrontSubsidies + upfrontSubsidiesDelta,
            UpfrontCosts = (decimal)UpfrontCosts + (decimal)IdcOther + upfrontCostsDelta,
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
