using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.Engine.Core;
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
    private DealEngine _dealEngine = null!; // Initialized in InitializeAsync
    private readonly VehicleCatalogService _vehicleCatalog = new();
    private readonly StandardRateService _standardRates = new();
    private readonly CommissionService _commission = new();

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
    private string _status = "Ready";
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    private bool _isCalculating = false;
    public bool IsCalculating { get => _isCalculating; set => SetProperty(ref _isCalculating, value); }
    public IRelayCommand RecalculateCommand { get; }

    // MARK: Notification
    private bool _isNotificationOpen;
    public bool IsNotificationOpen { get => _isNotificationOpen; set => SetProperty(ref _isNotificationOpen, value); }
    private string _notificationMessage = "";
    public string NotificationMessage { get => _notificationMessage; set => SetProperty(ref _notificationMessage, value); }
    private Microsoft.UI.Xaml.Controls.InfoBarSeverity _notificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational;
    public Microsoft.UI.Xaml.Controls.InfoBarSeverity NotificationSeverity { get => _notificationSeverity; set => SetProperty(ref _notificationSeverity, value); }

    public MainViewModel()
    {
        RecalculateCommand = new AsyncRelayCommand(RecalculateAsync);

        // Initialize sub-viewmodels
        DealInput = new DealInputViewModel(_vehicleCatalog, _standardRates, _commission);
        DealInput.IdcOther = 0; // Default to 0, SubsidyBudget is separate now

        // Initialize data on UI thread with proper error handling
        _ = InitializeAsync();
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
            
            // Trigger refresh of main calculator view (debounced to avoid race conditions from mutual exclusion updates)
            _debounceActive.DebounceAsync(50, async () => await RefreshActiveSelectionAsync());
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            Status = "Initializing...";

            await Task.Delay(200);

            // Initialize Deal Engine (Async)
            var riskRepo = new RiskParameterRepository();
            await riskRepo.LoadAsync(RiskParametersLocator.GetPath());
            _dealEngine = new DealEngine(riskRepo);

            // Subscribe to Campaign Manager changes
            CampaignManager.PropertyChanged += OnCampaignManagerPropertyChanged;

            await InitializeParameterSetAsync();

            // Load catalogs
            await _vehicleCatalog.LoadAsync();
            await _standardRates.LoadAsync();

            // Populate vehicles (classes followed by models)
            var classes = _vehicleCatalog.GetVehicleClasses();
            foreach (var c in classes)
            {
                var avg = _vehicleCatalog.GetClassAverage(c);
                if (avg != null) DealInput.AllVehicles.Add(avg);
            }
            // Separator if needed, but ComboBox doesn't support it easily without templating.
            // Just add models now.
            foreach (var c in classes)
            {
                foreach (var v in _vehicleCatalog.GetVehiclesByClass(c))
                {
                    DealInput.AllVehicles.Add(v);
                }
            }
    
            // Populate MBSP packages
            foreach (var p in _vehicleCatalog.MbspPackages) DealInput.MbspPackages.Add(p);

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



    // MARK: Actions
    private async Task RecalculateAsync()
    {
        try
        {
            IsCalculating = true;
            Status = "Calculating...";

            // Always calculate with local C# engine for high-fidelity cashflows and IRR per spec
            var scenario = _dealEngine.Calculate(new DealEngine.DealInput
            {
                Market = "TH",
                Product = DealInput.Product,
                Timing = DealInput.Timing,
                TermMonths = DealInput.TermMonths,
                VehiclePrice = (decimal)DealInput.PriceExTax,
                AdditionalFinancedItems = (decimal)DealInput.AdditionalFinancedItems,
                DownIsPercent = string.Equals(DealInput.DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase),
                DownValue = (decimal)DealInput.DownPaymentValueEntry,
                BalloonIsPercent = string.Equals(DealInput.BalloonUnit, "%", StringComparison.OrdinalIgnoreCase),
                BalloonValue = (decimal)DealInput.BalloonValueEntry,
                CustomerRatePercent = (decimal)DealInput.CustomerNominalRate,
                // For the main calculator tab (manual scenario), we use the full subsidy budget as upfront subsidy
                UpfrontSubsidies = (decimal)DealInput.SubsidyBudget,
                UpfrontCosts = (decimal)(DealInput.DealerCommissionResolvedAmt + DealInput.IdcOther),
                SubdownIsPercent = false,
                SubdownValue = 0,
                // Risk Parameters
                CustomerType = DealInput.SelectedCustomerType,
                AssetState = string.Equals(DealInput.SelectedAssetState, "New", StringComparison.OrdinalIgnoreCase) ? "N" : "U",
                AssetValuationCurve = DealInput.SelectedAssetValuationCurve,
                Rating = DealInput.SelectedRating
            });

            // Update key metrics from local engine (monthly, flat rate, financed amount)
            Results.Metrics = new MetricsViewModel
            {
                MonthlyInstallment = ((double)scenario.Deal.MonthlyRate).ToString("N0", CultureInfo.InvariantCulture),
                NominalRate = (DealInput.CustomerNominalRate / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
                FlatRate = ((double)scenario.Deal.FlatRatePercentPerAnnum / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
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
            var (outc, profit) = await CalculateCampaignAsync(ActiveCampaign, autoClampToBudget: autoClamp);

            if (outc != null)
            {
                 PopulateCashflows(LocalScheduleToDto(outc.Schedule));
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
        OnPropertyChanged(nameof(WfDealIRRText));
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
            if (active == null)
            {
                 Status = "No campaign selected for export.";
                 return;
            }

            // Use unified calculation
            var (outc, profit) = await CalculateCampaignAsync(active, autoClampToBudget: !IsMyCampaign(active));

            if (outc == null || profit == null)
            {
                Status = "Export failed during calculation.";
                return;
            }

            // Refresh profit details (handled by CalculateCampaignAsync if active, but harmless to repeat if needed for latest values)
            // Actually, CalculateCampaignAsync calls UpdateMetricsFromCampaign if active, which calls RefreshProfitabilityDetailsLocal.
            // So we can rely on VM properties if we wanted, but using 'outc' and 'profit' directly is safer for export consistency.

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Deal Summary");
            sb.AppendLine("Key,Value");
            sb.AppendLine($"Selected Campaign,{active.Title}");
            sb.AppendLine($"Monthly Installment (THB),{outc.MonthlyRate.ToString("N0", CultureInfo.InvariantCulture)}");
            // Use nominal rate from VM (might be target rate) or DealInput
            var nominalRate = active.TargetRatePct ?? DealInput.CustomerNominalRate;
            sb.AppendLine($"Nominal Rate,{(nominalRate / 100.0).ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Flat Rate,{((double)outc.FlatRatePercentPerAnnum / 100.0).ToString("0.00%", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Financed Amount (THB),{outc.FinancedAmount.ToString("N0", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Acq. RoRAC,{((double)profit.AcquisitionRoRac).ToString("0.00%", CultureInfo.InvariantCulture)}");
            
            // Re-resolve commission for export (it was calculated inside CalculateCampaignAsync but not returned directly,
            // though it is in ActiveCampaign.DealerCommission string... let's re-calculate or grab from DealInput if it was updated)
            // CalculateCampaignAsync updated DealInput.DealerCommissionResolvedAmt if active.
            sb.AppendLine($"Dealer Commission (THB),{DealInput.DealerCommissionResolvedAmt.ToString("N0", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"IDC - Other (THB),{DealInput.IdcOther.ToString("N0", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"IDC Total (THB),{(DealInput.DealerCommissionResolvedAmt + DealInput.IdcOther).ToString("N0", CultureInfo.InvariantCulture)}");
            sb.AppendLine();

            // Profitability Details
            sb.AppendLine("Profitability Details");
            sb.AppendLine("Metric,Value");
            sb.AppendLine($"Deal IRR,{_wfDealIRREffective.ToString("0.00%", CultureInfo.InvariantCulture)}");
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
            foreach (var r in outc.Schedule)
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

    // MARK: Comparison Actions
    [RelayCommand]
    private void AddToComparison()
    {
        try
        {
             var deal = new DealComparisonItemViewModel
             {
                 Title = $"Scenario {Comparison.ComparedDeals.Count + 1}",
                 VehicleName = DealInput.SelectedVehicle?.ModelName ?? "Unknown Vehicle",
                 Product = DealInput.Product,
                 Price = DealInput.PriceExTax.ToString("N0", CultureInfo.InvariantCulture),
                 DownPayment = $"{DealInput.DownPaymentValueEntry.ToString("N0", CultureInfo.InvariantCulture)} {DealInput.DownPaymentUnit}",
                 Term = DealInput.TermMonths,
                 NominalRate = (DealInput.CustomerNominalRate / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
                 FlatRate = DealInput.CustomerFlatRate.ToString("0.00%"), // Assuming it's already a percent value in UI
                 Balloon = $"{DealInput.BalloonValueEntry.ToString("N0", CultureInfo.InvariantCulture)} {DealInput.BalloonUnit}",
                 
                 MonthlyInstallment = Results.Metrics.MonthlyInstallment,
                 FinancedAmount = Results.Metrics.FinancedAmount,
                 RoRAC = Results.Metrics.RoRAC,
                 TotalInterest = Results.TotalInterestPaid
             };

             // Add waterfall steps based on current waterfall metrics
             deal.WaterfallSteps.Add(new WaterfallStepViewModel { Label = "Cust. Rate", Value = _wfCustomerRate, FormattedValue = WfCustomerRateText, ColorHex = "#FF0078D7", HeightFactor = Math.Abs(_wfCustomerRate) * 50 });
             deal.WaterfallSteps.Add(new WaterfallStepViewModel { Label = "CoF", Value = -_wfCostOfDebtMatched, FormattedValue = $"-{WfCostOfDebtMatchedText}", ColorHex = "#FFD13438", HeightFactor = Math.Abs(_wfCostOfDebtMatched) * 50 });
             deal.WaterfallSteps.Add(new WaterfallStepViewModel { Label = "Risk", Value = -_wfCostOfCreditRisk, FormattedValue = $"-{WfCostOfCreditRiskText}", ColorHex = "#FFD13438", HeightFactor = Math.Abs(_wfCostOfCreditRisk) * 50 });
             deal.WaterfallSteps.Add(new WaterfallStepViewModel { Label = "OPEX", Value = -_wfOPEX, FormattedValue = $"-{WfOPEXText}", ColorHex = "#FFAA00", HeightFactor = Math.Abs(_wfOPEX) * 50 });
             
             // Net IDC/Subsidy
             double netIdc = _wfIDCUpfrontAnnualized - _wfSubsidyUpfrontAnnualized; // Simplified for now
             
             deal.WaterfallSteps.Add(new WaterfallStepViewModel {
                 Label = "Net IDC",
                 Value = -netIdc,
                 FormattedValue = (netIdc >= 0 ? "-" : "+") + Math.Abs(netIdc).ToString("0.00%", CultureInfo.InvariantCulture),
                 ColorHex = netIdc >= 0 ? "#FFD13438" : "#FF107C10",
                 HeightFactor = Math.Abs(netIdc) * 50
             });

             // RoRAC needs careful parsing from string like "1.45%"
             double roracVal = 0;
             var roracStr = Results.Metrics.RoRAC.TrimEnd('%');
             if (double.TryParse(roracStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var rval))
             {
                 roracVal = rval / 100.0;
             }

             deal.WaterfallSteps.Add(new WaterfallStepViewModel { Label = "RoRAC", Value = roracVal, FormattedValue = Results.Metrics.RoRAC, IsTotal = true, ColorHex = "#FF005A9E", HeightFactor = Math.Abs(roracVal) * 50 });

             Comparison.ComparedDeals.Add(deal);
             Status = $"Added {deal.Title} to comparison";
        }
        catch (Exception ex)
        {
            Status = $"Error adding to comparison: {ex.Message}";
        }
    }

    // MARK: Goal Seek
    private bool _isGoalSeekOpen;
    public bool IsGoalSeekOpen { get => _isGoalSeekOpen; set => SetProperty(ref _isGoalSeekOpen, value); }
    
    private int _goalVariableIndex = 0; // 0=Rate, 1=DownPayment
    public int GoalVariableIndex { get => _goalVariableIndex; set => SetProperty(ref _goalVariableIndex, value); }

    private int _goalMetricIndex = 0; // 0=Installment, 1=RoRAC
    public int GoalMetricIndex { get => _goalMetricIndex; set => SetProperty(ref _goalMetricIndex, value); }

    private double _goalTargetValue = 0;
    public double GoalTargetValue
    {
        get => _goalTargetValue;
        set
        {
            if (SetProperty(ref _goalTargetValue, value))
            {
                 OnPropertyChanged(nameof(IsGoalSeekTargetSet));
            }
        }
    }

    public bool IsGoalSeekTargetSet => GoalTargetValue > 0;

    [RelayCommand]
    private async Task RunGoalSeekAsync()
    {
        // Legacy wrapper using UI selected indices
        var variable = GoalVariableIndex == 0 ? GoalSeekEngine.GoalVariable.CustomerNominalRate : GoalSeekEngine.GoalVariable.DownPaymentAmount;
        // NOTE: If we add more radio buttons, this mapping needs update.
        
        var metric = GoalMetricIndex == 0 ? GoalSeekEngine.TargetMetric.MonthlyInstallment : GoalSeekEngine.TargetMetric.RoRAC;
        await RunGoalSeekWithParamsAsync(variable, metric, GoalTargetValue);
    }

    [RelayCommand]
    private void GoalSeekSolveForRate()
    {
        IsGoalSeekOpen = true;
        GoalVariableIndex = 0; // Customer Rate
    }

    [RelayCommand]
    private async Task GoalSeekSolveForRateAutoAsync()
    {
        // Auto-run: solve for Rate to achieve target RoRAC (as requested by user)
        // Ensure target RoRAC is taken from GoalTargetValue
        IsGoalSeekOpen = true;
        GoalVariableIndex = 0; // Rate
        GoalMetricIndex = 1;   // RoRAC
        await RunGoalSeekAsync();
    }

    [RelayCommand]
    private void GoalSeekSolveForDownPayment()
    {
        IsGoalSeekOpen = true;
        GoalVariableIndex = 1; // Down Payment
    }

    [RelayCommand]
    private async Task GoalSeekSolveForSubsidyAutoAsync()
    {
        // Auto-run: solve for Subsidy to achieve target RoRAC
        IsGoalSeekOpen = true;
        GoalVariableIndex = 3; // UpfrontSubsidy (I need to ensure enum maps to 3, it was 4th item so index 3 is correct if 0-indexed)
        // Wait, GoalVariableIndex in VM is just an int for RadioButtons. I need to update RunGoalSeekAsync to handle new variable type if I use the int.
        // Actually, I should probably update GoalVariableIndex to support 4 states if I want radio buttons to reflect it,
        // OR just bypass GoalVariableIndex in AutoAsync commands.
        // Let's bypass or update RunGoalSeekAsync to handle explicit override?
        // RunGoalSeekAsync uses the class properties.
        
        // Let's update RunGoalSeekAsync to map generic int to enum better, or use a dedicated property for the command.
        // For now, I'll hack it by setting a special index or just passing parameters to RunGoalSeekAsync if I refactor it.
        // Refactoring RunGoalSeekAsync to take optional params is cleaner.
        await RunGoalSeekWithParamsAsync(GoalSeekEngine.GoalVariable.UpfrontSubsidy, GoalSeekEngine.TargetMetric.RoRAC, GoalTargetValue);
    }

    private async Task RunGoalSeekWithParamsAsync(GoalSeekEngine.GoalVariable variable, GoalSeekEngine.TargetMetric metric, double targetValue)
    {
        try
        {
            IsCalculating = true;
            Status = $"Goal Seeking {variable}...";
            await Task.Delay(10);

            var gs = new GoalSeekEngine(_dealEngine);
            var baseInput = DealInput.BuildDealInput();
            
            double target = targetValue;
            if (metric == GoalSeekEngine.TargetMetric.RoRAC) target /= 100.0;

            double result = gs.Seek(baseInput, variable, metric, target);

            if (variable == GoalSeekEngine.GoalVariable.CustomerNominalRate)
            {
                DealInput.CustomerNominalRate = Math.Round(result, 2);
            }
            else if (variable == GoalSeekEngine.GoalVariable.DownPaymentAmount)
            {
                DealInput.DownPaymentUnit = "THB";
                DealInput.DownPaymentValueEntry = Math.Round(result, 0);
            }
            else if (variable == GoalSeekEngine.GoalVariable.UpfrontSubsidy)
            {
                DealInput.SubsidyBudget = Math.Round(result, 0);
            }

            Status = $"Goal Seek Complete. Result: {result:N2}";
            await RecalculateAsync();
        }
        catch (Exception ex)
        {
             Status = $"Goal Seek Error: {ex.Message}";
        }
        finally
        {
            IsCalculating = false;
        }
    }

    private void PopulateCashflows(IReadOnlyList<CashflowRowDto> schedule)
    {
        Results.Cashflows.Clear();
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
    private DealDto BuildDealFromInputs()
    {
        // Map unified entry + unit to engine-facing fields (legacy DTO for minor display helpers)
        double dpAmt = 0, dpPct = 0; string dpLock = "amount";
        if (string.Equals(DealInput.DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase))
        {
            dpPct = DealInput.DownPaymentValueEntry / 100.0;
            dpLock = "percent";
        }
        else
        {
            dpAmt = DealInput.DownPaymentValueEntry;
            dpLock = "amount";
        }

        double blAmt = 0, blPct = 0;
        if (string.Equals(DealInput.BalloonUnit, "%", StringComparison.OrdinalIgnoreCase))
        {
            blPct = DealInput.BalloonValueEntry / 100.0;
        }
        else
        {
            blAmt = DealInput.BalloonValueEntry;
        }

        return new DealDto
        {
            Product = DealInput.Product,
            PriceExTax = DealInput.PriceExTax,
            DownPaymentAmount = dpAmt,
            DownPaymentPercent = dpPct,
            DownPaymentLocked = dpLock,
            TermMonths = DealInput.TermMonths,
            BalloonPercent = blPct,
            BalloonAmount = blAmt,
            Timing = DealInput.Timing,
            RateMode = DealInput.RateMode,
            CustomerNominalRate = DealInput.CustomerNominalRate / 100.0,
            TargetInstallment = DealInput.TargetInstallment
        };
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
        var input1 = new DealEngine.DealInput
        {
            Market = "TH",
            Product = DealInput.Product,
            Timing = DealInput.Timing,
            TermMonths = DealInput.TermMonths,
            VehiclePrice = vehiclePrice,
            AdditionalFinancedItems = (decimal)DealInput.AdditionalFinancedItems,
            DownIsPercent = string.Equals(DealInput.DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase),
            DownValue = (decimal)DealInput.DownPaymentValueEntry,
            BalloonIsPercent = string.Equals(DealInput.BalloonUnit, "%", StringComparison.OrdinalIgnoreCase),
            BalloonValue = (decimal)DealInput.BalloonValueEntry,
            CustomerRatePercent = (decimal)(customerRateOverride ?? DealInput.CustomerNominalRate),
            UpfrontSubsidies = upfrontSubsidiesDelta,
            UpfrontCosts = (decimal)Math.Max(0, DealInput.IdcOther) + upfrontCostsDelta,
            SubdownIsPercent = subdownIsPercent,
            SubdownValue = subdownValue,
        };
        var out1 = _dealEngine.Calculate(input1);

        // Resolve commission based on financed amount
        var (pct, amt) = ResolveCommissionForFinanced(out1.Deal.FinancedAmount);

        // Second pass: include commission in upfront costs
        var input2 = input1 with { UpfrontCosts = input1.UpfrontCosts + (decimal)amt };
        var out2 = _dealEngine.Calculate(input2);

        return (out2.Deal, out2.Profit, pct, amt);
    }

    private (double pct, double amt) ResolveCommissionForFinanced(decimal financed)
    {
        double pct = DealInput.DealerCommissionMode == "override" ? (DealInput.DealerCommissionPct ?? DealInput.AutoCommissionPct) : DealInput.AutoCommissionPct;
        if (pct < 0) pct = 0;
        double amt = DealInput.DealerCommissionMode == "override" && DealInput.DealerCommissionAmt.HasValue
            ? DealInput.DealerCommissionAmt.Value
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

    private double CalculateLowestAchievableRate(decimal vehiclePrice, bool subdownIsPercent, decimal subdownValue, decimal upfrontCostsDelta, double baseRatePct, double availableBudget)
    {
        // Binary search for target rate that results in required subsidy <= availableBudget
        double low = 0;
        double high = baseRatePct;
        double bestRate = baseRatePct;

        for (int i = 0; i < 20; i++)
        {
            double mid = (low + high) / 2;
            double required = ComputeRequiredSubsidyForRateBuydown(vehiclePrice, subdownIsPercent, subdownValue, upfrontCostsDelta, baseRatePct, mid);
            
            if (required > availableBudget)
            {
                low = mid; // Need higher rate
            }
            else
            {
                bestRate = mid;
                high = mid; // Achievable, try lower
            }
        }
        return Math.Round(bestRate, 2);
    }

    // MARK: Bottom Summary Bindings for Details/Key Metrics
    private double _activeFsInsurance;
    private double _activeFsMbsp;
    private double _activeCashDiscount;

    public string ActiveFsInsuranceText => _activeFsInsurance.ToString("N0", CultureInfo.InvariantCulture);
    public string ActiveFsMbspText => _activeFsMbsp.ToString("N0", CultureInfo.InvariantCulture);
    private double _activeSubsidyUsed;
    public string ActiveSubsidyUtilizedText => _activeSubsidyUsed.ToString("N0", CultureInfo.InvariantCulture);
    public string SubsidyRemainingText => Math.Max(0, DealInput.SubsidyBudget - _activeSubsidyUsed).ToString("N0", CultureInfo.InvariantCulture);
    public string IdcOtherText => DealInput.IdcOther.ToString("N0", CultureInfo.InvariantCulture);
    public string IdcTotalText => (DealInput.DealerCommissionResolvedAmt + DealInput.IdcOther).ToString("N0", CultureInfo.InvariantCulture);

    // MARK: Profitability Waterfall (for RoRAC details panel)
    private double _wfCustomerRate;
    private double _wfIDCUpfrontAnnualized;
    private double _wfSubsidyUpfrontAnnualized;
    private double _wfDealIRREffective; // Keeping internal name for now if it maps to Engine's DealIrrEffective
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
    public string WfDealIRRText => Pct(_wfDealIRREffective);
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
    public string FlatRate { get; set; } = "";
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
    public string CustomerNominalRate { get; set; } = string.Empty;
    public string CustomerFlatRate { get; set; } = string.Empty;
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

    // MBSP Package Selection
    private string _selectedMbspPackage = "Easy Care 5";
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

    public CampaignSummaryViewModel Clone() => new CampaignSummaryViewModel
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
        Notes = this.Notes,
        CashDiscountAmount = this.CashDiscountAmount,
        FSSubDownAmount = this.FSSubDownAmount,
        FSSubInterestAmount = this.FSSubInterestAmount,
        SubinterestSubsidyAmount = this.SubinterestSubsidyAmount,
        IDC_MBSP_CostAmount = this.IDC_MBSP_CostAmount,
        FSFreeMBSPAmount = this.FSFreeMBSPAmount,
        TargetRatePct = this.TargetRatePct,
        SelectedMbspPackage = this.SelectedMbspPackage
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
