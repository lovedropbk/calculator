using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3;

public partial class App : Application
{
    public App()
    {
        Logger.Init("ui");
        this.UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
        {
            try { Logger.Warn($"FirstChance: {e.Exception.GetType().Name}: {e.Exception.Message}"); } catch {}
        };
        try
        {
            this.InitializeComponent();
            Logger.Info("App.InitializeComponent loaded resources");
        }
        catch (Microsoft.UI.Xaml.Markup.XamlParseException xpe)
        {
            Logger.Error("XamlParseException during App.InitializeComponent", xpe);
            throw;
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            Logger.Error("App UnhandledException", e.Exception);
        }
        catch
        {
            // ignore logging failures
        }
        // allow default behavior
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Logger.Info("App.OnLaunched - begin");

        // Respect FC_API_BASE if already set by launcher, otherwise try auto-start on 8123
        var existingBase = Environment.GetEnvironmentVariable("FC_API_BASE");
        if (string.IsNullOrWhiteSpace(existingBase))
        {
            try
            {
                var (proc, baseUrl) = await BackendLauncher.TryStartAsync(8123);
                Environment.SetEnvironmentVariable("FC_API_BASE", baseUrl);
                Logger.Info($"Backend ready at {baseUrl} (proc={(proc != null ? "spawned" : "reused")})");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Backend start failed, falling back to default. {ex}");
            }
        }
        else
        {
            Logger.Info($"Using existing FC_API_BASE: {existingBase}");
        }

        try
        {
            var window = new MainWindow();
            window.Activate();
            Logger.Info("MainWindow activated");
        }
        catch (XamlParseException xpe)
        {
            Logger.Error("XamlParseException creating MainWindow", xpe);
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception creating MainWindow", ex);
            throw;
        }
    }
}
