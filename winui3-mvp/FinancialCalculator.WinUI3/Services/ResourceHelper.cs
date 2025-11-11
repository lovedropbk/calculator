using System;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FinancialCalculator.WinUI3.Services;

public static class ResourceHelper
{
    public static string GetString(string key)
    {
        try
        {
            var loader = new ResourceLoader();
            var s = loader.GetString(key);
            if (!string.IsNullOrEmpty(s)) return s;
        }
        catch { }
        return key;
    }
}
