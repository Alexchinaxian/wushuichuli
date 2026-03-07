using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using IndustrialControlHMI.Models;

namespace IndustrialControlHMI.Infrastructure;

/// <summary>
/// 应用程序数据库上下文，用于管理SQLite数据库连接和实体集。
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// 报警记录表。
    /// </summary>
    public DbSet<AlarmRecord> AlarmRecords { get; set; }

    /// <summary>
    /// 设置表。
    /// </summary>
    public DbSet<Setting> Settings { get; set; }

    /// <summary>
    /// 配置数据库连接和选项。
    /// </summary>
    /// <param name="optionsBuilder">DbContext选项构建器。</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // 使用SQLite内存数据库进行快速测试，实际应用可改为文件数据库
        // const string connectionString = "Data Source=alarms.db";
        const string connectionString = "Data Source=:memory:";
        optionsBuilder.UseSqlite(connectionString);
    }

    /// <summary>
    /// 配置模型创建，包括索引、关系等。
    /// </summary>
    /// <param name="modelBuilder">模型构建器。</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置AlarmRecord实体
        modelBuilder.Entity<AlarmRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParameterName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AlarmType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Threshold).IsRequired();
            entity.Property(e => e.ActualValue).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.OccurrenceTime).IsRequired();
            entity.Property(e => e.AcknowledgedTime).IsRequired(false);
            entity.Property(e => e.ClearedTime).IsRequired(false);

            // 添加索引以提高查询性能
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OccurrenceTime);
            entity.HasIndex(e => new { e.ParameterName, e.AlarmType });
        });

        // 配置Setting实体
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.LastModified).IsRequired();
            entity.Property(e => e.ModifiedBy).IsRequired().HasMaxLength(100);

            // 复合唯一索引，确保每个分类+键唯一
            entity.HasIndex(e => new { e.Category, e.Key }).IsUnique();
        });
    }

    /// <summary>
    /// 确保数据库已创建并应用迁移（如果使用文件数据库）。
    /// 对于内存数据库，此方法将创建新的数据库。
    /// </summary>
    public void EnsureDatabaseCreated()
    {
        Database.EnsureCreated();
    }
}