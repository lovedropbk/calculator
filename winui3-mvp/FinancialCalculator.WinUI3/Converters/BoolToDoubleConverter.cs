using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FinancialCalculator.WinUI3.Converters
{
    // Maps a bool to a double using optional ConverterParameter "trueValue|falseValue"
    // Example: MinWidth="{Binding IsCollapsed, Converter={StaticResource BoolToDoubleConverter}, ConverterParameter='36|360'}"
    public sealed partial class BoolToDoubleConverter : IValueConverter
    {
        public double TrueValue { get; set; } = 0.0;
        public double FalseValue { get; set; } = 0.0;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool flag = value is bool b && b;
            var (t, f) = ParseParam(parameter as string);
            return flag ? t : f;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is double d)
            {
                return d.Equals(TrueValue);
            }
            return false;
        }

        private static (double, double) ParseParam(string? param)
        {
            if (string.IsNullOrWhiteSpace(param))
            {
                return (0.0, 0.0);
            }

            var parts = param.Split('|', StringSplitOptions.RemoveEmptyEntries);
            var t = parts.Length > 0 ? ParseDouble(parts[0]) : 0.0;
            var f = parts.Length > 1 ? ParseDouble(parts[1]) : 0.0;
            return (t, f);
        }

        private static double ParseDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0.0;
            if (double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                return d;
            }
            return 0.0;
        }
    }
}