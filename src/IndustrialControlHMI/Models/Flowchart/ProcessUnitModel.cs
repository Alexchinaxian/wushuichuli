using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IndustrialControlHMI.Models.Flowchart;

/// <summary>
/// 表示流程图中的一个处理单元（如"格栅机"、"调节池"）
/// </summary>
public class ProcessUnitModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 触发属性更改通知
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 设置属性值并触发通知
    /// </summary>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    /// <summary>
    /// 单元唯一标识符（对应HTML中的data-unit属性）
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 单元显示标题（例如："格栅机"）
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 单元类型（例如："格栅机", "调节池", "MBR膜池", "消毒池"）
    /// </summary>
    public string UnitType { get; set; } = string.Empty;
    
    private Point _position;
    private Size _size = new Size(140, 100);
    
    /// <summary>
    /// 单元在Canvas中的位置（相对于左上角）
    /// </summary>
    public Point Position
    {
        get => _position;
        set => SetField(ref _position, value);
    }
    
    /// <summary>
    /// 单元的尺寸（宽度，高度）
    /// </summary>
    public Size Size
    {
        get => _size;
        set => SetField(ref _size, value);
    }
    
    /// <summary>
    /// 单元中包含的设备列表
    /// </summary>
    public ObservableCollection<EquipmentItem> Equipment { get; set; } = new();
    
    /// <summary>
    /// 关联的PLC点位映射ID列表
    /// </summary>
    public List<string> PlcPointIds { get; set; } = new List<string>();
    
    /// <summary>
    /// 单元是否处于激活状态（点击选中）
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// 单元是否处于悬停状态
    /// </summary>
    public bool IsHovered { get; set; }
    
    /// <summary>
    /// 单元是否处于报警状态
    /// </summary>
    public bool IsAlarm { get; set; }
    
    /// <summary>
    /// 报警消息
    /// </summary>
    public string AlarmMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// 单元的整体状态（基于设备状态汇总）
    /// </summary>
    public EquipmentStatus Status
    {
        get
        {
            if (IsAlarm) return EquipmentStatus.Fault;
            if (Equipment.Count == 0) return EquipmentStatus.Running;
            
            var anyFault = Equipment.Any(e => e.Status == EquipmentStatus.Fault);
            var anyWarning = Equipment.Any(e => e.Status == EquipmentStatus.Warning);
            var anyRunning = Equipment.Any(e => e.Status == EquipmentStatus.Running);
            var anyStopped = Equipment.Any(e => e.Status == EquipmentStatus.Stopped);
            
            if (anyFault) return EquipmentStatus.Fault;
            if (anyWarning) return EquipmentStatus.Warning;
            if (anyRunning) return EquipmentStatus.Running;
            if (anyStopped) return EquipmentStatus.Stopped;
            
            return EquipmentStatus.Running;
        }
    }
    
    /// <summary>
    /// 是否正在运行动画（用于UI动画绑定）
    /// </summary>
    public bool IsAnimating { get; set; }
    
    /// <summary>
    /// 动画不透明度（用于呼吸效果）
    /// </summary>
    public double AnimationOpacity { get; set; } = 1.0;
    
    /// <summary>
    /// 缩放比例（用于悬停效果）
    /// </summary>
    public double ScaleFactor { get; set; } = 1.0;
    
    /// <summary>
    /// 单元的背景颜色（默认为白色）
    /// </summary>
    public System.Windows.Media.Color BackgroundColor { get; set; } = System.Windows.Media.Colors.White;
    
    /// <summary>
    /// 报警状态的背景颜色
    /// </summary>
    public System.Windows.Media.Color AlarmBackgroundColor { get; set; } = System.Windows.Media.Color.FromRgb(255, 235, 238);
    
    /// <summary>
    /// 运行状态的背景颜色（绿色渐变）
    /// </summary>
    public System.Windows.Media.Color RunningBackgroundColor { get; set; } = System.Windows.Media.Color.FromRgb(232, 245, 233);
    
    /// <summary>
    /// 停止状态的背景颜色（灰色渐变）
    /// </summary>
    public System.Windows.Media.Color StoppedBackgroundColor { get; set; } = System.Windows.Media.Color.FromRgb(245, 245, 245);
    
    /// <summary>
    /// 故障状态的背景颜色（红色渐变）
    /// </summary>
    public System.Windows.Media.Color FaultBackgroundColor { get; set; } = System.Windows.Media.Color.FromRgb(255, 235, 238);
    
    /// <summary>
    /// 警告状态的背景颜色（半透明橙色）
    /// </summary>
    public System.Windows.Media.Color WarningBackgroundColor { get; set; } = System.Windows.Media.Color.FromArgb(204, 58, 46, 31); // #3A2E1F CC
    
    /// <summary>
    /// 离线状态的背景颜色（半透明深灰色）
    /// </summary>
    public System.Windows.Media.Color OfflineBackgroundColor { get; set; } = System.Windows.Media.Color.FromArgb(204, 44, 44, 46); // #2C2C2E CC
    
    /// <summary>
    /// 单元的边框颜色
    /// </summary>
    public System.Windows.Media.Color BorderColor { get; set; } = System.Windows.Media.Color.FromRgb(52, 73, 94);
    
    /// <summary>
    /// 报警状态的边框颜色
    /// </summary>
    public System.Windows.Media.Color AlarmBorderColor { get; set; } = System.Windows.Media.Color.FromRgb(244, 67, 54);
    
    /// <summary>
    /// 激活状态的边框颜色
    /// </summary>
    public System.Windows.Media.Color ActiveBorderColor { get; set; } = System.Windows.Media.Color.FromRgb(52, 152, 219);
    
    /// <summary>
    /// 运行状态的边框颜色
    /// </summary>
    public System.Windows.Media.Color RunningBorderColor { get; set; } = System.Windows.Media.Color.FromRgb(0, 255, 0);
    
    /// <summary>
    /// 停止状态的边框颜色
    /// </summary>
    public System.Windows.Media.Color StoppedBorderColor { get; set; } = System.Windows.Media.Color.FromRgb(128, 128, 128);
    
    /// <summary>
    /// 警告状态的边框颜色（橙色）
    /// </summary>
    public System.Windows.Media.Color WarningBorderColor { get; set; } = System.Windows.Media.Color.FromRgb(255, 149, 0); // #FF9500
    
    /// <summary>
    /// 离线/停止状态的边框颜色（浅灰色）
    /// </summary>
    public System.Windows.Media.Color OfflineBorderColor { get; set; } = System.Windows.Media.Color.FromRgb(128, 128, 128); // #808080
    
    /// <summary>
    /// 主色调 - 运行态（蓝色）
    /// </summary>
    public System.Windows.Media.Color PrimaryColor { get; set; } = System.Windows.Media.Color.FromRgb(0, 255, 0); // #00FF00
    
    /// <summary>
    /// 主色半透明背景 - 运行态（深蓝色半透明）
    /// </summary>
    public System.Windows.Media.Color PrimaryBackgroundColor { get; set; } = System.Windows.Media.Color.FromArgb(204, 30, 42, 74); // #1E2A4A CC
    
    /// <summary>
    /// 报警态红色半透明背景
    /// </summary>
    public System.Windows.Media.Color AlarmRedBackgroundColor { get; set; } = System.Windows.Media.Color.FromArgb(204, 58, 31, 31); // #3A1F1F CC
    
    /// <summary>
    /// 获取当前边框颜色（根据状态）- 调节池专用状态样式
    /// </summary>
    public System.Windows.Media.Color CurrentBorderColor
    {
        get
        {
            // 优先级：报警 > 故障 > 警告 > 激活 > 停止/离线 > 运行
            if (IsAlarm) return System.Windows.Media.Color.FromRgb(255, 59, 48); // #FF3B30 红色
            if (Status == EquipmentStatus.Fault) return System.Windows.Media.Color.FromRgb(255, 59, 48); // #FF3B30
            if (Status == EquipmentStatus.Warning) return WarningBorderColor; // #FF9500 橙色
            if (IsActive) return ActiveBorderColor;
            if (Status == EquipmentStatus.Stopped) return OfflineBorderColor; // #8E8E93 浅灰
            return PrimaryColor; // #0077FF 主色蓝色
        }
    }
    
    /// <summary>
    /// 获取当前背景颜色（根据状态）- 调节池专用状态样式
    /// </summary>
    public System.Windows.Media.Color CurrentBackgroundColor
    {
        get
        {
            // 优先级：报警 > 故障 > 警告 > 停止/离线 > 运行
            if (IsAlarm) return AlarmRedBackgroundColor;
            if (Status == EquipmentStatus.Fault) return AlarmRedBackgroundColor;
            if (Status == EquipmentStatus.Warning) return WarningBackgroundColor;
            if (Status == EquipmentStatus.Stopped) return OfflineBackgroundColor;
            if (Status == EquipmentStatus.Running) return PrimaryBackgroundColor;
            return PrimaryBackgroundColor;
        }
    }
    
    /// <summary>
    /// 单元的边框厚度
    /// </summary>
    public double BorderThickness { get; set; } = 2.0;
    
    /// <summary>
    /// 报警状态的边框厚度
    /// </summary>
    public double AlarmBorderThickness { get; set; } = 3.0;
    
    /// <summary>
    /// 运行状态的边框厚度
    /// </summary>
    public double RunningBorderThickness { get; set; } = 2.5;
    
    /// <summary>
    /// 获取当前边框厚度
    /// </summary>
    public double CurrentBorderThickness
    {
        get
        {
            if (IsAlarm) return AlarmBorderThickness;
            if (Status == EquipmentStatus.Running) return RunningBorderThickness;
            return BorderThickness;
        }
    }
    
    /// <summary>
    /// 单元的圆角半径
    /// </summary>
    public double CornerRadius { get; set; } = 6.0;
    
    /// <summary>
    /// 单元的Z索引（用于控制绘制顺序）
    /// </summary>
    public int ZIndex { get; set; } = 10;
    
    /// <summary>
    /// 单元的工具提示文本（简单版）
    /// </summary>
    public string ToolTip { get; set; } = string.Empty;
    
    /// <summary>
    /// 详细的工具提示HTML内容（用于自定义ToolTip）
    /// </summary>
    public string ToolTipDetailed { get; set; } = string.Empty;
    
    /// <summary>
    /// 单元的关键参数值（例如："液位: 75%", "流量: 12.5 m³/h"）
    /// </summary>
    public Dictionary<string, string> KeyParameters { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 坐标备注信息，用于记录和修改位置
    /// </summary>
    public string PositionComment { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备运行时间（小时）
    /// </summary>
    public double RunningHours { get; set; }
    
    /// <summary>
    /// PLC点位数量
    /// </summary>
    public int PlcPointCount => PlcPointIds?.Count ?? 0;
    
    /// <summary>
    /// 获取关键参数显示文本（用于XAML绑定）
    /// </summary>
    public string KeyParametersText
    {
        get
        {
            if (KeyParameters.Count == 0)
                return string.Empty;
            
            var texts = new List<string>();
            foreach (var kvp in KeyParameters)
            {
                texts.Add($"{kvp.Key}: {kvp.Value}");
            }
            return string.Join(" | ", texts);
        }
    }
    
    /// <summary>
    /// 创建默认的处理单元模型（用于测试）
    /// </summary>
    public static ProcessUnitModel CreateDefault(string id, string title, Point position)
    {
        return new ProcessUnitModel
        {
            Id = id,
            Title = title,
            Position = position,
            Size = new Size(140, 100),
            ToolTip = $"点击查看{title}的详细信息"
        };
    }
    
    /// <summary>
    /// 创建MBR污水处理工艺单元
    /// </summary>
    public static ProcessUnitModel CreateMbrProcessUnit(string id, string title, Point position, string unitType = "")
    {
        return new ProcessUnitModel
        {
            Id = id,
            Title = title,
            UnitType = string.IsNullOrEmpty(unitType) ? title : unitType,
            Position = position,
            Size = new Size(160, 120),
            ToolTip = $"点击查看{title}的实时数据和设备状态"
        };
    }
    
    /// <summary>
    /// 获取单元的边界矩形
    /// </summary>
    public Rect GetBounds()
    {
        return new Rect(Position, Size);
    }
    
    /// <summary>
    /// 检查点是否在单元边界内
    /// </summary>
    public bool ContainsPoint(Point point)
    {
        var bounds = GetBounds();
        return bounds.Contains(point);
    }
    
    /// <summary>
    /// 更新单元报警状态
    /// </summary>
    public void UpdateAlarmStatus()
    {
        bool hasAlarm = false;
        string alarmMsg = string.Empty;
        
        foreach (var equipment in Equipment)
        {
            if (equipment.IsAlarm)
            {
                hasAlarm = true;
                if (string.IsNullOrEmpty(alarmMsg))
                    alarmMsg = $"{equipment.Name}: {equipment.AlarmMessage}";
                else
                    alarmMsg += $"; {equipment.Name}: {equipment.AlarmMessage}";
            }
        }
        
        IsAlarm = hasAlarm;
        AlarmMessage = alarmMsg;
        
        // 更新工具提示以包含报警信息
        if (hasAlarm)
        {
            ToolTip = $"{Title} - 报警: {alarmMsg}";
        }
        else
        {
            ToolTip = $"点击查看{Title}的实时数据和设备状态";
        }
        
        // 更新详细工具提示
        UpdateDetailedToolTip();
        
        LastUpdated = DateTime.Now;
    }
    
    /// <summary>
    /// 更新详细工具提示内容
    /// </summary>
    public void UpdateDetailedToolTip()
    {
        var lines = new List<string>();
        lines.Add($"<b>{Title}</b>");
        lines.Add($"类型: {UnitType}");
        lines.Add($"状态: {StatusText}");
        lines.Add($"位置: ({Position.X:F0}, {Position.Y:F0})");
        lines.Add($"尺寸: {Size.Width:F0}×{Size.Height:F0}");
        
        if (PlcPointCount > 0)
            lines.Add($"PLC点位: {PlcPointCount}个");
        
        if (RunningHours > 0)
            lines.Add($"运行时间: {RunningHours:F1}小时");
        
        if (KeyParameters.Count > 0)
        {
            lines.Add("关键参数:");
            foreach (var kvp in KeyParameters)
            {
                lines.Add($"  {kvp.Key}: {kvp.Value}");
            }
        }
        
        if (Equipment.Count > 0)
        {
            lines.Add($"设备数量: {Equipment.Count}");
            foreach (var eq in Equipment.Take(3))
            {
                lines.Add($"  {eq.Name}: {eq.StatusText}");
            }
            if (Equipment.Count > 3)
                lines.Add($"  ... 还有{Equipment.Count - 3}个设备");
        }
        
        if (!string.IsNullOrEmpty(AlarmMessage))
            lines.Add($"<b style='color:red'>报警: {AlarmMessage}</b>");
        
        ToolTipDetailed = string.Join("<br/>", lines);
    }
    
    /// <summary>
    /// 获取状态文本
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
    /// 模拟故障状态（用于测试）
    /// </summary>
    public void SimulateFault()
    {
        IsAlarm = true;
        AlarmMessage = "手动模拟故障";
        
        // 将第一个设备设置为故障状态
        if (Equipment.Count > 0)
        {
            Equipment[0].Status = EquipmentStatus.Fault;
            Equipment[0].IsAlarm = true;
            Equipment[0].AlarmMessage = "手动模拟故障";
        }
        
        UpdateDetailedToolTip();
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(CurrentBackgroundColor));
        OnPropertyChanged(nameof(CurrentBorderColor));
    }
    
    /// <summary>
    /// 复位故障状态
    /// </summary>
    public void ResetFault()
    {
        IsAlarm = false;
        AlarmMessage = string.Empty;
        
        foreach (var eq in Equipment)
        {
            if (eq.Status == EquipmentStatus.Fault || eq.Status == EquipmentStatus.Warning)
            {
                eq.Status = EquipmentStatus.Running;
                eq.IsAlarm = false;
                eq.AlarmMessage = string.Empty;
            }
        }
        
        UpdateDetailedToolTip();
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(CurrentBackgroundColor));
        OnPropertyChanged(nameof(CurrentBorderColor));
    }
    
    /// <summary>
    /// 开始运行动画
    /// </summary>
    public void StartAnimation()
    {
        IsAnimating = true;
        OnPropertyChanged(nameof(IsAnimating));
    }
    
    /// <summary>
    /// 停止动画
    /// </summary>
    public void StopAnimation()
    {
        IsAnimating = false;
        AnimationOpacity = 1.0;
        ScaleFactor = 1.0;
        OnPropertyChanged(nameof(IsAnimating));
        OnPropertyChanged(nameof(AnimationOpacity));
        OnPropertyChanged(nameof(ScaleFactor));
    }
    
    /// <summary>
    /// 更新动画参数
    /// </summary>
    public void UpdateAnimation(double opacity, double scale)
    {
        AnimationOpacity = opacity;
        ScaleFactor = scale;
        OnPropertyChanged(nameof(AnimationOpacity));
        OnPropertyChanged(nameof(ScaleFactor));
    }
    
    /// <summary>
    /// 添加关键参数
    /// </summary>
    public void AddKeyParameter(string name, string value)
    {
        if (KeyParameters.ContainsKey(name))
            KeyParameters[name] = value;
        else
            KeyParameters.Add(name, value);
    }
    
    /// <summary>
    /// 获取关键参数显示文本
    /// </summary>
    public string GetKeyParametersText()
    {
        if (KeyParameters.Count == 0)
            return string.Empty;
        
        var texts = new List<string>();
        foreach (var kvp in KeyParameters)
        {
            texts.Add($"{kvp.Key}: {kvp.Value}");
        }
        return string.Join(" | ", texts);
    }
}