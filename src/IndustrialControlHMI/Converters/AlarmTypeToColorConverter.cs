using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IndustrialControlHMI.Converters
{
    /// <summary>
    /// 报警类型到颜色转换器。
    /// </summary>
    public class AlarmTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string alarmType)
            {
                return alarmType.ToLower() switch
                {
                    "警告" => new SolidColorBrush(Color.FromRgb(255, 149, 0)), // #FF9500
                    "报警" => new SolidColorBrush(Color.FromRgb(255, 59, 48)),  // #FF3B30
                    "严重" => new SolidColorBrush(Color.FromRgb(255, 0, 0)),    // #FF0000
                    _ => new SolidColorBrush(Color.FromRgb(142, 142, 147))      // #8E8E93
                };
            }
            return new SolidColorBrush(Color.FromRgb(142, 142, 147));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}