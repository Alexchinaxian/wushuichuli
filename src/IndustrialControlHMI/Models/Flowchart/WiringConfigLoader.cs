using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using IndustrialControlHMI.Infrastructure.Config;
using IndustrialControlHMI.Services.Logging;

namespace IndustrialControlHMI.Models.Flowchart;

/// <summary>
/// 接线配置加载器
/// 从JSON配置文件加载接线配置，减少硬编码
/// </summary>
public static class WiringConfigLoader
{
    private static WiringConfig? _config;
    private static readonly object _lock = new object();
    private static readonly string ConfigPath = AppConfigPaths.GetConfigFilePath("wiring_config.json");

    /// <summary>
    /// 获取接线配置（单例模式）
    /// </summary>
    public static WiringConfig GetConfig()
    {
        lock (_lock)
        {
            if (_config == null)
            {
                _config = LoadConfig();
            }
            return _config;
        }
    }

    /// <summary>
    /// 重新加载配置
    /// </summary>
    public static void ReloadConfig()
    {
        lock (_lock)
        {
            _config = LoadConfig();
        }
    }

    /// <summary>
    /// 加载配置文件
    /// </summary>
    private static WiringConfig LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                AppRuntimeLogger.Warn($"接线配置文件不存在: {ConfigPath}");
                return CreateDefaultConfig();
            }

            string json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<WiringConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (config == null)
            {
                AppRuntimeLogger.Warn("接线配置反序列化失败，使用默认配置");
                return CreateDefaultConfig();
            }

            AppRuntimeLogger.Info($"接线配置加载成功，版本={config.Version}");
            return config;
        }
        catch (Exception ex)
        {
            AppRuntimeLogger.Error("接线配置加载失败", ex);
            return CreateDefaultConfig();
        }
    }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    private static WiringConfig CreateDefaultConfig()
    {
        return new WiringConfig
        {
            Version = "1.0",
            Description = "默认接线配置",
            GridSize = 10.0,
            DefaultLineThickness = 2.0,
            DefaultArrowSize = 8.0,
            ConnectionPoints = new Dictionary<string, Dictionary<string, Point>>(),
            WiringRules = new List<WiringRule>(),
            VisualSettings = new VisualSettings(),
            PerformanceSettings = new PerformanceSettings(),
            DebugSettings = new DebugSettings()
        };
    }

    /// <summary>
    /// 获取设备连接点
    /// </summary>
    public static Point? GetConnectionPoint(string deviceId, string pointName)
    {
        var config = GetConfig();

        if (config.ConnectionPoints.TryGetValue(deviceId, out var devicePoints))
        {
            if (devicePoints.TryGetValue(pointName, out var point))
            {
                return point;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取线条颜色
    /// </summary>
    public static string GetLineColor(FlowLineType lineType)
    {
        var config = GetConfig();

        if (config.VisualSettings.LineColors.TryGetValue(lineType.ToString(), out var color))
        {
            return color;
        }

        // 默认颜色
        return lineType switch
        {
            FlowLineType.MainProcess => "#808080",
            FlowLineType.WaterSupply => "#4CAF50",
            FlowLineType.Dosing => "#795548",
            FlowLineType.Reflux => "#2196F3",
            FlowLineType.Backwash => "#2196F3",
            FlowLineType.Aeration => "#FFC107",
            FlowLineType.Deodorization => "#795548",
            _ => "#000000"
        };
    }

    /// <summary>
    /// 获取虚线样式
    /// </summary>
    public static double[] GetDashStyle(string styleName)
    {
        var config = GetConfig();

        if (config.VisualSettings.LineDashStyles.TryGetValue(styleName, out var dashArray))
        {
            return dashArray;
        }

        return Array.Empty<double>(); // 实线
    }

    /// <summary>
    /// 检查是否应该显示箭头
    /// 修改：所有连线都显示箭头，像自来水补水和电磁阀连线那样
    /// </summary>
    public static bool ShouldShowArrow(double deltaX, double deltaY)
    {
        // 修改：所有连线都显示箭头
        return true;
        
        // 旧逻辑（已禁用）：
        // var config = GetConfig();
        // var arrowSettings = config.VisualSettings.ArrowVisibility;
        
        // // 如果是水平线（X变化远大于Y变化），不显示箭头
        // if (deltaX > arrowSettings.HorizontalThreshold && 
        //     deltaY < arrowSettings.VerticalThreshold)
        // {
        //     return false;
        // }
        
        // // 如果线段太短，也不显示箭头
        // var segmentLength = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        // if (segmentLength < arrowSettings.MinSegmentLength)
        // {
        //     return false;
        // }

        // return true;
    }
}

/// <summary>
/// 接线配置
/// </summary>
public class WiringConfig
{
    public string Version { get; set; } = "1.0";
    public string Description { get; set; } = "";
    public double GridSize { get; set; } = 10.0;
    public double DefaultLineThickness { get; set; } = 2.0;
    public double DefaultArrowSize { get; set; } = 8.0;

    public Dictionary<string, Dictionary<string, Point>> ConnectionPoints { get; set; } = new();
    public List<WiringRule> WiringRules { get; set; } = new();
    public VisualSettings VisualSettings { get; set; } = new();
    public PerformanceSettings PerformanceSettings { get; set; } = new();
    public DebugSettings DebugSettings { get; set; } = new();
}

/// <summary>
/// 接线规则
/// </summary>
public class WiringRule
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string PreferredStyle { get; set; } = "orthogonal"; // orthogonal, direct, curved
    public double TurnRadius { get; set; } = 0;
    public bool AvoidCrossing { get; set; } = true;
    public bool GridAlignment { get; set; } = true;
    public string LineStyle { get; set; } = "solid"; // solid, dashed, dotted
}

/// <summary>
/// 可视化设置
/// </summary>
public class VisualSettings
{
    public Dictionary<string, string> LineColors { get; set; } = new();
    public Dictionary<string, double[]> LineDashStyles { get; set; } = new();
    public ArrowVisibilitySettings ArrowVisibility { get; set; } = new();
    public HighlightSettings HighlightSettings { get; set; } = new();
}

/// <summary>
/// 箭头可见性设置
/// </summary>
public class ArrowVisibilitySettings
{
    public double HorizontalThreshold { get; set; } = 5.0;
    public double VerticalThreshold { get; set; } = 5.0;
    public double MinSegmentLength { get; set; } = 10.0;
}

/// <summary>
/// 高亮设置
/// </summary>
public class HighlightSettings
{
    public string SelectedColor { get; set; } = "#FF5722";
    public string HoverColor { get; set; } = "#FF9800";
    public string RelatedColor { get; set; } = "#9C27B0";
    public double HighlightThickness { get; set; } = 3.0;
}

/// <summary>
/// 性能设置
/// </summary>
public class PerformanceSettings
{
    public bool EnableCaching { get; set; } = true;
    public int CacheDuration { get; set; } = 5000; // 毫秒
    public int MaxLinesToOptimize { get; set; } = 100;
    public int OptimizationBatchSize { get; set; } = 10;
    public bool EnableLazyLoading { get; set; } = true;
}

/// <summary>
/// 调试设置
/// </summary>
public class DebugSettings
{
    public bool ShowConnectionPoints { get; set; } = false;
    public bool ShowGrid { get; set; } = false;
    public bool ShowBoundingBoxes { get; set; } = false;
    public bool LogPathGeneration { get; set; } = false;
    public bool LogConflictDetection { get; set; } = false;
}