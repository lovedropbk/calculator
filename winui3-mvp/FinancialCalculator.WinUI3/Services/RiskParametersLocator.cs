using System;
using System.IO;

namespace FinancialCalculator.WinUI3.Services;

public static class RiskParametersLocator
{
    public static string GetPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        
        // 1. Try relative to baseDir (deployment)
        var path = Path.Combine(baseDir, "docs", "parameters");
        if (Directory.Exists(path)) return path;

        // 2. Walk up to find 'winui3-mvp' folder or 'docs' folder (dev environment)
        var current = new DirectoryInfo(baseDir);
        int maxDepth = 10;
        while (current != null && maxDepth-- > 0)
        {
             var check = Path.Combine(current.FullName, "winui3-mvp", "docs", "parameters");
             if (Directory.Exists(check)) return check;
             
             check = Path.Combine(current.FullName, "docs", "parameters");
             if (Directory.Exists(check)) return check;

             current = current.Parent;
        }

        // Fallback to a default if nothing found (might fail later but better than nothing)
        return Path.Combine(baseDir, "Parameters");
    }
}