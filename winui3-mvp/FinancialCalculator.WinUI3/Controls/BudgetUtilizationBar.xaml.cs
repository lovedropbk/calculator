using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FinancialCalculator.WinUI3.Controls;

public sealed partial class BudgetUtilizationBar : UserControl
{
    public BudgetUtilizationBar()
    {
        this.InitializeComponent();
    }

    public FinancialCalculator.WinUI3.ViewModels.MainViewModel ViewModel
    {
        get => (FinancialCalculator.WinUI3.ViewModels.MainViewModel)((FrameworkElement)Window.Current.Content).DataContext;
    }
}
