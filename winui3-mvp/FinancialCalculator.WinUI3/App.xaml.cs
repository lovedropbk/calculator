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
        
        // Set up global exception handlers
        this.UnhandledException += App_UnhandledException;
        
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try 
            { 
                var ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    Logger.LogUnhandledException(ex);
                }
                else
                {
                    Logger.Error($"AppDomain.UnhandledException: {e.ExceptionObject}");
                }
            } 
            catch {}
        };
        
        AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
        {
            try 
            { 
                // Only log warnings for non-trivial exceptions
                if (e.Exception != null && 
                    !e.Exception.GetType().Name.Contains("Cancel") &&
                    !e.Exception.Message.Contains("OperationCanceled"))
                {
                    Logger.Debug($"FirstChance: {e.Exception.GetType().Name}: {e.Exception.Message}");
                }
            } 
            catch {}
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
        catch (Exception ex)
        {
            Logger.Error("Exception during App initialization", ex);
            throw;
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            Logger.LogUnhandledException(e.Exception);
            
            // Try to prevent app crash for recoverable errors
            if (e.Exception is HttpRequestException || 
                e.Exception is TaskCanceledException ||
                e.Exception.Message.Contains("API Error"))
            {
                e.Handled = true;
                Logger.Info("Handled recoverable exception - app continues");
            }
        }
        catch
        {
            // ignore logging failures
        }
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
                Logger.Warn($"Backend start failed, falling back to default. {ex.Message}");
                // Set a default value to prevent null reference
                Environment.SetEnvironmentVariable("FC_API_BASE", "http://localhost:8123/");
            }
        }
        else
        {
            Logger.Info($"Using existing FC_API_BASE: {existingBase}");
        }

        try
        {
            Logger.Info("Creating MainWindow");
            var window = new MainWindow();
            window.Activate();
            Logger.Info("MainWindow activated successfully");
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
