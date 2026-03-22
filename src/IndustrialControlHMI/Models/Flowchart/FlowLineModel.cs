using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace IndustrialControlHMI.Models.Flowchart;

/// <summary>
/// 流程图连接线类型
/// </summary>
public enum FlowLineType
{
    /// <summary>
    /// 主工艺流程（灰色箭头）
    /// </summary>
    MainProcess,
    
    /// <summary>
    /// 补水线路（绿色箭头）
    /// </summary>
    WaterSupply,
    
    /// <summary>
    /// 加药线路（棕色箭头）
    /// </summary>
    Dosing,
    
    /// <summary>
    /// 回流线路（蓝色箭头）
    /// </summary>
    Reflux,
    
    /// <summary>
    /// 反洗线路（蓝色虚线）
    /// </summary>
    Backwash,
    
    /// <summary>
    /// 曝气流程（黄色箭头）
    /// </summary>
    Aeration,
    
    /// <summary>
    /// 除臭流程（棕色箭头）
    /// </summary>
    Deodorization
}

/// <summary>
/// 连接线状态
/// </summary>
public enum FlowLineStatus
{
    /// <summary>
    /// 正常流动
    /// </summary>
    Flowing,
    
    /// <summary>
    /// 停止流动
    /// </summary>
    Stopped,
    
    /// <summary>
    /// 故障状态
    /// </summary>
    Fault
}

