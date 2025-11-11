using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3;

public partial class App : Application
{
    private static AppSettingsService? _settings;
    public static AppSettingsService Settings => _settings ??= new AppSettingsService();
    private MainWindow? _window;
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
            // Initialize XAML. Language override will be applied in OnLaunched before creating the main window.
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
            Settings.Load();
            // Apply stored language before creating window
            try { Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = Settings.LanguageTag; } catch { }
            Settings.ApplyDensity();
            Logger.Info("Creating MainWindow - about to call constructor");
            _window = new MainWindow(Settings);
            Logger.Info("MainWindow constructor completed");

            var window = _window;

            Logger.Info("Waiting for MainViewModel to initialize...");
            await window.ViewModel.InitializationNotifier;
            Logger.Info("MainViewModel initialized successfully.");

            Logger.Info("About to activate MainWindow");
            window.Activate();
            Logger.Info("MainWindow activated successfully");

           // Hook language change to recreate window with new resources
           try
           {
               Settings.LanguageChanged -= OnLanguageChanged; // avoid multiple
               Settings.LanguageChanged += OnLanguageChanged;
           }
           catch { }

           // Theme is bound via XAML on the root Grid; avoid setting programmatically to preserve binding
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
    
    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        try
        {
            Logger.Info($"Language changed to: {Settings.LanguageTag}. Recreating window on UI thread.");
            var dispatcher = _window?.DispatcherQueue;
            if (dispatcher is null)
            {
                // Fallback if dispatcher is unavailable
                RecreateWindow();
                return;
            }
            dispatcher.TryEnqueue(() => RecreateWindow());
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to schedule window recreation after language change", ex);
        }
    }

    private async void RecreateWindow()
    {
        try
        {
            Logger.Info("Recreating MainWindow now...");
            var old = _window;
            _window = new MainWindow(Settings);
            
            // Wait for ViewModel initialization before activating
            await _window.ViewModel.InitializationNotifier;
            Logger.Info("New MainWindow ViewModel initialized after language change");
            
            _window.Activate();
            Logger.Info("New MainWindow activated after language change");
            
            try { old?.Close(); } catch { }
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to recreate window after language change", ex);
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


