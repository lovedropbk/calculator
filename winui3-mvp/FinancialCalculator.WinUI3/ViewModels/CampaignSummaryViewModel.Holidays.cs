using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.Engine.Models;

namespace FinancialCalculator.WinUI3.ViewModels
{
    // MARK: Campaign-level Payment Holidays
    // Per-campaign storage and commands so cashflow adjustments are tied to a specific campaign
    public partial class CampaignSummaryViewModel : ObservableObject
    {
        // Campaign-specific holidays
        public ObservableCollection<PaymentHolidayRule> PaymentHolidays { get; } = new();

        // Manual selector state
        [ObservableProperty]
        private int holidayStart = 1;

        [ObservableProperty]
        private int holidayMonths = 3;

        [ObservableProperty]
        private string? holidayError = string.Empty;

        // Compute the next available start period based on existing rules
        private int NextStart()
            => PaymentHolidays.Count == 0 ? 1 : PaymentHolidays.Max(h => h.EndPeriod) + 1;

        private bool TryAddHoliday(int startPeriod, int months)
        {
            HolidayError = string.Empty;

            if (months <= 0)
            {
                HolidayError = "Months must be greater than zero";
                return false;
            }
            if (startPeriod <= 0)
            {
                HolidayError = "From must be at least 1";
                return false;
            }

            int s = Math.Max(1, startPeriod);
            int e = s + months - 1;

            // Disallow overlap with existing entries
            bool overlaps = PaymentHolidays.Any(h => !(e < h.StartPeriod || s > h.EndPeriod));
            if (overlaps)
            {
                HolidayError = "Range overlaps with an existing holiday";
                return false;
            }

            PaymentHolidays.Add(new PaymentHolidayRule
            {
                StartPeriod = s,
                EndPeriod = e,
                RuleId = $"HOL-{DateTime.Now:HHmmssfff}"
            });
            return true;
        }

        // Quick-picks
        [RelayCommand]
        private void AddHoliday3() => TryAddHoliday(NextStart(), 3);

        [RelayCommand]
        private void AddHoliday6() => TryAddHoliday(NextStart(), 6);

        [RelayCommand]
        private void AddHoliday9() => TryAddHoliday(NextStart(), 9);

        // Manual (From + Months)
        [RelayCommand]
        private void AddHolidayCustom()
        {
            var ok = TryAddHoliday(HolidayStart, HolidayMonths);
            if (!ok && string.IsNullOrWhiteSpace(HolidayError))
            {
                HolidayError = "Invalid or overlapping range";
            }
        }

        // Clear all for the campaign
        [RelayCommand]
        private void ClearHolidays()
        {
            HolidayError = string.Empty;
            PaymentHolidays.Clear();
        }
    }
}