using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FinancialCalculator.WinUI3.Converters
{
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool flag = false;
            if (value is bool b) flag = b;
            else if (value is bool?) flag = ((bool?)value) ?? false;
            bool invert = parameter?.ToString() == "Inverse";
            if (invert) flag = !flag;
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility v)
            {
                bool invert = parameter?.ToString() == "Inverse";
                var result = v == Visibility.Visible;
                return invert ? !result : result;
            }
            return false;
        }
    }
}