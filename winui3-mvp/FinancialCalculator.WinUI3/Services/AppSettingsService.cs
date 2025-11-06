using Microsoft.UI.Xaml;
using Windows.Storage;

namespace FinancialCalculator.WinUI3.Services;

public sealed class AppSettingsService
{
    private const string KeyAutoExpand = "AutoExpandRightOnLeftCollapse";
    private const string KeyTheme = "AppTheme";

    public bool AutoExpandRightOnLeftCollapse { get; set; } = true;
    public ElementTheme AppTheme { get; private set; } = ElementTheme.Default;

    public void Load()
    {
        var ls = ApplicationData.Current.LocalSettings;
        if (ls.Values.TryGetValue(KeyAutoExpand, out var a) && a is bool b)
            AutoExpandRightOnLeftCollapse = b;
        if (ls.Values.TryGetValue(KeyTheme, out var t) && t is string ts && Enum.TryParse<ElementTheme>(ts, out var theme))
            AppTheme = theme;
    }

    public void Save()
    {
        var ls = ApplicationData.Current.LocalSettings;
        ls.Values[KeyAutoExpand] = AutoExpandRightOnLeftCollapse;
        ls.Values[KeyTheme] = AppTheme.ToString();
    }

    public void SetTheme(ElementTheme theme)
    {
        AppTheme = theme;
        Save();
    }

    public void ApplyTheme(FrameworkElement root)
    {
        if (root != null) root.RequestedTheme = AppTheme;
    }
}
