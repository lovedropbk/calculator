using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FinancialCalculator.WinUI3.Converters
{
    // Maps a bool to a GridLength using an optional ConverterParameter:
    // - Parameter format: "trueValue|falseValue"
    // - Values can be: "Auto", "1*" (star), "2*" (star), "0.5*" (star), or a pixel number like "36"
    public sealed partial class BoolToGridLengthConverter : IValueConverter
    {
        public GridLength TrueLength { get; set; } = new GridLength(1, GridUnitType.Auto);
        public GridLength FalseLength { get; set; } = new GridLength(1, GridUnitType.Star);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool flag = value is bool b && b;
            var (t, f) = ParseParam(parameter as string);
            return flag ? t : f;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is GridLength gl)
            {
                return gl.Equals(TrueLength);
            }
            return false;
        }

        private static (GridLength, GridLength) ParseParam(string? param)
        {
            if (string.IsNullOrWhiteSpace(param))
            {
                return (GridLength.Auto, new GridLength(1, GridUnitType.Star));
            }

            var parts = param.Split('|', StringSplitOptions.RemoveEmptyEntries);
            var t = parts.Length > 0 ? ParseGridLength(parts[0]) : new GridLength(1, GridUnitType.Auto);
            var f = parts.Length > 1 ? ParseGridLength(parts[1]) : new GridLength(1, GridUnitType.Star);
            return (t, f);
        }

        private static GridLength ParseGridLength(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return GridLength.Auto;

            s = s.Trim();
            if (s.Equals("Auto", StringComparison.OrdinalIgnoreCase)) return GridLength.Auto;

            if (s.EndsWith("*", StringComparison.Ordinal))
            {
                var weightStr = s.Substring(0, s.Length - 1).Trim();
                if (string.IsNullOrEmpty(weightStr)) return new GridLength(1, GridUnitType.Star);
                if (double.TryParse(weightStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var star))
                {
                    return new GridLength(star, GridUnitType.Star);
                }
                return new GridLength(1, GridUnitType.Star);
            }

            // pixels
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var px))
            {
                return new GridLength(px, GridUnitType.Pixel);
            }

            return GridLength.Auto;
        }
    }
}