using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using FinancialCalculator.WinUI3.ViewModels;
using WinRT.Interop;
using System.Collections.Generic;

namespace FinancialCalculator.WinUI3;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel();

        if (this.Content is FrameworkElement fe)
        {
            fe.DataContext = ViewModel;
        }

        TryApplySystemBackdrop();
        CustomizeTitleBar();
    }

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