/// <summary>
/// 表示流程图中的一条连接线
/// </summary>
public class FlowLineModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    /// <summary>
    /// 线条的唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 线条的起点坐标
    /// </summary>
    public Point Start { get; set; }
    
    /// <summary>
    /// 线条的终点坐标
    /// </summary>
    public Point End { get; set; }
    
    /// <summary>
    /// 线条类型
    /// </summary>
    public FlowLineType Type { get; set; } = FlowLineType.MainProcess;
    
    /// <summary>
    /// 是否为虚线
    /// </summary>
    public bool IsDashed { get; set; }
    
    /// <summary>
    /// 线条厚度
    /// </summary>
    public double Thickness { get; set; } = 2.0;
    
    /// <summary>
    /// 是否显示箭头
    /// </summary>
    public bool ShowArrow { get; set; } = true;
    
    /// <summary>
    /// 箭头大小
    /// </summary>
    public double ArrowSize { get; set; } = 8.0;
    
    private FlowLineStatus _status = FlowLineStatus.Flowing;
    /// <summary>
    /// 线条状态
    /// </summary>
    public FlowLineStatus Status
    {
        get => _status;
        set
        {
            if (SetField(ref _status, value))
            {
                OnPropertyChanged(nameof(LineColorWithStatus));
                OnPropertyChanged(nameof(IsAnimating));
            }
        }
    }
    
    private bool _isAnimating = true;
    /// <summary>
    /// 是否正在播放流动动画
    /// </summary>
    public bool IsAnimating
    {
        get => _isAnimating;
        set => SetField(ref _isAnimating, value);
    }
    
    private double _flowSpeed = 1.0;
    /// <summary>
    /// 流动速度系数（1.0为正常速度）
    /// </summary>
    public double FlowSpeed
    {
        get => _flowSpeed;
        set => SetField(ref _flowSpeed, value);
    }
    
    private double _animationOffset = 0.0;
    /// <summary>
    /// 动画偏移量（用于虚线流动效果）
    /// </summary>
    public double AnimationOffset
    {
        get => _animationOffset;
        set => SetField(ref _animationOffset, value);
    }
    
    /// <summary>
    /// 根据状态调整后的线条颜色
    /// </summary>
    public Color LineColorWithStatus
    {
        get
        {
            var baseColor = LineColor;
            if (Status == FlowLineStatus.Fault)
            {
                // 故障状态显示为红色
                return Color.FromRgb(255, 0, 0);
            }
            else if (Status == FlowLineStatus.Stopped)
            {
                // 停止状态显示为灰色
                return Color.FromRgb(200, 200, 200);
            }
            return baseColor;
        }
    }
    
    /// <summary>
    /// 获取线条的颜色（根据类型）
    /// </summary>
    public Color LineColor
    {
        get
        {
            // 从配置系统获取颜色
            var colorHex = WiringConfigLoader.GetLineColor(Type);
            
            try
            {
                // 将十六进制颜色转换为Color
                if (colorHex.StartsWith("#") && colorHex.Length == 7)
                {
                    var r = Convert.ToByte(colorHex.Substring(1, 2), 16);
                    var g = Convert.ToByte(colorHex.Substring(3, 2), 16);
                    var b = Convert.ToByte(colorHex.Substring(5, 2), 16);
                    return Color.FromRgb(r, g, b);
                }
            }
            catch
            {
                // 如果配置解析失败，使用默认颜色
            }
            
            // 默认颜色（兼容旧代码）
            return Type switch
            {
                FlowLineType.MainProcess => Color.FromRgb(128, 128, 128),  // 灰色
                FlowLineType.WaterSupply => Color.FromRgb(39, 174, 96),    // 绿色
                FlowLineType.Dosing => Color.FromRgb(139, 69, 19),         // 棕色
                FlowLineType.Reflux => Color.FromRgb(52, 152, 219),        // 蓝色
                FlowLineType.Backwash => Color.FromRgb(52, 152, 219),      // 蓝色
                FlowLineType.Aeration => Color.FromRgb(255, 204, 0),       // 黄色
                FlowLineType.Deodorization => Color.FromRgb(139, 69, 19),  // 棕色（与加药相同）
                _ => Colors.Blue
            };
        }
    }
    
    /// <summary>
    /// 获取线条的虚线样式（如果是虚线）
    /// </summary>
    public DoubleCollection? DashStyle => IsDashed ? new DoubleCollection { 4, 3 } : null;
    
    /// <summary>
    /// 计算线条的长度
    /// </summary>
    public double Length
    {
        get
        {
            var deltaX = End.X - Start.X;
            var deltaY = End.Y - Start.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
    }
    
    /// <summary>
    /// 计算线条的角度（弧度）
    /// </summary>
    public double AngleRadians
    {
        get
        {
            var deltaX = End.X - Start.X;
            var deltaY = End.Y - Start.Y;
            return Math.Atan2(deltaY, deltaX);
        }
    }
    
    /// <summary>
    /// 计算线条的角度（度）
    /// </summary>
    public double AngleDegrees => AngleRadians * 180 / Math.PI;
    
    /// <summary>
    /// 箭头左侧点
    /// </summary>
    public Point ArrowLeftPoint
    {
        get
        {
            var angle = AngleRadians;
            var leftX = End.X - ArrowSize * Math.Cos(angle - Math.PI / 6);
            var leftY = End.Y - ArrowSize * Math.Sin(angle - Math.PI / 6);
            return new Point(leftX, leftY);
        }
    }
    
    /// <summary>
    /// 箭头右侧点
    /// </summary>
    public Point ArrowRightPoint
    {
        get
        {
            var angle = AngleRadians;
            var rightX = End.X - ArrowSize * Math.Cos(angle + Math.PI / 6);
            var rightY = End.Y - ArrowSize * Math.Sin(angle + Math.PI / 6);
            return new Point(rightX, rightY);
        }
    }
    
    /// <summary>
    /// 创建一条水平连接线
    /// </summary>
    public static FlowLineModel CreateHorizontal(Point start, double length, FlowLineType type, bool isDashed = false)
    {
        return new FlowLineModel
        {
            Start = start,
            End = new Point(start.X + length, start.Y),
            Type = type,
            IsDashed = isDashed
        };
    }
    
    /// <summary>
    /// 创建一条垂直连接线
    /// </summary>
    public static FlowLineModel CreateVertical(Point start, double length, FlowLineType type, bool isDashed = false)
    {
        return new FlowLineModel
        {
            Start = start,
            End = new Point(start.X, start.Y + length),
            Type = type,
            IsDashed = isDashed
        };
    }
    
    /// <summary>
    /// 创建一条对角线连接线
    /// </summary>
    public static FlowLineModel CreateDiagonal(Point start, Point end, FlowLineType type, bool isDashed = false)
    {
        return new FlowLineModel
        {
            Start = start,
            End = end,
            Type = type,
            IsDashed = isDashed
        };
    }
    
    /// <summary>
    /// 创建直角连接线（横平竖直）- 硬编码路径
    /// </summary>
    /// <param name="waypoints">路径点序列（包含起点、中间点、终点）</param>
    /// <param name="type">线条类型</param>
    /// <param name="isDashed">是否虚线</param>
    public static FlowLineModel CreateOrthogonal(Point[] waypoints, FlowLineType type, bool isDashed = false)
    {
        if (waypoints == null || waypoints.Length < 2)
            throw new ArgumentException("路径点至少需要包含起点和终点");
        
        var line = new FlowLineModel
        {
            Start = waypoints[0],
            End = waypoints[waypoints.Length - 1],
            Type = type,
            IsDashed = isDashed,
            IsOrthogonal = true
        };
        
        // 添加中间点（不包括起点和终点）
        for (int i = 1; i < waypoints.Length - 1; i++)
        {
            line.IntermediatePoints.Add(waypoints[i]);
        }
        
        return line;
    }
    
    /// <summary>
    /// 是否为直角连接线（横平竖直）
    /// </summary>
    public bool IsOrthogonal { get; set; } = false;
    
    /// <summary>
    /// 直角连接线的中间转折点（按顺序）
    /// </summary>
    public List<Point> IntermediatePoints { get; set; } = new List<Point>();
    
    /// <summary>
    /// 获取完整路径点（起点 + 中间点 + 终点）
    /// </summary>
    public List<Point> GetPathPoints()
    {
        var points = new List<Point> { Start };
        points.AddRange(IntermediatePoints);
        points.Add(End);
        return points;
    }
}