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
    public bool IsCampaignDetailsCollapsed { get => _isCampaignDetailsCollapsed; set => SetProperty(ref _isCampaignDetailsCollapsed, value); }

    private void OnIsCampaignDetailsCollapsedChanged(bool value)
    {
        // No-op, handled by XAML converter
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

    // Copy a campaign into the Campaign Designer (creates a CampaignTileViewModel with term breakdown)
    [RelayCommand(CanExecute = nameof(CanCopyToMyCampaigns))]
    private async Task CopyToDesignerAsync(CampaignSummaryViewModel? item)
    {
        if (item is null) item = CampaignManager.SelectedCampaign;
        if (item is null) return;

        try
        {
            var baseRequest = DealInput.BuildScenarioRequest();
            var termSvc = new CampaignTermBreakdownService(_financialFacade, _standardRates);
            var breakdown = await termSvc.CalculateTermBreakdownAsync(item, baseRequest, DealInput);

            var tile = new CampaignTileViewModel
            {
                CampaignId = string.IsNullOrWhiteSpace(item.CampaignId) ? Guid.NewGuid().ToString() : item.CampaignId,
                Title = item.Title,
                Product = string.IsNullOrWhiteSpace(item.CampaignType) ? DealInput.Product : item.CampaignType,
                CampaignVolumePct = 0.0,
                ModelName = DealInput.SelectedVehicle?.ModelName ?? string.Empty
            };

            // Wire live per-term RoRAC recalculation (rate/term aware with commission and CoF)
            tile.TermRoRacCalculator = async (t, r) =>
            {
                var req = DealInput.BuildScenarioRequest(); // always reflect latest inputs
                // Include campaign adjustments (cash discount, subdown, free insurance/MBSP) when recomputing per-term RoRAC
                return await termSvc.CalculateTermRoRACAsync(req, DealInput, item, t, r);
            };

            tile.SetTermBreakdown(breakdown);

            Comparison.DesignerCampaigns.Add(tile);
            Status = $"Added {tile.Title} to Campaign Designer";
        }
        catch (Exception ex)
        {
            Status = $"Error copying to designer: {ex.Message}";
        }
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

    private async Task<FinancialCalculator.Engine.Models.Facade.ScenarioResult?> CalculateCampaignAsync(CampaignSummaryViewModel vm, bool autoClampToBudget = false)
    {
        try
        {
            if (!autoClampToBudget) IsCalculating = true;

            var baseRequest = DealInput.BuildScenarioRequest();
            var (res, commPct, commAmt) = await _campaignService.CalculateCampaignAsync(
                vm,
                baseRequest,
                DealInput.SubsidyBudget,
                DealInput,
                autoClampToBudget);

            if (res == null) return null;

            // If this is the active campaign, also update the main metrics area
            if (vm == ActiveCampaign)
            {
                // Need to re-calculate some used values for UI update if not returned by service fully decomposed.
                // Service returns updated VM, so we can read from VM.
                double.TryParse(vm.FSSubInterest, NumberStyles.Any, CultureInfo.InvariantCulture, out var fsIns);
                double.TryParse(vm.FSFreeMBSP, NumberStyles.Any, CultureInfo.InvariantCulture, out var fsMbsp);
                double.TryParse(vm.SubsidyUsed, NumberStyles.Any, CultureInfo.InvariantCulture, out var subsidyUsed);
                double.TryParse(vm.SubinterestSubsidy, NumberStyles.Any, CultureInfo.InvariantCulture, out var subinterestSubsidy);

                UpdateMetricsFromCampaign(vm, res, commAmt, subsidyUsed, fsIns, fsMbsp, subinterestSubsidy);
            }

            Status = "Done";
            return res;
        }
        catch (Exception ex)
        {
            Status = $"Error calculating {vm.Title}: {ex.Message}";
            return null;
        }
        finally
        {
            IsCalculating = false;
        }
    }

    private void UpdateMetricsFromCampaign(CampaignSummaryViewModel vm, FinancialCalculator.Engine.Models.Facade.ScenarioResult res, double commAmt, double subsidyUsed, double fsIns, double fsMbsp, double rateSubsidy = 0)
    {
        Results.Metrics = new MetricsViewModel
        {
            MonthlyInstallment = ((double)res.MonthlyInstallment).ToString("N0", CultureInfo.InvariantCulture),
            NominalRate = ((vm.TargetRatePct ?? DealInput.CustomerNominalRate) / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
            FlatRate = ((double)res.FlatRatePercent / 100.0).ToString("0.00%", CultureInfo.InvariantCulture),
            FinancedAmount = ((double)res.FinancedAmount).ToString("N0", CultureInfo.InvariantCulture),
            RoRAC = ((double)res.AcquisitionRoRacPercent).ToString("0.00%"),
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

        RefreshProfitabilityDetailsLocal(res.Profitability);

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

            // Baseline (no campaign) via Service
            var baselineVm = new CampaignSummaryViewModel
            {
                CampaignId = "baseline",
                CampaignType = "No Campaign (Baseline)",
                Title = "No Campaign (Baseline)",
                Notes = "Baseline scenario without campaigns"
            };
            
            await _campaignService.CalculateCampaignAsync(baselineVm, DealInput.BuildScenarioRequest(), DealInput.SubsidyBudget, DealInput, true);
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
                    Logger.Warn($"Error computing campaign '{c.Id}': {ex.Message}");
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

        // Designer campaigns persistence (save/load)
        private static string DesignerCampaignsPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FinancialCalculator", "designer_campaigns.json");
    
        [RelayCommand]
        private async Task SaveDesignerCampaignsAsync()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(DesignerCampaignsPath)!;
                System.IO.Directory.CreateDirectory(dir);
                var list = Comparison.DesignerCampaigns.ToList();
                var json = System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(DesignerCampaignsPath, json);
                Status = $"Saved {list.Count} designer campaigns";
            }
            catch (Exception ex)
            {
                Status = $"Save error (designer campaigns): {ex.Message}";
            }
        }
    
        [RelayCommand]
        private async Task LoadDesignerCampaignsAsync()
        {
            try
            {
                if (!System.IO.File.Exists(DesignerCampaignsPath)) { Status = "No saved designer campaigns"; return; }
                var json = await System.IO.File.ReadAllTextAsync(DesignerCampaignsPath);
                var list = System.Text.Json.JsonSerializer.Deserialize<List<CampaignTileViewModel>>(json) ?? new();
                var termSvc = new CampaignTermBreakdownService(_financialFacade, _standardRates);

                Comparison.DesignerCampaigns.Clear();
                foreach (var c in list)
                {
                    // Rehydrate live calculator and fill missing model name from current selection if needed
                    c.TermRoRacCalculator = async (t, r) =>
                    {
                        var req = DealInput.BuildScenarioRequest();
                        return await termSvc.CalculateTermRoRACAsync(req, DealInput, t, r);
                    };
                    if (string.IsNullOrWhiteSpace(c.ModelName))
                        c.ModelName = DealInput.SelectedVehicle?.ModelName ?? c.ModelName;

                    c.RecalculateAggregates();
                    Comparison.DesignerCampaigns.Add(c);
                }

                Status = $"Loaded {Comparison.DesignerCampaigns.Count} designer campaigns";
            }
            catch (Exception ex)
            {
                Status = $"Load error (designer campaigns): {ex.Message}";
            }
        }
    }