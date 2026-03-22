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
        bool invert = parameter as string == "invert";
        
        if (value is string str)
        {
            bool isEmpty = string.IsNullOrEmpty(str);
            if (invert)
                isEmpty = !isEmpty;
                
            return isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
        
        return invert ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}