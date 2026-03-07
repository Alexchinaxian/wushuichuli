using System;
using System.Collections.Generic;

namespace IndustrialControlHMI.Models.Flowchart;

/// <summary>
/// PLC点位映射配置，将PLC地址与流程图元素关联
/// </summary>
public class PlcPointMapping
{
    /// <summary>
    /// 映射项唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// PLC变量名（例如："调节池提升1号故障"）
    /// </summary>
    public string VariableName { get; set; } = string.Empty;
    
    /// <summary>
    /// PLC寄存器地址（例如："I000.0", "VDF100"）
    /// </summary>
    public string RegisterAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 数据类型（INTEGER, SINGLE等）
    /// </summary>
    public string DataType { get; set; } = string.Empty;
    
    /// <summary>
    /// 关联的处理单元ID（对应ProcessUnitModel.Id）
    /// </summary>
    public string UnitId { get; set; } = string.Empty;
    
    /// <summary>
    /// 关联的设备名称（对应EquipmentItem.Name，如为空则表示关联整个单元）
    /// </summary>
    public string EquipmentName { get; set; } = string.Empty;
    
    /// <summary>
    /// 点位用途描述（例如："故障状态", "控制输出", "液位数据"）
    /// </summary>
    public string Purpose { get; set; } = string.Empty;
    
    /// <summary>
    /// 当前值（从PLC读取）
    /// </summary>
    public object? CurrentValue { get; set; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 是否报警
    /// </summary>
    public bool IsAlarm { get; set; }
    
    /// <summary>
    /// 报警阈值（高限）
    /// </summary>
    public double? AlarmHigh { get; set; }
    
    /// <summary>
    /// 报警阈值（低限）
    /// </summary>
    public double? AlarmLow { get; set; }
}

/// <summary>
/// PLC点位映射提供器，根据PLC点位文件生成映射配置
/// </summary>
public static class PlcPointMappingProvider
{
    /// <summary>
    /// 加载默认的PLC点位映射（基于中信国际污水项目PLC点位）
    /// </summary>
    public static List<PlcPointMapping> LoadDefaultMappings()
    {
        var mappings = new List<PlcPointMapping>();
        
        // 故障状态点位（I输入继电器）
        AddFaultMappings(mappings);
        
        // 控制输出点位（Q输出继电器）
        AddControlMappings(mappings);
        
        // 液位数据点位（V数据寄存器）
        AddLevelMappings(mappings);
        
        // 定时参数点位
        AddTimerMappings(mappings);
        
        return mappings;
    }
    
    private static void AddFaultMappings(List<PlcPointMapping> mappings)
    {
        // 调节池故障
        mappings.Add(new PlcPointMapping
        {
            VariableName = "调节池提升1号故障",
            RegisterAddress = "I000.0",
            DataType = "INTEGER",
            UnitId = "regulating-tank",
            EquipmentName = "1#提升泵",
            Purpose = "故障状态"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "调节池提升2号故障",
            RegisterAddress = "I000.1",
            DataType = "INTEGER",
            UnitId = "regulating-tank",
            EquipmentName = "2#提升泵",
            Purpose = "故障状态"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "调节池补水电磁阀故障",
            RegisterAddress = "I000.2",
            DataType = "INTEGER",
            UnitId = "regulating-tank",
            EquipmentName = "补水电磁阀",
            Purpose = "故障状态"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "碳源搅拌故障",
            RegisterAddress = "I000.3",
            DataType = "INTEGER",
            UnitId = "dosing-1",
            EquipmentName = "碳源搅拌",
            Purpose = "故障状态"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "碳源加药故障",
            RegisterAddress = "I000.4",
            DataType = "INTEGER",
            UnitId = "dosing-1",
            EquipmentName = "碳源加药",
            Purpose = "故障状态"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "格栅机故障",
            RegisterAddress = "I000.5",
            DataType = "INTEGER",
            UnitId = "bar-screen",
            EquipmentName = "格栅机",
            Purpose = "故障状态"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "缺氧池搅拌故障",
            RegisterAddress = "I000.6",
            DataType = "INTEGER",
            UnitId = "anoxic-tank",
            EquipmentName = "缺氧池搅拌机",
            Purpose = "故障状态"
        });
        
        // 污泥回流泵故障
        mappings.Add(new PlcPointMapping
        {
            VariableName = "污泥回流1号泵故障",
            RegisterAddress = "I000.7",
            DataType = "INTEGER",
            UnitId = "mbr-tank",
            EquipmentName = "1#回流泵",
            Purpose = "故障状态"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "污泥回流2号泵故障",
            RegisterAddress = "I001.0",
            DataType = "INTEGER",
            UnitId = "mbr-tank",
            EquipmentName = "2#回流泵",
            Purpose = "故障状态"
        });
        
        // 产水泵故障
        mappings.Add(new PlcPointMapping
        {
            VariableName = "产水泵1号故障",
            RegisterAddress = "I001.1",
            DataType = "INTEGER",
            UnitId = "production-pump",
            EquipmentName = "1#产水泵",
            Purpose = "故障状态"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "产水泵2号故障",
            RegisterAddress = "I001.2",
            DataType = "INTEGER",
            UnitId = "production-pump",
            EquipmentName = "2#产水泵",
            Purpose = "故障状态"
        });
        
        // 风机故障
        mappings.Add(new PlcPointMapping
        {
            VariableName = "风机1号故障",
            RegisterAddress = "I001.6",
            DataType = "INTEGER",
            UnitId = "mbr-tank",
            EquipmentName = "1#鼓风机",
            Purpose = "故障状态"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "风机2号故障",
            RegisterAddress = "I001.7",
            DataType = "INTEGER",
            UnitId = "mbr-tank",
            EquipmentName = "2#鼓风机",
            Purpose = "故障状态"
        });
    }
    
