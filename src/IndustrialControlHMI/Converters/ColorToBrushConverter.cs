using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IndustrialControlHMI.Converters;

/// <summary>
/// System.Windows.Media.Color 到 SolidColorBrush 的转换器
/// </summary>
public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
            return new SolidColorBrush(color);
        if (value is Brush brush)
            return brush;
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush solid)
            return solid.Color;
        return Colors.Transparent;
    }
}
