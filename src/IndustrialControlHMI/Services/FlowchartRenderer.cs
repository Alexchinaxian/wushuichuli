using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using IndustrialControlHMI.Models.Flowchart;

namespace IndustrialControlHMI.Services;

/// <summary>
/// 流程图渲染服务，负责从配置文件加载坐标数据并渲染到WPF控件
/// </summary>
public class FlowchartRenderer
{
    private double _canvasWidth = 1800.0;
    private double _canvasHeight = 900.0;

    /// <summary>
    /// 当前画布宽度
    /// </summary>
    public double CanvasWidth
    {
        get => _canvasWidth;
        set
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "画布宽度必须大于0");
            _canvasWidth = value;
        }
    }

    /// <summary>
    /// 当前画布高度
    /// </summary>
    public double CanvasHeight
    {
        get => _canvasHeight;
        set
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "画布高度必须大于0");
            _canvasHeight = value;
        }
    }

    /// <summary>
    /// 是否启用坐标对齐（水平对齐）
    /// </summary>
    public bool AlignUnitsHorizontally { get; set; } = false;

    /// <summary>
    /// 基准Y坐标（用于水平对齐）
    /// </summary>
    public double BaselineY { get; set; } = 150.0;

    /// <summary>
    /// 初始化流程图渲染服务
    /// </summary>
    public FlowchartRenderer()
    {
    }

    /// <summary>
    /// 异步加载所有处理单元
    /// </summary>
    /// <returns>处理单元集合</returns>
    public async Task<ObservableCollection<ProcessUnitModel>> LoadProcessUnitsAsync()
    {
        return await Task.Run(() =>
        {
            var units = FlowchartDataProvider.LoadProcessUnits();
            AdjustUnitsToCanvas(units);
            return units;
        });
    }

    /// <summary>
    /// 异步加载所有连接线
    /// </summary>
    /// <param name="units">处理单元集合（用于计算连接点）</param>
    /// <returns>连接线集合</returns>
    public async Task<ObservableCollection<FlowLineModel>> LoadFlowLinesAsync(ObservableCollection<ProcessUnitModel> units)
    {
        return await Task.Run(() =>
        {
            var lines = FlowchartDataProvider.LoadFlowLines();
            AdjustLinesToCanvas(lines, units);
            return lines;
        });
    }

    /// <summary>
    /// 加载坐标配置文件（如果存在）并应用坐标
    /// </summary>
    /// <param name="units">要应用坐标的处理单元集合</param>
    /// <returns>是否成功应用配置</returns>
    public async Task<bool> ApplyCoordinatesConfigAsync(ObservableCollection<ProcessUnitModel> units)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 通过FlowchartDataProvider内部机制加载配置（已经内置）
                // 这里我们手动调整画布尺寸
                AdjustUnitsToCanvas(units);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
    }

    /// <summary>
    /// 将单元位置调整到当前画布尺寸
    /// </summary>
    private void AdjustUnitsToCanvas(ObservableCollection<ProcessUnitModel> units)
    {
        if (units == null || units.Count == 0) return;

        // 如果启用了水平对齐，将所有单元对齐到基准Y坐标
        if (AlignUnitsHorizontally)
        {
            foreach (var unit in units)
            {
                unit.Position = new Point(unit.Position.X, BaselineY);
            }
        }

        // 确保单元位置在画布范围内（可选，可以根据需要调整）
        foreach (var unit in units)
        {
            // 检查是否超出画布边界
            double x = unit.Position.X;
            double y = unit.Position.Y;
            double width = unit.Size.Width;
            double height = unit.Size.Height;

            if (x + width > CanvasWidth)
                x = CanvasWidth - width - 10;
            if (y + height > CanvasHeight)
                y = CanvasHeight - height - 10;
            if (x < 0) x = 10;
            if (y < 0) y = 10;

            if (x != unit.Position.X || y != unit.Position.Y)
            {
                unit.Position = new Point(x, y);
            }
        }
    }

    /// <summary>
    /// 将连接线调整到当前画布尺寸
    /// </summary>
    private void AdjustLinesToCanvas(ObservableCollection<FlowLineModel> lines, ObservableCollection<ProcessUnitModel> units)
    {
        if (lines == null || units == null) return;

        // 连接线的坐标基于单元中心点，单元位置调整后连接线会自动适应
        // 这里可以添加额外的逻辑，比如检查连接线是否超出画布等
        foreach (var line in lines)
        {
            // 确保起点和终点在画布内
            if (line.Start.X < 0 || line.Start.X > CanvasWidth ||
                line.Start.Y < 0 || line.Start.Y > CanvasHeight ||
                line.End.X < 0 || line.End.X > CanvasWidth ||
                line.End.Y < 0 || line.End.Y > CanvasHeight)
            {
                // 如果超出画布，可以尝试调整（但通常不会发生，因为单元位置已调整）
                // 这里仅记录警告
                System.Diagnostics.Debug.WriteLine($"[FlowchartRenderer] 连接线 {line.Id} 坐标超出画布范围");
            }
        }
    }

    /// <summary>
    /// 计算单元在画布上的边界矩形
    /// </summary>
    /// <param name="unit">处理单元</param>
    /// <returns>边界矩形</returns>
    public Rect GetUnitBounds(ProcessUnitModel unit)
    {
        return new Rect(unit.Position, unit.Size);
    }

    /// <summary>
    /// 检查点是否在任意单元内
    /// </summary>
    /// <param name="point">画布坐标点</param>
    /// <param name="units">单元集合</param>
    /// <returns>命中的单元，如果没有则返回null</returns>
    public ProcessUnitModel? HitTest(Point point, ObservableCollection<ProcessUnitModel> units)
    {
        return units.FirstOrDefault(u => u.ContainsPoint(point));
    }

    /// <summary>
    /// 刷新所有单元和连接线的视觉状态（如报警状态、激活状态）
    /// </summary>
    /// <param name="units">单元集合</param>
    /// <param name="lines">连接线集合</param>
    public void RefreshVisualState(ObservableCollection<ProcessUnitModel> units, ObservableCollection<FlowLineModel> lines)
    {
        // 仅更新报警状态，UI绑定将根据属性变更通知自动更新
        foreach (var unit in units)
        {
            unit.UpdateAlarmStatus();
        }
    }

    /// <summary>
    /// 保存当前单元位置到配置文件
    /// </summary>
    /// <param name="units">单元集合</param>
    /// <returns>是否保存成功</returns>
    public async Task<bool> SaveCoordinatesAsync(ObservableCollection<ProcessUnitModel> units)
    {
        return await Task.Run(() =>
        {
            try
            {
                return FlowchartDataProvider.SaveCoordinatesConfig(units);
            }
            catch (Exception)
            {
                return false;
            }
        });
    }

    /// <summary>
    /// 获取画布布局描述信息
    /// </summary>
    /// <returns>布局描述字典</returns>
    public System.Collections.Generic.Dictionary<string, string> GetLayoutDescription()
    {
        return FlowchartDataProvider.GetLayoutDescription();
    }

    /// <summary>
    /// 设置画布尺寸（并更新所有单元和连接线位置）
    /// </summary>
    /// <param name="width">新宽度</param>
    /// <param name="height">新高度</param>
    /// <param name="units">单元集合（可选）</param>
    /// <param name="lines">连接线集合（可选）</param>
    public void SetCanvasSize(double width, double height, 
        ObservableCollection<ProcessUnitModel>? units = null, 
        ObservableCollection<FlowLineModel>? lines = null)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException("画布尺寸必须大于0");

        CanvasWidth = width;
        CanvasHeight = height;

        // 如果提供了单元和连接线，调整它们的位置
        if (units != null)
            AdjustUnitsToCanvas(units);
        if (lines != null && units != null)
            AdjustLinesToCanvas(lines, units);
    }
}