using System;
using Microsoft.UI.Xaml.Data;

namespace FinancialCalculator.WinUI3.Converters;

public sealed class StringEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var lhs = value?.ToString() ?? string.Empty;
        var rhs = parameter?.ToString() ?? string.Empty;
        return string.Equals(lhs, rhs, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        // not used for this scenario
        return null!;
    }
}
