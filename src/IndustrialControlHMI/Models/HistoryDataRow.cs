using System;

namespace IndustrialControlHMI.Models;

/// <summary>
/// 历史数据预览/导出行。
/// </summary>
public sealed class HistoryDataRow
{
    public DateTime TimestampLocal { get; set; }

    public string ProcessUnitTitle { get; set; } = string.Empty;

    public string ParameterName { get; set; } = string.Empty;

    public double Value { get; set; }

    /// <summary>质量码：1=好，0=坏。</summary>
    public byte Quality { get; set; } = 1;

    public string QualityText => Quality == 1 ? "好" : "坏";

    public bool IsSimulated { get; set; }
}

