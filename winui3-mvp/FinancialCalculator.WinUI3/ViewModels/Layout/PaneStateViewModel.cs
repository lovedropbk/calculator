using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinancialCalculator.WinUI3.ViewModels.Layout;

public sealed partial class PaneStateViewModel : ObservableObject, IDisposable
{
    private readonly Func<bool> _get;
    private readonly Action<bool> _set;
    private readonly INotifyPropertyChanged? _source;
    private readonly string? _propertyName;

    public PaneStateViewModel(Func<bool> get, Action<bool> set, INotifyPropertyChanged? source = null, string? propertyName = null)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _set = set ?? throw new ArgumentNullException(nameof(set));
        _source = source;
        _propertyName = propertyName;

        if (_source != null && !string.IsNullOrWhiteSpace(_propertyName))
        {
            _source.PropertyChanged += OnSourcePropertyChanged;
        }
    }

    public bool IsCollapsed
    {
        get => _get();
        set
        {
            if (_get() != value)
            {
                _set(value);
                OnPropertyChanged(nameof(IsCollapsed));
            }
        }
    }

    [RelayCommand]
    private void Toggle() => IsCollapsed = !IsCollapsed;

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(_propertyName) || e.PropertyName == _propertyName)
        {
            OnPropertyChanged(nameof(IsCollapsed));
        }
    }

    public void Dispose()
    {
        if (_source != null && !string.IsNullOrWhiteSpace(_propertyName))
        {
            try { _source.PropertyChanged -= OnSourcePropertyChanged; } catch { }
        }
    }
}
