using System;
using System.Globalization;
using System.Windows.Data;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Converts;

public class MultiParameterConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.Clone(); // Return as an array of objects
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
