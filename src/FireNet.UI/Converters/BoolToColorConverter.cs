using System;
using System.Windows.Data;
using System.Windows.Media;

public class BoolToColorConverter : IValueConverter
{
    public Brush TrueBrush { get; set; }
    public Brush FalseBrush { get; set; }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        bool val = value is bool b && b;
        return val ? TrueBrush : FalseBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return null;
    }
}
