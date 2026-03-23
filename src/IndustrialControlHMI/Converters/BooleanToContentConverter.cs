using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IndustrialControlHMI.Converters;

/// <summary>
/// 布尔值到内容的转换器（True:"断开PLC"，False:"连接PLC"）
/// </summary>
public class BooleanToContentConverter : IValueConverter
{
    public string TrueContent { get; set; } = "断开PLC";
    public string FalseContent { get; set; } = "连接PLC";
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueContent : FalseContent;
        }
        
        return FalseContent;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}