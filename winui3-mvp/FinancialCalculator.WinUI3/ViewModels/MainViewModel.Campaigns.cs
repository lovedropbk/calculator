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
    
    private void OnIsCampaignDetailsCollapsedChanged(bool value)
    {
        CampaignDetailsColumnWidth = value ? "36" : "Auto";
    }

    [RelayCommand]
    private void ToggleCampaignDetailsCollapsed()
    {
        IsCampaignDetailsCollapsed = !IsCampaignDetailsCollapsed;
        // Listen for property changes in CampaignSummaryViewModel to trigger recalcs for My Campaigns
        CampaignManager.MyCampaigns.CollectionChanged += MyCampaigns_CollectionChanged;
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
        if (sender is CampaignSummaryViewModel vm && vm == CampaignManager.SelectedMyCampaign && IsCampaignInputProperty(e.PropertyName))
        {
            // Trigger recalculation for this specific campaign
            await CalculateCampaignAsync(vm, autoClampToBudget: false);
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

    // MARK: Collections & Selection (Proxies to CampaignManager)
    public ObservableCollection<CampaignSummaryViewModel> StandardCampaigns => CampaignManager.StandardCampaigns;
    public ObservableCollection<CampaignSummaryViewModel> MyCampaigns => CampaignManager.MyCampaigns;
    
    private void ScheduleSummariesRefresh()
    {
        // Use the full debounce for refreshing the campaign list
        _debounceFull.DebounceAsync(300, async () => await LoadSummariesLocalAsync());
    }

    // Back-compat alias for internal use if needed, but better to use CampaignManager directly
    // public ObservableCollection<CampaignSummaryViewModel> CampaignSummaries { get; } = new();
    
    public CampaignSummaryViewModel? ActiveCampaign => CampaignManager.ActiveCampaign;
    public bool IsMyCampaignSelected => CampaignManager.SelectedMyCampaign != null;

    // Copy a standard campaign to My Campaigns
    [RelayCommand(CanExecute = nameof(CanCopyToMyCampaigns))]
    private void CopyToMyCampaigns(CampaignSummaryViewModel? item)
    {
        if (item is null) item = CampaignManager.SelectedCampaign;
        if (item is null) return;
        var clone = item.Clone();
        // Tag as custom for clarity
        if (!clone.Title.StartsWith("Custom:", StringComparison.OrdinalIgnoreCase))
            clone.Title = $"Custom: {clone.Title}";
        clone.CampaignId = Guid.NewGuid().ToString();
        CampaignManager.MyCampaigns.Add(clone);
        CampaignManager.SelectedMyCampaign = clone;
        Logger.Info($"MyCampaigns: copied from standard '{clone.Title}' (ID={clone.CampaignId})");
        
        // Clear standard selection since we're now selecting the copied campaign
        CampaignManager.SelectedCampaign = null;
        
        // Trigger refresh to update metrics with the new campaign
        OnPropertyChanged(nameof(ActiveCampaign));
        ScheduleSummariesRefresh();
    }

    private bool CanCopyToMyCampaigns(CampaignSummaryViewModel? item) => item != null || CampaignManager.SelectedCampaign != null;


    // MARK: My Campaigns persistence
    private static string MyCampaignsPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FinancialCalculator", "my_campaigns.json");

    [RelayCommand]
    private void NewBankCampaign()
    {
        var vm = new CampaignSummaryViewModel { Title = "Custom: Bank Campaign", Notes = "", CashDiscountAmount = 0, FSSubDownAmount = 0, FSSubInterestAmount = 0, IDC_MBSP_CostAmount = 0, FSFreeMBSPAmount = 0 };
        CampaignManager.MyCampaigns.Add(vm);
        CampaignManager.SelectedMyCampaign = vm;
    }

    [RelayCommand]
    private async Task SaveAllCampaignsAsync()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(MyCampaignsPath)!;
            System.IO.Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(CampaignManager.MyCampaigns, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(MyCampaignsPath, json);
            Status = $"Saved {CampaignManager.MyCampaigns.Count} campaigns";
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
            CampaignManager.MyCampaigns.Clear();
            foreach (var c in list) CampaignManager.MyCampaigns.Add(c);
            Status = $"Loaded {CampaignManager.MyCampaigns.Count} campaigns";
        }
        catch (Exception ex)
        {
            Status = $"Load error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearCampaigns()
    {
        CampaignManager.MyCampaigns.Clear();
        CampaignManager.SelectedMyCampaign = null;
    }

    private double ComputeDownpaymentDisplay(decimal vehiclePrice)
    {
        if (string.Equals(DealInput.DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase))
        {
            return (double)(vehiclePrice * (decimal)DealInput.DownPaymentValueEntry / 100m);
        }
        return DealInput.DownPaymentValueEntry;
    }

    private async Task<(FinancialCalculator.Engine.Models.CalculatorOutputs? Outputs, FinancialCalculator.Engine.Models.Profitability? Profit)> CalculateCampaignAsync(CampaignSummaryViewModel vm, bool autoClampToBudget = false)
    {
        // Small delay if not auto-clamping (likely user edit)
        if (!autoClampToBudget) await Task.Delay(1);

        try
        {
            if (!autoClampToBudget)
            {
                IsCalculating = true;
                Status = $"Recalculating {vm.Title}...";
            }

            // 1. Gather inputs from vm
            decimal vehiclePrice = (decimal)DealInput.PriceExTax;
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
                decimal upfrontCostsDelta = (decimal)(fsFreeInsurance + fsFreeMbsp);
                double required = ComputeRequiredSubsidyForRateBuydown(transactionPrice, false, (decimal)fsSubDown, upfrontCostsDelta, DealInput.CustomerNominalRate, targetRatePct.Value);
                
                // Auto-clamp for standard campaigns if over budget
                // Standard campaigns must show achievable scenarios within the current budget.
                // If the required subsidy for the target rate exceeds the budget, we clamp the rate to what is achievable with remaining budget.
                // Custom campaigns (autoClampToBudget=false) are allowed to exceed budget to show user the required overrun.
                double leftoverBudget = DealInput.SubsidyBudget - (cashDiscount + fsSubDown + fsFreeInsurance + fsFreeMbsp);
                if (autoClampToBudget && required > leftoverBudget && leftoverBudget >= 0)
                {
                     targetRatePct = CalculateLowestAchievableRate(transactionPrice, false, (decimal)fsSubDown, upfrontCostsDelta, DealInput.CustomerNominalRate, leftoverBudget);
                     required = leftoverBudget;
                     vm.TargetRatePct = targetRatePct;
                }
                subinterestSubsidy = (decimal)required;
            }

            // 3. Calculate Unallocated Subsidy
            // We pass the FULL budget as upfront subsidy to the engine.
            // We must also pass ALL costs (including Cash Discount which we pay to dealer despite lowering customer price) as Upfront Costs.
            
            // 4. Compute full scenario
            var (outc, profit, commPct, commAmt) = ComputeScenarioWithCommission(
                transactionPrice,
                false,
                (decimal)fsSubDown,
                (decimal)(fsFreeInsurance + fsFreeMbsp),
                (decimal)(DealInput.SubsidyBudget - cashDiscount),
                targetRatePct
            );

            // 5. Update VM with results
            vm.Monthly = ((double)outc.MonthlyRate).ToString("N0", CultureInfo.InvariantCulture);
            vm.CustomerFlatRate = ((double)outc.FlatRatePercentPerAnnum / 100.0).ToString("0.00%", CultureInfo.InvariantCulture);
            vm.TransactionPrice = transactionPrice.ToString("N0", CultureInfo.InvariantCulture);
            vm.Downpayment = ComputeDownpaymentDisplay(transactionPrice).ToString("N0", CultureInfo.InvariantCulture);
            vm.CashDiscount = cashDiscount.ToString("N0", CultureInfo.InvariantCulture);
            vm.FSSubDown = fsSubDown.ToString("N0", CultureInfo.InvariantCulture);
            vm.FSSubInterest = fsFreeInsurance.ToString("N0", CultureInfo.InvariantCulture);
            vm.FSFreeMBSP = fsFreeMbsp.ToString("N0", CultureInfo.InvariantCulture);
            vm.SubinterestSubsidy = subinterestSubsidy.ToString("N0", CultureInfo.InvariantCulture);
            
            double subsidyUsed = cashDiscount + fsSubDown + fsFreeInsurance + fsFreeMbsp + (double)subinterestSubsidy;
            vm.SubsidyUsed = subsidyUsed.ToString("N0", CultureInfo.InvariantCulture);
            
            double idcsTotal = commAmt + fsFreeInsurance + fsFreeMbsp + DealInput.IdcOther;
            vm.IDCsTotal = idcsTotal.ToString("N0", CultureInfo.InvariantCulture);
            
            vm.DealerCommission = $"{commPct.ToString("0.00%", CultureInfo.InvariantCulture)} ({commAmt.ToString("N0", CultureInfo.InvariantCulture)} THB)";
            vm.RoRAC = ((double)profit.AcquisitionRoRac).ToString("0.00%");

            // 6. If this is the active campaign, also update the main metrics area
            if (vm == ActiveCampaign)
            {
                UpdateMetricsFromCampaign(vm, outc, profit, commAmt, subsidyUsed, fsFreeInsurance, fsFreeMbsp, (double)subinterestSubsidy);
            }

            Status = "Done";
            return (outc, profit);
        }
        catch (Exception ex)
        {
            Status = $"Error calculating {vm.Title}: {ex.Message}";
            return (null, null);
        }
        finally
        {
            IsCalculating = false;
        }
    }

    private void UpdateMetricsFromCampaign(CampaignSummaryViewModel vm, FinancialCalculator.Engine.Models.CalculatorOutputs outc, FinancialCalculator.Engine.Models.Profitability profit, double commAmt, double subsidyUsed, double fsIns, double fsMbsp, double rateSubsidy = 0)
    {
        Results.Metrics = new MetricsViewModel
        {
            MonthlyInstallment = ((double)outc.MonthlyRate).ToString("N0", CultureInfo.InvariantCulture),
            NominalRate = ((vm.TargetRatePct ?? DealInput.CustomerNominalRate) / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
            FlatRate = ((double)outc.FlatRatePercentPerAnnum / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
            FinancedAmount = ((double)outc.FinancedAmount).ToString("N0", CultureInfo.InvariantCulture),
            RoRAC = ((double)profit.AcquisitionRoRac).ToString("0.00%"),
        };

        _activeFsInsurance = fsIns;
        _activeFsMbsp = fsMbsp;
        _activeSubsidyUsed = subsidyUsed;
        
        // Update DealInput for display if needed, though it might affect main tab if switched back.
        // Keeping it for now as per original logic.
        try
        {
            _suppressRecalculation = true;
            DealInput.DealerCommissionResolvedAmt = commAmt;
        }
        finally
        {
            _suppressRecalculation = false;
        }
        
        OnPropertyChanged(nameof(ActiveFsInsuranceText));
        OnPropertyChanged(nameof(ActiveFsMbspText));
        OnPropertyChanged(nameof(ActiveSubsidyUtilizedText));
        OnPropertyChanged(nameof(SubsidyRemainingText));

        UpdateBudgetUtilization(
            vm.CashDiscountAmount,
            vm.FSSubDownAmount,
             rateSubsidy,
             fsIns + fsMbsp,
             DealInput.SubsidyBudget - subsidyUsed
        );

        RefreshProfitabilityDetailsLocal(profit);

        // Force main UI update if this is the selected campaign
        if (vm == CampaignManager.SelectedMyCampaign)
        {
             // Trigger update of dependent properties that might not have fired
             OnPropertyChanged(nameof(CampaignManager.SelectedMyCampaign));
        }
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
            CampaignManager.StandardCampaigns.Clear();
            // CampaignSummaries.Clear(); // Removed alias

            bool mbspUnavailable = false;

            // Baseline (no campaign) - but should still apply leftover subsidy budget for consistent calculation!
            var leftoverBudgetForBaseline = Math.Max(0, DealInput.SubsidyBudget);  // All budget is available for baseline
            var baseline = ComputeScenarioWithCommission(
                vehiclePrice: (decimal)DealInput.PriceExTax,
                subdownIsPercent: false,
                subdownValue: 0,
                upfrontCostsDelta: 0m,
                upfrontSubsidiesDelta: (decimal)leftoverBudgetForBaseline,  // Apply leftover budget for consistency
                customerRateOverride: null
            );

            var baselineDp = ComputeDownpaymentDisplay((decimal)DealInput.PriceExTax);
            
            // Calculate IDCs Total for baseline (dealer commission + IDC Other)
            var baselineIdcsTotal = baseline.commissionAmt + DealInput.IdcOther;
            
            var baselineVm = new CampaignSummaryViewModel
            {
                CampaignId = "baseline",
                CampaignType = "No Campaign (Baseline)",
                Title = "No Campaign (Baseline)",
                DealerCommission = $"{baseline.commissionPct.ToString("0.00%", CultureInfo.InvariantCulture)} ({baseline.commissionAmt.ToString("N0", CultureInfo.InvariantCulture)} THB)",
                Monthly = ((double)baseline.outputs.MonthlyRate).ToString("N0", CultureInfo.InvariantCulture),
                // Show the customer's Flat Rate
                CustomerFlatRate = ((double)baseline.outputs.FlatRatePercentPerAnnum / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
                CustomerNominalRate = (DealInput.CustomerNominalRate / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
                Downpayment = baselineDp.ToString("N0", CultureInfo.InvariantCulture),
                TransactionPrice = ((decimal)DealInput.PriceExTax).ToString("N0", CultureInfo.InvariantCulture),
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
            CampaignManager.StandardCampaigns.Add(baselineVm);
            // CampaignSummaries.Add(baselineVm);

            // Standard campaigns
            foreach (var c in _campaigns.GetStandard())
            {
                try
                {
                    var vm = new CampaignSummaryViewModel
                    {
                        CampaignId = c.Id,
                        CampaignType = c.Type,
                        Title = c.Type,
                        Notes = string.Empty
                    };

                    // Map raw definition to unified VM inputs
                    decimal vehiclePrice = (decimal)DealInput.PriceExTax;
                    switch (c.Type)
                    {
                        case "subdown":
                            if (c.SubsidyPercent.HasValue) vm.FSSubDownAmount = (double)(vehiclePrice * (decimal)c.SubsidyPercent.Value);
                            else if (c.SubsidyAmount.HasValue) vm.FSSubDownAmount = c.SubsidyAmount.Value;
                            break;
                        case "free_insurance":
                            if (c.InsuranceCost.HasValue) vm.FSSubInterestAmount = c.InsuranceCost.Value;
                            break;
                        case "free_mbsp":
                            if (c.MbspCost.HasValue)
                            {
                                double actualMbspCost = c.MbspCost.Value;
                                bool mbspAvailable = true;
                                if (DealInput.SelectedVehicle != null)
                                {
                                     if (DealInput.SelectedVehicle.MbspCosts.TryGetValue("Easy Care 5", out var vehCost))
                                     {
                                         actualMbspCost = vehCost;
                                     }
                                     else
                                     {
                                         mbspAvailable = false;
                                         mbspUnavailable = true;
                                     }
                                }
                                if (mbspAvailable) vm.FSFreeMBSPAmount = actualMbspCost;
                                else continue;
                            }
                            break;
                        case "cash_discount":
                            if (c.DiscountPercent.HasValue) vm.CashDiscountAmount = (double)(vehiclePrice * (decimal)c.DiscountPercent.Value);
                            else if (c.DiscountAmount.HasValue) vm.CashDiscountAmount = c.DiscountAmount.Value;
                            break;
                        case "subinterest":
                            if (c.TargetRate.HasValue) vm.TargetRatePct = c.TargetRate.Value * 100.0;
                            break;
                    }

                    // Calculate using unified method, with auto-clamping for standard campaigns
                    await CalculateCampaignAsync(vm, autoClampToBudget: true);
                    
                    // Extract sorting keys if possible, otherwise fallback to 0
                    double monthly = 0, flat = 0;
                    double.TryParse(vm.Monthly.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out monthly);
                    double.TryParse(vm.CustomerFlatRate.TrimEnd('%'), NumberStyles.Any, CultureInfo.InvariantCulture, out flat);

                    temp.Add((vm, monthly, flat));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error computing campaign '{c.Id}': {ex.Message}");
                }
            }

            foreach (var (vm, _, _) in temp.OrderBy(t => t.monthly).ThenBy(t => t.eff))
            {
                CampaignManager.StandardCampaigns.Add(vm);
                // CampaignSummaries.Add(vm);
            }

            // Restore selection
            if (selectedId != null)
            {
                var toRestore = CampaignManager.StandardCampaigns.FirstOrDefault(c => c.CampaignId == selectedId);
                if (toRestore != null)
                {
                    CampaignManager.SelectedCampaign = toRestore;
                }
            }

            // Set default selection if none (only if no MyCampaign is selected either)
            if (CampaignManager.SelectedCampaign == null && CampaignManager.SelectedMyCampaign == null && CampaignManager.StandardCampaigns.Count > 0)
                CampaignManager.SelectedCampaign = CampaignManager.StandardCampaigns.FirstOrDefault(c => c.CampaignId == "baseline");

            Status = $"Loaded {CampaignManager.StandardCampaigns.Count} options";

            if (mbspUnavailable)
            {
                NotificationMessage = "MBSP campaign not available due to missing MBSP offer for this model.";
                NotificationSeverity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
                IsNotificationOpen = true;
            }
            else
            {
                IsNotificationOpen = false;
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        await Task.CompletedTask;
    }
}