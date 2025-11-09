using System;
using Microsoft.UI.Xaml.Data;

namespace FinancialCalculator.WinUI3.Converters
{
    // MARK: Inverts a boolean. Useful to disable inputs when a condition is true.
    public sealed partial class BoolNotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b) return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b) return !b;
            return false;
        }
    }
}