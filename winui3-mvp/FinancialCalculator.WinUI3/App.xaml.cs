using System;
using Microsoft.UI.Xaml;

namespace FinancialCalculator.WinUI3;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Attempt to start or reuse embedded backend, then set FC_API_BASE for this process
        try
        {
            var (proc, baseUrl) = await Services.BackendLauncher.TryStartAsync(8223);
            Environment.SetEnvironmentVariable("FC_API_BASE", baseUrl);
        }
        catch
        {
            // Fallback: rely on existing FC_API_BASE or default in ApiClient
        }

        var window = new MainWindow();
        window.Activate();
    }
}
