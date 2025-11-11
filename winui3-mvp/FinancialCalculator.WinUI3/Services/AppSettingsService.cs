using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.ApplicationModel.Resources.Core;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FinancialCalculator.WinUI3.Services;

using Windows.Globalization;

public enum AppDensity
{
    Compact,
    Comfortable
}

public sealed class AppSettingsService
{
    private const string KeyAutoExpand = "AutoExpandRightOnLeftCollapse";
    private const string KeyTheme = "AppTheme";
    private const string KeyDensity = "AppDensity";
    private const string KeyLanguage = "AppLanguage";

    public event EventHandler? LanguageChanged;

    private static string SettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FinancialCalculator",
        "settings.json");

    public bool AutoExpandRightOnLeftCollapse { get; set; } = true;
    public ElementTheme AppTheme { get; private set; } = ElementTheme.Default;
    public AppDensity Density { get; private set; } = AppDensity.Compact;
    public string LanguageTag { get; private set; } = "en-US";

    public void Load()
    {
        // Try packaged LocalSettings first
        try
        {
            var ls = ApplicationData.Current.LocalSettings;
            if (ls.Values.TryGetValue(KeyAutoExpand, out var a) && a is bool b)
                AutoExpandRightOnLeftCollapse = b;
            if (ls.Values.TryGetValue(KeyTheme, out var t) && t is string ts && Enum.TryParse<ElementTheme>(ts, out var theme))
                AppTheme = theme;
            if (ls.Values.TryGetValue(KeyDensity, out var d) && d is string ds && Enum.TryParse<AppDensity>(ds, out var density))
                Density = density;
            if (ls.Values.TryGetValue(KeyLanguage, out var l) && l is string lang && !string.IsNullOrWhiteSpace(lang))
            {
                LanguageTag = lang;
            }
            return;
        }
        catch (InvalidOperationException)
        {
            // Unpackaged: fall back to file
        }

        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var dto = JsonSerializer.Deserialize<SettingsDto>(json);
                if (dto != null)
                {
                    AutoExpandRightOnLeftCollapse = dto.AutoExpandRightOnLeftCollapse;
                    if (Enum.TryParse<ElementTheme>(dto.AppTheme, out var t2)) AppTheme = t2;
                    if (Enum.TryParse<AppDensity>(dto.AppDensity, out var d2)) Density = d2;
                    if (!string.IsNullOrWhiteSpace(dto.AppLanguage)) LanguageTag = dto.AppLanguage;
                }
            }
        }
        catch { }
    }

    public void Save()
    {
        // Try packaged LocalSettings first
        try
        {
            var ls = ApplicationData.Current.LocalSettings;
            ls.Values[KeyAutoExpand] = AutoExpandRightOnLeftCollapse;
            ls.Values[KeyTheme] = AppTheme.ToString();
            ls.Values[KeyDensity] = Density.ToString();
            ls.Values[KeyLanguage] = LanguageTag;
            return;
        }
        catch (InvalidOperationException)
        {
            // Unpackaged: fall back to file
        }

        try
        {
            var dto = new SettingsDto
            {
                AutoExpandRightOnLeftCollapse = AutoExpandRightOnLeftCollapse,
                AppTheme = AppTheme.ToString(),
                AppDensity = Density.ToString(),
                AppLanguage = LanguageTag
            };
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch { }
    }

    public void SetTheme(ElementTheme theme)
    {
        AppTheme = theme;
        Save();
    }

    public void SetLanguage(string languageTag)
    {
        try
        {
            // Persist and set language
            LanguageTag = languageTag;
            Save();

            // Update resource qualifiers so new XAML resolves to the selected language
            try { Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", languageTag); } catch { }
            try { ApplicationLanguages.PrimaryLanguageOverride = languageTag; } catch { }

            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
        catch { }
    }

    public void ApplyTheme(FrameworkElement root)
    {
        if (root != null) root.RequestedTheme = AppTheme;
    }

    public void SetDensity(AppDensity mode)
    {
        Density = mode;
        Save();
        ApplyDensity();
    }

    public void ApplyDensity()
    {
        try
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            // Remove any existing density dictionaries
            for (int i = dictionaries.Count - 1; i >= 0; i--)
            {
                var src = dictionaries[i].Source?.ToString() ?? string.Empty;
                if (src.EndsWith("Styles/Density.Compact.xaml", StringComparison.OrdinalIgnoreCase) || src.EndsWith("Styles/Density.Comfortable.xaml", StringComparison.OrdinalIgnoreCase))
                {
                    dictionaries.RemoveAt(i);
                }
            }

            var uri = Density == AppDensity.Comfortable
                ? new Uri("ms-appx:///Styles/Density.Comfortable.xaml")
                : new Uri("ms-appx:///Styles/Density.Compact.xaml");
            dictionaries.Add(new ResourceDictionary { Source = uri });
        }
        catch { }
    }

    private sealed class SettingsDto
    {
        public bool AutoExpandRightOnLeftCollapse { get; set; } = true;
        public string AppTheme { get; set; } = "Default";
        public string AppDensity { get; set; } = "Compact";
        public string AppLanguage { get; set; } = "en-US";
    }
}
