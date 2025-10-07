using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FinancialCalculator.WinUI3.Converters
{
    public sealed partial class StringEqualsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var a = value?.ToString() ?? string.Empty;
            var b = parameter?.ToString() ?? string.Empty;
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}