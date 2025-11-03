using System;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3.ViewModels
{
    // Split conversions and editor-visibility helpers to keep DealInputViewModel under 500 lines
    public partial class DealInputViewModel
    {
        // MARK: Commission editor gate
        private bool _isCommissionEditorVisible = false;
        public bool IsCommissionEditorVisible
        {
            get => _isCommissionEditorVisible;
            set => SetProperty(ref _isCommissionEditorVisible, value);
        }

        [RelayCommand]
        private void ShowCommissionEditor()
        {
            IsCommissionEditorVisible = true;
            if (string.Equals(CommissionEntryUnit, "auto", StringComparison.OrdinalIgnoreCase))
            {
                CommissionEntryUnit = "%";
            }

            // Seed both fields from current state (auto or override)
            try
            {
                _isUpdatingCommissionUi = true;
                var baseFinanced = ApproxFinanced();
                var pct100 = (DealerCommissionMode == "override" ? (DealerCommissionPct ?? AutoCommissionPct) : AutoCommissionPct) * 100.0;
                CommissionPctEntry = UnitConversionHelper.ClampPercent(pct100);
                CommissionAmtEntry = DealerCommissionResolvedAmt > 0
                    ? DealerCommissionResolvedAmt
                    : UnitConversionHelper.PercentToMoney(CommissionPctEntry, baseFinanced, baseFinanced * (AutoCommissionPct > 0 ? AutoCommissionPct : 0.03));
            }
            finally
            {
                _isUpdatingCommissionUi = false;
            }
        }

        // MARK: Down Payment conversions/clamping
        private void ConvertDownPaymentOnUnitChange(string newUnit)
        {
            double baseThb = TransactionPrice;
            if (string.Equals(newUnit, "%", StringComparison.OrdinalIgnoreCase))
            {
                // Existing entry is THB -> convert to %
                DownPaymentValueEntry = UnitConversionHelper.MoneyToPercent(DownPaymentValueEntry, baseThb, 20.0);
            }
            else
            {
                // Existing entry is % -> convert to THB (fallback 20% of price)
                var fallback = baseThb > 0 ? baseThb * 0.20 : 0;
                DownPaymentValueEntry = UnitConversionHelper.PercentToMoney(DownPaymentValueEntry, baseThb, fallback);
            }
        }

        private void ClampDownPaymentEntry()
        {
            if (string.Equals(DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase))
            {
                var clamped = UnitConversionHelper.ClampPercent(DownPaymentValueEntry);
                if (!clamped.Equals(DownPaymentValueEntry))
                {
                    DownPaymentValueEntry = clamped;
                }
            }
            else
            {
                var sanitized = UnitConversionHelper.SanitizeAmount(DownPaymentValueEntry);
                if (!sanitized.Equals(DownPaymentValueEntry))
                {
                    DownPaymentValueEntry = sanitized;
                }
            }
        }

        // MARK: Balloon conversions/clamping
        private void ConvertBalloonOnUnitChange(string newUnit)
        {
            double baseThb = TransactionPrice;
            if (string.Equals(newUnit, "%", StringComparison.OrdinalIgnoreCase))
            {
                BalloonValueEntry = UnitConversionHelper.MoneyToPercent(BalloonValueEntry, baseThb, 0.0);
            }
            else
            {
                BalloonValueEntry = UnitConversionHelper.PercentToMoney(BalloonValueEntry, baseThb, 0.0);
            }
        }

        private void ClampBalloonEntry()
        {
            if (string.Equals(BalloonUnit, "%", StringComparison.OrdinalIgnoreCase))
            {
                var clamped = UnitConversionHelper.ClampPercent(BalloonValueEntry);
                if (!clamped.Equals(BalloonValueEntry))
                {
                    BalloonValueEntry = clamped;
                }
            }
            else
            {
                var sanitized = UnitConversionHelper.SanitizeAmount(BalloonValueEntry);
                if (!sanitized.Equals(BalloonValueEntry))
                {
                    BalloonValueEntry = sanitized;
                }
            }
        }

        // MARK: Commission UI dual-entry (Pct and Amount) for same-row editor
        private bool _isUpdatingCommissionUi = false;

        private double _commissionPctEntry = 0;
        public double CommissionPctEntry
        {
            get => _commissionPctEntry;
            set
            {
                if (SetProperty(ref _commissionPctEntry, value))
                {
                    OnCommissionPctEntryChanged(value);
                }
            }
        }

        private double _commissionAmtEntry = 0;
        public double CommissionAmtEntry
        {
            get => _commissionAmtEntry;
            set
            {
                if (SetProperty(ref _commissionAmtEntry, value))
                {
                    OnCommissionAmtEntryChanged(value);
                }
            }
        }

        private void SyncCommissionUiFromModel()
        {
            try
            {
                _isUpdatingCommissionUi = true;
                CommissionAmtEntry = DealerCommissionResolvedAmt;
                var baseFinanced = ApproxFinanced();
                CommissionPctEntry = UnitConversionHelper.MoneyToPercent(DealerCommissionResolvedAmt, baseFinanced, (AutoCommissionPct > 0 ? AutoCommissionPct * 100.0 : 3.0));
            }
            finally { _isUpdatingCommissionUi = false; }
        }

        private void OnCommissionPctEntryChanged(double value)
        {
            if (_isUpdatingCommissionUi) return;

            var pct = UnitConversionHelper.ClampPercent(value);
            try
            {
                _isUpdatingCommissionUi = true;
                DealerCommissionMode = "override";
                DealerCommissionPct = pct / 100.0;
                DealerCommissionAmt = null;

                var baseFinanced = ApproxFinanced();
                var amt = UnitConversionHelper.PercentToMoney(pct, baseFinanced, baseFinanced * (AutoCommissionPct > 0 ? AutoCommissionPct : 0.03));
                CommissionAmtEntry = amt;

                UpdateDealerCommissionResolved();
                OnPropertyChanged(nameof(DealerCommissionPctText));
            }
            finally
            {
                _isUpdatingCommissionUi = false;
            }
        }

        private void OnCommissionAmtEntryChanged(double value)
        {
            if (_isUpdatingCommissionUi) return;

            var amt = UnitConversionHelper.SanitizeAmount(value);
            try
            {
                _isUpdatingCommissionUi = true;
                DealerCommissionMode = "override";
                DealerCommissionAmt = amt;
                DealerCommissionPct = null;

                var baseFinanced = ApproxFinanced();
                var pct = UnitConversionHelper.MoneyToPercent(amt, baseFinanced, (AutoCommissionPct > 0 ? AutoCommissionPct * 100.0 : 3.0));
                CommissionPctEntry = pct;

                UpdateDealerCommissionResolved();
                OnPropertyChanged(nameof(DealerCommissionPctText));
            }
            finally
            {
                _isUpdatingCommissionUi = false;
            }
        }

        // MARK: Commission conversions/clamping
        private double ApproxFinanced()
        {
            // Mirror UpdateDealerCommissionResolved base to avoid surprises
            double basePrice = TransactionPrice;
            double dpVal = string.Equals(DownPaymentUnit, "%", StringComparison.OrdinalIgnoreCase)
                ? basePrice * DownPaymentValueEntry / 100.0
                : DownPaymentValueEntry;

            var fin = Math.Max(0, basePrice - dpVal);
            return fin;
        }

        private void HandleCommissionUnitChange(string newUnit)
        {
            if (string.Equals(newUnit, "auto", StringComparison.OrdinalIgnoreCase))
            {
                DealerCommissionMode = "auto";
                DealerCommissionPct = null;
                DealerCommissionAmt = null;
                CommissionEntryValue = 0;
                IsCommissionEditorVisible = false;
                return;
            }

            DealerCommissionMode = "override";
            IsCommissionEditorVisible = true;
            SyncCommissionUiFromModel();

            var baseFinanced = ApproxFinanced();

            if (string.Equals(newUnit, "%", StringComparison.OrdinalIgnoreCase))
            {
                // We assume current CommissionEntryValue is THB; convert to %
                double fallbackPct = (AutoCommissionPct > 0 ? AutoCommissionPct * 100.0 : 3.0);
                CommissionEntryValue = UnitConversionHelper.MoneyToPercent(CommissionEntryValue, baseFinanced, fallbackPct);
            }
            else if (string.Equals(newUnit, "THB", StringComparison.OrdinalIgnoreCase))
            {
                // We assume current CommissionEntryValue is %; convert to THB
                double fallbackAmt = baseFinanced * (AutoCommissionPct > 0 ? AutoCommissionPct : 0.03);
                CommissionEntryValue = UnitConversionHelper.PercentToMoney(CommissionEntryValue, baseFinanced, fallbackAmt);
            }
        }

        private void SanitizeCommissionEntryValue()
        {
            if (string.Equals(CommissionEntryUnit, "auto", StringComparison.OrdinalIgnoreCase))
                return;

            double sanitized = CommissionEntryValue;

            if (string.Equals(CommissionEntryUnit, "%", StringComparison.OrdinalIgnoreCase))
            {
                sanitized = UnitConversionHelper.ClampPercent(CommissionEntryValue);
                DealerCommissionPct = sanitized / 100.0;
                DealerCommissionAmt = null;
            }
            else // THB
            {
                sanitized = UnitConversionHelper.SanitizeAmount(CommissionEntryValue);
                DealerCommissionAmt = sanitized;
                DealerCommissionPct = null;
            }

            if (!sanitized.Equals(CommissionEntryValue))
            {
                CommissionEntryValue = sanitized;
            }

            UpdateDealerCommissionResolved();
            OnPropertyChanged(nameof(DealerCommissionPctText));
        }
    }
}