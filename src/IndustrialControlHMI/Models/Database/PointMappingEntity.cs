using System;

namespace IndustrialControlHMI.Models.Database;

/// <summary>
/// PLC 点位映射（由点位表解析或内置默认映射同步入库）。
/// </summary>
public class PointMappingEntity
{
    public long Id { get; set; }

    /// <summary>与运行时 PlcPointMapping.Id 对齐的可选外部 Id。</summary>
    public string? ExternalId { get; set; }

    /// <summary>PLC 寄存器地址，全库唯一，用于 upsert。</summary>
    public string RegisterAddress { get; set; } = string.Empty;

    public string VariableName { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public string UnitId { get; set; } = string.Empty;

    public string EquipmentName { get; set; } = string.Empty;

    public long? EquipmentId { get; set; }

    public double? AlarmHigh { get; set; }

    public double? AlarmLow { get; set; }

    /// <summary>是否参与历史高频存储（可由配置覆盖）。</summary>
    public bool HistoryEnabled { get; set; } = true;

    /// <summary>建议采样间隔毫秒，0 表示跟随全局轮询。</summary>
    public int SuggestedIntervalMs { get; set; }

    public string Source { get; set; } = "embedded";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
