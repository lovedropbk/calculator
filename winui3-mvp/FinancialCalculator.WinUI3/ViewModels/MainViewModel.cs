using System;
using System.Collections.ObjectModel;
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
    [ObservableProperty] private int termMonths = 36;
    [ObservableProperty] private string timing = "arrears"; // arrears|advance
    [ObservableProperty] private double balloonPercent = 0;
    [ObservableProperty] private string lockMode = "amount"; // amount|percent

    // MARK: Rate Mode
    [ObservableProperty] private string rateMode = "fixed_rate"; // fixed_rate|target_installment
    [ObservableProperty] private double customerRatePct = 3.99;
    [ObservableProperty] private double targetInstallment = 0;

    // MARK: Subsidy & IDC
    [ObservableProperty] private double subsidyBudget = 100_000;
    [ObservableProperty] private string dealerCommissionMode = "auto"; // auto|override
    [ObservableProperty] private double? dealerCommissionPct;
    [ObservableProperty] private double? dealerCommissionAmt;
    [ObservableProperty] private double dealerCommissionResolvedAmt;

    // Auto policy (fetched)
    [ObservableProperty] private double autoCommissionPct; // fraction (e.g., 0.03)
    [ObservableProperty] private string commissionPolicyVersion = string.Empty;

    [ObservableProperty] private double idcOther = 0;
    [ObservableProperty] private bool idcOtherUserEdited = false;

    public string DealerCommissionPctText => ((DealerCommissionMode == "override" ? (DealerCommissionPct ?? AutoCommissionPct) : AutoCommissionPct) * 100.0).ToString("0.00", CultureInfo.InvariantCulture);
    public string DealerCommissionResolvedAmtText => DealerCommissionResolvedAmt.ToString("N0", CultureInfo.InvariantCulture);


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

                var vm = new CampaignSummaryViewModel
                {
                    CampaignId = r.CampaignId,
                    CampaignType = r.CampaignType,
                    Title = r.CampaignType,
                    DealerCommission = $"{r.DealerCommissionPct:P2} ({r.DealerCommissionAmt:N0} THB)",
                    Monthly = calcRes.Quote.MonthlyInstallment.ToString("N0", CultureInfo.InvariantCulture),
                    Effective = (calcRes.Quote.CustomerRateEffective).ToString("0.00%"),
                    Downpayment = DownPaymentAmount.ToString("N0", CultureInfo.InvariantCulture),
                    SubsidyUsed = SubsidyBudget.ToString("N0", CultureInfo.InvariantCulture),
                    FreeInsurance = comps.freeInsurance.ToString("N0", CultureInfo.InvariantCulture),
                    MBSP = comps.mbsp.ToString("N0", CultureInfo.InvariantCulture),
                    CashDiscount = comps.cashDiscount.ToString("N0", CultureInfo.InvariantCulture),
                    RoRAC = (calcRes.Quote.Profitability.AcquisitionRoRAC).ToString("0.00%"),
                    Notes = string.Empty,
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
        return new DealDto
        {
            Product = Product,
            PriceExTax = PriceExTax,
            DownPaymentAmount = DownPaymentAmount,
            DownPaymentPercent = 0,
            DownPaymentLocked = LockMode,
            TermMonths = TermMonths,
            BalloonPercent = BalloonPercent,
            BalloonAmount = 0,
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
    public string SubsidyUsed { get; set; } = string.Empty;
    public string FreeInsurance { get; set; } = string.Empty;
    public string MBSP { get; set; } = string.Empty;
    public string CashDiscount { get; set; } = string.Empty;
    public string RoRAC { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public CampaignSummaryViewModel Clone() => new CampaignSummaryViewModel
    {
        CampaignId = this.CampaignId,
        CampaignType = this.CampaignType,
        Title = this.Title,
        DealerCommission = this.DealerCommission,
        Monthly = this.Monthly,
        Effective = this.Effective,
        Downpayment = this.Downpayment,
        SubsidyUsed = this.SubsidyUsed,
        FreeInsurance = this.FreeInsurance,
        MBSP = this.MBSP,
        CashDiscount = this.CashDiscount,
        RoRAC = this.RoRAC,
        Notes = this.Notes
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
