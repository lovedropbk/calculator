using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.Engine.Models;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3.ViewModels
{
    // MARK: Payment Holidays - partial to keep DealInputViewModel under file-size limits
    public partial class DealInputViewModel
    {
        // User-defined payment holiday intervals; passed into ScenarioRequest
        public ObservableCollection<PaymentHolidayRule> PaymentHolidays { get; } = new();

        // Global Include Payment Holiday selection to apply to all campaigns
        [ObservableProperty]
        private int? includeHolidayMonths = null; // null=off; 3|6|9 months

        public string IncludeHolidaySelectionLabel
        {
            get
            {
                if (!IncludeHolidayMonths.HasValue || IncludeHolidayMonths.Value <= 0)
                    return ResourceHelper.GetString("DealInputs_IncludeHoliday_None.Text");
                return IncludeHolidayMonths.Value switch
                {
                    3 => ResourceHelper.GetString("DealInputs_IncludeHoliday_3M.Text"),
                    6 => ResourceHelper.GetString("DealInputs_IncludeHoliday_6M.Text"),
                    9 => ResourceHelper.GetString("DealInputs_IncludeHoliday_9M.Text"),
                    _ => $"{IncludeHolidayMonths.Value} months"
                };
            }
        }

        // Helper: Build merged holiday rules for scenario request
        private System.Collections.Generic.List<PaymentHolidayRule> BuildMergedHolidays()
        {
            var intervals = new System.Collections.Generic.List<(int s, int e)>();
            // From manual deal-level holidays
            foreach (var h in PaymentHolidays)
            {
                int s = Math.Clamp(h.StartPeriod, 1, TermMonths);
                int e = Math.Clamp(h.EndPeriod, 1, TermMonths);
                if (e >= s) intervals.Add((s, e));
            }
            // From global include selection
            if (IncludeHolidayMonths.HasValue && IncludeHolidayMonths.Value > 0)
            {
                int m = Math.Clamp(IncludeHolidayMonths.Value, 1, TermMonths);
                intervals.Add((1, m));
            }
            if (intervals.Count == 0) return new System.Collections.Generic.List<PaymentHolidayRule>();
            // Sort and coalesce overlaps/adjacent
            intervals.Sort((a, b) => a.s != b.s ? a.s.CompareTo(b.s) : a.e.CompareTo(b.e));
            var merged = new System.Collections.Generic.List<(int s, int e)>();
            foreach (var iv in intervals)
            {
                if (merged.Count == 0) { merged.Add(iv); continue; }
                var last = merged[merged.Count - 1];
                if (iv.s <= last.e + 1) // overlap or adjacent
                {
                    merged[merged.Count - 1] = (last.s, Math.Max(last.e, iv.e));
                }
                else
                {
                    merged.Add(iv);
                }
            }
            var outList = new System.Collections.Generic.List<PaymentHolidayRule>();
            int idx = 1;
            foreach (var m in merged)
            {
                outList.Add(new PaymentHolidayRule { StartPeriod = m.s, EndPeriod = m.e, RuleId = $"HOL-MERGED-{idx++:00}" });
            }
            return outList;
        }


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

        partial void OnIncludeHolidayMonthsChanged(int? value)
        {
            // This is a global override that instructs the system to include a base-level holiday for all campaigns
            // Implemented by MainViewModel when building standard campaigns; here we just notify changes
            InputsChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(IncludeHolidaySelectionLabel));
        }

        // Command: Set the global include-holiday months via dropdown selections
        [RelayCommand]
        private void SetIncludeHolidayMonths(int months)
        {
            IncludeHolidayMonths = months <= 0 ? null : months;
        }
    }
}