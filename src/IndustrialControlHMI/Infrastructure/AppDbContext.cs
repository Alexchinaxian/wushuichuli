using Microsoft.EntityFrameworkCore;
using IndustrialControlHMI.Infrastructure.Database;
using IndustrialControlHMI.Models;
using IndustrialControlHMI.Models.Database;

namespace IndustrialControlHMI.Infrastructure;

/// <summary>
/// 应用程序数据库上下文：报警、设置、中信污水点位/设备/历史/规则/报表模板。
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<AlarmRecord> AlarmRecords { get; set; }

    public DbSet<Setting> Settings { get; set; }

    public DbSet<EquipmentEntity> Equipments { get; set; }

    public DbSet<PointMappingEntity> PointMappings { get; set; }

    public DbSet<PointHistorySample> PointHistorySamples { get; set; }

    public DbSet<AlarmRuleEntity> AlarmRules { get; set; }

    public DbSet<ReportTemplateEntity> ReportTemplates { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(ZhongxinSewageConnection.GetConnectionString());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OccurrenceTime);
            entity.HasIndex(e => new { e.ParameterName, e.AlarmType });
            entity.HasIndex(e => e.PointMappingId);
        });

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
            entity.HasIndex(e => new { e.Category, e.Key }).IsUnique();
        });

        modelBuilder.Entity<EquipmentEntity>(entity =>
        {
            entity.ToTable("Equipments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(64);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UnitTitle).HasMaxLength(200);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.UnitId);
        });

        modelBuilder.Entity<PointMappingEntity>(entity =>
        {
            entity.ToTable("PointMappings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).HasMaxLength(64);
            entity.Property(e => e.RegisterAddress).IsRequired().HasMaxLength(64);
            entity.Property(e => e.VariableName).IsRequired().HasMaxLength(300);
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Purpose).IsRequired().HasMaxLength(64);
            entity.Property(e => e.UnitId).HasMaxLength(100);
            entity.Property(e => e.EquipmentName).HasMaxLength(200);
            entity.Property(e => e.Source).HasMaxLength(32);
            entity.HasIndex(e => e.RegisterAddress).IsUnique();
            entity.HasIndex(e => e.Purpose);
            entity.HasIndex(e => e.UnitId);
            entity.HasOne<EquipmentEntity>()
                .WithMany()
                .HasForeignKey(e => e.EquipmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PointHistorySample>(entity =>
        {
            entity.ToTable("PointHistorySamples");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TimestampUtc).IsRequired();
            entity.HasIndex(e => new { e.PointMappingId, e.TimestampUtc });
            entity.HasOne<PointMappingEntity>()
                .WithMany()
                .HasForeignKey(e => e.PointMappingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlarmRuleEntity>(entity =>
        {
            entity.ToTable("AlarmRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(32);
            entity.Property(e => e.MessageTemplate).HasMaxLength(500);
            entity.HasIndex(e => e.PointMappingId).IsUnique();
            entity.HasOne<PointMappingEntity>()
                .WithMany()
                .HasForeignKey(e => e.PointMappingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReportTemplateEntity>(entity =>
        {
            entity.ToTable("ReportTemplates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DefinitionJson).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Category);
        });
    }

    public void EnsureDatabaseCreated()
    {
        Database.EnsureCreated();
    }
}
