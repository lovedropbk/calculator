using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FinancialCalculator.WinUI3.ViewModels;

namespace FinancialCalculator.WinUI3;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        ViewModel = new MainViewModel();
        Root.DataContext = ViewModel;
    }
}
