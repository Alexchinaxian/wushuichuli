using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IndustrialControlHMI.Converters
{
    /// <summary>
    /// 将所选类别ID与参数匹配，决定控件可见性的转换器。
    /// 如果 SelectedCategory.Id 与 ConverterParameter 相同，返回 Visible，否则 Collapsed。
    /// </summary>
    public class CategoryToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果 value 是 null，隐藏
            if (value == null)
                return Visibility.Collapsed;

            string categoryId = value.ToString();
            string targetId = parameter?.ToString();

            // 如果参数为空，默认显示
            if (string.IsNullOrEmpty(targetId))
                return Visibility.Visible;

            // 不区分大小写比较
            bool match = string.Equals(categoryId, targetId, StringComparison.OrdinalIgnoreCase);
            return match ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}