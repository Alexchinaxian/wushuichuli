using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IndustrialControlHMI.Converters
{
    /// <summary>
    /// 报警状态到颜色转换器。
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "激活" => new SolidColorBrush(Color.FromRgb(255, 59, 48)),   // #FF3B30
                    "确认" => new SolidColorBrush(Color.FromRgb(255, 149, 0)),   // #FF9500
                    "清除" => new SolidColorBrush(Color.FromRgb(76, 217, 100)),  // #4CD964
                    _ => new SolidColorBrush(Color.FromRgb(142, 142, 147))       // #8E8E93
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