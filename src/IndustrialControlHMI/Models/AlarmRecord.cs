using System;

namespace IndustrialControlHMI.Models
{
    /// <summary>
    /// 报警记录数据模型。
    /// </summary>
    public class AlarmRecord
    {
        /// <summary>
        /// 记录ID（自增主键）。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 报警参数名称。
        /// </summary>
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// 报警类型（警告、报警、严重）。
        /// </summary>
        public string AlarmType { get; set; } = string.Empty;

        /// <summary>
        /// 报警阈值。
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// 实际值。
        /// </summary>
        public double ActualValue { get; set; }

        /// <summary>
        /// 报警消息。
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 报警状态（激活、确认、清除）。
        /// </summary>
        public string Status { get; set; } = "激活";

        /// <summary>
        /// 报警发生时间。
        /// </summary>
        public DateTime OccurrenceTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 报警确认时间。
        /// </summary>
        public DateTime? AcknowledgedTime { get; set; }

        /// <summary>
        /// 报警清除时间。
        /// </summary>
        public DateTime? ClearedTime { get; set; }
    }
}