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
        _ = RefreshCommissionPolicyAsync();
        _ = LoadSummariesAsync();
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
            var state = new DealStateDto
            {
                dealerCommission = new DealerCommissionDto { mode = DealerCommissionMode, pct = DealerCommissionPct, amt = DealerCommissionAmt, resolvedAmt = DealerCommissionResolvedAmt },
                idcOther = new IDCOtherDto { value = IdcOtherUserEdited ? IdcOther : SubsidyBudget, userEdited = IdcOtherUserEdited }
            };
            var catalog = await _api.GetCampaignCatalogAsync();
            var req = new CampaignSummariesRequestDto
            {
                deal = deal,
                state = state,
                campaigns = catalog.Select(c => new CampaignDto { Id = c.Id, Type = c.Type, Funder = c.Funder, Description = c.Description, Parameters = c.Parameters }).ToList()
            };
            var rows = await _api.GetCampaignSummariesAsync(req);

            var temp = new List<(CampaignSummaryViewModel vm, double monthly, double eff)>();
            StandardCampaigns.Clear();
            CampaignSummaries.Clear();
            foreach (var r in rows)
            {
                var calcReq = new CalculationRequestDto
                {
                    Deal = deal,
                    Campaigns = new List<CampaignDto> { new CampaignDto { Id = r.CampaignId, Type = r.CampaignType } },
                    IdcItems = new(),
                    Options = new() { ["derive_idc_from_cf"] = true }
                };
                var calcRes = await _api.CalculateAsync(calcReq);

                var comps = ExtractAuditComponents(calcRes.Quote.CampaignAudit);
                
                // Derive subsidy components for consistent UI usage and remaining budget math
                double fsDownAmt = 0;
                double fsInsAmt = Math.Max(0, comps.freeInsurance);
                double fsMbspAmt = Math.Max(0, comps.mbsp);
                double subsidyUtilized = fsDownAmt + fsInsAmt + fsMbspAmt;
                
                var vm = new CampaignSummaryViewModel
                {
                    CampaignId = r.CampaignId,
                    CampaignType = r.CampaignType,
                    Title = r.CampaignType,
                    DealerCommission = $"{r.DealerCommissionPct:P2} ({r.DealerCommissionAmt:N0} THB)",
                    Monthly = calcRes.Quote.MonthlyInstallment.ToString("N0", CultureInfo.InvariantCulture),
                    Effective = (calcRes.Quote.CustomerRateEffective).ToString("0.00%"),
                    Downpayment = DownPaymentAmount.ToString("N0", CultureInfo.InvariantCulture),
                    SubsidyUsed = subsidyUtilized.ToString("N0", CultureInfo.InvariantCulture),
                    FSSubDown = fsDownAmt.ToString("N0", CultureInfo.InvariantCulture),
                    FSSubInterest = fsInsAmt.ToString("N0", CultureInfo.InvariantCulture),
                    FSFreeMBSP = fsMbspAmt.ToString("N0", CultureInfo.InvariantCulture),
                    CashDiscount = comps.cashDiscount.ToString("N0", CultureInfo.InvariantCulture),
                    RoRAC = (calcRes.Quote.Profitability.AcquisitionRoRAC).ToString("0.00%"),
                    Notes = string.Empty,
                    FSSubDownAmount = fsDownAmt,
                    FSSubInterestAmount = fsInsAmt,
                    FSFreeMBSPAmount = fsMbspAmt,
                };
                temp.Add((vm, calcRes.Quote.MonthlyInstallment, calcRes.Quote.CustomerRateEffective));
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
            var req = new CalculationRequestDto
            {
                Deal = deal,
                Campaigns = new(),
                IdcItems = new(),
                Options = new() { ["derive_idc_from_cf"] = true },
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
            PopulateMetrics(res);
            PopulateCashflows(res.Schedule);
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    private void PopulateMetrics(CalculationResponseDto res)
    {
        Metrics = new MetricsViewModel
        {
            MonthlyInstallment = res.Quote.MonthlyInstallment.ToString("N0", CultureInfo.InvariantCulture),
            NominalRate = (res.Quote.CustomerRateNominal).ToString("0.00%"),
            EffectiveRate = (res.Quote.CustomerRateEffective).ToString("0.00%"),
            FinancedAmount = res.Quote.FinancedAmount.ToString("N0", CultureInfo.InvariantCulture),
            RoRAC = (res.Quote.Profitability.AcquisitionRoRAC).ToString("0.00%"),
        };
    
        // Update dependent computed sections (breakdown lines and IDC totals)
        RefreshProfitabilityDetails(res);
        OnPropertyChanged(nameof(ActiveSubsidyUtilizedText));
        OnPropertyChanged(nameof(SubsidyRemainingText));
        OnPropertyChanged(nameof(IdcTotalText));
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

private void RefreshProfitabilityDetails(CalculationResponseDto res)
{
    var comps = ExtractAuditComponents(res.Quote.CampaignAudit);
    _activeFsInsurance = Math.Max(0, comps.freeInsurance);
    _activeFsMbsp = Math.Max(0, comps.mbsp);
    _activeCashDiscount = comps.cashDiscount;

    OnPropertyChanged(nameof(ActiveFsInsuranceText));
    OnPropertyChanged(nameof(ActiveFsMbspText));
    OnPropertyChanged(nameof(ActiveSubsidyUtilizedText));
    OnPropertyChanged(nameof(SubsidyRemainingText));
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
        foreach (var r in schedule)
        {
            Cashflows.Add(new CashflowRowViewModel
            {
                Period = r.Period,
                Principal = r.Principal.ToString("N0", CultureInfo.InvariantCulture),
                Interest = r.Interest.ToString("N0", CultureInfo.InvariantCulture),
                Fees = r.Fees.ToString("N0", CultureInfo.InvariantCulture),
                Balance = r.Balance.ToString("N0", CultureInfo.InvariantCulture),
                Cashflow = r.Cashflow.ToString("N0", CultureInfo.InvariantCulture),
            });
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

public class MetricsViewModel : ObservableObject
{
    public string MonthlyInstallment { get; set; } = "";
    public string NominalRate { get; set; } = "";
    public string EffectiveRate { get; set; } = "";
    public string FinancedAmount { get; set; } = "";
    public string RoRAC { get; set; } = "";
}

public class CampaignSummaryViewModel : ObservableObject
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

public class CashflowRowViewModel : ObservableObject
{
   public int Period { get; set; }
   public string Principal { get; set; } = "";
   public string Interest { get; set; } = "";
   public string Fees { get; set; } = "";
   public string Balance { get; set; } = "";
   public string Cashflow { get; set; } = "";
}
