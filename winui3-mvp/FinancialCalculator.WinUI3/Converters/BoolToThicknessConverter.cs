using System;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FinancialCalculator.WinUI3.Converters
{
    public sealed partial class BoolToThicknessConverter : IValueConverter
    {
        public Thickness TrueThickness { get; set; } = new Thickness(16,16,16,16);
        public Thickness FalseThickness { get; set; } = new Thickness(4,4,4,4);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool flag = ToBool(value);
            var (t, f) = ParseParam(parameter as string);
            return flag ? t : f;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Thickness th)
            {
                return th.Equals(TrueThickness);
            }
            return false;
        }

        private static bool ToBool(object value)
        {
            if (value is bool b) return b;
            // Nullable<bool> boxes to either null or bool; treat null as false.
            return false;
        }

        private static (Thickness, Thickness) ParseParam(string? param)
        {
            if (string.IsNullOrWhiteSpace(param))
            {
                return (new Thickness(16), new Thickness(4));
            }
            var parts = param.Split('|');
            var t = parts.Length > 0 ? ParseThicknessOrResource(parts[0]) : new Thickness(16);
            var f = parts.Length > 1 ? ParseThicknessOrResource(parts[1]) : new Thickness(4);
            return (t, f);
        }

        private static Thickness ParseThicknessOrResource(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new Thickness(0);
            // Resource key path (tokenization): look up in Application resources first
            var key = s.Trim();
            var app = Application.Current;
            if (app is not null)
            {
                try
                {
                    if (app.Resources.TryGetValue(key, out var obj))
                    {
                        if (obj is Thickness thRes) return thRes;
                        if (obj is string str) return ParseThicknessString(str);
                        if (obj is double d) return new Thickness(d);
                    }
                }
                catch { /* fall back to parsing below */ }
            }

            // Fallback: parse inline numeric thickness (e.g., "12,12,4,12")
            return ParseThicknessString(key);
        }

        private static Thickness ParseThicknessString(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new Thickness(0);
            var tokens = s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim())
                          .ToArray();
            if (tokens.Length == 1 && double.TryParse(tokens[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var all))
            {
                return new Thickness(all);
            }
            if (tokens.Length == 2 &&
                double.TryParse(tokens[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var lr) &&
                double.TryParse(tokens[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var tb))
            {
                return new Thickness(lr, tb, lr, tb);
            }
            if (tokens.Length >= 4 &&
                double.TryParse(tokens[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var l) &&
                double.TryParse(tokens[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var t) &&
                double.TryParse(tokens[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var r) &&
                double.TryParse(tokens[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var b))
            {
                return new Thickness(l, t, r, b);
            }
            return new Thickness(0);
        }
    }
}
