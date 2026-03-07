using System;

namespace IndustrialControlHMI.Models
{
    /// <summary>
    /// 设置项数据模型，表示存储在数据库中的配置项。
    /// </summary>
    public class Setting
    {
        /// <summary>
        /// 唯一标识符。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 设置分类，如 "Modbus"、"Alarm"、"UI" 等。
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 设置键名。
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 设置值（字符串形式）。
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 数据类型，用于反序列化。
        /// </summary>
        public string DataType { get; set; } = "string";

        /// <summary>
        /// 设置项描述。
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 最后修改时间。
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改者。
        /// </summary>
        public string ModifiedBy { get; set; } = "System";
    }

    /// <summary>
    /// 数据类型常量。
    /// </summary>
    public static class DataType
    {
        public const string String = "string";
        public const string Integer = "int";
        public const string Float = "float";
        public const string Boolean = "bool";
        public const string DateTime = "datetime";
        public const string Json = "json";
    }

    /// <summary>
    /// 设置分类。
    /// </summary>
    public class SettingsCategory
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 报警阈值设置。
    /// </summary>
    public class AlarmThresholdSetting
    {
        public string ParameterName { get; set; } = string.Empty;
        public double HighHigh { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double LowLow { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}