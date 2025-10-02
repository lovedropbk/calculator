using System;
using Microsoft.UI.Xaml;

namespace FinancialCalculator.WinUI3;

public partial class App : Application
{
    public App()
    {
        TryInitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Attempt to start or reuse embedded backend, then set FC_API_BASE for this process
        try
        {
            var (proc, baseUrl) = await Services.BackendLauncher.TryStartAsync(8223);
            Environment.SetEnvironmentVariable("FC_API_BASE", baseUrl);
            // Note: we intentionally do not keep a reference; backend will exit on app close because it runs as child.
        }
        catch
        {
            // Fallback: rely on existing FC_API_BASE or default in ApiClient
        }

        var window = new MainWindow();
        window.Activate();
    }

    private void TryInitializeComponent()
    {
        try
        {
            var mi = typeof(App).GetMethod("InitializeComponent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (mi != null)
            {
                mi.Invoke(this, null);
            }
            else
            {
                // Fallback for environments where XAML generator isn't running (linting contexts)
                var uri = new Uri("ms-appx:///App.xaml");
                Application.LoadComponent(this, uri);
            }
        }
        catch
        {
            // Safe no-op: app resources may not be fully loaded in lint-only contexts
        }
    }
}
