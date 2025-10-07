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
    private readonly ApiClient _api = new ApiClient();
    private readonly DebounceDispatcher _debounce = new();
    
    // MARK: Parameter Set Caching
    private Dictionary<string, object>? _cachedParameterSet;

    // MARK: Deal Inputs
    [ObservableProperty] private string product = "HP";
    [ObservableProperty] private double priceExTax = 1_000_000;
    [ObservableProperty] private double downPaymentAmount = 200_000;
    // Unified entry + unit for Down Payment and Balloon
    [ObservableProperty] private string downPaymentUnit = "THB"; // THB | %
    [ObservableProperty] private double downPaymentValueEntry = 200_000;
    [ObservableProperty] private string balloonUnit = "%"; // THB | %
    [ObservableProperty] private double balloonValueEntry = 0;
    [ObservableProperty] private int termMonths = 36;
    [ObservableProperty] private string timing = "arrears"; // arrears|advance
    [ObservableProperty] private double balloonPercent = 0;
    [ObservableProperty] private string lockMode = "amount"; // amount|percent

    // MARK: Rate Mode
    [ObservableProperty] private string rateMode = "fixed_rate"; // fixed_rate|target_installment
    [ObservableProperty] private int rateModeIndex = 0; // 0=fixed_rate, 1=target_installment
    public bool IsFixedRateMode => string.Equals(RateMode, "fixed_rate", StringComparison.OrdinalIgnoreCase);
    public bool IsTargetInstallmentMode => string.Equals(RateMode, "target_installment", StringComparison.OrdinalIgnoreCase);
    [ObservableProperty] private double customerRatePct = 3.99;
    [ObservableProperty] private double targetInstallment = 0;

    // MARK: Subsidy & IDC
    [ObservableProperty] private double subsidyBudget = 100_000;
    [ObservableProperty] private bool subsidyBudgetIsEnabled = false; // enabled only if MyCampaign selected and total allocation exceeds initial budget
    [ObservableProperty] private string dealerCommissionMode = "auto"; // auto|override
    [ObservableProperty] private double? dealerCommissionPct;
    [ObservableProperty] private double? dealerCommissionAmt;
    [ObservableProperty] private double dealerCommissionResolvedAmt;

    // Unified commission entry (auto | % | THB)
    [ObservableProperty] private string commissionEntryUnit = "auto"; // auto | % | THB
    [ObservableProperty] private double commissionEntryValue = 0;

    // Auto policy (fetched)
    [ObservableProperty] private double autoCommissionPct; // fraction (e.g., 0.03)
    [ObservableProperty] private string commissionPolicyVersion = string.Empty;

    [ObservableProperty] private double idcOther = 0;
    [ObservableProperty] private bool idcOtherUserEdited = false;

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


    // MARK: Collections & Selection
    public ObservableCollection<CampaignSummaryViewModel> StandardCampaigns { get; } = new();
    public ObservableCollection<CampaignSummaryViewModel> CampaignSummaries { get; } = new(); // back-compat alias
    public ObservableCollection<CampaignSummaryViewModel> MyCampaigns { get; } = new();

    // Selections
    [ObservableProperty] private CampaignSummaryViewModel? selectedCampaign; // Standard selection
    [ObservableProperty] private CampaignSummaryViewModel? selectedMyCampaign;

    // Cashflows grid for active selection
    public ObservableCollection<CashflowRowViewModel> Cashflows { get; } = new();
    
    // Cashflow summary properties
    [ObservableProperty] private string cashflowCampaignName = "";
    [ObservableProperty] private string totalPrincipalPaid = "0";
    [ObservableProperty] private string totalInterestPaid = "0";
    [ObservableProperty] private string totalFeesPaid = "0";
    [ObservableProperty] private string netAmountFinanced = "0";

    // Active selection prefers MyCampaigns, else Standard
    public CampaignSummaryViewModel? ActiveCampaign => SelectedMyCampaign ?? SelectedCampaign;

    // MARK: Metrics & Status
    [ObservableProperty] private MetricsViewModel metrics = new();
    [ObservableProperty] private string status = "Ready";

    public IRelayCommand RecalculateCommand { get; }

    public MainViewModel()
    {
        RecalculateCommand = new AsyncRelayCommand(RecalculateAsync);
        idcOther = SubsidyBudget; // initial mapping per spec
        
        // Initialize data on UI thread with proper error handling
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            Status = "Initializing...";
            
            // Give backend time to fully start
            await Task.Delay(500);
            
            // Load parameter set first
            await InitializeParameterSetAsync();
            
            // Get commission policy
            await RefreshCommissionPolicyAsync();
            
            // Load campaign summaries
            await LoadSummariesAsync();
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
        try
        {
            _cachedParameterSet = await _api.GetCurrentParametersAsync();
            if (_cachedParameterSet != null && _cachedParameterSet.Count > 0)
            {
                Status = $"Loaded {_cachedParameterSet.Count} parameters";
            }
        }
        catch (Exception ex)
        {
            // Log error but don't block - calculations can proceed without parameter set
            _cachedParameterSet = null;
            Status = $"Parameter set load failed (non-blocking): {ex.Message}";
            // Continue without parameter set - don't throw
        }
    }

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

    // Copy a standard campaign to My Campaigns
    [RelayCommand(CanExecute = nameof(CanCopy))]
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
        ScheduleSummariesRefresh();
    }

    public bool CanCopy => SelectedCampaign != null;

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

    // MARK: Data Loading
    private async Task LoadSummariesAsync()
    {
        // Ensure we have the commission auto policy first
        if (string.Equals(DealerCommissionMode, "auto", StringComparison.OrdinalIgnoreCase))
        {
            await RefreshCommissionPolicyAsync();
        }

        try
        {
            var deal = BuildDealFromInputs();
            
            // Validate deal for RoRAC calculation requirements
            if (deal.PriceExTax <= 0)
            {
                Status = "Error: Invalid price for RoRAC calculation";
                return;
            }
            
            var state = new DealStateDto
            {
                DealerCommission = new DealerCommissionDto { Mode = DealerCommissionMode, Pct = DealerCommissionPct, Amt = DealerCommissionAmt, ResolvedAmt = DealerCommissionResolvedAmt },
                IdcOther = new IDCOtherDto { Value = IdcOtherUserEdited ? IdcOther : SubsidyBudget, UserEdited = IdcOtherUserEdited },
                BudgetTHB = SubsidyBudget
            };
            var catalog = await _api.GetCampaignCatalogAsync();
            var req = new CampaignSummariesRequestDto
            {
                Deal = deal,
                State = state,
                Campaigns = catalog.Select(c => new CampaignDto { Id = c.Id, Type = c.Type, Funder = c.Funder, Description = c.Description, Parameters = c.Parameters }).ToList()
            };
            var rows = await _api.GetCampaignSummariesAsync(req);

            var temp = new List<(CampaignSummaryViewModel vm, double monthly, double eff)>();
            StandardCampaigns.Clear();
            CampaignSummaries.Clear();
            
            // Create "No Campaign" baseline option
            // Calculate baseline monthly installment without any campaign modifications
            var baselineReq = new CalculationRequestDto
            {
                Deal = deal,
                Campaigns = new(), // Empty campaigns list for baseline
                IdcItems = new List<IdcItemDto>(), // No IDC items for baseline
                Options = new() { ["derive_idc_from_cf"] = true },
                ParameterSet = _cachedParameterSet
            };
            
            try
            {
                var baselineRes = await _api.CalculateAsync(baselineReq);
                
                // Create the baseline CampaignSummaryViewModel
                var baselineVm = new CampaignSummaryViewModel
                {
                    CampaignId = "baseline",
                    CampaignType = "No Campaign (Baseline)",
                    Title = "No Campaign (Baseline)",
                    DealerCommission = $"{0.00.ToString("0.00%", CultureInfo.InvariantCulture)} ({0.ToString("N0", CultureInfo.InvariantCulture)} THB)",
                    Monthly = baselineRes.Quote.MonthlyInstallment.ToString("N0", CultureInfo.InvariantCulture),
                    Effective = baselineRes.Quote.CustomerRateEffective.ToString("0.00%"),
                    Downpayment = DownPaymentAmount.ToString("N0", CultureInfo.InvariantCulture),
                    SubsidyUsed = "0",
                    FSSubDown = "0",
                    FSSubInterest = "0",
                    FSFreeMBSP = "0",
                    CashDiscount = "0",
                    RoRAC = "0.00%", // No RoRAC for baseline
                    Notes = "Baseline scenario without any promotional campaigns",
                    FSSubDownAmount = 0,
                    FSSubInterestAmount = 0,
                    FSFreeMBSPAmount = 0,
                };
                
                // Add baseline as the first item
                StandardCampaigns.Add(baselineVm);
                CampaignSummaries.Add(baselineVm);
            }
            catch (Exception ex)
            {
                // Log error but continue with other campaigns
                System.Diagnostics.Debug.WriteLine($"Error creating baseline campaign: {ex.Message}");
            }
            
            foreach (var r in rows)
            {
                try
                {
                    // Validate RoRAC campaign data before adding
                    var roracValue = r.AcquisitionRoRAC;
                    
                    // Set default value if RoRAC is invalid or missing
                    if (double.IsNaN(roracValue) || double.IsInfinity(roracValue))
                    {
                        roracValue = 0.0;
                    }
                    
                    var vm = new CampaignSummaryViewModel
                    {
                        CampaignId = r.CampaignId,
                        CampaignType = r.CampaignType,
                        Title = r.CampaignType,
                        DealerCommission = $"{r.DealerCommissionPct.ToString("0.00%", CultureInfo.InvariantCulture)} ({r.DealerCommissionAmt.ToString("N0", CultureInfo.InvariantCulture)} THB)",
                        Monthly = r.MonthlyInstallment.ToString("N0", CultureInfo.InvariantCulture),
                        Effective = r.CustomerRateEffective.ToString("0.00%"),
                        Downpayment = DownPaymentAmount.ToString("N0", CultureInfo.InvariantCulture),
                        SubsidyUsed = r.SubsidyUsedTHB.ToString("N0", CultureInfo.InvariantCulture),
                        FSSubDown = r.FSSubDownTHB.ToString("N0", CultureInfo.InvariantCulture),
                        FSSubInterest = r.FreeInsuranceTHB.ToString("N0", CultureInfo.InvariantCulture),
                        FSFreeMBSP = r.FreeMBSPTHB.ToString("N0", CultureInfo.InvariantCulture),
                        CashDiscount = r.CashDiscountTHB.ToString("N0", CultureInfo.InvariantCulture),
                        RoRAC = roracValue.ToString("0.00%"),
                        Notes = string.IsNullOrWhiteSpace(r.ViabilityReason) ? (r.Notes ?? string.Empty) : r.ViabilityReason,
                        FSSubDownAmount = r.FSSubDownTHB,
                        FSSubInterestAmount = r.FreeInsuranceTHB,
                        FSFreeMBSPAmount = r.FreeMBSPTHB,
                    };
                    temp.Add((vm, r.MonthlyInstallment, r.CustomerRateEffective));
                }
                catch (Exception ex)
                {
                    // Log error but continue with other campaigns
                    System.Diagnostics.Debug.WriteLine($"Error processing campaign {r.CampaignId}: {ex.Message}");
                    continue;
                }
            }
 
            foreach (var (vm, _, _) in temp.OrderBy(t => t.monthly).ThenBy(t => t.eff))
            {
                StandardCampaigns.Add(vm);
                CampaignSummaries.Add(vm);
            }
            Status = $"Loaded {CampaignSummaries.Count} options";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    // MARK: Actions
    private async Task RecalculateAsync()
    {
        try
        {
            Status = "Calculating...";
            var deal = BuildDealFromInputs();
            
            // Build IDC items list
            var idcItems = new List<IdcItemDto>();
            
            // Add Dealer Commission IDC item
            // Using "broker_commission" category to match backend constants (types.IDCBrokerCommission)
            if (DealerCommissionResolvedAmt > 0)
            {
                idcItems.Add(new IdcItemDto
                {
                    Category = "broker_commission", // Matches backend constant: types.IDCBrokerCommission
                    Amount = DealerCommissionResolvedAmt,
                    Description = "Dealer Commission",
                    Financed = true,        // All IDC items are financed
                    Timing = "upfront",     // Commission is always upfront
                    IsCost = true,          // Commission is a cost, not revenue
                    IsRevenue = false
                });
            }
            
            // Add IDC Other item
            // Using "internal_processing" category to match backend constants (types.IDCInternalProcessing)
            if (IdcOther > 0)
            {
                idcItems.Add(new IdcItemDto
                {
                    Category = "internal_processing", // Matches backend constant: types.IDCInternalProcessing
                    Amount = IdcOther,
                    Description = "Other IDC",
                    Financed = true,        // All IDC items are financed
                    Timing = "upfront",     // Processing costs are upfront
                    IsCost = true,          // Processing is a cost, not revenue
                    IsRevenue = false
                });
            }
            
            var req = new CalculationRequestDto
            {
                Deal = deal,
                Campaigns = new(),
                IdcItems = idcItems,
                Options = new() { ["derive_idc_from_cf"] = true },
                ParameterSet = _cachedParameterSet // Pin to cached parameter set
            };
            var res = await _api.CalculateAsync(req);
            PopulateMetrics(res);
            // Do not override Cashflows here; selection-specific refresh handles that
            Status = "Done";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    private async Task RefreshActiveSelectionAsync()
    {
        try
        {
            var active = ActiveCampaign;
            var deal = BuildDealFromInputs();
            
            // Validate deal for RoRAC calculation requirements
            if (deal.PriceExTax <= 0)
            {
                Status = "Error: Invalid price for RoRAC calculation";
                return;
            }
            
            // Validate financed amount for RoRAC calculations
            var financedAmount = deal.PriceExTax - deal.DownPaymentAmount;
            if (financedAmount <= 0)
            {
                Status = "Error: Invalid financed amount for RoRAC calculation";
                return;
            }
            
            // Build IDC items list
            var idcItems = new List<IdcItemDto>();
            
            // Add Dealer Commission IDC item
            // Using "broker_commission" category to match backend constants (types.IDCBrokerCommission)
            if (DealerCommissionResolvedAmt > 0)
            {
                idcItems.Add(new IdcItemDto
                {
                    Category = "broker_commission", // Matches backend constant: types.IDCBrokerCommission
                    Amount = DealerCommissionResolvedAmt,
                    Description = "Dealer Commission",
                    Financed = true,        // All IDC items are financed
                    Timing = "upfront",     // Commission is always upfront
                    IsCost = true,          // Commission is a cost, not revenue
                    IsRevenue = false
                });
            }
            
            // Add IDC Other item
            // Using "internal_processing" category to match backend constants (types.IDCInternalProcessing)
            if (IdcOther > 0)
            {
                idcItems.Add(new IdcItemDto
                {
                    Category = "internal_processing", // Matches backend constant: types.IDCInternalProcessing
                    Amount = IdcOther,
                    Description = "Other IDC",
                    Financed = true,        // All IDC items are financed
                    Timing = "upfront",     // Processing costs are upfront
                    IsCost = true,          // Processing is a cost, not revenue
                    IsRevenue = false
                });
            }
            
            // Build campaigns list with parameters for My Campaigns
            var campaigns = new List<CampaignDto>();
            if (active != null)
            {
                var campaignDto = new CampaignDto
                {
                    Id = active.CampaignId,
                    Type = active.CampaignType
                };
                
                // Add parameters if this is a user-edited campaign from My Campaigns
                if (IsMyCampaign(active))
                {
                    campaignDto.Parameters = BuildCampaignParameters(active);
                }
                
                campaigns.Add(campaignDto);
            }
            
            var req = new CalculationRequestDto
            {
                Deal = deal,
                Campaigns = campaigns,
                IdcItems = idcItems,
                Options = new() { ["derive_idc_from_cf"] = true },
                ParameterSet = _cachedParameterSet // Pin to cached parameter set
            };
            var res = await _api.CalculateAsync(req);
            PopulateMetrics(res);
            PopulateCashflows(res.Schedule);
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    // MARK: Helper - Check if campaign is from My Campaigns
    private bool IsMyCampaign(CampaignSummaryViewModel campaign)
    {
        // Check if the campaign exists in MyCampaigns collection
        return MyCampaigns.Contains(campaign);
    }

    // MARK: Helper - Build campaign parameters based on type
    // PURPOSE: Maps UI campaign values to backend API parameter keys for My Campaigns
    // NOTE: Only applies to user-edited campaigns (My Campaigns), not standard catalog campaigns
    private Dictionary<string, object> BuildCampaignParameters(CampaignSummaryViewModel campaign)
    {
        var parameters = new Dictionary<string, object>();
        
        var campaignType = campaign.CampaignType?.ToLowerInvariant() ?? "";
        
        // IMPORTANT: These parameter mappings are critical for campaign calculations
        // See docs/IDC_SUBSIDY_IMPLEMENTATION.md for complete mapping reference
        switch (campaignType)
        {
            case "subdown":
                if (campaign.FSSubDownAmount > 0)
                {
                    // TODO: Verify parameter key with backend team
                    // Expected keys: "subsidy_amount", "subdown_amount", "down_payment_subsidy"
                    parameters["subsidy_amount"] = campaign.FSSubDownAmount; // ⚠️ CRITICAL: Backend parameter name
                }
                break;
                
            case "free_insurance":
                if (campaign.FSSubInterestAmount > 0)
                {
                    // TODO: Verify parameter key with backend team
                    // Expected keys: "insurance_cost", "free_insurance_amount", "insurance_subsidy"
                    parameters["insurance_cost"] = campaign.FSSubInterestAmount; // ⚠️ CRITICAL: Backend parameter name
                }
                break;
                
            case "free_mbsp":
                // Use FSFreeMBSPAmount if available, otherwise fall back to IDC_MBSP_CostAmount
                // ASSUMPTION: FSFreeMBSPAmount takes precedence over IDC_MBSP_CostAmount
                var mbspCost = campaign.FSFreeMBSPAmount > 0 ? campaign.FSFreeMBSPAmount : campaign.IDC_MBSP_CostAmount;
                if (mbspCost > 0)
                {
                    // TODO: Verify parameter key with backend team
                    // Expected keys: "mbsp_cost", "free_mbsp_amount", "service_plan_subsidy"
                    parameters["mbsp_cost"] = mbspCost; // ⚠️ CRITICAL: Backend parameter name
                }
                break;
                
            case "cash_discount":
                if (campaign.CashDiscountAmount > 0)
                {
                    // TODO: Verify parameter key with backend team
                    // Expected keys: "discount_amount", "cash_discount", "price_discount"
                    parameters["discount_amount"] = campaign.CashDiscountAmount; // ⚠️ CRITICAL: Backend parameter name
                }
                break;
                
            // NOTE: Additional campaign types may need to be added here
            // Contact backend team for complete list of supported campaign types
        }
        
        return parameters;
    }

    private void PopulateMetrics(CalculationResponseDto res)
    {
        try
        {
            // Validate and handle RoRAC value
            var roracValue = res.Quote?.Profitability?.AcquisitionRoRAC ?? 0;
            if (double.IsNaN(roracValue) || double.IsInfinity(roracValue))
            {
                roracValue = 0.0;
            }
            
            Metrics = new MetricsViewModel
            {
                MonthlyInstallment = res.Quote?.MonthlyInstallment.ToString("N0", CultureInfo.InvariantCulture) ?? "0",
                NominalRate = res.Quote?.CustomerRateNominal.ToString("0.00%") ?? "0.00%",
                EffectiveRate = res.Quote?.CustomerRateEffective.ToString("0.00%") ?? "0.00%",
                FinancedAmount = res.Quote?.FinancedAmount.ToString("N0", CultureInfo.InvariantCulture) ?? "0",
                RoRAC = roracValue.ToString("0.00%"),
            };
        
            // Update dependent computed sections (breakdown lines and IDC totals)
            RefreshProfitabilityDetails(res);
            OnPropertyChanged(nameof(ActiveSubsidyUtilizedText));
            OnPropertyChanged(nameof(SubsidyRemainingText));
            OnPropertyChanged(nameof(IdcTotalText));
        }
        catch (Exception ex)
        {
            // Handle errors in RoRAC calculations gracefully
            System.Diagnostics.Debug.WriteLine($"Error populating metrics: {ex.Message}");
            
            // Set default values on error
            Metrics = new MetricsViewModel
            {
                MonthlyInstallment = "0",
                NominalRate = "0.00%",
                EffectiveRate = "0.00%",
                FinancedAmount = "0",
                RoRAC = "0.00%",
            };
        }
    }
// MARK: Bottom Summary Bindings for Details/Key Metrics
private double _activeFsInsurance;
private double _activeFsMbsp;
private double _activeCashDiscount;

public string ActiveFsInsuranceText => _activeFsInsurance.ToString("N0", CultureInfo.InvariantCulture);
public string ActiveFsMbspText => _activeFsMbsp.ToString("N0", CultureInfo.InvariantCulture);
public string ActiveSubsidyUtilizedText => (_activeFsInsurance + _activeFsMbsp).ToString("N0", CultureInfo.InvariantCulture);
public string SubsidyRemainingText => Math.Max(0, SubsidyBudget - (_activeFsInsurance + _activeFsMbsp)).ToString("N0", CultureInfo.InvariantCulture);
public string IdcOtherText => IdcOther.ToString("N0", CultureInfo.InvariantCulture);
public string IdcTotalText => (DealerCommissionResolvedAmt + IdcOther).ToString("N0", CultureInfo.InvariantCulture);

// MARK: Profitability Waterfall (for RoRAC details panel)
private double _wfDealIRREffective;
private double _wfDealIRRNominal;
private double _wfCostOfDebtMatched;
private double _wfMatchedFundedSpread;
private double _wfGrossInterestMargin;
private double _wfCapitalAdvantage;
private double _wfNetInterestMargin;
private double _wfCostOfCreditRisk;
private double _wfOPEX;
private double _wfIDCUpfront;
private double _wfIDCPeriodic;
private double _wfNetEBITMargin;
private double _wfEconomicCapital;

// MARK: Separated IDC/Subsidy fields
private double _wfIDCUpfrontCostPct;
private double _wfIDCPeriodicCostPct;
private double _wfSubsidyUpfrontPct;
private double _wfSubsidyPeriodicPct;

// Percent formatting helper
private static string Pct(double v) => v.ToString("0.00%", CultureInfo.InvariantCulture);

// Exposed formatted texts
public string WfDealIRREffectiveText => Pct(_wfDealIRREffective);
public string WfDealIRRNominalText => Pct(_wfDealIRRNominal);
public string WfCostOfDebtMatchedText => Pct(_wfCostOfDebtMatched);
public string WfMatchedFundedSpreadText => Pct(_wfMatchedFundedSpread);
public string WfGrossInterestMarginText => Pct(_wfGrossInterestMargin);
public string WfCapitalAdvantageText => Pct(_wfCapitalAdvantage);
public string WfNetInterestMarginText => Pct(_wfNetInterestMargin);
public string WfCostOfCreditRiskText => Pct(_wfCostOfCreditRisk);
public string WfOPEXText => Pct(_wfOPEX);
public string WfIDCUpfrontText => Pct(_wfIDCUpfront);
public string WfIDCPeriodicText => Pct(_wfIDCPeriodic);
public string WfNetEBITMarginText => Pct(_wfNetEBITMargin);
public string WfEconomicCapitalText => Pct(_wfEconomicCapital);

// Exposed formatted texts for separated IDC/Subsidy fields
public string WfIDCUpfrontCostPctText => Pct(_wfIDCUpfrontCostPct);
public string WfIDCPeriodicCostPctText => Pct(_wfIDCPeriodicCostPct);
public string WfSubsidyUpfrontPctText => Pct(_wfSubsidyUpfrontPct);
public string WfSubsidyPeriodicPctText => Pct(_wfSubsidyPeriodicPct);

private void RefreshProfitabilityDetails(CalculationResponseDto res)
{
    var comps = ExtractAuditComponents(res.Quote.CampaignAudit);
    _activeFsInsurance = Math.Max(0, comps.freeInsurance);
    _activeFsMbsp = Math.Max(0, comps.mbsp);
    _activeCashDiscount = comps.cashDiscount;

    // Update waterfall fields if available
    var p = res.Quote?.Profitability;
    if (p != null)
    {
        _wfDealIRREffective    = p.DealIRREffective;
        _wfDealIRRNominal      = p.DealIRRNominal;
        _wfCostOfDebtMatched   = p.CostOfDebtMatched;
        _wfMatchedFundedSpread = p.MatchedFundedSpread;
        _wfGrossInterestMargin = p.GrossInterestMargin;
        _wfCapitalAdvantage    = p.CapitalAdvantage;
        _wfNetInterestMargin   = p.NetInterestMargin;
        _wfCostOfCreditRisk    = p.CostOfCreditRisk;
        _wfOPEX                = p.OPEX;
        _wfIDCUpfront          = p.IDCSubsidiesFeesUpfront;
        _wfIDCPeriodic         = p.IDCSubsidiesFeesPeriodic;
        _wfNetEBITMargin       = p.NetEBITMargin;
        _wfEconomicCapital     = p.EconomicCapital;
        
        // Populate separated IDC/Subsidy fields with null safety
        _wfIDCUpfrontCostPct   = p.IDCUpfrontCostPct ?? 0;
        _wfIDCPeriodicCostPct  = p.IDCPeriodicCostPct ?? 0;
        _wfSubsidyUpfrontPct   = p.SubsidyUpfrontPct ?? 0;
        _wfSubsidyPeriodicPct  = p.SubsidyPeriodicPct ?? 0;
    }
    else
    {
        _wfDealIRREffective = _wfDealIRRNominal = _wfCostOfDebtMatched = _wfMatchedFundedSpread =
        _wfGrossInterestMargin = _wfCapitalAdvantage = _wfNetInterestMargin =
        _wfCostOfCreditRisk = _wfOPEX = _wfIDCUpfront = _wfIDCPeriodic =
        _wfNetEBITMargin = _wfEconomicCapital = 0;
        
        // Reset separated fields
        _wfIDCUpfrontCostPct = _wfIDCPeriodicCostPct = _wfSubsidyUpfrontPct = _wfSubsidyPeriodicPct = 0;
    }

    // Notify UI
    OnPropertyChanged(nameof(ActiveFsInsuranceText));
    OnPropertyChanged(nameof(ActiveFsMbspText));
    OnPropertyChanged(nameof(ActiveSubsidyUtilizedText));
    OnPropertyChanged(nameof(SubsidyRemainingText));
    OnPropertyChanged(nameof(IdcOtherText));
    OnPropertyChanged(nameof(IdcTotalText));

    OnPropertyChanged(nameof(WfDealIRREffectiveText));
    OnPropertyChanged(nameof(WfDealIRRNominalText));
    OnPropertyChanged(nameof(WfCostOfDebtMatchedText));
    OnPropertyChanged(nameof(WfMatchedFundedSpreadText));
    OnPropertyChanged(nameof(WfGrossInterestMarginText));
    OnPropertyChanged(nameof(WfCapitalAdvantageText));
    OnPropertyChanged(nameof(WfNetInterestMarginText));
    OnPropertyChanged(nameof(WfCostOfCreditRiskText));
    OnPropertyChanged(nameof(WfOPEXText));
    OnPropertyChanged(nameof(WfIDCUpfrontText));
    OnPropertyChanged(nameof(WfIDCPeriodicText));
    OnPropertyChanged(nameof(WfNetEBITMarginText));
    OnPropertyChanged(nameof(WfEconomicCapitalText));
    
    // Notify UI for separated IDC/Subsidy fields
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
        // This will be handled by the view (MainWindow) to show a dialog
        // The Cashflows collection is already populated via RefreshActiveSelectionAsync
        if (ActiveCampaign == null)
        {
            Status = "No campaign selected";
            return;
        }
        
        // Trigger refresh to ensure cashflows are current
        await RefreshActiveSelectionAsync();
    }
    catch (Exception ex)
    {
        Status = $"Error loading cashflows: {ex.Message}";
    }
}

// MARK: Export - lightweight Excel-friendly CSV (saved as .xlsx for user flow)
[RelayCommand]
private async Task ExportXlsxAsync()
{
    try
    {
        Status = "Preparing export...";
        var deal = BuildDealFromInputs();
        var active = ActiveCampaign;

        var req = new CalculationRequestDto
        {
            Deal = deal,
            Campaigns = active != null
                ? new List<CampaignDto> { new CampaignDto { Id = active.CampaignId, Type = active.CampaignType } }
                : new List<CampaignDto>(),
            IdcItems = new(),
            Options = new() { ["derive_idc_from_cf"] = true },
        };
        var res = await _api.CalculateAsync(req);
        
        // Refresh profitability details to ensure we have the latest separated values
        RefreshProfitabilityDetails(res);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Deal Summary");
        sb.AppendLine("Key,Value");
        sb.AppendLine($"Selected Campaign,{(active?.Title ?? "-")}");
        sb.AppendLine($"Monthly Installment (THB),{res.Quote.MonthlyInstallment.ToString("N0", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Nominal Rate,{res.Quote.CustomerRateNominal.ToString("0.00%")}");
        sb.AppendLine($"Effective Rate,{res.Quote.CustomerRateEffective.ToString("0.00%")}");
        sb.AppendLine($"Financed Amount (THB),{res.Quote.FinancedAmount.ToString("N0", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Acq. RoRAC,{res.Quote.Profitability.AcquisitionRoRAC.ToString("0.00%")}");
        sb.AppendLine($"Dealer Commission (THB),{DealerCommissionResolvedAmt.ToString("N0", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"IDC - Other (THB),{IdcOther.ToString("N0", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"IDC Total (THB),{(DealerCommissionResolvedAmt + IdcOther).ToString("N0", CultureInfo.InvariantCulture)}");
        sb.AppendLine();
        
        // Add Profitability Details section with separated IDC/Subsidy values
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
        
        // Add separated IDC/Subsidy values with visual separation
        sb.AppendLine("Separated Values:");
        sb.AppendLine($"IDC Upfront Cost %,{_wfIDCUpfrontCostPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"IDC Periodic Cost %,{_wfIDCPeriodicCostPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Subsidy Upfront %,{_wfSubsidyUpfrontPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Subsidy Periodic %,{_wfSubsidyPeriodicPct.ToString("0.00%", CultureInfo.InvariantCulture)}");
        sb.AppendLine();
        
        sb.AppendLine("Cashflow Schedule");
        sb.AppendLine("Period,Principal,Interest,Fees,Balance,Cashflow");
        foreach (var r in res.Schedule)
        {
            sb.AppendLine($"{r.Period},{r.Principal.ToString("0.00", CultureInfo.InvariantCulture)},{r.Interest.ToString("0.00", CultureInfo.InvariantCulture)},{r.Fees.ToString("0.00", CultureInfo.InvariantCulture)},{r.Balance.ToString("0.00", CultureInfo.InvariantCulture)},{r.Cashflow.ToString("0.00", CultureInfo.InvariantCulture)}");
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
        
        // Track cumulative values and totals
        double cumulativePrincipal = 0;
        double cumulativeInterest = 0;
        double totalPrincipal = 0;
        double totalInterest = 0;
        double totalFees = 0;
        
        foreach (var r in schedule)
        {
            cumulativePrincipal += r.Principal;
            cumulativeInterest += r.Interest;
            totalPrincipal += r.Principal;
            totalInterest += r.Interest;
            totalFees += r.Fees;
            
            var totalPayment = r.Principal + r.Interest + r.Fees;
            
            // Calculate IDC breakdown for first period (upfront costs)
            string idcBreakdown = "";
            if (r.Period == 1)
            {
                var idcTotal = DealerCommissionResolvedAmt + IdcOther;
                if (idcTotal > 0)
                {
                    idcBreakdown = idcTotal.ToString("N0", CultureInfo.InvariantCulture);
                }
            }
            
            // Calculate subsidy allocation (simplified - could be enhanced based on campaign type)
            string subsidyAllocation = "";
            if (r.Period == 1 && ActiveCampaign != null)
            {
                double subsidyAmount = 0;
                if (IsMyCampaign(ActiveCampaign))
                {
                    subsidyAmount = ActiveCampaign.FSSubDownAmount +
                                  ActiveCampaign.FSSubInterestAmount +
                                  ActiveCampaign.FSFreeMBSPAmount;
                }
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
                Fees = r.Fees.ToString("N0", CultureInfo.InvariantCulture),
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
        TotalFeesPaid = totalFees.ToString("N0", CultureInfo.InvariantCulture);
        
        // Calculate net amount financed
        var netFinanced = Math.Max(0, PriceExTax - DownPaymentAmount);
        NetAmountFinanced = netFinanced.ToString("N0", CultureInfo.InvariantCulture);
        
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

    private async Task RefreshCommissionPolicyAsync()
    {
        try
        {
            var res = await _api.GetCommissionAutoAsync(Product);
            AutoCommissionPct = res.Percent;
            CommissionPolicyVersion = res.PolicyVersion ?? string.Empty;
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
        // Map unified entry + unit to engine-facing fields
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

    private static (double freeInsurance, double mbsp, double cashDiscount) ExtractAuditComponents(IReadOnlyList<CampaignAuditEntryDto> audit)
    {
        if (audit == null || audit.Count == 0) return (0, 0, 0);
        double freeIns = 0, mbsp = 0, cash = 0;
        foreach (var e in audit)
        {
            var desc = (e.Description ?? string.Empty).ToLowerInvariant();
            if (desc.Contains("insurance")) freeIns += e.Impact;
            else if (desc.Contains("mbsp") || desc.Contains("service")) mbsp += e.Impact;
            else if (desc.Contains("cash") && desc.Contains("discount")) cash += e.Impact;
        }
        return (freeIns, mbsp, cash);
    }

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
    public string FSSubInterest { get; set; } = string.Empty;
    public string FSFreeMBSP { get; set; } = string.Empty;
    public string SubsidyUsed { get; set; } = string.Empty;
    public string RoRAC { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // Editable amounts for My Campaigns (impact calculators)
    private double _cashDiscountAmount;
    public double CashDiscountAmount { get => _cashDiscountAmount; set { if (_cashDiscountAmount != value) { _cashDiscountAmount = value; OnPropertyChanged(nameof(CashDiscountAmount)); } } }
    private double _fsSubDownAmount;
    public double FSSubDownAmount { get => _fsSubDownAmount; set { if (_fsSubDownAmount != value) { _fsSubDownAmount = value; OnPropertyChanged(nameof(FSSubDownAmount)); } } }
    private double _fsSubInterestAmount;
    public double FSSubInterestAmount { get => _fsSubInterestAmount; set { if (_fsSubInterestAmount != value) { _fsSubInterestAmount = value; OnPropertyChanged(nameof(FSSubInterestAmount)); } } }
    private double _idcMbspCostAmount;
    public double IDC_MBSP_CostAmount { get => _idcMbspCostAmount; set { if (_idcMbspCostAmount != value) { _idcMbspCostAmount = value; OnPropertyChanged(nameof(IDC_MBSP_CostAmount)); } } }
    private double _fsFreeMbspAmount;
    public double FSFreeMBSPAmount { get => _fsFreeMbspAmount; set { if (_fsFreeMbspAmount != value) { _fsFreeMbspAmount = value; OnPropertyChanged(nameof(FSFreeMBSPAmount)); } } }

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
        FSFreeMBSP = this.FSFreeMBSP,
        SubsidyUsed = this.SubsidyUsed,
        RoRAC = this.RoRAC,
        Notes = this.Notes,
        CashDiscountAmount = this.CashDiscountAmount,
        FSSubDownAmount = this.FSSubDownAmount,
        FSSubInterestAmount = this.FSSubInterestAmount,
        IDC_MBSP_CostAmount = this.IDC_MBSP_CostAmount,
        FSFreeMBSPAmount = this.FSFreeMBSPAmount
    };
}

public partial class CashflowRowViewModel : ObservableObject
{
    public int Period { get; set; }
    public string Principal { get; set; } = "";
    public string Interest { get; set; } = "";
    public string Fees { get; set; } = "";
    public string Balance { get; set; } = "";
    public string Cashflow { get; set; } = "";
    
    // New detailed breakdown properties
    public string PrincipalRunoff { get; set; } = "";  // Cumulative principal paid
    public string InterestRunoff { get; set; } = "";   // Cumulative interest paid
    public string SubsidyAllocation { get; set; } = ""; // Subsidy amount if any
    public string IdcBreakdown { get; set; } = "";      // Commission and other IDCs per period
    public string TotalPayment { get; set; } = "";      // Principal + Interest + Fees
}
