using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.WinUI3.ViewModels
{
    // MARK: Payment Holidays - partial to keep DealInputViewModel under file-size limits
    public partial class DealInputViewModel
    {
        // User-defined payment holiday intervals; passed into ScenarioRequest
        public ObservableCollection<PaymentHolidayRule> PaymentHolidays { get; } = new();

        // MARK: Manual selector state
        [ObservableProperty]
        private int holidayStart = 1;

        [ObservableProperty]
        private int holidayMonths = 3;

        [ObservableProperty]
        private string? holidayError = string.Empty;

        private int ComputeNextStartPeriod()
        {
            if (PaymentHolidays.Count == 0) return 1;
            var lastEnd = PaymentHolidays.Max(h => h.EndPeriod);
            return Math.Min(TermMonths, lastEnd + 1);
        }

        // Adds a holiday with monthly capitalization and unchanged maturity, enforcing non-overlap
        private bool TryAddHoliday(int startPeriod, int months)
        {
            if (months <= 0) return false;
            if (TermMonths <= 0) return false;

            int s = Math.Clamp(startPeriod, 1, TermMonths);
            int e = Math.Clamp(s + months - 1, 1, TermMonths);
            if (e < s) return false;

            // Disallow overlap
            bool overlaps = PaymentHolidays.Any(h => !(e < h.StartPeriod || s > h.EndPeriod));
            if (overlaps) return false;

            PaymentHolidays.Add(new PaymentHolidayRule
            {
                StartPeriod = s,
                EndPeriod = e,
                RuleId = $"HOL-{DateTime.Now:HHmmssfff}"
            });

            // Trigger recalculation via existing event pipeline
            InputsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        // Quick-picks: 3, 6, 9 months beginning at the next available period
        [RelayCommand]
        private void AddHoliday3() => TryAddHoliday(ComputeNextStartPeriod(), 3);

        [RelayCommand]
        private void AddHoliday6() => TryAddHoliday(ComputeNextStartPeriod(), 6);

        [RelayCommand]
        private void AddHoliday9() => TryAddHoliday(ComputeNextStartPeriod(), 9);

        // Manual From/Months apply
        [RelayCommand]
        private void AddHolidayCustom()
        {
            HolidayError = string.Empty;
            var ok = TryAddHoliday(HolidayStart, HolidayMonths);
            if (!ok)
            {
                HolidayError = "Invalid or overlapping range. Ensure within term and non-overlapping.";
            }
        }

        [RelayCommand]
        private void ClearHolidays()
        {
            PaymentHolidays.Clear();
            HolidayError = string.Empty;
            InputsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}