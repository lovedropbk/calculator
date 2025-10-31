using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.Engine.Core;
using FinancialCalculator.Engine.Models;
using FinancialCalculator.Engine.Models.Facade;
using FinancialCalculator.WinUI3.Models;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class DealInputViewModel : ObservableValidator
{
    private readonly IVehicleCatalogService _vehicleCatalog;
    private readonly IStandardRateService _standardRates;
    private readonly ICommissionService _commission;

    // Event to notify parent to recalculate
    public event EventHandler? InputsChanged;

    public DealInputViewModel(IVehicleCatalogService vehicleCatalog, IStandardRateService standardRates, ICommissionService commission)
    {
        _vehicleCatalog = vehicleCatalog;
        _standardRates = standardRates;
        _commission = commission;
    }

    // Test-friendly overload: allows constructing without a vehicle catalog
    public DealInputViewModel(IStandardRateService standardRates, ICommissionService commission)
    {
        _vehicleCatalog = new NullVehicleCatalogService();
        _standardRates = standardRates;
        _commission = commission;
    }

    // MARK: Vehicle Selection
    private Vehicle? _selectedVehicle;
    public Vehicle? SelectedVehicle { get => _selectedVehicle; set { if (SetProperty(ref _selectedVehicle, value)) OnSelectedVehicleChanged(value); } }

    public ObservableCollection<Vehicle> AllVehicles { get; } = new();
    public ObservableCollection<string> MbspPackages { get; } = new();

    // MARK: Deal Inputs
    private string _product = "HP";
    public string Product { get => _product; set { if (SetProperty(ref _product, value)) OnProductChanged(value); } }

    [Range(0, 1_000_000_000, ErrorMessage = "Invalid Price")]
    private double _priceExTax = 1_000_000;
    public double PriceExTax { get => _priceExTax; set { if (SetProperty(ref _priceExTax, value, true)) OnPriceExTaxChanged(value); } }

    // MBTh manufacturer discount and computed Transaction Price
    private double _mbthDiscount = 0;
    public double MbthDiscount { get => _mbthDiscount; set { if (SetProperty(ref _mbthDiscount, value)) OnMbthDiscountChanged(value); } }

    // Computed: MSRP - MBTh discount; used for engine VehiclePrice and UI
    public double TransactionPrice => Math.Max(0, PriceExTax - MbthDiscount);

    [Range(0, 1_000_000_000, ErrorMessage = "Invalid Amount")]
    private double _additionalFinancedItems = 0;
    public double AdditionalFinancedItems { get => _additionalFinancedItems; set => SetProperty(ref _additionalFinancedItems, value, true); }

    private double _downPaymentAmount = 200_000;
    public double DownPaymentAmount { get => _downPaymentAmount; set { if (SetProperty(ref _downPaymentAmount, value)) OnDownPaymentAmountChanged(value); } }

    private string _downPaymentUnit = "THB"; // THB | %
    public string DownPaymentUnit { get => _downPaymentUnit; set { if (SetProperty(ref _downPaymentUnit, value)) OnDownPaymentUnitChanged(value); } }

    private double _downPaymentValueEntry = 200_000;
    public double DownPaymentValueEntry { get => _downPaymentValueEntry; set { if (SetProperty(ref _downPaymentValueEntry, value)) OnDownPaymentValueEntryChanged(value); } }

    private string _balloonUnit = "%"; // THB | %
    public string BalloonUnit { get => _balloonUnit; set { if (SetProperty(ref _balloonUnit, value)) OnBalloonUnitChanged(value); } }

    private double _balloonValueEntry = 0;
    public double BalloonValueEntry { get => _balloonValueEntry; set { if (SetProperty(ref _balloonValueEntry, value)) OnBalloonValueEntryChanged(value); } }

    [Range(1, 120, ErrorMessage = "Term must be 1-120")]
    private int _termMonths = 36;
    public int TermMonths { get => _termMonths; set { if (SetProperty(ref _termMonths, value, true)) OnTermMonthsChanged(value); } }

    private string _timing = "arrears"; // arrears|advance
    public string Timing { get => _timing; set { if (SetProperty(ref _timing, value)) OnTimingChanged(value); } }

    private double _balloonPercent = 0;
    public double BalloonPercent { get => _balloonPercent; set { if (SetProperty(ref _balloonPercent, value)) OnBalloonPercentChanged(value); } }

    private string _lockMode = "amount"; // amount|percent
    public string LockMode { get => _lockMode; set { if (SetProperty(ref _lockMode, value)) OnLockModeChanged(value); } }

    // MARK: Rate Mode
    private bool _isUpdatingRate = false;
    private string _rateMode = "fixed_rate"; // fixed_rate|target_installment
    public string RateMode { get => _rateMode; set { if (SetProperty(ref _rateMode, value)) OnRateModeChanged(value); } }
    private int _rateModeIndex = 0; // 0=fixed_rate, 1=target_installment
    public int RateModeIndex { get => _rateModeIndex; set { if (SetProperty(ref _rateModeIndex, value)) OnRateModeIndexChanged(value); } }
    public bool IsFixedRateMode => string.Equals(RateMode, "fixed_rate", StringComparison.OrdinalIgnoreCase);
    public bool IsTargetInstallmentMode => string.Equals(RateMode, "target_installment", StringComparison.OrdinalIgnoreCase);

    [Range(0, 100, ErrorMessage = "Rate must be 0-100%")]
    private double _customerNominalRate = 3.99;
    public double CustomerNominalRate
    {
        get => _customerNominalRate;
        set
        {
            if (SetProperty(ref _customerNominalRate, value, true))
            {
                OnCustomerNominalRateChanged(value);
            }
        }
    }

    private double _customerFlatRate = 0;
    public double CustomerFlatRate
    {
        get => _customerFlatRate;
        set
        {
            if (SetProperty(ref _customerFlatRate, value))
            {
                OnCustomerFlatRateChanged(value);
            }
        }
    }

    private double _targetInstallment = 0;
    public double TargetInstallment { get => _targetInstallment; set { if (SetProperty(ref _targetInstallment, value)) OnTargetInstallmentChanged(value); } }

    // MARK: Subsidy & IDC
    private double _subsidyBudget = 100_000;
    public double SubsidyBudget { get => _subsidyBudget; set { if (SetProperty(ref _subsidyBudget, value)) OnSubsidyBudgetChanged(value); } }
    private bool _subsidyBudgetIsEnabled = true;
    public bool SubsidyBudgetIsEnabled { get => _subsidyBudgetIsEnabled; set => SetProperty(ref _subsidyBudgetIsEnabled, value); }
    private string _dealerCommissionMode = "auto"; // auto|override
    public string DealerCommissionMode { get => _dealerCommissionMode; set { if (SetProperty(ref _dealerCommissionMode, value)) OnDealerCommissionModeChanged(value); } }
    private double? _dealerCommissionPct;
    public double? DealerCommissionPct { get => _dealerCommissionPct; set { if (SetProperty(ref _dealerCommissionPct, value)) OnDealerCommissionPctChanged(value); } }
    private double? _dealerCommissionAmt;
    public double? DealerCommissionAmt { get => _dealerCommissionAmt; set { if (SetProperty(ref _dealerCommissionAmt, value)) OnDealerCommissionAmtChanged(value); } }
    private double _dealerCommissionResolvedAmt;
    public double DealerCommissionResolvedAmt { get => _dealerCommissionResolvedAmt; set { if (SetProperty(ref _dealerCommissionResolvedAmt, value)) OnDealerCommissionResolvedAmtChanged(value); } }

    private string _commissionEntryUnit = "auto"; // auto | % | THB
    public string CommissionEntryUnit { get => _commissionEntryUnit; set { if (SetProperty(ref _commissionEntryUnit, value)) OnCommissionEntryUnitChanged(value); } }
    private double _commissionEntryValue = 0;
    public double CommissionEntryValue { get => _commissionEntryValue; set { if (SetProperty(ref _commissionEntryValue, value)) OnCommissionEntryValueChanged(value); } }

    private double _autoCommissionPct;
    public double AutoCommissionPct { get => _autoCommissionPct; set => SetProperty(ref _autoCommissionPct, value); }
    private string _commissionPolicyVersion = string.Empty;
    public string CommissionPolicyVersion { get => _commissionPolicyVersion; set => SetProperty(ref _commissionPolicyVersion, value); }

    private double _idcOther = 0;
    public double IdcOther { get => _idcOther; set { if (SetProperty(ref _idcOther, value)) OnIdcOtherChanged(value); } }
    private double _upfrontSubsidies = 0;
    public double UpfrontSubsidies { get => _upfrontSubsidies; set => SetProperty(ref _upfrontSubsidies, value); }
    public bool IdcOtherUserEdited { get; set; } = false;

    public string DealerCommissionPctText => ((DealerCommissionMode == "override" ? (DealerCommissionPct ?? AutoCommissionPct) : AutoCommissionPct) * 100.0).ToString("0.00", CultureInfo.InvariantCulture);
    public string DealerCommissionResolvedAmtText => DealerCommissionResolvedAmt.ToString("N0", CultureInfo.InvariantCulture);
    public string SubsidyBudgetText => SubsidyBudget.ToString("N0", CultureInfo.InvariantCulture);

    // UI helpers
    public string PricePlaceholder => "THB";
    public string PriceUnitSuffix => "THB";
    public string DownPaymentPlaceholder => DownPaymentUnit;
    public string DownPaymentUnitSuffix => DownPaymentUnit;
    public string BalloonPlaceholder => BalloonUnit;
    public string BalloonUnitSuffix => BalloonUnit;
    public bool IsBalloonEnabled => true;
    public bool IsCommissionEntryEditable => !string.Equals(CommissionEntryUnit, "auto", StringComparison.OrdinalIgnoreCase);

    private bool _isDealInputsCollapsed = false;
    public bool IsDealInputsCollapsed { get => _isDealInputsCollapsed; set => SetProperty(ref _isDealInputsCollapsed, value); }

    // MARK: Risk Parameters
    private string _selectedCustomerType = "RETAIL PRIVATE";
    public string SelectedCustomerType { get => _selectedCustomerType; set { if (SetProperty(ref _selectedCustomerType, value)) NotifyChanged(); } }
    
    private string _selectedAssetState = "New";
    public string SelectedAssetState { get => _selectedAssetState; set { if (SetProperty(ref _selectedAssetState, value)) NotifyChanged(); } }

    private string _selectedAssetValuationCurve = "MBPC";
    public string SelectedAssetValuationCurve { get => _selectedAssetValuationCurve; set { if (SetProperty(ref _selectedAssetValuationCurve, value)) NotifyChanged(); } }

    private string _selectedRating = "5, 5.0";
    public string SelectedRating { get => _selectedRating; set { if (SetProperty(ref _selectedRating, value)) NotifyChanged(); } }

    public ObservableCollection<string> CustomerTypes { get; } = new() { "RETAIL PRIVATE", "RETAIL SMALL BUSINESS", "FLEET", "DEALER" };
    public ObservableCollection<string> AssetStates { get; } = new() { "New", "Used" };
    public ObservableCollection<string> AssetValuationCurves { get; } = new() { "MBPC", "MBVA", "OOPC", "MBCV", "FUCV" };
    public ObservableCollection<string> CreditRatings { get; } = new() { "1, 1.0", "2, 2.0", "3, 3.0", "3.5", "4, 4.0", "5, 5.0", "6, 6.0", "7, 7.0", "8, 8.0", "Not Rated" };

    // MARK: Property Change Handlers
    private void NotifyChanged() => InputsChanged?.Invoke(this, EventArgs.Empty);

    private void OnProductChanged(string value)
    {
        RefreshCommissionPolicyLocal();
        OnPropertyChanged(nameof(IsBalloonEnabled));
        UpdateStandardRate();
        if (_selectedVehicle != null) OnSelectedVehicleChanged(_selectedVehicle);
        NotifyChanged();
    }
    private void OnPriceExTaxChanged(double value)
    {
        UpdateDealerCommissionResolved();
        OnPropertyChanged(nameof(DealerCommissionPctText));
        OnPropertyChanged(nameof(TransactionPrice));
        UpdateStandardRate();
        NotifyChanged();
    }
    private void OnMbthDiscountChanged(double value)
    {
        OnPropertyChanged(nameof(TransactionPrice));
        UpdateDealerCommissionResolved();
        UpdateStandardRate();
        NotifyChanged();
    }
    private void OnDownPaymentAmountChanged(double value) { UpdateDealerCommissionResolved(); OnPropertyChanged(nameof(DealerCommissionPctText)); NotifyChanged(); }
    private void OnTermMonthsChanged(int value)
    {
        UpdateStandardRate();
        if (string.Equals(Product, "mySTAR", System.StringComparison.OrdinalIgnoreCase) && _selectedVehicle != null)
        {
            OnSelectedVehicleChanged(_selectedVehicle);
        }
        if (!_isUpdatingRate)
        {
            _isUpdatingRate = true;
            CustomerFlatRate = (double)RateConverter.ConvertNominalToFlat((decimal)CustomerNominalRate, TermMonths, GetPaymentMode());
            _isUpdatingRate = false;
        }
        NotifyChanged();
    }

    private void OnCustomerNominalRateChanged(double value)
    {
        if (!_isUpdatingRate)
        {
            _isUpdatingRate = true;
            CustomerFlatRate = (double)RateConverter.ConvertNominalToFlat((decimal)value, TermMonths, GetPaymentMode());
            _isUpdatingRate = false;
        }
        CheckRateDeviation();
        NotifyChanged();
    }

    private void OnCustomerFlatRateChanged(double value)
    {
        if (!_isUpdatingRate)
        {
            _isUpdatingRate = true;
            CustomerNominalRate = (double)RateConverter.ConvertFlatToNominal((decimal)value, TermMonths, GetPaymentMode());
            _isUpdatingRate = false;
        }
        CheckRateDeviation();
        NotifyChanged();
    }

    private PaymentMode GetPaymentMode() => string.Equals(Timing, "advance", StringComparison.OrdinalIgnoreCase) ? PaymentMode.InAdvance : PaymentMode.InArrears;
    private void OnSubsidyBudgetChanged(double value) { OnPropertyChanged(nameof(SubsidyBudgetText)); NotifyChanged(); }
    // Let's keep simple inputs here. MainVM can observe SubsidyBudget change.

    private void OnTimingChanged(string value)
    {
        UpdateStandardRate();
        if (!_isUpdatingRate)
        {
            _isUpdatingRate = true;
            CustomerFlatRate = (double)RateConverter.ConvertNominalToFlat((decimal)CustomerNominalRate, TermMonths, GetPaymentMode());
            _isUpdatingRate = false;
        }
        NotifyChanged();
    }
    private void OnBalloonPercentChanged(double value) => NotifyChanged();
    private void OnDownPaymentUnitChanged(string value) { OnPropertyChanged(nameof(DownPaymentPlaceholder)); OnPropertyChanged(nameof(DownPaymentUnitSuffix)); UpdateStandardRate(); NotifyChanged(); }
    private void OnDownPaymentValueEntryChanged(double value) { UpdateStandardRate(); NotifyChanged(); }
    private void OnBalloonUnitChanged(string value) { OnPropertyChanged(nameof(BalloonPlaceholder)); OnPropertyChanged(nameof(BalloonUnitSuffix)); NotifyChanged(); }
    private void OnBalloonValueEntryChanged(double value) => NotifyChanged();
    private void OnLockModeChanged(string value) { }

    private void OnRateModeChanged(string value) { OnPropertyChanged(nameof(IsFixedRateMode)); OnPropertyChanged(nameof(IsTargetInstallmentMode)); RateModeIndex = string.Equals(RateMode, "fixed_rate", System.StringComparison.OrdinalIgnoreCase) ? 0 : 1; NotifyChanged(); }
    private void OnRateModeIndexChanged(int value) { RateMode = value == 0 ? "fixed_rate" : "target_installment"; }
    private void OnTargetInstallmentChanged(double value) => NotifyChanged();

    private void OnDealerCommissionModeChanged(string value) => NotifyChanged();
    private void OnCommissionEntryUnitChanged(string value)
    {
        if (string.Equals(value, "auto", System.StringComparison.OrdinalIgnoreCase))
        {
            DealerCommissionMode = "auto"; DealerCommissionPct = null; DealerCommissionAmt = null; CommissionEntryValue = 0;
        }
        else
        {
            DealerCommissionMode = "override";
        }
        NotifyChanged();
    }
    private void OnCommissionEntryValueChanged(double value)
    {
        if (!string.Equals(CommissionEntryUnit, "auto", System.StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(CommissionEntryUnit, "%", System.StringComparison.OrdinalIgnoreCase)) { DealerCommissionPct = value / 100.0; DealerCommissionAmt = null; }
            else { DealerCommissionAmt = value; DealerCommissionPct = null; }
            UpdateDealerCommissionResolved();
            OnPropertyChanged(nameof(DealerCommissionPctText));
        }
        NotifyChanged();
    }
    private void OnDealerCommissionPctChanged(double? value)
    {
        if (value.HasValue) DealerCommissionMode = "override";
        UpdateDealerCommissionResolved();
        OnPropertyChanged(nameof(DealerCommissionPctText));
        NotifyChanged();
    }
    private void OnDealerCommissionAmtChanged(double? value)
    {
        if (value.HasValue) DealerCommissionMode = "override";
        UpdateDealerCommissionResolved();
        OnPropertyChanged(nameof(DealerCommissionPctText));
        NotifyChanged();
    }
    private void OnDealerCommissionResolvedAmtChanged(double value)
    {
        OnPropertyChanged(nameof(DealerCommissionResolvedAmtText));
        NotifyChanged();
    }

    private void OnIdcOtherChanged(double value)
    {
        IdcOtherUserEdited = true;
        NotifyChanged();
    }
    private void OnIdcOtherUserEditedChanged(bool value) => NotifyChanged();

    private void OnIsDealInputsCollapsedChanged(bool value) { } // No-op, handled by XAML converter

    [RelayCommand]
    private void ToggleDealInputsCollapsed()
    {
        IsDealInputsCollapsed = !IsDealInputsCollapsed;
    }

    private void OnSelectedVehicleChanged(Vehicle? value)
    {
        if (value != null)
        {
            PriceExTax = value.MSRP;
             if (string.Equals(Product, "mySTAR", System.StringComparison.OrdinalIgnoreCase))
            {
                var rv = value.GetRVForTerm(TermMonths);
                if (rv.HasValue)
                {
                    BalloonUnit = "%";
                    BalloonValueEntry = rv.Value * 100;
                }
            }
            // MBSP cost update needs to happen but might need campaign context.
            // Will handle in MainVM for now or pass campaign in.
        }
    }

    // Standard Rate & Deviation
    private bool _isRateDeviation = false;
    public bool IsRateDeviation { get => _isRateDeviation; set => SetProperty(ref _isRateDeviation, value); }
    private double? _standardRateForCurrentSelection;

    private void UpdateStandardRate()
    {
        double downPaymentPct;
        if (string.Equals(DownPaymentUnit, "%", System.StringComparison.OrdinalIgnoreCase))
        {
            downPaymentPct = DownPaymentValueEntry / 100.0;
        }
        else
        {
            downPaymentPct = TransactionPrice > 0 ? DownPaymentValueEntry / TransactionPrice : 0;
        }

        _standardRateForCurrentSelection = _standardRates.GetStandardRate(Product, TermMonths, downPaymentPct, Timing);
        
        if (_standardRateForCurrentSelection.HasValue)
        {
            CustomerNominalRate = _standardRateForCurrentSelection.Value;
        }
        
        CheckRateDeviation();
    }

    private void CheckRateDeviation()
    {
        if (_standardRateForCurrentSelection.HasValue)
        {
            IsRateDeviation = Math.Abs(CustomerNominalRate - _standardRateForCurrentSelection.Value) > 0.001;
        }
        else
        {
            IsRateDeviation = false;
        }
    }

    public void RefreshCommissionPolicyLocal()
    {
        try
        {
            AutoCommissionPct = _commission.GetAutoCommissionPct(Product);
            CommissionPolicyVersion = _commission.PolicyVersion;
            UpdateDealerCommissionResolved();
            OnPropertyChanged(nameof(DealerCommissionPctText));
        }
        catch { /* swallow */ }
    }

    public (double pct, double amt) ResolveCommissionForFinanced(double financed)
    {
        double pct = DealerCommissionMode == "override" ? (DealerCommissionPct ?? AutoCommissionPct) : AutoCommissionPct;
        if (pct < 0) pct = 0;
        double amt = DealerCommissionMode == "override" && DealerCommissionAmt.HasValue
            ? DealerCommissionAmt.Value
            : Math.Round(financed * pct);
        return (pct, Math.Max(0, amt));
    }

    private void UpdateDealerCommissionResolved()
    {
        try
        {
            // Compute financed amount from current inputs
            double basePrice = TransactionPrice;
            double dpVal = string.Equals(DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase)
                ? basePrice * DownPaymentValueEntry / 100.0
                : DownPaymentValueEntry;

            var fin = Math.Max(0, basePrice - dpVal);

            var (_, amt) = ResolveCommissionForFinanced(fin);
            DealerCommissionResolvedAmt = Math.Max(0, amt);
        }
        catch
        {
            DealerCommissionResolvedAmt = 0;
        }
    }

    public ScenarioRequest BuildScenarioRequest()
    {
        return new ScenarioRequest
        {
             Market = "TH",
             Product = Product,
             Timing = Timing,
             TermMonths = TermMonths,
             VehiclePrice = (decimal)TransactionPrice,
             AdditionalFinancedItems = (decimal)AdditionalFinancedItems,
             DownIsPercent = string.Equals(DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase),
             DownValue = (decimal)DownPaymentValueEntry,
             BalloonIsPercent = string.Equals(BalloonUnit, "%", StringComparison.OrdinalIgnoreCase),
             BalloonValue = (decimal)BalloonValueEntry,
             CustomerRatePercent = (decimal)CustomerNominalRate,
             UpfrontSubsidies = (decimal)SubsidyBudget,
             UpfrontCosts = (decimal)(DealerCommissionResolvedAmt + IdcOther),
             SubdownIsPercent = false,
             SubdownValue = 0,
             CustomerType = SelectedCustomerType,
             AssetState = string.Equals(SelectedAssetState, "New", StringComparison.OrdinalIgnoreCase) ? "N" : "U",
             AssetValuationCurve = SelectedAssetValuationCurve,
             Rating = SelectedRating
        };
    }
}