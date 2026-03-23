using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IndustrialControlHMI.Converters;

/// <summary>
/// 整数到Visibility的转换器（0为Collapsed，大于0为Visible）
/// </summary>
public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool inverse = VisibilityConverterHelper.IsInverseParameter(parameter);
        if (value is int intValue)
        {
            return VisibilityConverterHelper.FromInt64(intValue, inverse);
        }
        
        if (value is long longValue)
        {
            return VisibilityConverterHelper.FromInt64(longValue, inverse);
        }
        
        return VisibilityConverterHelper.FromInt64(0, inverse);
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}