    private static void AddControlMappings(List<PlcPointMapping> mappings)
    {
        // 控制输出（Q输出继电器）
        mappings.Add(new PlcPointMapping
        {
            VariableName = "调节池提升1号",
            RegisterAddress = "Q000.0",
            DataType = "INTEGER",
            UnitId = "regulating-tank",
            EquipmentName = "1#提升泵",
            Purpose = "控制输出"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "调节池提升2号",
            RegisterAddress = "Q000.1",
            DataType = "INTEGER",
            UnitId = "regulating-tank",
            EquipmentName = "2#提升泵",
            Purpose = "控制输出"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "格栅机",
            RegisterAddress = "Q000.5",
            DataType = "INTEGER",
            UnitId = "bar-screen",
            EquipmentName = "格栅机",
            Purpose = "控制输出"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "缺氧池搅拌",
            RegisterAddress = "Q000.6",
            DataType = "INTEGER",
            UnitId = "anoxic-tank",
            EquipmentName = "缺氧池搅拌机",
            Purpose = "控制输出"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "污泥回流1号泵",
            RegisterAddress = "Q000.7",
            DataType = "INTEGER",
            UnitId = "mbr-tank",
            EquipmentName = "1#回流泵",
            Purpose = "控制输出"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "污泥回流2号泵",
            RegisterAddress = "Q001.0",
            DataType = "INTEGER",
            UnitId = "mbr-tank",
            EquipmentName = "2#回流泵",
            Purpose = "控制输出"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "产水泵1号",
            RegisterAddress = "Q001.1",
            DataType = "INTEGER",
            UnitId = "production-pump",
            EquipmentName = "1#产水泵",
            Purpose = "控制输出"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "产水泵2号",
            RegisterAddress = "Q001.2",
            DataType = "INTEGER",
            UnitId = "production-pump",
            EquipmentName = "2#产水泵",
            Purpose = "控制输出"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "风机1号",
            RegisterAddress = "Q001.6",
            DataType = "INTEGER",
            UnitId = "mbr-tank",
            EquipmentName = "1#鼓风机",
            Purpose = "控制输出"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "风机2号",
            RegisterAddress = "Q001.7",
            DataType = "INTEGER",
            UnitId = "mbr-tank",
            EquipmentName = "2#鼓风机",
            Purpose = "控制输出"
        });
    }
    
    private static void AddLevelMappings(List<PlcPointMapping> mappings)
    {
        // 调节池液位
        mappings.Add(new PlcPointMapping
        {
            VariableName = "调节池当前液位",
            RegisterAddress = "VDF100",
            DataType = "SINGLE",
            UnitId = "regulating-tank",
            Purpose = "液位数据",
            AlarmHigh = 80.0,
            AlarmLow = 20.0
        });
        
        // MBR膜池液位
        mappings.Add(new PlcPointMapping
        {
            VariableName = "MBR膜池当前液位",
            RegisterAddress = "VDF200",
            DataType = "SINGLE",
            UnitId = "mbr-tank",
            Purpose = "液位数据",
            AlarmHigh = 85.0,
            AlarmLow = 15.0
        });
        
        // 中间水池液位
        mappings.Add(new PlcPointMapping
        {
            VariableName = "中间水池当前液位",
            RegisterAddress = "VDF300",
            DataType = "SINGLE",
            UnitId = "intermediate-tank",
            Purpose = "液位数据",
            AlarmHigh = 90.0,
            AlarmLow = 10.0
        });
        
        // 电磁流量计
        mappings.Add(new PlcPointMapping
        {
            VariableName = "电磁流量计",
            RegisterAddress = "VDF500",
            DataType = "SINGLE",
            UnitId = "production-pump",
            Purpose = "流量数据",
            AlarmHigh = 15.0,
            AlarmLow = 5.0
        });
    }
    
    private static void AddTimerMappings(List<PlcPointMapping> mappings)
    {
        // 定时参数
        mappings.Add(new PlcPointMapping
        {
            VariableName = "延时启动",
            RegisterAddress = "VWUB130",
            DataType = "SINGLE",
            UnitId = "regulating-tank",
            Purpose = "定时参数"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "启动时长",
            RegisterAddress = "VWUB140",
            DataType = "SINGLE",
            UnitId = "regulating-tank",
            Purpose = "定时参数"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "碳源搅拌启动",
            RegisterAddress = "VWUB160",
            DataType = "SINGLE",
            UnitId = "dosing-1",
            Purpose = "定时参数"
        });
        
        mappings.Add(new PlcPointMapping
        {
            VariableName = "缺氧搅拌启动",
            RegisterAddress = "VWUB170",
            DataType = "SINGLE",
            UnitId = "anoxic-tank",
            Purpose = "定时参数"
        });
    }
    
    /// <summary>
    /// 根据单元ID获取相关的PLC点位映射
    /// </summary>
    public static List<PlcPointMapping> GetMappingsByUnitId(string unitId)
    {
        var allMappings = LoadDefaultMappings();
        return allMappings.FindAll(m => m.UnitId == unitId);
    }
    
    /// <summary>
    /// 根据设备名称获取PLC点位映射
    /// </summary>
    public static List<PlcPointMapping> GetMappingsByEquipment(string unitId, string equipmentName)
    {
        var allMappings = LoadDefaultMappings();
        return allMappings.FindAll(m => m.UnitId == unitId && m.EquipmentName == equipmentName);
    }
}