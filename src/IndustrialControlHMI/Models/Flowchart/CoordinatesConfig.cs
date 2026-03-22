using System;
using System.Collections.Generic;
using System.Windows;

namespace IndustrialControlHMI.Models.Flowchart;

/// <summary>
/// 坐标配置项，描述流程图元素的位置和备注信息
/// </summary>
public class CoordinatesConfig
{
    /// <summary>
    /// 元素ID（对应ProcessUnitModel.Id或EquipmentItem.Name）
    /// </summary>
    public string ElementId { get; set; } = string.Empty;

    /// <summary>
    /// 元素类型（"ProcessUnit" 或 "Equipment"）
    /// </summary>
    public string ElementType { get; set; } = string.Empty;

    /// <summary>
    /// 元素在Canvas中的X坐标
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// 元素在Canvas中的Y坐标
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 元素的宽度（可选）
    /// </summary>
    public double Width { get; set; } = 160;

    /// <summary>
    /// 元素的高度（可选）
    /// </summary>
    public double Height { get; set; } = 120;

    /// <summary>
    /// 坐标备注信息，用于描述位置用途或修改说明
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 所属处理单元ID（对于设备类型）
    /// </summary>
    public string ParentUnitId { get; set; } = string.Empty;

    /// <summary>
    /// 创建处理单元坐标配置
    /// </summary>
    public static CoordinatesConfig CreateProcessUnitConfig(string id, double x, double y, string comment = "")
    {
        return new CoordinatesConfig
        {
            ElementId = id,
            ElementType = "ProcessUnit",
            X = x,
            Y = y,
            Width = 160,
            Height = 120,
            Comment = comment,
            IsVisible = true
        };
    }

    /// <summary>
    /// 创建设备坐标配置
    /// </summary>
    public static CoordinatesConfig CreateEquipmentConfig(string id, string parentUnitId, double x, double y, string comment = "")
    {
        return new CoordinatesConfig
        {
            ElementId = id,
            ElementType = "Equipment",
            X = x,
            Y = y,
            Width = 80,
            Height = 60,
            Comment = comment,
            IsVisible = true,
            ParentUnitId = parentUnitId
        };
    }

    /// <summary>
    /// 获取位置点
    /// </summary>
    public Point GetPosition() => new Point(X, Y);

    /// <summary>
    /// 获取尺寸
    /// </summary>
    public Size GetSize() => new Size(Width, Height);
}

/// <summary>
/// 连接线配置项
/// </summary>
public class FlowLineConfig
{
    /// <summary>
    /// 连接线唯一标识符
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 源设备元素ID
    /// </summary>
    public string SourceElementId { get; set; } = string.Empty;

    /// <summary>
    /// 目标设备元素ID（如为空则使用TargetPoint）
    /// </summary>
    public string TargetElementId { get; set; } = string.Empty;

    /// <summary>
    /// 目标点坐标（当TargetElementId为空时使用）
    /// </summary>
    public Point? TargetPoint { get; set; }

    /// <summary>
    /// 连接线类型（MainProcess/WaterSupply/Dosing/Reflux/Backwash等）
    /// </summary>
    public string LineType { get; set; } = "MainProcess";

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否虚线
    /// </summary>
    public bool IsDashed { get; set; }

    /// <summary>
    /// 拐点坐标列表（按顺序）
    /// </summary>
    public List<Point> Waypoints { get; set; } = new List<Point>();
}

/// <summary>
/// 坐标配置集合，包含所有元素的坐标信息
/// </summary>
public class CoordinatesConfigCollection
{
    /// <summary>
    /// 配置版本号
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    /// <summary>
    /// 画布总宽度
    /// </summary>
    public double CanvasWidth { get; set; } = 1400;

    /// <summary>
    /// 画布总高度
    /// </summary>
    public double CanvasHeight { get; set; } = 700;

    /// <summary>
    /// 是否所有处理单元在同一水平线
    /// </summary>
    public bool AlignUnitsHorizontally { get; set; } = false;

    /// <summary>
    /// 水平基准线Y坐标
    /// </summary>
    public double BaselineY { get; set; } = 150;

    /// <summary>
    /// 水平间距
    /// </summary>
    public double HorizontalSpacing { get; set; } = 180;

    /// <summary>
    /// 垂直间距
    /// </summary>
    public double VerticalSpacing { get; set; } = 150;

    /// <summary>
    /// 所有坐标配置项的列表
    /// </summary>
    public List<CoordinatesConfig> Items { get; set; } = new List<CoordinatesConfig>();

    /// <summary>
    /// 所有连接线配置的列表
    /// </summary>
    public List<FlowLineConfig> FlowLines { get; set; } = new List<FlowLineConfig>();

    /// <summary>
    /// 添加处理单元配置
    /// </summary>
    public void AddProcessUnit(string id, double x, double y, string comment = "")
    {
        Items.Add(CoordinatesConfig.CreateProcessUnitConfig(id, x, y, comment));
    }

    /// <summary>
    /// 添加设备配置
    /// </summary>
    public void AddEquipment(string id, string parentUnitId, double x, double y, string comment = "")
    {
        Items.Add(CoordinatesConfig.CreateEquipmentConfig(id, parentUnitId, x, y, comment));
    }

    /// <summary>
    /// 根据元素ID查找配置
    /// </summary>
    public CoordinatesConfig? FindById(string elementId)
    {
        return Items.Find(item => item.ElementId == elementId);
    }

    /// <summary>
    /// 根据元素类型查找配置
    /// </summary>
    public List<CoordinatesConfig> FindByType(string elementType)
    {
        return Items.FindAll(item => item.ElementType == elementType);
    }

    /// <summary>
    /// 获取所有处理单元配置
    /// </summary>
    public List<CoordinatesConfig> GetProcessUnits()
    {
        return FindByType("ProcessUnit");
    }

    /// <summary>
    /// 获取所有设备配置
    /// </summary>
    public List<CoordinatesConfig> GetEquipment()
    {
        return FindByType("Equipment");
    }

    /// <summary>
    /// 获取指定处理单元下的设备配置
    /// </summary>
    public List<CoordinatesConfig> GetEquipmentByParent(string parentUnitId)
    {
        return Items.FindAll(item => item.ElementType == "Equipment" && item.ParentUnitId == parentUnitId);
    }
}