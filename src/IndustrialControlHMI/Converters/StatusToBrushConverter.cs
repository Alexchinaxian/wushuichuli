using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using IndustrialControlHMI.Models.Flowchart;

namespace IndustrialControlHMI.Converters
{
    /// <summary>
    /// 设备状态到画刷转换器，用于状态指示灯
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EquipmentStatus status)
            {
                return status switch
                {
                    // 运行态：绿色 #00FF00
                    EquipmentStatus.Running => new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                    // 停止/离线态：灰色 #808080
                    EquipmentStatus.Stopped => new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                    // 报警态：红色 #FF3B30
                    EquipmentStatus.Fault => new SolidColorBrush(Color.FromRgb(255, 59, 48)),
                    // 警告态：橙色 #FF9500
                    EquipmentStatus.Warning => new SolidColorBrush(Color.FromRgb(255, 149, 0)),
                    _ => new SolidColorBrush(Color.FromRgb(142, 142, 147))                         // 默认灰色
                };
            }
            
            // 如果值不是EquipmentStatus，返回默认灰色
            return new SolidColorBrush(Color.FromRgb(189, 189, 189));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}