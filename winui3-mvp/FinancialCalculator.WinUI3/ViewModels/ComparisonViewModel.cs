using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class ComparisonViewModel : ObservableObject
{
    public ObservableCollection<DealComparisonItemViewModel> ComparedDeals { get; } = new();

    [RelayCommand]
    private void RemoveDeal(DealComparisonItemViewModel deal)
    {
        if (ComparedDeals.Contains(deal))
        {
            ComparedDeals.Remove(deal);
        }
    }

    [RelayCommand]
    private void ClearComparison()
    {
        ComparedDeals.Clear();
    }
}

public partial class DealComparisonItemViewModel : ObservableObject
{
    public string Title { get; set; } = "Scenario";
    
    // Key Inputs
    public string VehicleName { get; set; } = "";
    public string Product { get; set; } = "";
    public string Price { get; set; } = "";
    public string DownPayment { get; set; } = "";
    public int Term { get; set; }
    public string NominalRate { get; set; } = "";
    public string FlatRate { get; set; } = "";
    public string Balloon { get; set; } = "";

    // Key Outputs
    public string MonthlyInstallment { get; set; } = "";
    public string FinancedAmount { get; set; } = "";
    public string TotalInterest { get; set; } = "";
    public string RoRAC { get; set; } = "";

    // Waterfall Data
    public ObservableCollection<WaterfallStepViewModel> WaterfallSteps { get; } = new();
}

public class WaterfallStepViewModel
{
    public string Label { get; set; } = "";
    public double Value { get; set; }
    public string FormattedValue { get; set; } = "";
    public bool IsTotal { get; set; }
    public string ColorHex { get; set; } = "#FF0078D7"; // Default blue
    public double HeightFactor { get; set; } // For UI relative sizing
}