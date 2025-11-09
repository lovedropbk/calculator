using System;
using System.ComponentModel;
using FinancialCalculator.WinUI3.ViewModels;

namespace FinancialCalculator.WinUI3.Services;

public sealed class LayoutCoordinator : IDisposable
{
    private MainViewModel? _vm;
    private AppSettingsService? _settings;
    private ViewModels.Layout.PaneStateViewModel? _leftPane;

    public void Attach(MainViewModel vm, AppSettingsService settings)
    {
        // Detach any previous
        Dispose();

        _vm = vm;
        _settings = settings;

        // Prefer using LayoutViewModel (decoupled from DealInput/RightPane specifics)
        _leftPane = _vm.Layout?.LeftPane;
        if (_leftPane != null)
        {
            _leftPane.PropertyChanged += OnLeftPanePropertyChanged;
        }
    }

    private void OnLeftPanePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_vm == null || _settings == null) return;
        if (e.PropertyName == nameof(ViewModels.Layout.PaneStateViewModel.IsCollapsed))
        {
            // If left collapsed and auto-expand is enabled, ensure right is expanded
            if (_vm.Layout?.LeftPane?.IsCollapsed == true && _settings.AutoExpandRightOnLeftCollapse)
            {
                if (_vm.Layout?.RightPane != null)
                {
                    _vm.Layout.RightPane.IsCollapsed = false;
                }
                else
                {
                    // Fallback to legacy property
                    _vm.IsCampaignDetailsCollapsed = false;
                }
            }
        }
    }

    public void Dispose()
    {
        if (_leftPane != null)
        {
            _leftPane.PropertyChanged -= OnLeftPanePropertyChanged;
            _leftPane = null;
        }
        _vm = null;
        _settings = null;
    }
}
