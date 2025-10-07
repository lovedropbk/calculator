using System;
using Microsoft.UI.Xaml.Data;

namespace FinancialCalculator.WinUI3.Converters
{
    public sealed partial class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool invert = parameter?.ToString() == "Inverse";
            bool isNotNull = value != null;
            return invert ? !isNotNull : isNotNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}