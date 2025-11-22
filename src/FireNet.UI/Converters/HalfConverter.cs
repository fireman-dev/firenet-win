using System;
using System.Globalization;
using System.Windows.Data;

namespace FireNet.UI.Converters
{
    public class HalfConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is double d ? d / 2 : 20;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
