using System;

namespace IndustrialControlHMI.Models.Database;

/// <summary>
/// 中信国际污水项目设备主数据（泵、风机、阀门、搅拌机等）。
/// </summary>
public class EquipmentEntity
{
    public long Id { get; set; }

    /// <summary>站内唯一编码，如 PUMP-REG-01。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>显示名称，如 1#提升泵。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>流程单元 Id（与流程图 ProcessUnitModel.Id 一致）。</summary>
    public string UnitId { get; set; } = string.Empty;

    /// <summary>单元标题（冗余，便于报表）。</summary>
    public string? UnitTitle { get; set; }

    /// <summary>设备类别：泵、风机、阀门、搅拌机、仪表 等。</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>排序。</summary>
    public int SortOrder { get; set; }

    public string? Remark { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
