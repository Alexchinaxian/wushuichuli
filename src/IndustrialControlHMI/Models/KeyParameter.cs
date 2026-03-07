namespace IndustrialControlHMI.Models
{
    /// <summary>
    /// 关键参数数据模型（用于右侧面板显示）。
    /// </summary>
    public class KeyParameter
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
    }
}