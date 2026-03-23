using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IndustrialControlHMI.Models.Database;

namespace IndustrialControlHMI.Services.Database;

/// <summary>
/// 高频历史采样写入与区间查询。
/// </summary>
public interface IPointHistoryRepository
{
    Task AppendSamplesAsync(IReadOnlyList<PointHistorySample> samples, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PointHistorySample>> QueryRangeAsync(
        long pointMappingId,
        System.DateTime fromUtc,
        System.DateTime toUtc,
        int takeLimit = 50000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量查询：对每个 pointMappingId 按 TimestampUtc 升序取前 takeLimit 条。
    /// </summary>
    Task<IReadOnlyList<PointHistorySample>> QueryRangeManyAsync(
        IReadOnlyList<long> pointMappingIds,
        System.DateTime fromUtc,
        System.DateTime toUtc,
        int takeLimit = 50000,
        CancellationToken cancellationToken = default);
}
