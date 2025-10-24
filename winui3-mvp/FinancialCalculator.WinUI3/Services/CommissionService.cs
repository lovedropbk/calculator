using System;

namespace FinancialCalculator.WinUI3.Services;

public class CommissionService : ICommissionService
{
    public string PolicyVersion => "local-v1";

    public double GetAutoCommissionPct(string product)
    {
        var p = (product ?? string.Empty).Trim().ToUpperInvariant();
        return p == "HP" ? 0.03 : 0.07;
    }
}