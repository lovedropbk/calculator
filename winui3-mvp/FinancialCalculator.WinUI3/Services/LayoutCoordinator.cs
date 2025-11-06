using System;
using System.ComponentModel;
using FinancialCalculator.WinUI3.ViewModels;

namespace FinancialCalculator.WinUI3.Services;

public sealed class LayoutCoordinator : IDisposable
{
    private MainViewModel? _vm;
    private AppSettingsService? _settings;

    public void Attach(MainViewModel vm, AppSettingsService settings)
    {
        _vm = vm;
        _settings = settings;
        if (_vm?.DealInput != null)
            _vm.DealInput.PropertyChanged += OnDealInputPropertyChanged;
    }

    private void OnDealInputPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_vm == null || _settings == null) return;
        if (e.PropertyName == nameof(DealInputViewModel.IsDealInputsCollapsed))
        {
            if (_vm.DealInput.IsDealInputsCollapsed && _settings.AutoExpandRightOnLeftCollapse)
            {
                _vm.IsCampaignDetailsCollapsed = false;
            }
        }
    }

    public void Dispose()
    {
        if (_vm?.DealInput != null)
            _vm.DealInput.PropertyChanged -= OnDealInputPropertyChanged;
        _vm = null;
        _settings = null;
    }
}
