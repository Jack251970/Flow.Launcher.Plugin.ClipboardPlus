using System;
using System.Globalization;
using System.Windows.Data;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Converts;

public class BoolReverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolean)
        {
            return !boolean;
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolean)
        {
            return !boolean;
        }

        return value;
    }
}
