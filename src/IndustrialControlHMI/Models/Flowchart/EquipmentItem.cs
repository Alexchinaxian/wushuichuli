using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace IndustrialControlHMI.Models.Flowchart;

/// <summary>
/// 表示流程图中的单个设备项（如"格栅机搅拌机"）
/// </summary>
public class EquipmentItem
{
    /// <summary>
    /// 设备名称（例如："格栅机搅拌机"、"1#鼓风机"）
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备类型（例如："泵", "风机", "搅拌机", "阀门"）
    /// </summary>
    public string EquipmentType { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备状态
    /// </summary>
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Running;
    
    /// <summary>
    /// 当前值（可选，例如："12.5 m³/h"、"75%"）
    /// </summary>
    public string? Value { get; set; }
    
    /// <summary>
    /// 原始数值（用于计算和报警）
    /// </summary>
    public double? NumericValue { get; set; }
    
    /// <summary>
    /// 数值单位（例如："m³/h", "%", "℃"）
    /// </summary>
    public string ValueUnit { get; set; } = string.Empty;
    
    /// <summary>
    /// 关联的PLC点位映射ID列表
    /// </summary>
    public List<string> PlcPointIds { get; set; } = new List<string>();
    
    /// <summary>
    /// 是否报警
    /// </summary>
    public bool IsAlarm { get; set; }
    
    /// <summary>
    /// 报警消息
    /// </summary>
    public string AlarmMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 设备在Canvas中的位置（相对于左上角）
    /// </summary>
    public Point Position { get; set; }
    
    /// <summary>
    /// 设备的尺寸（宽度，高度）
    /// </summary>
    public Size Size { get; set; } = new Size(80, 60);
    
    /// <summary>
    /// 坐标备注信息，用于记录和修改位置
    /// </summary>
    public string PositionComment { get; set; } = string.Empty;
    
    /// <summary>
    /// 所属处理单元ID
    /// </summary>
    public string ParentUnitId { get; set; } = string.Empty;
    
    /// <summary>
    /// 根据状态获取对应的显示颜色
    /// </summary>
    public Brush StatusColor => Status switch
    {
        EquipmentStatus.Running => new SolidColorBrush(Color.FromRgb(0, 255, 0)),
        EquipmentStatus.Stopped => new SolidColorBrush(Color.FromRgb(128, 128, 128)),
        EquipmentStatus.Fault => Brushes.Red,
        EquipmentStatus.Warning => Brushes.Orange,
        _ => Brushes.Gray
    };
    
    /// <summary>
    /// 状态指示器的文本表示
    /// </summary>
    public string StatusText => Status switch
    {
        EquipmentStatus.Running => "运行",
        EquipmentStatus.Stopped => "停止",
        EquipmentStatus.Fault => "故障",
        EquipmentStatus.Warning => "警告",
        _ => "未知"
    };
    
    /// <summary>
    /// 获取显示值（带单位）
    /// </summary>
    public string DisplayValue
    {
        get
        {
            if (!string.IsNullOrEmpty(Value))
                return Value;
            
            if (NumericValue.HasValue)
            {
                string unit = string.IsNullOrEmpty(ValueUnit) ? "" : $" {ValueUnit}";
                return $"{NumericValue.Value:F1}{unit}";
            }
            
            return string.Empty;
        }
    }
    
    /// <summary>
    /// 更新设备状态基于PLC值
    /// </summary>
    /// <param name="plcValue">PLC读取的值</param>
    /// <param name="isFault">是否为故障点</param>
    public void UpdateFromPlcValue(object? plcValue, bool isFault = false)
    {
        LastUpdated = DateTime.Now;
        
        if (plcValue == null)
        {
            Status = EquipmentStatus.Stopped;
            Value = "无数据";
            return;
        }
        
        if (isFault)
        {
            // 故障点位：1表示故障，0表示正常
            if (plcValue is int faultValue)
            {
                if (faultValue == 1)
                {
                    Status = EquipmentStatus.Fault;
                    IsAlarm = true;
                    AlarmMessage = "设备故障";
                }
                else
                {
                    Status = EquipmentStatus.Running;
                    IsAlarm = false;
                    AlarmMessage = string.Empty;
                }
            }
        }
        else
        {
            // 非故障点位，根据值类型处理
            if (plcValue is float floatValue)
            {
                NumericValue = floatValue;
                Value = $"{floatValue:F1}";
                
                // 简单判断：如果值为0则停止，大于0则运行
                if (floatValue == 0)
                    Status = EquipmentStatus.Stopped;
                else
                    Status = EquipmentStatus.Running;
            }
            else if (plcValue is int intValue)
            {
                NumericValue = intValue;
                Value = $"{intValue}";
                
                if (intValue == 0)
                    Status = EquipmentStatus.Stopped;
                else
                    Status = EquipmentStatus.Running;
            }
            else
            {
                Value = plcValue.ToString();
            }
        }
    }
}

/// <summary>
/// 设备状态枚举
/// </summary>
public enum EquipmentStatus
{
    /// <summary>
    /// 正常运行
    /// </summary>
    Running,
    
    /// <summary>
    /// 正常停止
    /// </summary>
    Stopped,
    
    /// <summary>
    /// 故障状态
    /// </summary>
    Fault,
    
    /// <summary>
    /// 警告状态
    /// </summary>
    Warning
}