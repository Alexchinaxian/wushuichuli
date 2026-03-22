using System;

namespace IndustrialControlHMI.Models
{
    /// <summary>
    /// 参数监控项数据模型。
    /// </summary>
    public class ParameterItem
    {
        /// <summary>
        /// 参数名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 当前值。
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// 单位。
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 最后更新时间。
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 状态描述（正常、警告、报警等）。
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 状态颜色（十六进制或颜色值）。
        /// </summary>
        public string StatusColor { get; set; } = "#4CD964"; // 默认绿色

        /// <summary>
        /// 最小值。
        /// </summary>
        public double MinValue { get; set; }

        /// <summary>
        /// 最大值。
        /// </summary>
        public double MaxValue { get; set; }

        /// <summary>
        /// 警告阈值。
        /// </summary>
        public double WarningThreshold { get; set; }

        /// <summary>
        /// 报警阈值。
        /// </summary>
        public double AlarmThreshold { get; set; }
    }
}