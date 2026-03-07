using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IndustrialControlHMI.Converters;

/// <summary>
/// 将对象是否为 null 转换为 Visibility。Parameter 传 "Inverse" 时取反。
/// </summary>
public class ObjectToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        bool inverse = string.Equals(parameter as string, "Inverse", StringComparison.OrdinalIgnoreCase);
        bool visible = inverse ? !isNull : isNull;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
