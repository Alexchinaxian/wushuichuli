using System;

namespace IndustrialControlHMI.Models.Database;

/// <summary>
/// 基于点位阈值的报警规则（与 <see cref="PointMappingEntity"/> 关联）。
/// </summary>
public class AlarmRuleEntity
{
    public long Id { get; set; }

    public long PointMappingId { get; set; }

    public string RuleName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public double? HighThreshold { get; set; }

    public double? LowThreshold { get; set; }

    /// <summary>回差，减少抖动。</summary>
    public double? Hysteresis { get; set; }

    /// <summary>信息 / 警告 / 报警 / 紧急。</summary>
    public string Severity { get; set; } = "警告";

    public string? MessageTemplate { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedUtc { get; set; }
}
