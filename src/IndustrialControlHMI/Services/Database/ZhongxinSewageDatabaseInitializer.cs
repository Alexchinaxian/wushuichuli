using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using IndustrialControlHMI.Infrastructure;
using IndustrialControlHMI.Models.Database;
using IndustrialControlHMI.Models.Flowchart;
using IndustrialControlHMI.Services.S7;
using Microsoft.EntityFrameworkCore;

namespace IndustrialControlHMI.Services.Database;

/// <summary>
/// 中信国际污水库：设备种子、点位表同步、阈值规则、报表模板。
/// </summary>
public static class ZhongxinSewageDatabaseInitializer
{
    /// <summary>
    /// 在 <see cref="AppDbContext.EnsureDatabaseCreated"/> 之后调用。
    /// </summary>
    public static void Initialize(AppDbContext db)
    {
        SeedEquipmentsIfEmpty(db);
        db.SaveChanges();

        var mappings = LoadPointMappingsFromProject();
        UpsertPointMappings(db, mappings);
        db.SaveChanges();

        UpsertAlarmRulesFromPointMappings(db);
        db.SaveChanges();

        SeedReportTemplatesIfEmpty(db);
        db.SaveChanges();
    }

    private static IReadOnlyList<PlcPointMapping> LoadPointMappingsFromProject()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "中信国际污水项目PLC点位.md");
        if (File.Exists(path))
            return PlcPointTableParser.ParseFromFile(path).ToList();

        return PlcPointMappingProvider.LoadDefaultMappings();
    }

    private static void SeedEquipmentsIfEmpty(AppDbContext db)
    {
        if (db.Equipments.Any())
            return;

        foreach (var row in GetZhongxinEquipmentSeed())
            db.Equipments.Add(row);
    }

    private static IEnumerable<EquipmentEntity> GetZhongxinEquipmentSeed()
    {
        var order = 0;
        EquipmentEntity EquipmentRow(string code, string name, string unitId, string unitTitle, string cat) => new EquipmentEntity
        {
            Code = code,
            DisplayName = name,
            UnitId = unitId,
            UnitTitle = unitTitle,
            Category = cat,
            SortOrder = order++
        };

        yield return EquipmentRow("SCR-001", "格栅机", "bar-screen", "格栅池", "机械");
        yield return EquipmentRow("PMP-RT-1", "1#提升泵", "regulating-tank", "调节池", "泵");
        yield return EquipmentRow("PMP-RT-2", "2#提升泵", "regulating-tank", "调节池", "泵");
        yield return EquipmentRow("VLV-RT-W", "补水电磁阀", "regulating-tank", "调节池", "阀门");
        yield return EquipmentRow("MIX-DOS-1", "碳源搅拌", "dosing-1", "加药间", "搅拌机");
        yield return EquipmentRow("DOS-C-1", "碳源加药", "dosing-1", "加药间", "加药");
        yield return EquipmentRow("MIX-AX-1", "缺氧池搅拌机", "anoxic-tank", "缺氧池", "搅拌机");
        yield return EquipmentRow("PMP-SR-1", "1#回流泵", "mbr-tank", "MBR 膜池", "泵");
        yield return EquipmentRow("PMP-SR-2", "2#回流泵", "mbr-tank", "MBR 膜池", "泵");
        yield return EquipmentRow("PMP-PR-1", "1#产水泵", "production-pump", "产水泵房", "泵");
        yield return EquipmentRow("PMP-PR-2", "2#产水泵", "production-pump", "产水泵房", "泵");
        yield return EquipmentRow("BLW-MBR-1", "1#鼓风机", "mbr-tank", "MBR 膜池", "风机");
        yield return EquipmentRow("BLW-MBR-2", "2#鼓风机", "mbr-tank", "MBR 膜池", "风机");

        yield return EquipmentRow("UNIT-REG", "调节池单元", "regulating-tank", "调节池", "单元");
        yield return EquipmentRow("UNIT-MBR", "MBR 膜池单元", "mbr-tank", "MBR 膜池", "单元");
        yield return EquipmentRow("UNIT-IMT", "中间水池单元", "intermediate-tank", "中间水池", "单元");
    }

    private static void UpsertPointMappings(AppDbContext db, IReadOnlyList<PlcPointMapping> mappings)
    {
        var equipLookup = db.Equipments.AsNoTracking()
            .ToDictionary(e => (e.UnitId, e.DisplayName), e => e.Id);

        var existingByAddr = db.PointMappings.ToDictionary(p => p.RegisterAddress);

        foreach (var m in mappings)
        {
            long? equipmentId = null;
            if (!string.IsNullOrWhiteSpace(m.EquipmentName) &&
                equipLookup.TryGetValue((m.UnitId, m.EquipmentName), out var eid))
            {
                equipmentId = eid;
            }

            var addr = m.RegisterAddress.Trim();
            var now = DateTime.UtcNow;

            if (existingByAddr.TryGetValue(addr, out var existing))
            {
                existing.ExternalId = m.Id ?? existing.ExternalId;
                existing.VariableName = m.VariableName ?? existing.VariableName;
                existing.DataType = m.DataType ?? existing.DataType;
                existing.Purpose = m.Purpose ?? existing.Purpose;
                if (!string.IsNullOrWhiteSpace(m.UnitId))
                    existing.UnitId = m.UnitId;
                if (!string.IsNullOrWhiteSpace(m.EquipmentName))
                    existing.EquipmentName = m.EquipmentName;
                existing.EquipmentId = equipmentId ?? existing.EquipmentId;
                existing.AlarmHigh = m.AlarmHigh ?? existing.AlarmHigh;
                existing.AlarmLow = m.AlarmLow ?? existing.AlarmLow;
                existing.UpdatedUtc = now;
            }
            else
            {
                var row = new PointMappingEntity
                {
                    ExternalId = m.Id,
                    RegisterAddress = addr,
                    VariableName = m.VariableName ?? string.Empty,
                    DataType = m.DataType ?? string.Empty,
                    Purpose = m.Purpose ?? string.Empty,
                    UnitId = m.UnitId ?? string.Empty,
                    EquipmentName = m.EquipmentName ?? string.Empty,
                    EquipmentId = equipmentId,
                    AlarmHigh = m.AlarmHigh,
                    AlarmLow = m.AlarmLow,
                    HistoryEnabled = ShouldHistoryByPurpose(m.Purpose),
                    Source = "plc_table",
                    CreatedUtc = now,
                    UpdatedUtc = now
                };
                db.PointMappings.Add(row);
                existingByAddr[addr] = row;
            }
        }
    }

    private static bool ShouldHistoryByPurpose(string? purpose)
    {
        return purpose is "液位数据" or "流量数据" or "V区数据" or "定时参数";
    }

    private static void UpsertAlarmRulesFromPointMappings(AppDbContext db)
    {
        var points = db.PointMappings.AsNoTracking()
            .Where(p => p.AlarmHigh != null || p.AlarmLow != null)
            .ToList();

        foreach (var p in points)
        {
            var name = $"阈值::{p.VariableName}";
            var existing = db.AlarmRules.FirstOrDefault(r => r.PointMappingId == p.Id);
            if (existing == null)
            {
                db.AlarmRules.Add(new AlarmRuleEntity
                {
                    PointMappingId = p.Id,
                    RuleName = name,
                    Enabled = true,
                    HighThreshold = p.AlarmHigh,
                    LowThreshold = p.AlarmLow,
                    Hysteresis = 0.5,
                    Severity = "警告",
                    MessageTemplate = "{name} 超出范围：当前 {value}，高限 {high}，低限 {low}"
                });
            }
            else
            {
                existing.RuleName = name;
                existing.HighThreshold = p.AlarmHigh;
                existing.LowThreshold = p.AlarmLow;
                existing.ModifiedUtc = DateTime.UtcNow;
            }
        }
    }

    private static void SeedReportTemplatesIfEmpty(AppDbContext db)
    {
        if (db.ReportTemplates.Any())
            return;

        var opt = new JsonSerializerOptions { WriteIndented = false };

        db.ReportTemplates.Add(new ReportTemplateEntity
        {
            Name = "液位趋势（24h）",
            Slug = "level-trend-24h",
            Category = "运行",
            Description = "主要液位点位折线，默认最近 24 小时",
            DefinitionJson = JsonSerializer.Serialize(new
            {
                version = 1,
                timeRange = new { preset = "Last24Hours" },
                series = new[]
                {
                    new { registerAddress = "VDF100", label = "调节池液位", aggregation = "none" },
                    new { registerAddress = "VDF200", label = "MBR 液位", aggregation = "none" },
                    new { registerAddress = "VDF300", label = "中间水池液位", aggregation = "none" }
                },
                chart = new { type = "line" }
            }, opt),
            IsActive = true
        });

        db.ReportTemplates.Add(new ReportTemplateEntity
        {
            Name = "流量与产水概要",
            Slug = "flow-summary-daily",
            Category = "环保",
            Description = "流量均值与极值（可按需扩展时间窗）",
            DefinitionJson = JsonSerializer.Serialize(new
            {
                version = 1,
                timeRange = new { preset = "Last7Days" },
                kpis = new[]
                {
                    new { registerAddress = "VDF500", label = "电磁流量计", stat = "avg" }
                },
                chart = new { type = "bar" }
            }, opt),
            IsActive = true
        });

        db.ReportTemplates.Add(new ReportTemplateEntity
        {
            Name = "报警与规则清单",
            Slug = "alarm-rules-inventory",
            Category = "管理",
            Description = "导出当前阈值规则（非实时过程值）",
            DefinitionJson = JsonSerializer.Serialize(new
            {
                version = 1,
                dataSource = "AlarmRules",
                columns = new[] { "RuleName", "Severity", "HighThreshold", "LowThreshold", "Enabled" },
                export = new { format = "csv" }
            }, opt),
            IsActive = true
        });
    }
}
