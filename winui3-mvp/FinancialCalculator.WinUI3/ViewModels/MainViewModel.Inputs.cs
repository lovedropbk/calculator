using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class MainViewModel
{
    // MARK: Deal Inputs
    private string _product = "HP";
    public string Product { get => _product; set { if (SetProperty(ref _product, value)) OnProductChanged(value); } }
    private double _priceExTax = 1_000_000;
    public double PriceExTax { get => _priceExTax; set { if (SetProperty(ref _priceExTax, value)) OnPriceExTaxChanged(value); } }
    private double _additionalFinancedItems = 0;
    public double AdditionalFinancedItems { get => _additionalFinancedItems; set => SetProperty(ref _additionalFinancedItems, value); }
    private double _downPaymentAmount = 200_000;
    public double DownPaymentAmount { get => _downPaymentAmount; set { if (SetProperty(ref _downPaymentAmount, value)) OnDownPaymentAmountChanged(value); } }
    // Unified entry + unit for Down Payment and Balloon
    private string _downPaymentUnit = "THB"; // THB | %
    public string DownPaymentUnit { get => _downPaymentUnit; set { if (SetProperty(ref _downPaymentUnit, value)) OnDownPaymentUnitChanged(value); } }
    private double _downPaymentValueEntry = 200_000;
    public double DownPaymentValueEntry { get => _downPaymentValueEntry; set { if (SetProperty(ref _downPaymentValueEntry, value)) OnDownPaymentValueEntryChanged(value); } }
    private string _balloonUnit = "%"; // THB | %
    public string BalloonUnit { get => _balloonUnit; set { if (SetProperty(ref _balloonUnit, value)) OnBalloonUnitChanged(value); } }
    private double _balloonValueEntry = 0;
    public double BalloonValueEntry { get => _balloonValueEntry; set { if (SetProperty(ref _balloonValueEntry, value)) OnBalloonValueEntryChanged(value); } }
    private int _termMonths = 36;
    public int TermMonths { get => _termMonths; set { if (SetProperty(ref _termMonths, value)) OnTermMonthsChanged(value); } }
    private string _timing = "arrears"; // arrears|advance
    public string Timing { get => _timing; set { if (SetProperty(ref _timing, value)) OnTimingChanged(value); } }
    private double _balloonPercent = 0;
    public double BalloonPercent { get => _balloonPercent; set { if (SetProperty(ref _balloonPercent, value)) OnBalloonPercentChanged(value); } }
    private string _lockMode = "amount"; // amount|percent
    public string LockMode { get => _lockMode; set { if (SetProperty(ref _lockMode, value)) OnLockModeChanged(value); } }

    // MARK: Rate Mode
    private string _rateMode = "fixed_rate"; // fixed_rate|target_installment
    public string RateMode { get => _rateMode; set { if (SetProperty(ref _rateMode, value)) OnRateModeChanged(value); } }
    private int _rateModeIndex = 0; // 0=fixed_rate, 1=target_installment
    public int RateModeIndex { get => _rateModeIndex; set { if (SetProperty(ref _rateModeIndex, value)) OnRateModeIndexChanged(value); } }
    public bool IsFixedRateMode => string.Equals(RateMode, "fixed_rate", StringComparison.OrdinalIgnoreCase);
    public bool IsTargetInstallmentMode => string.Equals(RateMode, "target_installment", StringComparison.OrdinalIgnoreCase);
    private double _customerRatePct = 3.99;
    public double CustomerRatePct { get => _customerRatePct; set { if (SetProperty(ref _customerRatePct, value)) OnCustomerRatePctChanged(value); } }
    private double _targetInstallment = 0;
    public double TargetInstallment { get => _targetInstallment; set { if (SetProperty(ref _targetInstallment, value)) OnTargetInstallmentChanged(value); } }

    // MARK: Subsidy & IDC
    private double _subsidyBudget = 100_000;
    public double SubsidyBudget { get => _subsidyBudget; set { if (SetProperty(ref _subsidyBudget, value)) OnSubsidyBudgetChanged(value); } }
    private bool _subsidyBudgetIsEnabled = true; // Always editable
    public bool SubsidyBudgetIsEnabled { get => _subsidyBudgetIsEnabled; set => SetProperty(ref _subsidyBudgetIsEnabled, value); }
    private string _dealerCommissionMode = "auto"; // auto|override
    public string DealerCommissionMode { get => _dealerCommissionMode; set { if (SetProperty(ref _dealerCommissionMode, value)) OnDealerCommissionModeChanged(value); } }
    private double? _dealerCommissionPct;
    public double? DealerCommissionPct { get => _dealerCommissionPct; set { if (SetProperty(ref _dealerCommissionPct, value)) OnDealerCommissionPctChanged(value); } }
    private double? _dealerCommissionAmt;
    public double? DealerCommissionAmt { get => _dealerCommissionAmt; set { if (SetProperty(ref _dealerCommissionAmt, value)) OnDealerCommissionAmtChanged(value); } }
    private double _dealerCommissionResolvedAmt;
    public double DealerCommissionResolvedAmt { get => _dealerCommissionResolvedAmt; set { if (SetProperty(ref _dealerCommissionResolvedAmt, value)) OnDealerCommissionResolvedAmtChanged(value); } }

    // Unified commission entry (auto | % | THB)
    private string _commissionEntryUnit = "auto"; // auto | % | THB
    public string CommissionEntryUnit { get => _commissionEntryUnit; set { if (SetProperty(ref _commissionEntryUnit, value)) OnCommissionEntryUnitChanged(value); } }
    private double _commissionEntryValue = 0;
    public double CommissionEntryValue { get => _commissionEntryValue; set { if (SetProperty(ref _commissionEntryValue, value)) OnCommissionEntryValueChanged(value); } }

    // Auto policy (local)
    private double _autoCommissionPct; // fraction (e.g., 0.03)
    public double AutoCommissionPct { get => _autoCommissionPct; set => SetProperty(ref _autoCommissionPct, value); }
    private string _commissionPolicyVersion = string.Empty;
    public string CommissionPolicyVersion { get => _commissionPolicyVersion; set => SetProperty(ref _commissionPolicyVersion, value); }

    private double _idcOther = 0;
    public double IdcOther { get => _idcOther; set { if (SetProperty(ref _idcOther, value)) OnIdcOtherChanged(value); } }
    private double _upfrontSubsidies = 0;
    public double UpfrontSubsidies { get => _upfrontSubsidies; set => SetProperty(ref _upfrontSubsidies, value); }
    private bool _idcOtherUserEdited = false;
    public bool IdcOtherUserEdited { get => _idcOtherUserEdited; set { if (SetProperty(ref _idcOtherUserEdited, value)) OnIdcOtherUserEditedChanged(value); } }

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

    private bool _isDealInputsCollapsed = false;
    public bool IsDealInputsCollapsed { get => _isDealInputsCollapsed; set { if (SetProperty(ref _isDealInputsCollapsed, value)) OnIsDealInputsCollapsedChanged(value); } }

    // Column width of the left Deal Inputs panel; bound to ColumnDefinition.Width
    private string _dealInputsColumnWidth = "420";
    public string DealInputsColumnWidth { get => _dealInputsColumnWidth; set => SetProperty(ref _dealInputsColumnWidth, value); }

    [RelayCommand]
    private void ToggleDealInputsCollapsed()
    {
        IsDealInputsCollapsed = !IsDealInputsCollapsed;
    }
}