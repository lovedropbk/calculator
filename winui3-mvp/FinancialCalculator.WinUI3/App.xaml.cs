using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3;

public partial class App : Application
{
    private readonly AppSettingsService _settings = new();
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

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Logger.Info("App.OnLaunched - begin");

        try
        {
            // Load settings and apply density before creating window
            _settings.Load();
            _settings.ApplyDensity();
            Logger.Info("Creating MainWindow - about to call constructor");
            var window = new MainWindow();
            Logger.Info("MainWindow constructor completed");

            Logger.Info("Waiting for MainViewModel to initialize...");
            await window.ViewModel.InitializationNotifier;
            Logger.Info("MainViewModel initialized successfully.");

            Logger.Info("About to activate MainWindow");
            window.Activate();
            Logger.Info("MainWindow activated successfully");

            // Apply theme preference after window exists
            if (window.Content is FrameworkElement fe)
                _settings.ApplyTheme(fe);

            // Add a visual indicator if window is created but not showing content
            if (window.Content == null)
            {
                Logger.Error("WARNING: MainWindow.Content is null after activation!");
            }
            else
            {
                Logger.Info($"MainWindow.Content type: {window.Content.GetType().Name}");
            }
        }
        catch (Microsoft.UI.Xaml.Markup.XamlParseException xpe)
        {
            Logger.Error($"XamlParseException creating MainWindow: {xpe.Message}", xpe);
            if (xpe.InnerException != null)
            {
                Logger.Error($"  Inner: {xpe.InnerException.Message}");
            }

            // Show a fallback error window
            ShowErrorWindow($"XAML Parse Error: {xpe.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception creating MainWindow: {ex.Message}", ex);
            Logger.Error($"  StackTrace: {ex.StackTrace}");
            
            // Show a fallback error window
            ShowErrorWindow($"Initialization Error: {ex.Message}");
            throw;
        }
    }
    
    private void ShowErrorWindow(string message)
    {
        try
        {
            var errorWindow = new Window();
            errorWindow.Content = new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = $"Failed to start application:\n\n{message}\n\nCheck logs for details.",
                Margin = new Microsoft.UI.Xaml.Thickness(20),
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
            };
            errorWindow.Activate();
        }
        catch
        {
            // Even the error window failed, just log it
            Logger.Error($"Could not show error window: {message}");
        }
    }
}


