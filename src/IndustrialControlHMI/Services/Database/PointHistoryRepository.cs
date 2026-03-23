using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndustrialControlHMI.Infrastructure;
using IndustrialControlHMI.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace IndustrialControlHMI.Services.Database;

public sealed class PointHistoryRepository : IPointHistoryRepository
{
    private readonly AppDbContext _db;

    public PointHistoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AppendSamplesAsync(IReadOnlyList<PointHistorySample> samples, CancellationToken cancellationToken = default)
    {
        if (samples.Count == 0)
            return;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        _db.PointHistorySamples.AddRange(samples);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PointHistorySample>> QueryRangeAsync(
        long pointMappingId,
        System.DateTime fromUtc,
        System.DateTime toUtc,
        int takeLimit = 50000,
        CancellationToken cancellationToken = default)
    {
        return await _db.PointHistorySamples
            .AsNoTracking()
            .Where(s => s.PointMappingId == pointMappingId && s.TimestampUtc >= fromUtc && s.TimestampUtc <= toUtc)
            .OrderBy(s => s.TimestampUtc)
            .Take(takeLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PointHistorySample>> QueryRangeManyAsync(
        IReadOnlyList<long> pointMappingIds,
        System.DateTime fromUtc,
        System.DateTime toUtc,
        int takeLimit = 50000,
        CancellationToken cancellationToken = default)
    {
        if (pointMappingIds.Count == 0 || takeLimit <= 0)
            return Array.Empty<PointHistorySample>();

        // SQLite 的 IN 子句对参数数量通常有限制；做分批，避免一次性拼太多参数。
        const int maxIdsPerChunk = 900;
        var distinctIds = pointMappingIds.Distinct().ToArray();
        var allResults = new List<PointHistorySample>(capacity: distinctIds.Length);

        for (int offset = 0; offset < distinctIds.Length; offset += maxIdsPerChunk)
        {
            var chunk = distinctIds.Skip(offset).Take(maxIdsPerChunk).ToArray();
            if (chunk.Length == 0) break;

            // 用 window function：对每个 PointMappingId 分区后取前 takeLimit 条。
            var idParamNames = new string[chunk.Length];
            var parameters = new List<object>(chunk.Length + 3);
            for (int i = 0; i < chunk.Length; i++)
            {
                var pName = $"@id{i}";
                idParamNames[i] = pName;
                parameters.Add(new SqliteParameter(pName, chunk[i]));
            }

            parameters.Add(new SqliteParameter("@fromUtc", fromUtc));
            parameters.Add(new SqliteParameter("@toUtc", toUtc));
            parameters.Add(new SqliteParameter("@takeLimit", takeLimit));

            var sql = $@"
SELECT Id, PointMappingId, TimestampUtc, ValueReal, Quality
FROM (
    SELECT s.Id, s.PointMappingId, s.TimestampUtc, s.ValueReal, s.Quality,
           ROW_NUMBER() OVER (PARTITION BY s.PointMappingId ORDER BY s.TimestampUtc) AS rn
    FROM PointHistorySamples s
    WHERE s.PointMappingId IN ({string.Join(",", idParamNames)})
      AND s.TimestampUtc >= @fromUtc AND s.TimestampUtc <= @toUtc
) t
WHERE t.rn <= @takeLimit
ORDER BY t.PointMappingId, t.TimestampUtc;";

            var rows = await _db.PointHistorySamples
                .FromSqlRaw(sql, parameters.ToArray())
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            allResults.AddRange(rows);
        }

        return allResults;
    }
}
