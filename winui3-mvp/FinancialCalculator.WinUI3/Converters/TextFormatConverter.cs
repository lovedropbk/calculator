using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace FinancialCalculator.WinUI3.Converters
{
    // Format a bound value using a ConverterParameter format string, e.g. "Show breakdown for {0}"
    public sealed class TextFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var fmt = parameter as string;
            if (string.IsNullOrWhiteSpace(fmt))
                fmt = "{0}";

            return string.Format(CultureInfo.InvariantCulture, fmt, value ?? string.Empty);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotSupportedException("TextFormatConverter does not support ConvertBack.");
    }
}