using CommunityToolkit.Mvvm.ComponentModel;
using FinancialCalculator.WinUI3.Services;
using Microsoft.UI.Xaml;

namespace FinancialCalculator.WinUI3.ViewModels.Settings;

public partial class SettingsViewModel : ObservableObject
{
    public AppSettingsService Service { get; }

    public SettingsViewModel(AppSettingsService service)
    {
        Service = service;
    }

    public bool AutoExpandRightOnLeftCollapse
    {
        get => Service.AutoExpandRightOnLeftCollapse;
        set
        {
            if (Service.AutoExpandRightOnLeftCollapse != value)
            {
                Service.AutoExpandRightOnLeftCollapse = value;
                OnPropertyChanged();
                Service.Save();
            }
        }
    }

    public ElementTheme Theme
    {
        get => Service.AppTheme;
        set
        {
            if (Service.AppTheme != value)
            {
                Service.SetTheme(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsThemeDefault));
                OnPropertyChanged(nameof(IsThemeLight));
                OnPropertyChanged(nameof(IsThemeDark));
            }
        }
    }

    public bool IsThemeDefault
    {
        get => Theme == ElementTheme.Default;
        set { if (value) Theme = ElementTheme.Default; }
    }
    public bool IsThemeLight
    {
        get => Theme == ElementTheme.Light;
        set { if (value) Theme = ElementTheme.Light; }
    }
    public bool IsThemeDark
    {
        get => Theme == ElementTheme.Dark;
        set { if (value) Theme = ElementTheme.Dark; }
    }

    public AppDensity Density
    {
        get => Service.Density;
        set
        {
            if (Service.Density != value)
            {
                Service.SetDensity(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDensityCompact));
                OnPropertyChanged(nameof(IsDensityComfortable));
            }
        }
    }
    public bool IsDensityCompact
    {
        get => Density == AppDensity.Compact;
        set { if (value) Density = AppDensity.Compact; }
    }
    public bool IsDensityComfortable
    {
        get => Density == AppDensity.Comfortable;
        set { if (value) Density = AppDensity.Comfortable; }
    }

    public void Load() => Service.Load();
    public void Save() => Service.Save();
    public void ApplyTheme(FrameworkElement root) => Service.ApplyTheme(root);
}
