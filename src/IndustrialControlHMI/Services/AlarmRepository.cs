using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IndustrialControlHMI.Infrastructure;
using IndustrialControlHMI.Models;

namespace IndustrialControlHMI.Services;

/// <summary>
/// 报警记录存储库实现，使用SQLite数据库进行持久化。
/// </summary>
public class AlarmRepository : IAlarmRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// 初始化报警存储库。
    /// </summary>
    /// <param name="dbContext">数据库上下文。</param>
    public AlarmRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AlarmRecord>> GetAllAsync()
    {
        return await _dbContext.AlarmRecords
            .OrderByDescending(a => a.OccurrenceTime)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AlarmRecord?> GetByIdAsync(int id)
    {
        return await _dbContext.AlarmRecords.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AlarmRecord>> GetUnacknowledgedAsync()
    {
        return await _dbContext.AlarmRecords
            .Where(a => a.Status == "激活" || a.Status == "确认")
            .OrderByDescending(a => a.OccurrenceTime)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AlarmRecord>> GetByStatusAsync(string status)
    {
        return await _dbContext.AlarmRecords
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.OccurrenceTime)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AlarmRecord> AddAsync(AlarmRecord record)
    {
        if (record == null)
            throw new ArgumentNullException(nameof(record));

        record.OccurrenceTime = DateTime.UtcNow;
        _dbContext.AlarmRecords.Add(record);
        await _dbContext.SaveChangesAsync();
        return record;
    }

    /// <inheritdoc />
    public async Task<AlarmRecord> UpdateAsync(AlarmRecord record)
    {
        if (record == null)
            throw new ArgumentNullException(nameof(record));

        var existing = await _dbContext.AlarmRecords.FindAsync(record.Id);
        if (existing == null)
            throw new InvalidOperationException($"Alarm record with ID {record.Id} not found.");

        // 更新可修改属性
        existing.ParameterName = record.ParameterName;
        existing.AlarmType = record.AlarmType;
        existing.Threshold = record.Threshold;
        existing.ActualValue = record.ActualValue;
        existing.Message = record.Message;
        existing.Status = record.Status;
        existing.AcknowledgedTime = record.AcknowledgedTime;
        existing.ClearedTime = record.ClearedTime;

        await _dbContext.SaveChangesAsync();
        return existing;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id)
    {
        var record = await _dbContext.AlarmRecords.FindAsync(id);
        if (record == null)
            return false;

        _dbContext.AlarmRecords.Remove(record);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<AlarmRecord?> AcknowledgeAsync(int id)
    {
        var record = await _dbContext.AlarmRecords.FindAsync(id);
        if (record == null)
            return null;

        record.Status = "确认";
        record.AcknowledgedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return record;
    }

    /// <inheritdoc />
    public async Task<AlarmRecord?> ResolveAsync(int id)
    {
        var record = await _dbContext.AlarmRecords.FindAsync(id);
        if (record == null)
            return null;

        record.Status = "清除";
        record.ClearedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return record;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AlarmRecord>> GetRecentAsync(int count = 50)
    {
        return await _dbContext.AlarmRecords
            .OrderByDescending(a => a.OccurrenceTime)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveAlarmsAsync()
    {
        return await _dbContext.AlarmRecords
            .AnyAsync(a => a.Status == "激活");
    }
}