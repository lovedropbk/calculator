using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.Engine;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models.Facade;
using FinancialCalculator.WinUI3.Models;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class MainViewModel : ObservableValidator
{
    // NOTE: Use Services.DebounceDispatcher to capture UI SynchronizationContext.
    // Do not use ViewModels.DebounceDispatcher (deprecated) to avoid background-thread UI updates.
    // We use two dispatchers: one for fast updates (active selection) and one for slower full-grid refreshes.
    private readonly FinancialCalculator.WinUI3.Services.DebounceDispatcher _debounceActive = new();
    private readonly FinancialCalculator.WinUI3.Services.DebounceDispatcher _debounceFull = new();
    // private readonly LocalEngineService _local = new(); // Unused now
    private readonly LocalCampaignsProvider _campaigns = new();
    // private readonly LocalScenarioService _scenarios = new(); // Replaced by DealEngine
    private FinancialFacade _financialFacade = null!; // Initialized in InitializeAsync
    private readonly VehicleCatalogService _vehicleCatalog = new();
    private readonly StandardRateService _standardRates = new();
    private readonly CommissionService _commission = new();
    private readonly ExportService _exportService = new();
    private readonly ComparisonService _comparisonService = new();
    private readonly InsuranceCatalogService _insurance = new();
    private CampaignCalculationService _campaignService = null!; // Initialized in InitializeAsync

    // MARK: Parameter Set Caching (legacy no-op)

    // MARK: Deal Inputs (Moved to MainViewModel.Inputs.cs)

    // MARK: Campaigns & Selection (Moved to MainViewModel.Campaigns.cs)

    // Cashflows grid for active selection
    public ObservableCollection<CashflowRowViewModel> Cashflows { get; } = new();

    // MARK: Sub-ViewModels
    public DealInputViewModel DealInput { get; }
    public CampaignManagerViewModel CampaignManager { get; } = new();
    public ResultsViewModel Results { get; } = new();
    public ComparisonViewModel Comparison { get; } = new();
    public CampaignDetailsViewModel CampaignDetails { get; }
    public ViewModels.Layout.LayoutViewModel Layout { get; }
    public ViewModels.Settings.SettingsViewModel Settings { get; }
    public ViewModels.Exports.ExportViewModel Export { get; }
    public GoalSeekViewModel GoalSeek { get; private set; } = null!; // Initialized in InitializeAsync due to Facade dependency
    private string _status = "";
    public string Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(ErrorStatus));
            }
        }
    }
    private bool _isCalculating = false;
    public bool IsCalculating { get => _isCalculating; set => SetProperty(ref _isCalculating, value); }

    public string ErrorStatus
    {
        get
        {
            var s = _status ?? string.Empty;
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            // Consider as error for bottom display if it starts with "Error" or contains "fail"
            return s.StartsWith("Error", StringComparison.InvariantCultureIgnoreCase)
                   || s.Contains("fail", StringComparison.InvariantCultureIgnoreCase)
                ? s
                : string.Empty;
        }
    }

    private bool _isInitializing = true;
    public bool IsInitializing { get => _isInitializing; set => SetProperty(ref _isInitializing, value); }

    [ObservableProperty]
    private bool _cashflowNeedsRefresh;

    public Task InitializationNotifier { get; }
    public IRelayCommand RecalculateCommand { get; }

    // MARK: Notifications (extracted viewmodel)
    public NotificationsViewModel Notifications { get; } = new();

    // Back-compat properties for XAML x:Bind still pointing to ViewModel.*
    public bool IsNotificationOpen { get => Notifications.IsOpen; set { Notifications.IsOpen = value; OnPropertyChanged(nameof(IsNotificationOpen)); } }
    public string NotificationMessage { get => Notifications.Message; set { Notifications.Message = value; OnPropertyChanged(nameof(NotificationMessage)); } }
    public Microsoft.UI.Xaml.Controls.InfoBarSeverity NotificationSeverity { get => Notifications.Severity; set { Notifications.Severity = value; OnPropertyChanged(nameof(NotificationSeverity)); } }

    public MainViewModel()
    {
        RecalculateCommand = new AsyncRelayCommand(RecalculateAsync);
        InitializeGoalSeekCommands();
    
        // Initialize sub-viewmodels
        DealInput = new DealInputViewModel(_vehicleCatalog, _standardRates, _commission);
        CampaignDetails = new CampaignDetailsViewModel(DealInput);
        Layout = new ViewModels.Layout.LayoutViewModel(
            dealInput: DealInput,
            getRight: () => IsCampaignDetailsCollapsed,
            setRight: v => IsCampaignDetailsCollapsed = v
        );
        Settings = new ViewModels.Settings.SettingsViewModel(new Services.AppSettingsService());
        Export = new ViewModels.Exports.ExportViewModel(
            _exportService,
            getCurrent: () => (ActiveCampaign, (FinancialCalculator.Engine.Models.Facade.ScenarioResult?)null),
            setStatus: s => Status = s
        );
        DealInput.IdcOther = 0; // Default to 0, SubsidyBudget is separate now
        // Subscribe to input changes to keep UI and calculations in sync
        DealInput.InputsChanged += OnDealInputsChanged;

        // Initialize data on UI thread with proper error handling
        InitializationNotifier = InitializeAsync();
    }

    private static bool IsAmgModel(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name.Trim().StartsWith("Mercedes-AMG", StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool IsMaybachModel(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name.Trim().StartsWith("Mercedes-Maybach", StringComparison.InvariantCultureIgnoreCase);
    }

    private void OnCampaignManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CampaignManager.ActiveCampaign) ||
            e.PropertyName == nameof(CampaignManager.SelectedCampaign) ||
            e.PropertyName == nameof(CampaignManager.SelectedMyCampaign))
        {
            // Notify MainVM properties that depend on active campaign
            OnPropertyChanged(nameof(ActiveCampaign));
            OnPropertyChanged(nameof(IsMyCampaignSelected));

            // Ensure we listen to property changes on the new ActiveCampaign (Standard or My Campaigns)
            SubscribeToActiveCampaignChanges();
            
            // Trigger refresh of main calculator view (debounced to avoid race conditions from mutual exclusion updates)
            _debounceActive.DebounceAsync(50, async () => await RefreshActiveSelectionAsync());
        }
    }

    private bool _suppressRecalculation = false;

    private void OnDealInputsChanged(object? sender, EventArgs e)
    {
        if (_suppressRecalculation) return;
        // Debounce to avoid excessive recalculations while the user is typing
        _debounceActive.DebounceAsync(150, async () => await RecalculateAsync());
    }

    private async Task InitializeAsync()
    {
        try
        {
            Status = "Initializing...";

            await Task.Delay(200);

            // Initialize Deal Engine (Async)
            var riskRepo = new RiskParameterRepository(new FileService());
            await riskRepo.LoadAsync(RiskParametersLocator.GetPath());
            _financialFacade = new FinancialFacade(riskRepo);
            GoalSeek = new GoalSeekViewModel(_financialFacade, DealInput, RecalculateAsync);
            OnPropertyChanged(nameof(GoalSeek));
            _campaignService = new CampaignCalculationService(_financialFacade, _standardRates);

            GoalSeekSolveForRateAutoAsyncCommand?.NotifyCanExecuteChanged();
            GoalSeekSolveForSubsidyAutoAsyncCommand?.NotifyCanExecuteChanged();
            GoalSeekSolveForDownPaymentAutoAsyncCommand?.NotifyCanExecuteChanged();

            try
            {
                GoalSeek.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(GoalSeekViewModel.TargetRoRac) ||
                        e.PropertyName == nameof(GoalSeekViewModel.TargetInstallment) ||
                        e.PropertyName == nameof(GoalSeekViewModel.IsAnyTargetSet) ||
                        e.PropertyName == nameof(GoalSeekViewModel.IsCalculating))
                    {
                        Logger.Debug($"[GoalSeek] PropertyChanged -> {e.PropertyName}; IsAnyTargetSet={GoalSeek.IsAnyTargetSet}, IsCalculating={GoalSeek.IsCalculating}, Target=(metric={GoalSeek.ActiveTarget.metric}, value={GoalSeek.ActiveTarget.value})");
                        GoalSeekSolveForRateAutoAsyncCommand?.NotifyCanExecuteChanged();
                        GoalSeekSolveForSubsidyAutoAsyncCommand?.NotifyCanExecuteChanged();
                        GoalSeekSolveForDownPaymentAutoAsyncCommand?.NotifyCanExecuteChanged();
                        OnPropertyChanged(nameof(GoalSeek));
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Warn($"[GoalSeek] Failed to subscribe PropertyChanged: {ex.Message}");
            }


            // Subscribe to Campaign Manager changes
            CampaignManager.PropertyChanged += OnCampaignManagerPropertyChanged;

            // Load catalogs
            await _vehicleCatalog.LoadAsync();
            await _standardRates.LoadAsync();
            await _insurance.LoadAsync();

            // Populate vehicles (classes followed by ALL models globally sorted A�Z with Mercedes-AMG next and Mercedes-Maybach last)
            var classes = _vehicleCatalog.GetVehicleClasses().ToList();
            foreach (var c in classes)
            {
                var avg = _vehicleCatalog.GetClassAverage(c);
                if (avg != null) DealInput.AllVehicles.Add(avg);
            }

            // Gather all vehicles across classes and sort globally
            var allVehicles = classes.SelectMany(c => _vehicleCatalog.GetVehiclesByClass(c)).ToList();
            var ordered = allVehicles
                .OrderBy(v => IsMaybachModel(v.ModelName) ? 2 : IsAmgModel(v.ModelName) ? 1 : 0)
                .ThenBy(v => v.ModelName, StringComparer.InvariantCultureIgnoreCase);
            foreach (var v in ordered)
            {
                DealInput.AllVehicles.Add(v);
            }
    
            // Populate MBSP packages
            foreach (var p in _vehicleCatalog.MbspPackages) DealInput.MbspPackages.Add(p);

            // Commission policy: compute locally
            RefreshCommissionPolicyLocal();

            // Load campaign summaries with local engine
            await LoadSummariesLocalAsync();

            // Subscribe to property changes on current ActiveCampaign so UI toggles (e.g., Consume subsidy) trigger recalcs
            SubscribeToActiveCampaignChanges();
            
            // Trigger initial refresh if there's a default selection
            if (ActiveCampaign != null)
            {
                await RefreshActiveSelectionAsync();
            }

            Status = "Ready";
        }
        catch (Exception ex)
        {
            Status = "Initialization failed";
            Logger.Error("MainViewModel initialization error", ex);
            NotificationMessage = $"Initialization error: {ex.Message}";
            NotificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
            IsNotificationOpen = true;
        }
        finally
        {
            IsInitializing = false;
        }
    }




    // MARK: Actions
    private ScenarioRequest? _lastCalculationRequest;

    private async Task RecalculateAsync()
    {
        try
        {
            var request = DealInput.BuildScenarioRequest();
            if (_lastCalculationRequest != null && request == _lastCalculationRequest)
            {
                return;
            }
            _lastCalculationRequest = request;

            IsCalculating = true;
            Status = "Calculating...";

            // Always calculate with local C# engine for high-fidelity cashflows and IRR per spec
            var result = _financialFacade.Calculate(request);

            // Update key metrics from local engine (monthly, flat rate, financed amount)
            Results.Metrics = new MetricsViewModel
            {
                MonthlyInstallment = ((double)result.MonthlyInstallment).ToString("N0", CultureInfo.InvariantCulture),
                NominalRate = (DealInput.CustomerNominalRate / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
                FlatRate = ((double)result.FlatRatePercent / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
                FinancedAmount = ((double)result.FinancedAmount).ToString("N0", CultureInfo.InvariantCulture),
                RoRAC = ((double)result.AcquisitionRoRacPercent).ToString("0.00%"),
            };

            // Populate cashflows from local engine schedule
            PopulateCashflows(result.Schedule);

            // Populate profitability detail waterfall
            RefreshProfitabilityDetailsLocal(result.Profitability);
            // Also sync details allocations for right pane when recalculating stand-alone
            var ac = ActiveCampaign;
            if (ac != null)
            {
                double.TryParse(ac.FSSubInterest, NumberStyles.Any, CultureInfo.InvariantCulture, out var fsIns);
                double.TryParse(ac.FSFreeMBSP, NumberStyles.Any, CultureInfo.InvariantCulture, out var fsMbsp);
                double.TryParse(ac.SubsidyUsed, NumberStyles.Any, CultureInfo.InvariantCulture, out var subsidyUsed);
                CampaignDetails.UpdateActiveAllocations(fsIns, fsMbsp, subsidyUsed);
            }

            Status = "Ready";

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

    private async Task RefreshActiveSelectionAsync()
    {
        try
        {
            if (ActiveCampaign == null)
            {
                Results.CashflowCampaignName = "No Campaign Selected";
                Results.Cashflows.Clear();
                return;
            }

            // Unified recalculation for both standard and custom campaigns.
            // Standard campaigns should auto-clamp to budget if they exceed it due to new inputs.
            // My Campaigns (custom) should NOT auto-clamp, allowing user to see they are over budget.
            bool autoClamp = !IsMyCampaign(ActiveCampaign);
            var res = await CalculateCampaignAsync(ActiveCampaign, autoClampToBudget: autoClamp);

            if (res != null)
            {
                 PopulateCashflows(res.Schedule);
            }

            var campaignTypeStr = IsMyCampaign(ActiveCampaign) ? "My Campaign" : "Standard Campaign";
            Results.CashflowCampaignName = $"{campaignTypeStr}: {ActiveCampaign.CampaignId}";
            
            // Notifications handled by CalculateCampaignAsync -> UpdateMetricsFromCampaign
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    // MARK: Helper - Check if campaign is from My Campaigns
    private bool IsMyCampaign(CampaignSummaryViewModel campaign)
    {
        return CampaignManager.MyCampaigns.Contains(campaign);
    }

    // MARK: Profitability details (local)
    private void RefreshProfitabilityDetailsLocal(FinancialCalculator.Engine.Models.Facade.ProfitabilityDetails p)
    => CampaignDetails.UpdateFromProfitability(p, ActiveCampaign);

    // Legacy right-pane implementation removed after refactor; functionality now handled by CampaignDetailsViewModel.

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
            if (active == null)
            {
                 Status = "No campaign selected for export.";
                 return;
            }

            // Use unified calculation
            var res = await CalculateCampaignAsync(active, autoClampToBudget: !IsMyCampaign(active));

            if (res == null)
            {
                Status = "Export failed during calculation.";
                return;
            }

            // Refresh profit details (handled by CalculateCampaignAsync if active, but harmless to repeat if needed for latest values)
            var file = await _exportService.ExportScenarioAsync(active, res, DealInput.CustomerNominalRate, DealInput.DealerCommissionResolvedAmt, DealInput.IdcOther);

            Status = $"Exported XLSX to {file}";
        }
        catch (Exception ex)
        {
            Status = $"Export failed: {ex.Message}";
        }
    }

    // MARK: Comparison Actions
    [RelayCommand]
    private async Task AddToComparison()
    {
        try
        {
            Logger.Info("AddToComparison invoked");

            // Prefer adding to Campaign Designer (as per UX expectation)
            var active = ActiveCampaign ?? CampaignManager.SelectedCampaign ?? CampaignManager.SelectedMyCampaign;
            if (active != null)
            {
                await CopyToDesignerAsync(active);
                Status = $"Added '{active.Title}' to Campaign Designer";
                return;
            }

            // Fallback: synthesize a simple campaign from current inputs
            var synth = new CampaignSummaryViewModel
            {
                CampaignId = Guid.NewGuid().ToString(),
                Title = "Custom: From Deal Inputs",
                Notes = ""
            };
            await CopyToDesignerAsync(synth);
            Status = $"Added '{synth.Title}' to Campaign Designer";
        }
        catch (Exception ex)
        {
            Logger.Error("AddToComparison failed", ex);
            Status = $"Error adding to designer: {ex.Message}";
        }
    }

    // MARK: Insurance action (Campaign Designer)
    [RelayCommand]
    private async Task ApplyInsuranceCostAsync()
    {
        try
        {
            var vm = ActiveCampaign;
            if (vm == null)
            {
                Status = "No active campaign";
                return;
            }

            var vehicle = DealInput.SelectedVehicle;
            if (vehicle == null)
            {
                NotificationMessage = "Select a vehicle before applying insurance.";
                NotificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
                IsNotificationOpen = true;
                return;
            }

            var price = _insurance.TryGetInsuranceCost(vehicle);

            if (!price.HasValue)
            {
                NotificationMessage = $"No insurance match for '{vehicle.ModelName}'. Please enter the cost manually.";
                NotificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
                IsNotificationOpen = true;
                return;
            }

            vm.FSSubInterestAmount = price.Value;

            // Recalculate with updated IDC
            await CalculateCampaignAsync(vm, autoClampToBudget: !IsMyCampaign(vm));
            Status = "Applied insurance cost";
        }
        catch (Exception ex)
        {
            Status = $"Insurance apply error: {ex.Message}";
        }
    }

    // MARK: Goal Seek -> Delegated to GoalSeekViewModel
    [RelayCommand]
    private void GoalSeekSolveForRate() => GoalSeek.OpenForRate();

    [RelayCommand]
    private void GoalSeekSolveForDownPayment() => GoalSeek.OpenForDownPayment();

    private void PopulateCashflows(IReadOnlyList<FinancialCalculator.Engine.Models.Facade.CashflowRow> schedule)
    {
        Results.Cashflows.Clear();
        if (schedule == null) return;

        double cumulativePrincipal = 0;
        double cumulativeInterest = 0;
        double totalPrincipal = 0;
        double totalInterest = 0;

        foreach (var r in schedule)
        {
            double principal = (double)r.Principal;
            double interest = (double)r.Interest;
            double balance = (double)r.Balance;
            double cashflow = (double)r.Cashflow;

            cumulativePrincipal += principal;
            cumulativeInterest += interest;
            totalPrincipal += principal;
            totalInterest += interest;
            var totalPayment = principal + interest;

            string idcBreakdown = "";
            if (r.Period == 1)
            {
                var idcTotal = DealInput.DealerCommissionResolvedAmt + DealInput.IdcOther;
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

            Results.Cashflows.Add(new CashflowRowViewModel
            {
                Period = r.Period,
                PaymentType = r.PaymentKind.ToString(),
                CapInterest = r.CapitalizedInterest.ToString("N0", CultureInfo.InvariantCulture),
                Principal = principal.ToString("N0", CultureInfo.InvariantCulture),
                Interest = interest.ToString("N0", CultureInfo.InvariantCulture),

                Balance = balance.ToString("N0", CultureInfo.InvariantCulture),
                Cashflow = cashflow.ToString("N0", CultureInfo.InvariantCulture),
                PrincipalRunoff = cumulativePrincipal.ToString("N0", CultureInfo.InvariantCulture),
                InterestRunoff = cumulativeInterest.ToString("N0", CultureInfo.InvariantCulture),
                SubsidyAllocation = subsidyAllocation,
                IdcBreakdown = idcBreakdown,
                TotalPayment = totalPayment.ToString("N0", CultureInfo.InvariantCulture)
            });
        }

        // Update summary properties
        Results.TotalPrincipalPaid = totalPrincipal.ToString("N0", CultureInfo.InvariantCulture);
        Results.TotalInterestPaid = totalInterest.ToString("N0", CultureInfo.InvariantCulture);
        Results.TotalFeesPaid = (DealInput.DealerCommissionResolvedAmt + DealInput.IdcOther).ToString("N0", CultureInfo.InvariantCulture);
        Results.TotalPayments = (totalPrincipal + totalInterest).ToString("N0", CultureInfo.InvariantCulture);

        // Calculate net amount financed (baseline proxy)
        var netFinanced = Math.Max(0, DealInput.PriceExTax - DealInput.DownPaymentAmount);
        Results.NetAmountFinanced = netFinanced.ToString("N0", CultureInfo.InvariantCulture);
    }

    private void RefreshCommissionPolicyLocal()
    {
        DealInput.RefreshCommissionPolicyLocal();
    }

    // MARK: Helpers

    // Budget Visualization
    // Delegate right-pane details to CampaignDetails VM
    public CampaignDetailsViewModel Details => CampaignDetails;
}

#if false

        set
        {
            _targetRatePct = value;
        }
    }

    // Consume remaining subsidy to improve RoRAC
    // private bool _consumeAllSubsidy;
    // public bool ConsumeAllSubsidy
        // get => _consumeAllSubsidy;
        // set
        // {
        //     if (_consumeAllSubsidy != value)
        //     {
        //         _consumeAllSubsidy = value;
        //         OnPropertyChanged(nameof(ConsumeAllSubsidy));
        //     }
        // }
    // }

    // Toggle: include insurance IDC from catalog/manual amount
    private bool _includeInsurance;
    public bool IncludeInsurance
    {
        get => _includeInsurance;
        set
        {
            if (_includeInsurance != value)
            {
                _includeInsurance = value;
                OnPropertyChanged(nameof(IncludeInsurance));
            }
        }
    }

    // MBSP Package Selection
    private string _selectedMbspPackage = "";
    public string SelectedMbspPackage
    {
        get => _selectedMbspPackage;
        set
        {
             if (SetProperty(ref _selectedMbspPackage, value))
             {
                  // Handled by parent VM if needed, or just used for binding
             }
        }
    }

    public CampaignSummaryViewModel Clone()
    {
        var copy = new CampaignSummaryViewModel
        {
            CampaignId = this.CampaignId,
            CampaignType = this.CampaignType,
            Title = this.Title,
            DealerCommission = this.DealerCommission,
            Monthly = this.Monthly,
            CustomerNominalRate = this.CustomerNominalRate,
            CustomerFlatRate = this.CustomerFlatRate,
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
            AvgRoRAC = this.AvgRoRAC,
            Notes = this.Notes,
            CashDiscountAmount = this.CashDiscountAmount,
            FSSubDownAmount = this.FSSubDownAmount,
            FSSubInterestAmount = this.FSSubInterestAmount,
            SubinterestSubsidyAmount = this.SubinterestSubsidyAmount,
            IDC_MBSP_CostAmount = this.IDC_MBSP_CostAmount,
            FSFreeMBSPAmount = this.FSFreeMBSPAmount,
            TargetRatePct = this.TargetRatePct,
            SelectedMbspPackage = this.SelectedMbspPackage,
            ConsumeAllSubsidy = this.ConsumeAllSubsidy,
            IncludeInsurance = this.IncludeInsurance
        };

        // Deep-copy term breakdown items
        if (this.TermBreakdown != null)
        {
            foreach (var tb in this.TermBreakdown)
            {
                copy.TermBreakdown.Add(new TermBreakdownItemViewModel
                {
                    Term = tb.Term,
                    CustomerRatePct = tb.CustomerRatePct,
                    RoRAC = tb.RoRAC,
                    DistributionPct = tb.DistributionPct
                });
            }
        }

        return copy;
    }
}
#endif

