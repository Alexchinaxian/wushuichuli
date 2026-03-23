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
        var inverse = VisibilityConverterHelper.IsInverseParameter(parameter);
        // 兼容现有语义：默认“对象为 null 时可见”，传 inverse 则反转。
        var isNullVisible = value == null;
        if (inverse) isNullVisible = !isNullVisible;
        return isNullVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
