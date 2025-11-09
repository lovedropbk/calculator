using CommunityToolkit.Mvvm.ComponentModel;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class MetricsViewModel : ObservableObject
{
    public string MonthlyInstallment { get; set; } = "";
    public string NominalRate { get; set; } = "";
    public string FlatRate { get; set; } = "";
    public string FinancedAmount { get; set; } = "";
    private string _roRAC = "";
    public string RoRAC
    {
        get => _roRAC;
        set => SetProperty(ref _roRAC, value);
    }
}
