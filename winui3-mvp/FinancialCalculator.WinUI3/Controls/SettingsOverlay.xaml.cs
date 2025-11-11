using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FinancialCalculator.WinUI3;

namespace FinancialCalculator.WinUI3.Controls;

public sealed partial class SettingsOverlay : UserControl
{
    public MainWindow? HostWindow { get; set; }

    public SettingsOverlay()
    {
        this.InitializeComponent();
        Loaded += SettingsOverlay_Loaded;
    }

    private void SettingsOverlay_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Nav.SelectedItem = Nav.MenuItems[0];
            ShowPanel("display");
        }
        catch { }
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString() ?? "display";
        ShowPanel(tag);
    }

    private void ShowPanel(string tag)
    {
        DisplayPanel.Visibility = tag == "display" ? Visibility.Visible : Visibility.Collapsed;
        BehaviorPanel.Visibility = tag == "behavior" ? Visibility.Visible : Visibility.Collapsed;
        LanguagePanel.Visibility = tag == "language" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnLanguageEnglishChecked(object sender, RoutedEventArgs e)
    {
        try 
        { 
            if (HostWindow?.ViewModel?.Settings != null)
                HostWindow.ViewModel.Settings.LanguageTag = "en-US"; 
        } 
        catch { }
    }

    private void OnLanguageThaiChecked(object sender, RoutedEventArgs e)
    {
        try 
        { 
            if (HostWindow?.ViewModel?.Settings != null)
                HostWindow.ViewModel.Settings.LanguageTag = "th-TH"; 
        } 
        catch { }
    }
}
