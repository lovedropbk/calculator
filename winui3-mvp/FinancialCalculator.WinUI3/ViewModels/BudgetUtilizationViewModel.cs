using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class BudgetUtilizationViewModel : ObservableObject
{
    // Using GridLength to support proportional sizing in XAML
    public GridLength CashDiscountPct { get; set; } = new(0, GridUnitType.Star);
    public GridLength SubDownPct { get; set; } = new(0, GridUnitType.Star);
    public GridLength RateSubsidyPct { get; set; } = new(0, GridUnitType.Star);
    public GridLength IdcPct { get; set; } = new(0, GridUnitType.Star);
    public GridLength UnallocatedPct { get; set; } = new(1, GridUnitType.Star); // Default all unallocated
}
