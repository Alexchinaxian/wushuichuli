using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndustrialControlHMI.Infrastructure;
using IndustrialControlHMI.Models.Database;
using Microsoft.EntityFrameworkCore;

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
}
