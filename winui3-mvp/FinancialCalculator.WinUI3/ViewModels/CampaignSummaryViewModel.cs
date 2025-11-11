using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class CampaignSummaryViewModel : ObservableObject
{
    public string CampaignId { get; set; } = string.Empty;
    public string CampaignType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DealerCommission { get; set; } = string.Empty;
    private string _monthly = string.Empty;
    public string Monthly { get => _monthly; set => SetProperty(ref _monthly, value); }
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
    private string _roRAC = string.Empty;
    public string RoRAC
    {
        get => _roRAC;
        set => SetProperty(ref _roRAC, value);
    }
    public string Notes { get; set; } = string.Empty;


    // New: per-term breakdown (editable by user in Campaign Designer)
    public ObservableCollection<TermBreakdownItemViewModel> TermBreakdown { get; } = new();

    // Aggregated average RoRAC across distribution (computed by services)
    private string _avgRoRAC = "0.00%";
    public string AvgRoRAC { get => _avgRoRAC; set => SetProperty(ref _avgRoRAC, value); }

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

    // Consume remaining subsidy to improve RoRAC
    private bool _consumeAllSubsidy;
    public bool ConsumeAllSubsidy
    {
        get => _consumeAllSubsidy;
        set
        {
            if (_consumeAllSubsidy != value)
            {
                _consumeAllSubsidy = value;
                OnPropertyChanged(nameof(ConsumeAllSubsidy));
            }
        }
    }

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

        // Deep-copy campaign-specific payment holidays
        if (this.PaymentHolidays != null)
        {
            foreach (var h in this.PaymentHolidays)
            {
                copy.PaymentHolidays.Add(new PaymentHolidayRule { StartPeriod = h.StartPeriod, EndPeriod = h.EndPeriod, RuleId = h.RuleId });
            }
        }

        return copy;
    }
}
