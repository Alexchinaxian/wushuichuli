using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using IndustrialControlHMI.Models.Flowchart;

namespace IndustrialControlHMI.Services.S7;

/// <summary>
/// 解析 MCGS 导出的中信国际污水项目 PLC 点位表（制表符分隔），生成 <see cref="PlcPointMapping"/> 列表。
/// </summary>
public static class PlcPointTableParser
{
    private static readonly Regex AddressRegex = new(
        @"(?<addr>(?:[IQM]\d+\.\d+)|(?:VDF\d+)|(?:VWUB\d+)|(?:VBUB\d+))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// 从点位表文件解析映射。
    /// </summary>
    public static IReadOnlyList<PlcPointMapping> ParseFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("点位表文件不存在", filePath);

        var text = File.ReadAllText(filePath);
        return ParseFromText(text);
    }

    /// <summary>
    /// 从文本内容解析（便于单元测试）。
    /// </summary>
    public static IReadOnlyList<PlcPointMapping> ParseFromText(string text)
    {
        var list = new List<PlcPointMapping>();
        foreach (var rawLine in text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            var match = AddressRegex.Match(line);
            if (!match.Success) continue;

            var cols = line.Split('\t');
            if (cols.Length < 2) continue;

            var variableName = cols[1].Trim();
            if (string.IsNullOrEmpty(variableName)) continue;

            var registerAddress = match.Groups["addr"].Value;
            var dataType = cols.Length > 2 ? cols[2].Trim() : "INTEGER";

            var purpose = ClassifyPurpose(registerAddress, variableName);
            var unitId = ClassifyUnitId(variableName);

            list.Add(new PlcPointMapping
            {
                Id = Guid.NewGuid().ToString(),
                VariableName = variableName,
                RegisterAddress = registerAddress,
                DataType = dataType,
                UnitId = unitId ?? string.Empty,
                EquipmentName = string.Empty,
                Purpose = purpose
            });
        }

        return list;
    }

    private static string ClassifyPurpose(string registerAddress, string variableName)
    {
        var v = variableName;
        if (registerAddress.StartsWith("I", StringComparison.OrdinalIgnoreCase))
        {
            if (v.Contains("故障")) return "故障状态";
            return "输入";
        }

        if (registerAddress.StartsWith("Q", StringComparison.OrdinalIgnoreCase))
            return "控制输出";

        if (registerAddress.StartsWith("M", StringComparison.OrdinalIgnoreCase))
            return "内部继电器";

        if (registerAddress.StartsWith("V", StringComparison.OrdinalIgnoreCase))
        {
            if (v.Contains("液位")) return "液位数据";
            return "V区数据";
        }

        return "通用";
    }

    private static string? ClassifyUnitId(string variableName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
            return null;

        var v = variableName;

        // 依据点位变量名的常见命名，粗粒度归类到流程单元（用于“处理单元(多选)”筛选）。
        if (v.Contains("调节池", StringComparison.OrdinalIgnoreCase))
            return "regulating-tank";
        if (v.Contains("MBR膜池", StringComparison.OrdinalIgnoreCase) || v.Contains("MBR", StringComparison.OrdinalIgnoreCase))
            return "mbr-tank";
        if (v.Contains("中间水池", StringComparison.OrdinalIgnoreCase) || v.Contains("中水池", StringComparison.OrdinalIgnoreCase))
            return "intermediate-tank";
        if (v.Contains("缺氧池", StringComparison.OrdinalIgnoreCase))
            return "anoxic-tank";
        if (v.Contains("碳源", StringComparison.OrdinalIgnoreCase))
            return "dosing-1";
        if (v.Contains("格栅机", StringComparison.OrdinalIgnoreCase))
            return "bar-screen";

        // 产水泵/电磁流量计通常在同一段工艺里展示
        if (v.Contains("产水泵", StringComparison.OrdinalIgnoreCase) || v.Contains("电磁流量计", StringComparison.OrdinalIgnoreCase))
            return "production-pump";

        return null;
    }

    /// <summary>
    /// 将映射列表写入 JSON，便于离线检查与版本管理。
    /// </summary>
    public static void SaveMappingsToJson(IEnumerable<PlcPointMapping> mappings, string jsonPath)
    {
        var dir = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(mappings, options));
    }
}
