using System;

namespace IndustrialControlHMI.Models.Database;

/// <summary>
/// 点位历史采样（面向高频写入：批量插入 + 按时间与点位索引查询）。
/// </summary>
public class PointHistorySample
{
    public long Id { get; set; }

    public long PointMappingId { get; set; }

    /// <summary>UTC 时间戳。</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>采样值（模拟量统一用双精度；数字量 0/1）。</summary>
    public double ValueReal { get; set; }

    /// <summary>质量码：1=好，0=坏/无效。</summary>
    public byte Quality { get; set; } = 1;
}
