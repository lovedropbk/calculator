using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class MainViewModel
{
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

    // Active selection prefers MyCampaigns, else Standard
    public CampaignSummaryViewModel? ActiveCampaign => SelectedMyCampaign ?? SelectedCampaign;

    public bool IsMyCampaignSelected => SelectedMyCampaign != null;

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

            // Set default selection if none (only if no MyCampaign is selected either)
            if (SelectedCampaign == null && SelectedMyCampaign == null && CampaignSummaries.Count > 0)
                SelectedCampaign = CampaignSummaries.FirstOrDefault(c => c.CampaignId == "baseline");

            Status = $"Loaded {CampaignSummaries.Count} options";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        await Task.CompletedTask;
    }
}