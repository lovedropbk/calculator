using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using FinancialCalculator.Engine.Models.Facade;
using Microsoft.UI.Xaml;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class CampaignDetailsViewModel : ObservableObject
{
    private readonly DealInputViewModel _dealInput;

    public CampaignDetailsViewModel(DealInputViewModel dealInput)
    {
        _dealInput = dealInput;
    }

    // Budget Visualization segments
    [ObservableProperty]
    private BudgetUtilizationViewModel _budgetUtilization = new();

    // Active campaign allocations and summaries
    private double _activeFsInsurance;
    private double _activeFsMbsp;
    private double _activeCashDiscount;
    private double _activeSubsidyUsed;

    public string ActiveFsInsuranceText => _activeFsInsurance.ToString("N0", CultureInfo.InvariantCulture);
    public string ActiveFsMbspText => _activeFsMbsp.ToString("N0", CultureInfo.InvariantCulture);
    public string ActiveSubsidyUtilizedText => _activeSubsidyUsed.ToString("N0", CultureInfo.InvariantCulture);
    public string SubsidyRemainingText => Math.Max(0, _dealInput.SubsidyBudget - _activeSubsidyUsed).ToString("N0", CultureInfo.InvariantCulture);
    public string IdcOtherText => _dealInput.IdcOther.ToString("N0", CultureInfo.InvariantCulture);
    public string IdcTotalText => (_dealInput.DealerCommissionResolvedAmt + _dealInput.IdcOther + _activeFsInsurance + _activeFsMbsp).ToString("N0", CultureInfo.InvariantCulture);

    // Profitability Waterfall backing values
    private double _wfCustomerRate;
    private double _wfIDCUpfrontAnnualized;
    private double _wfSubsidyUpfrontAnnualized;
    private double _wfDealIRREffective;
    private double _wfDealIRRNominal;
    private double _wfCostOfDebtMatched;
    private double _wfMatchedFundedSpread;
    private double _wfGrossInterestMargin;
    private double _wfNetInterestMargin;
    private double _wfCostOfCreditRisk;
    private double _wfOPEX;
    private double _wfCapitalAdvantage;
    private double _wfNetEBITMargin;
    private double _wfEconomicCapital;

    // Additional breakdown (separated IDC/Subsidy values)
    private double _wfIDCUpfrontCostPct;
    private double _wfIDCPeriodicCostPct;
    private double _wfSubsidyUpfrontPct;
    private double _wfSubsidyPeriodicPct;
    private double _wfIDCUpfront;  // Net IDC (upfront)
    private double _wfIDCPeriodic; // Net IDC (periodic)

    private static string Pct(double v) => v.ToString("0.00%", CultureInfo.InvariantCulture);

    // Exposed formatted waterfall texts
    public string WfCustomerRateText => Pct(_wfCustomerRate);
    public string WfIDCUpfrontAnnualizedText => Pct(_wfIDCUpfrontAnnualized);
    public string WfSubsidyUpfrontAnnualizedText => Pct(_wfSubsidyUpfrontAnnualized);
    public string WfDealIRRText => Pct(_wfDealIRREffective);
    public string WfDealIRRNominalText => Pct(_wfDealIRRNominal);
    public string WfCostOfDebtMatchedText => Pct(_wfCostOfDebtMatched);
    public string WfMatchedFundedSpreadText => Pct(_wfMatchedFundedSpread);
    public string WfGrossInterestMarginText => Pct(_wfGrossInterestMargin);
    public string WfNetInterestMarginText => Pct(_wfNetInterestMargin);
    public string WfCostOfCreditRiskText => Pct(_wfCostOfCreditRisk);
    public string WfOPEXText => Pct(_wfOPEX);
    public string WfCapitalAdvantageText => Pct(_wfCapitalAdvantage);
    public string WfNetEBITMarginText => Pct(_wfNetEBITMargin);
    public string WfEconomicCapitalText => Pct(_wfEconomicCapital);

    public string WfIDCUpfrontCostPctText => Pct(_wfIDCUpfrontCostPct);
    public string WfIDCPeriodicCostPctText => Pct(_wfIDCPeriodicCostPct);
    public string WfSubsidyUpfrontPctText => Pct(_wfSubsidyUpfrontPct);
    public string WfSubsidyPeriodicPctText => Pct(_wfSubsidyPeriodicPct);

    // Update helpers
    public void UpdateFromProfitability(ProfitabilityDetails p, CampaignSummaryViewModel? activeCampaign)
    {
        _wfCustomerRate = (double)p.CustomerRatePercent;
        _wfDealIRREffective = (double)p.DealIrrEffectivePercent;
        _wfDealIRRNominal = (double)p.DealIrrNominalPercent;
        _wfIDCUpfrontAnnualized = (double)p.IdcUpfrontAnnualizedPercent;
        _wfSubsidyUpfrontAnnualized = (double)p.SubsidyUpfrontAnnualizedPercent;
        _wfCostOfDebtMatched = (double)p.CostOfDebtMatchedPercent;
        _wfMatchedFundedSpread = (double)p.MatchedFundingSpreadPercent;
        _wfGrossInterestMargin = (double)p.GrossInterestMarginPercent;
        _wfNetInterestMargin = (double)p.NetInterestMarginPercent;
        _wfCostOfCreditRisk = (double)p.CostOfCreditRiskPercent;
        _wfOPEX = (double)p.OpexPercent;
        _wfCapitalAdvantage = (double)p.CapitalAdvantagePercent;
        _wfNetEBITMargin = (double)p.NetEbitMarginPercent;
        _wfEconomicCapital = (double)p.EconomicCapitalPercent;

        _wfIDCUpfrontCostPct = (double)p.IdcUpfrontAnnualizedPercent;
        _wfIDCPeriodicCostPct = (double)p.IdcPeriodicPercent;
        _wfSubsidyUpfrontPct = (double)p.SubsidyUpfrontAnnualizedPercent;
        _wfSubsidyPeriodicPct = (double)p.SubsidyPeriodicPercent;

        _wfIDCUpfront = (double)(p.IdcUpfrontAnnualizedPercent - p.SubsidyUpfrontAnnualizedPercent);
        _wfIDCPeriodic = (double)(p.IdcPeriodicPercent - p.SubsidyPeriodicPercent);

        // Sync active allocations from campaign for bottom summary
        if (activeCampaign != null)
        {
            _activeFsInsurance = Math.Max(0, activeCampaign.FSSubInterestAmount);
            _activeFsMbsp = Math.Max(0, activeCampaign.FSFreeMBSPAmount);
            _activeCashDiscount = Math.Max(0, activeCampaign.CashDiscountAmount);
        }
        else
        {
            _activeFsInsurance = _activeFsMbsp = _activeCashDiscount = 0;
        }

        // Notify bindings for metrics and dependent texts
        RaiseDetailsChanged();
    }

    public void UpdateActiveAllocations(double fsInsurance, double fsMbsp, double subsidyUsed)
    {
        _activeFsInsurance = Math.Max(0, fsInsurance);
        _activeFsMbsp = Math.Max(0, fsMbsp);
        _activeSubsidyUsed = Math.Max(0, subsidyUsed);
        RaiseDetailsChanged();
    }

    public void UpdateBudgetUtilization(double cashDiscount, double subDown, double rateSubsidy, double idcs, double unallocated)
    {
        // Clamp to non-negative to avoid layout issues
        cashDiscount = Math.Max(0, cashDiscount);
        subDown = Math.Max(0, subDown);
        rateSubsidy = Math.Max(0, rateSubsidy);
        idcs = Math.Max(0, idcs);
        unallocated = Math.Max(0, unallocated);

        if (cashDiscount + subDown + rateSubsidy + idcs + unallocated <= 0)
        {
            unallocated = 1; // make at least one star to show the bar
        }

        BudgetUtilization = new BudgetUtilizationViewModel
        {
            CashDiscountPct = new GridLength(cashDiscount, GridUnitType.Star),
            SubDownPct = new GridLength(subDown, GridUnitType.Star),
            RateSubsidyPct = new GridLength(rateSubsidy, GridUnitType.Star),
            IdcPct = new GridLength(idcs, GridUnitType.Star),
            UnallocatedPct = new GridLength(unallocated, GridUnitType.Star)
        };
    }

    private void RaiseDetailsChanged()
    {
        OnPropertyChanged(nameof(ActiveFsInsuranceText));
        OnPropertyChanged(nameof(ActiveFsMbspText));
        OnPropertyChanged(nameof(ActiveSubsidyUtilizedText));
        OnPropertyChanged(nameof(SubsidyRemainingText));
        OnPropertyChanged(nameof(IdcOtherText));
        OnPropertyChanged(nameof(IdcTotalText));

        OnPropertyChanged(nameof(WfCustomerRateText));
        OnPropertyChanged(nameof(WfDealIRRText));
        OnPropertyChanged(nameof(WfDealIRRNominalText));
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

        OnPropertyChanged(nameof(WfIDCUpfrontCostPctText));
        OnPropertyChanged(nameof(WfIDCPeriodicCostPctText));
        OnPropertyChanged(nameof(WfSubsidyUpfrontPctText));
        OnPropertyChanged(nameof(WfSubsidyPeriodicPctText));
    }
}

