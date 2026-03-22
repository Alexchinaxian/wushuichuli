using System;

namespace IndustrialControlHMI.Models.Database;

/// <summary>
/// 运行报表模板定义（JSON 描述列、时间范围、聚合方式等）。
/// </summary>
public class ReportTemplateEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>程序内引用键，唯一。</summary>
    public string Slug { get; set; } = string.Empty;

    public string Category { get; set; } = "运行";

    public string? Description { get; set; }

    /// <summary>
    /// JSON：版本、时间范围预设、列（点位/聚合）、图表类型等。
    /// </summary>
    public string DefinitionJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedUtc { get; set; }
}
