using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using FinancialCalculator.WinUI3.ViewModels;
using FinancialCalculator.WinUI3.Services;
using WinRT.Interop;
using System.Collections.Generic;

namespace FinancialCalculator.WinUI3;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        // Important: initialize ViewModel BEFORE InitializeComponent so x:Bind can resolve during load
        ViewModel = new MainViewModel();

        try
        {
            Logger.Info("MainWindow: InitializeComponent start");
            InitializeComponent();
            Logger.Info("MainWindow: InitializeComponent end");
        }
        catch (Microsoft.UI.Xaml.Markup.XamlParseException xpe)
        {
            Logger.Error("XamlParseException in MainWindow.InitializeComponent", xpe);
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception in MainWindow.InitializeComponent", ex);
            throw;
        }

        // Ensure Content DataContext (in case template swapped)
        if (this.Content is FrameworkElement fe)
        {
            fe.DataContext = ViewModel;
        }

        TryApplySystemBackdrop();
        CustomizeTitleBar();
    }

    private void OnStandardCopyClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.DataContext is CampaignSummaryViewModel row)
            {
                // Copy to My Campaigns (existing behavior)
                if (ViewModel.CopyToMyCampaignsCommand.CanExecute(row))
                {
                    ViewModel.CopyToMyCampaignsCommand.Execute(row);
                }

                // Also offer to add to Campaign Designer when available
                if (ViewModel.CopyToDesignerCommand?.CanExecute(row) ?? false)
                {
                    ViewModel.CopyToDesignerCommand.Execute(row);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Copy click handler failed", ex);
        }
    }

    // Remove tile (X button) with confirmation dialog
    private async void OnDesignerRemoveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.DataContext is CampaignTileViewModel tile)
            {
                var dialog = new ContentDialog
                {
                    Title = "Delete campaign?",
                    Content = $"Are you sure you want to delete '{tile.Title}'?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = (this.Content as FrameworkElement)?.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    ViewModel.Comparison.DesignerCampaigns.Remove(tile);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Remove designer tile failed", ex);
        }
    }

    private void OnEscapeInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.GoalSeek.IsOpen || ViewModel.GoalSeek.IsTargetSet)
        {
            ViewModel.GoalSeek.CloseCommand.Execute(null);
            args.Handled = true;
        }
    }

    // Duplicate handler removed. See earlier OnDesignerRemoveClick implementation.

    private void TryApplySystemBackdrop()
    {
        // Apply Mica for a modern look; safely ignore if not supported on thisOS
        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch
        {
            // no-op
        }
    }

    private void CustomizeTitleBar()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.Title = "Financial Calculator";
                var titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
        }
        catch
        {
            // Safe no-op on environments that don't support AppWindow (older Windows)
        }
    }
}
