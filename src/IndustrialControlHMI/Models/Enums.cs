using System.Windows.Media;

namespace IndustrialControlHMI.Models
{
    /// <summary>
    /// 连接状态枚举。
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// 设备状态枚举。
    /// </summary>
    public enum DeviceStatus
    {
        Offline,
        Online,
        Running,
        Fault,
        Maintenance
    }

    /// <summary>
    /// 报警状态枚举。
    /// </summary>
    public enum AlarmStatus
    {
        Normal,
        Warning,
        Alarm,
        Critical
    }

    /// <summary>
    /// 颜色常量，匹配SVG图像设计。
    /// </summary>
    public static class StatusColors
    {
        public static Color Normal = Color.FromRgb(76, 217, 100);     // #4CD964
        public static Color Warning = Color.FromRgb(255, 149, 0);     // #FF9500
        public static Color Alarm = Color.FromRgb(255, 59, 48);       // #FF3B30
        public static Color Offline = Color.FromRgb(142, 142, 147);   // #8E8E93
        public static Color Connected = Color.FromRgb(0, 168, 255);   // #00A8FF
        public static Color Disconnected = Color.FromRgb(142, 142, 147);
        public static Color Error = Color.FromRgb(255, 59, 48);
    }
}