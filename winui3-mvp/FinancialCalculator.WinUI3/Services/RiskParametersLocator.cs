using System;
using System.IO;

namespace FinancialCalculator.WinUI3.Services;

public static class RiskParametersLocator
{
    public static string GetPath()
    {
        return PathResolver.GetParametersDirectory();
    }
}