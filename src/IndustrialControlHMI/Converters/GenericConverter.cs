using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace IndustrialControlHMI.Converters;

/// <summary>
/// 通用值转换器，支持多种转换类型
/// </summary>
public class GenericConverter : IValueConverter
{
    /// <summary>
    /// 转换类型
    /// </summary>
    public ConverterType Type { get; set; } = ConverterType.BooleanToBrush;
    
    /// <summary>
    /// True时的值
    /// </summary>
    public object TrueValue { get; set; }
    
    /// <summary>
    /// False时的值
    /// </summary>
    public object FalseValue { get; set; }
    
    /// <summary>
    /// 默认值（当输入为null或转换失败时使用）
    /// </summary>
    public object DefaultValue { get; set; }
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Type switch
        {
            ConverterType.BooleanToBrush => ConvertBooleanToBrush(value),
            ConverterType.BooleanToContent => ConvertBooleanToContent(value),
            ConverterType.BooleanToVisibility => ConvertBooleanToVisibility(value),
            ConverterType.StatusToBrush => ConvertStatusToBrush(value),
            ConverterType.StatusToColor => ConvertStatusToColor(value),
            ConverterType.ObjectToVisibility => ConvertObjectToVisibility(value),
            ConverterType.StringToVisibility => ConvertStringToVisibility(value),
            ConverterType.IntToVisibility => ConvertIntToVisibility(value),
            ConverterType.CategoryToVisibility => ConvertCategoryToVisibility(value, parameter),
            _ => DefaultValue ?? DependencyProperty.UnsetValue
        };
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("GenericConverter只支持单向转换");
    }
    
    #region 具体转换方法
    
    private object ConvertBooleanToBrush(object value)
    {
        if (value is bool boolValue)
        {
            return boolValue 
                ? (TrueValue as Brush ?? Brushes.Green)
                : (FalseValue as Brush ?? Brushes.Red);
        }
        return DefaultValue as Brush ?? Brushes.Gray;
    }
    
    private object ConvertBooleanToContent(object value)
    {
        if (value is bool boolValue)
        {
            return boolValue 
                ? (TrueValue ?? "True")
                : (FalseValue ?? "False");
        }
        return DefaultValue ?? "N/A";
    }
    
    private object ConvertBooleanToVisibility(object value)
    {
        if (value is bool boolValue)
        {
            return boolValue 
                ? (TrueValue as Visibility? ?? Visibility.Visible)
                : (FalseValue as Visibility? ?? Visibility.Collapsed);
        }
        return DefaultValue as Visibility? ?? Visibility.Collapsed;
    }
    
    private object ConvertStatusToBrush(object value)
    {
        // 这里可以根据具体的状态枚举进行转换
        // 暂时使用简单实现
        if (value is Enum status)
        {
            var statusName = status.ToString();
            return statusName switch
            {
                "Running" or "Connected" => Brushes.Green,
                "Warning" => Brushes.Yellow,
                "Error" or "Fault" => Brushes.Red,
                "Stopped" or "Disconnected" => Brushes.Gray,
                _ => Brushes.Transparent
            };
        }
        return DefaultValue as Brush ?? Brushes.Transparent;
    }
    
    private object ConvertStatusToColor(object value)
    {
        var brush = ConvertStatusToBrush(value) as Brush;
        if (brush is SolidColorBrush solidBrush)
        {
            return solidBrush.Color;
        }
        return Colors.Transparent;
    }
    
    private object ConvertObjectToVisibility(object value)
    {
        return value != null 
            ? (TrueValue as Visibility? ?? Visibility.Visible)
            : (FalseValue as Visibility? ?? Visibility.Collapsed);
    }
    
    private object ConvertStringToVisibility(object value)
    {
        return !string.IsNullOrEmpty(value as string)
            ? (TrueValue as Visibility? ?? Visibility.Visible)
            : (FalseValue as Visibility? ?? Visibility.Collapsed);
    }
    
    private object ConvertIntToVisibility(object value)
    {
        if (value is int intValue)
        {
            return intValue > 0
                ? (TrueValue as Visibility? ?? Visibility.Visible)
                : (FalseValue as Visibility? ?? Visibility.Collapsed);
        }
        return DefaultValue as Visibility? ?? Visibility.Collapsed;
    }
    
    private object ConvertCategoryToVisibility(object value, object parameter)
    {
        if (value is string category && parameter is string targetCategory)
        {
            return category == targetCategory
                ? (TrueValue as Visibility? ?? Visibility.Visible)
                : (FalseValue as Visibility? ?? Visibility.Collapsed);
        }
        return DefaultValue as Visibility? ?? Visibility.Collapsed;
    }
    
    #endregion
}

/// <summary>
/// 转换器类型枚举
/// </summary>
public enum ConverterType
{
    BooleanToBrush,
    BooleanToContent,
    BooleanToVisibility,
    StatusToBrush,
    StatusToColor,
    ObjectToVisibility,
    StringToVisibility,
    IntToVisibility,
    CategoryToVisibility
}