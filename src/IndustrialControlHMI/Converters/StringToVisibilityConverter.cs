using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IndustrialControlHMI.Converters;

/// <summary>
/// 字符串到可见性转换器，空字符串或null时隐藏
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool inverse = VisibilityConverterHelper.IsInverseParameter(parameter);
        return VisibilityConverterHelper.FromString(value as string, inverse);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}