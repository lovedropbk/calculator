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
        }
        catch
        {
            // ignore logging failures
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Logger.Info("App.OnLaunched - begin");

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
