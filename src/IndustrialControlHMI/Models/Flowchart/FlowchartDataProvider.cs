using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace IndustrialControlHMI.Models.Flowchart;

/// <summary>
/// 提供从HTML文件提取的流程图数据
/// </summary>
public static class FlowchartDataProvider
{
    // 画布尺寸 - 确保所有设备都在图像内
    public const double CanvasWidth = 2500.0;
    public const double CanvasHeight = 850.0;
    
    private const double UnitWidth = 160.0;
    private const double UnitHeight = 120.0;
    private const double HorizontalSpacing = 180.0;
    private const double VerticalSpacing = 150.0;
    private const double StartX = 50.0;
    private const double StartY = 150.0;
    private static readonly string CoordinatesConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "CoordinatesConfig.json");

    /// <summary>
    /// 加载坐标配置文件（如果存在）
    /// </summary>
    private static CoordinatesConfigCollection? LoadCoordinatesConfig()
    {
        try
        {
            if (!File.Exists(CoordinatesConfigPath))
            {
                System.Diagnostics.Debug.WriteLine($"[坐标配置] 配置文件不存在: {CoordinatesConfigPath}");
                return null;
            }
            
            string json = File.ReadAllText(CoordinatesConfigPath);
            var config = JsonSerializer.Deserialize<CoordinatesConfigCollection>(json);
            
            if (config == null)
            {
                System.Diagnostics.Debug.WriteLine("[坐标配置] 配置文件反序列化失败");
                return null;
            }
            
            System.Diagnostics.Debug.WriteLine($"[坐标配置] 成功加载配置文件，包含 {config.Items.Count} 个配置项");
            System.Diagnostics.Debug.WriteLine($"[坐标配置] 配置属性: AlignUnitsHorizontally={config.AlignUnitsHorizontally}, BaselineY={config.BaselineY}, Canvas={config.CanvasWidth}x{config.CanvasHeight}");
            return config;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[坐标配置] 加载配置文件失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 加载工艺流程单元数据（硬编码布局 - 整齐排列）
    /// </summary>
    public static ObservableCollection<ProcessUnitModel> LoadProcessUnits()
    {
        System.Diagnostics.Debug.WriteLine("[流程图数据] 加载硬编码设备布局...");
        var units = new ObservableCollection<ProcessUnitModel>();

        // 辅助方法：创建设备单元
        void AddUnit(string id, string title, double x, double y, double w, double h, string type, string? paramName = null, string? paramValue = null)
        {
            var unit = ProcessUnitModel.CreateDefault(id, title, new Point(x, y));
            unit.Size = new Size(w, h);
            unit.UnitType = type;
            unit.PositionComment = title;
            
            if (type == "Equipment")
            {
                unit.Equipment.Add(new EquipmentItem { Name = title, EquipmentType = "设备", Status = EquipmentStatus.Stopped, Value = "停止" });
            }
            else if (type == "Valve")
            {
                unit.Equipment.Add(new EquipmentItem { Name = title, EquipmentType = "阀门", Status = EquipmentStatus.Stopped, Value = "关闭" });
            }
            
            if (paramName != null && paramValue != null)
                unit.AddKeyParameter(paramName, paramValue);
            
            units.Add(unit);
        }

        // ============================================================
        // 布局采用网格系统，确保整齐不重叠
        // 画布: 2500 x 850
        // ============================================================

        // ========== 第1列: 格栅机 (左侧) ==========
        // 微调格栅机Y位置，使右边点(140,470)与调节池左边点(200,470)在同一水平线，连线不弯折
        AddUnit("IiDKO4wh8ftm1ieObsXz-11", "格栅机", 40, 430, 100, 80, "Equipment");
        AddUnit("IiDKO4wh8ftm1ieObsXz-6", "除臭设备", 40, 10, 100, 80, "ProcessUnit");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-22", "自来水补水", 40, 150, 100, 80, "Valve");
        AddUnit("IiDKO4wh8ftm1ieObsXz-10", "电磁阀", 40, 290, 100, 80, "Valve");

        // ========== 第2列: 调节池 + 提升泵 ==========
        AddUnit("IiDKO4wh8ftm1ieObsXz-12", "调节池", 200, 420, 150, 100, "ProcessUnit", "液位", "75%");
        // 调整提升泵Y位置，使连线点居中对称
        // 1#提升泵中心Y=430，左边点Y=470与调节池右边点对齐
        AddUnit("IiDKO4wh8ftm1ieObsXz-17", "1#提升泵", 410, 390, 100, 80, "Equipment");
        // 2#提升泵中心Y=530，左边点Y=570（与1#对称分布）
        AddUnit("IiDKO4wh8ftm1ieObsXz-18", "2#提升泵", 410, 490, 100, 80, "Equipment");

        // ========== 第3列: 加药装置 ==========
        AddUnit("IiDKO4wh8ftm1ieObsXz-14", "搅拌机", 350, 90, 100, 80, "Equipment");
        AddUnit("IiDKO4wh8ftm1ieObsXz-15", "碳源投加", 450, 90, 100, 80, "Equipment");
        AddUnit("IiDKO4wh8ftm1ieObsXz-16", "加药装置", 350, 170, 200, 80, "ProcessUnit");

        // ========== 第4列: 缺氧池 ==========
        AddUnit("IiDKO4wh8ftm1ieObsXz-20", "缺氧池", 570, 420, 150, 100, "ProcessUnit", "液位", "75%");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-3", "搅拌机", 620, 270, 100, 80, "Equipment"); // 右移50像素，避免干扰走线

        // ========== 第5列: MBR膜池 + 鼓风机 ==========
        AddUnit("h1sOd0kz_K9NjbGk3U_x-4", "MBR膜池", 780, 420, 150, 100, "ProcessUnit", "液位", "75%");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-1", "1#鼓风机", 680, 60, 100, 80, "Equipment");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-2", "2#鼓风机", 680, 140, 100, 80, "Equipment");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-5", "1#回流泵", 760, 540, 100, 80, "Equipment");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-6", "2#回流泵", 760, 620, 100, 80, "Equipment");

        // ========== 第6列: 反洗设备 + 产水泵 ==========
        AddUnit("h1sOd0kz_K9NjbGk3U_x-7", "反洗罐", 940, 60, 120, 80, "Equipment");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-8", "1#反洗泵", 900, 190, 100, 80, "Equipment");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-9", "2#反洗泵", 900, 270, 100, 80, "Equipment");
        // 调整产水泵Y位置，使连线点居中对称（与提升泵一致）
        // 1#产水泵中心Y=430，左边点Y=430（在MBR右边点Y=470上方）
        AddUnit("h1sOd0kz_K9NjbGk3U_x-11", "1#产水泵", 990, 390, 100, 80, "Equipment");
        // 2#产水泵中心Y=530，左边点Y=530（在MBR右边点Y=470下方）
        AddUnit("h1sOd0kz_K9NjbGk3U_x-10", "2#产水泵", 990, 490, 100, 80, "Equipment");

        // ========== 第7列: 中间水池 ==========
        AddUnit("h1sOd0kz_K9NjbGk3U_x-13", "中间水池", 1150, 420, 150, 100, "ProcessUnit", "液位", "75%");
        AddUnit("IiDKO4wh8ftm1ieObsXz-9", "自来水补水", 1100, 150, 100, 80, "Valve");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-12", "电磁阀", 1100, 290, 100, 80, "Valve");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-14", "1#回用泵", 1150, 550, 100, 80, "Equipment");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-15", "2#回用泵", 1150, 630, 100, 80, "Equipment");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-16", "3#回用泵", 1150, 710, 100, 80, "Equipment");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-17", "变频模块", 1150, 790, 100, 80, "Equipment");

        // ========== 第8列: 加药装置(右) + 至用水单元 ==========
        AddUnit("h1sOd0kz_K9NjbGk3U_x-46", "次氯酸钠", 1240, 210, 100, 80, "Equipment");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-47", "加药装置", 1240, 290, 100, 80, "ProcessUnit");
        AddUnit("h1sOd0kz_K9NjbGk3U_x-55", "至用水单元", 1250, 710, 100, 80, "Equipment");

        System.Diagnostics.Debug.WriteLine($"[流程图数据] 总共创建单元数: {units.Count}");
        return units;
    }

    /// <summary>
    /// 回退加载硬编码的处理单元数据（兼容旧版本）
    /// </summary>
    private static ObservableCollection<ProcessUnitModel> LoadProcessUnitsFallback()
    {
        // 现在直接使用新的硬编码布局
        return LoadProcessUnits();
    }
    
    /// <summary>
    /// 根据单元位置与尺寸计算中心点
    /// </summary>
    private static Point GetUnitCenter(ProcessUnitModel unit)
    {
        return new Point(unit.Position.X + unit.Size.Width / 2, unit.Position.Y + unit.Size.Height / 2);
    }

    /// <summary>
    /// 设备连接点字典（硬编码）
    /// Key: 设备ID, Value: 四边连接点（上、右、下、左）
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, Point>> UnitConnectionPoints = new()
    {
        // ========== 第1列 ==========
        // 格栅机连接点更新: Y位置从420改为430，右边点Y从460改为470，与调节池左边点对齐
        ["IiDKO4wh8ftm1ieObsXz-11"] = new() { ["top"] = new Point(90, 430), ["right"] = new Point(140, 470), ["bottom"] = new Point(90, 510), ["left"] = new Point(40, 470) }, // 格栅机
        ["IiDKO4wh8ftm1ieObsXz-6"] = new() { ["top"] = new Point(90, 10), ["right"] = new Point(140, 50), ["bottom"] = new Point(90, 90), ["left"] = new Point(40, 50) }, // 除臭设备
        ["h1sOd0kz_K9NjbGk3U_x-22"] = new() { ["top"] = new Point(90, 150), ["right"] = new Point(140, 190), ["bottom"] = new Point(90, 230), ["left"] = new Point(40, 190) }, // 自来水补水
        ["IiDKO4wh8ftm1ieObsXz-10"] = new() { ["top"] = new Point(90, 290), ["right"] = new Point(140, 330), ["bottom"] = new Point(90, 370), ["left"] = new Point(40, 330) }, // 电磁阀
        
        // ========== 第2列 ==========
        ["IiDKO4wh8ftm1ieObsXz-12"] = new() { ["top"] = new Point(275, 420), ["right"] = new Point(350, 470), ["bottom"] = new Point(275, 520), ["left"] = new Point(200, 470) }, // 调节池
        // 1#提升泵连接点更新: 中心Y从400改为390，各点Y相应调整
        ["IiDKO4wh8ftm1ieObsXz-17"] = new() { ["top"] = new Point(460, 390), ["right"] = new Point(510, 430), ["bottom"] = new Point(460, 470), ["left"] = new Point(410, 430) }, // 1#提升泵
        // 2#提升泵连接点更新: 中心Y从500改为490，各点Y相应调整，左边点Y=530
        ["IiDKO4wh8ftm1ieObsXz-18"] = new() { ["top"] = new Point(460, 490), ["right"] = new Point(510, 530), ["bottom"] = new Point(460, 570), ["left"] = new Point(410, 530) }, // 2#提升泵
        
        // ========== 第3列 ==========
        ["IiDKO4wh8ftm1ieObsXz-14"] = new() { ["top"] = new Point(400, 90), ["right"] = new Point(450, 130), ["bottom"] = new Point(400, 170), ["left"] = new Point(350, 130) }, // 搅拌机
        ["IiDKO4wh8ftm1ieObsXz-15"] = new() { ["top"] = new Point(500, 90), ["right"] = new Point(550, 130), ["bottom"] = new Point(500, 170), ["left"] = new Point(450, 130) }, // 碳源投加
        ["IiDKO4wh8ftm1ieObsXz-16"] = new() { ["top"] = new Point(450, 170), ["right"] = new Point(550, 210), ["bottom"] = new Point(450, 250), ["left"] = new Point(350, 210) }, // 加药装置
        
        // ========== 第4列 ==========
        ["IiDKO4wh8ftm1ieObsXz-20"] = new() { ["top"] = new Point(645, 420), ["right"] = new Point(720, 470), ["bottom"] = new Point(645, 520), ["left"] = new Point(570, 470) }, // 缺氧池
        ["h1sOd0kz_K9NjbGk3U_x-3"] = new() { ["top"] = new Point(670, 270), ["right"] = new Point(720, 310), ["bottom"] = new Point(670, 350), ["left"] = new Point(620, 310) }, // 搅拌机（右移50像素）
        
        // ========== 第5列 ==========
        ["h1sOd0kz_K9NjbGk3U_x-4"] = new() { ["top"] = new Point(855, 420), ["right"] = new Point(930, 470), ["bottom"] = new Point(855, 520), ["left"] = new Point(780, 470) }, // MBR膜池
        ["h1sOd0kz_K9NjbGk3U_x-1"] = new() { ["top"] = new Point(730, 60), ["right"] = new Point(780, 100), ["bottom"] = new Point(730, 140), ["left"] = new Point(680, 100) }, // 1#鼓风机
        ["h1sOd0kz_K9NjbGk3U_x-2"] = new() { ["top"] = new Point(730, 140), ["right"] = new Point(780, 180), ["bottom"] = new Point(730, 220), ["left"] = new Point(680, 180) }, // 2#鼓风机
        ["h1sOd0kz_K9NjbGk3U_x-5"] = new() { ["top"] = new Point(810, 540), ["right"] = new Point(860, 580), ["bottom"] = new Point(810, 620), ["left"] = new Point(760, 580) }, // 1#回流泵
        ["h1sOd0kz_K9NjbGk3U_x-6"] = new() { ["top"] = new Point(810, 620), ["right"] = new Point(860, 660), ["bottom"] = new Point(810, 700), ["left"] = new Point(760, 660) }, // 2#回流泵
        
        // ========== 第6列 ==========
        ["h1sOd0kz_K9NjbGk3U_x-7"] = new() { ["top"] = new Point(1000, 60), ["right"] = new Point(1060, 100), ["bottom"] = new Point(1000, 140), ["left"] = new Point(940, 100) }, // 反洗罐
        ["h1sOd0kz_K9NjbGk3U_x-8"] = new() { ["top"] = new Point(950, 190), ["right"] = new Point(1000, 230), ["bottom"] = new Point(950, 270), ["left"] = new Point(900, 230) }, // 1#反洗泵
        ["h1sOd0kz_K9NjbGk3U_x-9"] = new() { ["top"] = new Point(950, 270), ["right"] = new Point(1000, 310), ["bottom"] = new Point(950, 350), ["left"] = new Point(900, 310) }, // 2#反洗泵
        // 1#产水泵连接点更新: 中心Y从400改为390，各点Y相应调整
        ["h1sOd0kz_K9NjbGk3U_x-11"] = new() { ["top"] = new Point(1040, 390), ["right"] = new Point(1090, 430), ["bottom"] = new Point(1040, 470), ["left"] = new Point(990, 430) }, // 1#产水泵
        // 2#产水泵连接点更新: 中心Y从500改为490，各点Y相应调整，左边点Y=530
        ["h1sOd0kz_K9NjbGk3U_x-10"] = new() { ["top"] = new Point(1040, 490), ["right"] = new Point(1090, 530), ["bottom"] = new Point(1040, 570), ["left"] = new Point(990, 530) }, // 2#产水泵
        
        // ========== 第7列 ==========
        ["h1sOd0kz_K9NjbGk3U_x-13"] = new() { ["top"] = new Point(1225, 420), ["right"] = new Point(1300, 470), ["bottom"] = new Point(1225, 520), ["left"] = new Point(1150, 470) }, // 中间水池
        ["IiDKO4wh8ftm1ieObsXz-9"] = new() { ["top"] = new Point(1150, 150), ["right"] = new Point(1200, 190), ["bottom"] = new Point(1150, 230), ["left"] = new Point(1100, 190) }, // 自来水补水
        ["h1sOd0kz_K9NjbGk3U_x-12"] = new() { ["top"] = new Point(1150, 290), ["right"] = new Point(1200, 330), ["bottom"] = new Point(1150, 370), ["left"] = new Point(1100, 330) }, // 电磁阀
        ["h1sOd0kz_K9NjbGk3U_x-14"] = new() { ["top"] = new Point(1200, 550), ["right"] = new Point(1250, 590), ["bottom"] = new Point(1200, 630), ["left"] = new Point(1150, 590) }, // 1#回用泵
        ["h1sOd0kz_K9NjbGk3U_x-15"] = new() { ["top"] = new Point(1200, 630), ["right"] = new Point(1250, 670), ["bottom"] = new Point(1200, 710), ["left"] = new Point(1150, 670) }, // 2#回用泵
        ["h1sOd0kz_K9NjbGk3U_x-16"] = new() { ["top"] = new Point(1200, 710), ["right"] = new Point(1250, 750), ["bottom"] = new Point(1200, 790), ["left"] = new Point(1150, 750) }, // 3#回用泵
        ["h1sOd0kz_K9NjbGk3U_x-17"] = new() { ["top"] = new Point(1200, 790), ["right"] = new Point(1250, 830), ["bottom"] = new Point(1200, 870), ["left"] = new Point(1150, 830) }, // 变频模块
        
        // ========== 第8列 ==========
        ["h1sOd0kz_K9NjbGk3U_x-46"] = new() { ["top"] = new Point(1290, 210), ["right"] = new Point(1340, 250), ["bottom"] = new Point(1290, 290), ["left"] = new Point(1240, 250) }, // 次氯酸钠投加
        ["h1sOd0kz_K9NjbGk3U_x-47"] = new() { ["top"] = new Point(1290, 290), ["right"] = new Point(1340, 330), ["bottom"] = new Point(1290, 370), ["left"] = new Point(1240, 330) }, // 加药装置(右)
        ["h1sOd0kz_K9NjbGk3U_x-55"] = new() { ["top"] = new Point(1300, 710), ["right"] = new Point(1350, 750), ["bottom"] = new Point(1300, 790), ["left"] = new Point(1250, 750) }, // 至用水单元
    };

    /// <summary>
    /// 获取设备四边连接点（上、右、下、左）- 硬编码
    /// </summary>
    public static Dictionary<string, Point> GetUnitConnectionPoints(ProcessUnitModel unit)
    {
        if (UnitConnectionPoints.TryGetValue(unit.Id, out var points))
        {
            return points;
        }
        
        // 如果找不到硬编码点，返回空字典
        return new Dictionary<string, Point>();
    }

    /// <summary>
    /// 加载工艺连接线数据
    /// </summary>
    public static ObservableCollection<FlowLineModel> LoadFlowLines()
    {
        System.Diagnostics.Debug.WriteLine("[流程图数据] 加载连接线...");
        var lines = new ObservableCollection<FlowLineModel>();
        
        // 辅助函数：添加直角线（水平-垂直或垂直-水平）
        void AddOrtho(Point start, Point end, FlowLineType type, bool isDashed = false)
        {
            var line = FlowLineModel.CreateOrthogonal(new[] { start, end }, type, isDashed);
            line.Thickness = 2.0;
            line.ShowArrow = true;
            lines.Add(line);
        }
        
        // 辅助函数：添加三段式直角线（水平-垂直-水平）
        void AddOrtho3(Point start, Point mid, Point end, FlowLineType type, bool isDashed = false)
        {
            var line = FlowLineModel.CreateOrthogonal(new[] { start, mid, end }, type, isDashed);
            line.Thickness = 2.0;
            line.ShowArrow = true;
            lines.Add(line);
        }
        
        // ========== 除臭设备连接线（棕色虚线）==========
        // 除臭设备右边点
        var 除臭设备_右 = new Point(140, 50);
        
        // 定义目标设备连接点
        var 调节池_上 = new Point(275, 420);
        
        // 调节池上边线三个连接点（根据用户位置分析）
        // 调节池位置：X=270, Y=420, 120×110
        // 上边线中心点：(330, 420)
        // 三个分点应该在调节池的上边线上（Y=420）
        // X坐标统一缩小，再向左移动50像素，让中心点放在水池中间位置向左偏移50像素，左右各偏移70像素
        var 调节池_左边点 = new Point(260, 420);  // regulating-tank-left-point (上边线，距离中心点70像素)
        var 调节池_中心点 = new Point(280, 420);  // regulating-tank-center-point (上边线，水池中间向左偏移50像素)
        var 调节池_右边点 = new Point(300, 420);  // regulating-tank-right-point (上边线，距离中心点70像素)
        
        // 除臭设备 → 调节池上边线中心点（简洁的横线和竖线连接）直接L形连接
        // 路径：水平向右到调节池中心点X坐标(330,50) → 垂直向下到调节池上边线中心点(330,420)
        AddOrtho(除臭设备_右, new Point(调节池_中心点.X, 50), FlowLineType.Deodorization, true);  // 水平线
        AddOrtho(new Point(调节池_中心点.X, 50), 调节池_中心点, FlowLineType.Deodorization, true);    // 垂直线
        
        // 其他除臭设备连接线保持不变（如果需要连接其他设备）
        var 缺氧池_上 = new Point(645, 420);
        var MBR膜池_上 = new Point(855, 420);
        
        // 公共绕行路径：从除臭设备直接向右 → 再向下到目标
        // 水平绕行Y坐标（在设备下方，对齐到网格）
        double 水平绕行Y = 50;  // 保持与除臭设备右边点同一高度，对齐到网格
        
        // 除臭设备 → 缺氧池（水平向右，然后垂直向下）
        AddOrtho(除臭设备_右, new Point(缺氧池_上.X, 水平绕行Y), FlowLineType.Deodorization, true);
        AddOrtho(new Point(缺氧池_上.X, 水平绕行Y), 缺氧池_上, FlowLineType.Deodorization, true);
        
        // 除臭设备 → MBR膜池（水平向右，然后垂直向下）
        AddOrtho(除臭设备_右, new Point(MBR膜池_上.X, 水平绕行Y), FlowLineType.Deodorization, true);
        AddOrtho(new Point(MBR膜池_上.X, 水平绕行Y), MBR膜池_上, FlowLineType.Deodorization, true);
        
        // ========== 补水流程线（绿色实线）==========
        // 定义第1列左侧补水设备连接点
        var 自来水补水左_下 = new Point(90, 230);    // 自来水补水(左)的下点
        var 电磁阀左_上 = new Point(90, 290);        // 电磁阀(左)的上点
        var 电磁阀左_下 = new Point(90, 370);        // 电磁阀(左)的下点
        // 调节池_上 已在除臭设备部分定义: (275, 420)
        
        // 自来水补水(左)的下点 → 电磁阀(左)的上点：垂直向下
        AddOrtho(自来水补水左_下, 电磁阀左_上, FlowLineType.WaterSupply);
        
        // 电磁阀(左)的下点 → 调节池上边线左边点：修改为先向下→再向右→再向下
        // 从电磁阀下点(90,370)垂直向下到(90,400)，然后水平向右到(260,400)，最后垂直向下到调节池左边点(260,420)
        AddOrtho(电磁阀左_下, new Point(90, 400), FlowLineType.WaterSupply);
        AddOrtho(new Point(90, 400), new Point(260, 400), FlowLineType.WaterSupply);
        AddOrtho(new Point(260, 400), 调节池_左边点, FlowLineType.WaterSupply);
        
        // 定义右侧设备连接点（中间水池补水）
        var 自来水补水右_下 = new Point(1150, 230);  // 自来水补水(右)的下点
        var 电磁阀右_上 = new Point(1150, 290);      // 电磁阀(右)的上点
        var 电磁阀右_下 = new Point(1150, 370);      // 电磁阀(右)的下点
        var 中间水池_上 = new Point(1225, 420);
        
        // 自来水补水(右)的下点 → 电磁阀(右)的上点：垂直向下
        AddOrtho(自来水补水右_下, 电磁阀右_上, FlowLineType.WaterSupply);
        
        // 电磁阀(右)的下点 → 中间水池上点：水平向右 → 垂直向上（简洁的L形）
        AddOrtho(电磁阀右_下, new Point(中间水池_上.X, 370), FlowLineType.WaterSupply);
        AddOrtho(new Point(中间水池_上.X, 370), 中间水池_上, FlowLineType.WaterSupply);
        
        // ========== 主工艺流程线（灰色实线）==========
        // 格栅机(右) → 调节池(左)：水平直线（两点在同一水平线 Y=470）
        var 格栅机_右 = new Point(140, 470);
        var 调节池_左 = new Point(200, 470);
        AddOrtho(格栅机_右, 调节池_左, FlowLineType.MainProcess);
        
        // 调节池(右) → 1#提升泵(左) 和 2#提升泵(左)
        var 调节池_右 = new Point(350, 470);
        var 提升泵1_左 = new Point(410, 430);  // 1#提升泵左边点Y=430
        var 提升泵2_左 = new Point(410, 530);  // 2#提升泵左边点Y=530
        var 提升泵1_右 = new Point(510, 430);  // 1#提升泵右边点Y=430
        var 提升泵2_右 = new Point(510, 530);  // 2#提升泵右边点Y=530
        var 缺氧池_左 = new Point(570, 470);
        
        // 调节池 → 1#提升泵：统一中间过渡线X=380，Y从470到430
        AddOrtho3(调节池_右, new Point(380, 470), new Point(380, 430), FlowLineType.MainProcess);
        AddOrtho(new Point(380, 430), 提升泵1_左, FlowLineType.MainProcess);
        
        // 调节池 → 2#提升泵：统一中间过渡线X=380，Y从470到530
        AddOrtho3(调节池_右, new Point(380, 470), new Point(380, 530), FlowLineType.MainProcess);
        AddOrtho(new Point(380, 530), 提升泵2_左, FlowLineType.MainProcess);
        
        // 1#提升泵(右) → 缺氧池(左)：统一中间过渡线X=540，Y从430到470
        AddOrtho3(提升泵1_右, new Point(540, 430), new Point(540, 470), FlowLineType.MainProcess);
        AddOrtho(new Point(540, 470), 缺氧池_左, FlowLineType.MainProcess);
        
        // 2#提升泵(右) → 缺氧池(左)：统一中间过渡线X=540，Y从530到470
        AddOrtho3(提升泵2_右, new Point(540, 530), new Point(540, 470), FlowLineType.MainProcess);
        AddOrtho(new Point(540, 470), 缺氧池_左, FlowLineType.MainProcess);
        
        // ========== 缺氧池连接 ==========
        // 缺氧池(右) → MBR膜池(左)：水平直线
        var 缺氧池_右 = new Point(720, 470);
        var MBR_左 = new Point(780, 470);
        AddOrtho(缺氧池_右, MBR_左, FlowLineType.MainProcess);
        
        // 缺氧池(下) → 1#回流泵(左) 和 2#回流泵(左)
        var 缺氧池_下 = new Point(645, 520);
        var 回流泵1_左 = new Point(760, 580);
        var 回流泵2_左 = new Point(760, 660);
        var MBR_下 = new Point(855, 520);
        
        // 缺氧池 → 1#回流泵：垂直 → 水平 → 垂直（整齐路径）
        AddOrtho3(缺氧池_下, new Point(645, 580), new Point(760, 580), FlowLineType.Reflux, true);
        
        // 缺氧池 → 2#回流泵：垂直 → 水平（整齐路径）
        AddOrtho3(缺氧池_下, new Point(645, 660), 回流泵2_左, FlowLineType.Reflux, true);
        
        // ========== 回流泵连接 ==========
        // 1#回流泵(右) → MBR(下)：水平 → 垂直（统一出口位置）
        var 回流泵1_右 = new Point(860, 580);
        AddOrtho3(回流泵1_右, new Point(900, 580), new Point(900, 520), FlowLineType.Reflux, true);
        AddOrtho(new Point(900, 520), MBR_下, FlowLineType.Reflux, true);
        
        // 2#回流泵(右) → MBR(下)：水平 → 垂直（统一出口位置）
        var 回流泵2_右 = new Point(860, 660);
        AddOrtho3(回流泵2_右, new Point(920, 660), new Point(920, 520), FlowLineType.Reflux, true);
        AddOrtho(new Point(920, 520), MBR_下, FlowLineType.Reflux, true);
        
        // ========== 反洗罐连接（蓝色虚线）==========
        // 反洗罐(右) → 1#反洗泵(右) 和 2#反洗泵(右)
        var 反洗罐_右 = new Point(1060, 100);
        var 反洗泵1_右 = new Point(1000, 230);
        var 反洗泵2_右 = new Point(1000, 310);
        
        // 统一中转X坐标（在反洗罐右侧）
        double 反洗中转X = 1080;
        
        // 反洗罐 → 1#反洗泵：水平向右 → 垂直向下 → 水平向左
        AddOrtho3(反洗罐_右, new Point(反洗中转X, 100), new Point(反洗中转X, 230), FlowLineType.Backwash);
        AddOrtho(new Point(反洗中转X, 230), 反洗泵1_右, FlowLineType.Backwash);
        
        // 反洗罐 → 2#反洗泵：水平向右 → 垂直向下 → 水平向左
        AddOrtho3(反洗罐_右, new Point(反洗中转X, 100), new Point(反洗中转X, 310), FlowLineType.Backwash);
        AddOrtho(new Point(反洗中转X, 310), 反洗泵2_右, FlowLineType.Backwash);
        
        // ========== 反洗泵 → MBR膜池连接（蓝色虚线）==========
        // 1#反洗泵(左) → MBR膜池(上)：水平向左 → 垂直向下
        var 反洗泵1_左 = new Point(900, 230);
        var 反洗泵2_左 = new Point(900, 310);
        var MBR_上 = new Point(855, 420);
        
        // 统一中转X坐标
        double 反洗到MBR中转X = 880;
        
        // 1#反洗泵 → MBR：水平向左 → 垂直向下（修改：去掉最后的水平连接，直接垂直向下）
        AddOrtho(反洗泵1_左, new Point(855, 230), FlowLineType.Backwash);
        AddOrtho(new Point(855, 230), MBR_上, FlowLineType.Backwash);
        
        // 2#反洗泵 → MBR：水平向左 → 垂直向下（修改：去掉最后的水平连接，直接垂直向下）
        AddOrtho(反洗泵2_左, new Point(855, 310), FlowLineType.Backwash);
        AddOrtho(new Point(855, 310), MBR_上, FlowLineType.Backwash);
        
        // ========== 产水泵连接 ==========
        var MBR_右 = new Point(930, 470);
        var 产水泵1_左 = new Point(990, 430);  // 1#产水泵左边点Y=430
        var 产水泵2_左 = new Point(990, 530);  // 2#产水泵左边点Y=530
        var 产水泵1_右 = new Point(1090, 430); // 1#产水泵右边点Y=430
        var 产水泵2_右 = new Point(1090, 530); // 2#产水泵右边点Y=530
        var 中间水池_左 = new Point(1150, 470);
        
        // MBR膜池(右) → 1#产水泵(左)：统一中间过渡线X=960，Y从470到430
        AddOrtho3(MBR_右, new Point(960, 470), new Point(960, 430), FlowLineType.MainProcess);
        AddOrtho(new Point(960, 430), 产水泵1_左, FlowLineType.MainProcess);
        
        // MBR膜池(右) → 2#产水泵(左)：统一中间过渡线X=960，Y从470到530
        AddOrtho3(MBR_右, new Point(960, 470), new Point(960, 530), FlowLineType.MainProcess);
        AddOrtho(new Point(960, 530), 产水泵2_左, FlowLineType.MainProcess);
        
        // 1#产水泵(右) → 中间水池(左)：统一中间过渡线X=1120，Y从430到470
        AddOrtho3(产水泵1_右, new Point(1120, 430), new Point(1120, 470), FlowLineType.MainProcess);
        AddOrtho(new Point(1120, 470), 中间水池_左, FlowLineType.MainProcess);
        
        // 2#产水泵(右) → 中间水池(左)：统一中间过渡线X=1120，Y从530到470
        AddOrtho3(产水泵2_右, new Point(1120, 530), new Point(1120, 470), FlowLineType.MainProcess);
        AddOrtho(new Point(1120, 470), 中间水池_左, FlowLineType.MainProcess);
        
        // ========== 回用泵连接 ==========
        var 中间水池_下 = new Point(1225, 520);
        var 回用泵1_左 = new Point(1150, 590);
        var 回用泵2_左 = new Point(1150, 670);
        var 回用泵3_左 = new Point(1150, 750);
        var 回用泵1_右 = new Point(1250, 590);
        var 回用泵2_右 = new Point(1250, 670);
        var 回用泵3_右 = new Point(1250, 750);
        var 至用水单元_下 = new Point(1300, 790);
        var 至用水单元_左 = new Point(1250, 750);
        
        // 中间水池(下) → 各回用泵(左)：简洁的L形连接
        // 1#回用泵：水平向左 → 垂直向上
        AddOrtho(中间水池_下, new Point(回用泵1_左.X, 520), FlowLineType.MainProcess);
        AddOrtho(new Point(回用泵1_左.X, 520), 回用泵1_左, FlowLineType.MainProcess);
        
        // 2#回用泵：水平向左 → 垂直向下
        AddOrtho(中间水池_下, new Point(回用泵2_左.X, 520), FlowLineType.MainProcess);
        AddOrtho(new Point(回用泵2_左.X, 520), 回用泵2_左, FlowLineType.MainProcess);
        
        // 3#回用泵：水平向左 → 垂直向下
        AddOrtho(中间水池_下, new Point(回用泵3_左.X, 520), FlowLineType.MainProcess);
        AddOrtho(new Point(回用泵3_左.X, 520), 回用泵3_左, FlowLineType.MainProcess);
        
        // 回用泵 → 至用水单元：优化后的连线（用水单元已下移与回用泵3等高）
        
        // 1#回用泵(右) → 至用水单元(下)：水平向右 → 垂直向下（简洁的L形）
        AddOrtho(回用泵1_右, new Point(至用水单元_下.X, 590), FlowLineType.MainProcess);
        AddOrtho(new Point(至用水单元_下.X, 590), 至用水单元_下, FlowLineType.MainProcess);
        
        // 2#回用泵(右) → 至用水单元(下)：水平向右 → 垂直向下（简洁的L形）
        AddOrtho(回用泵2_右, new Point(至用水单元_下.X, 670), FlowLineType.MainProcess);
        AddOrtho(new Point(至用水单元_下.X, 670), 至用水单元_下, FlowLineType.MainProcess);
        
        // 3#回用泵(右) → 至用水单元(左)：水平向右直接连接（等高连接）
        AddOrtho(回用泵3_右, new Point(至用水单元_左.X, 750), FlowLineType.MainProcess);
        
        // ========== 鼓风机连接线（黄色实线 - 曝气线）==========
        // 1#鼓风机(右) → MBR膜池(上)：水平向右 → 垂直向下
        var 鼓风机1_右 = new Point(780, 100);
        var 鼓风机2_右 = new Point(780, 180);
        // MBR_上 已在反洗泵连接部分定义: (855, 420)
        
        // 统一中转X坐标
        double 鼓风机中转X = 820;
        
        // 1#鼓风机 → MBR：水平向右 → 垂直向下（修改：去掉最后的水平连接，直接垂直向下）
        AddOrtho(鼓风机1_右, new Point(855, 100), FlowLineType.Aeration);
        AddOrtho(new Point(855, 100), MBR_上, FlowLineType.Aeration);
        
        // 2#鼓风机 → MBR：水平向右 → 垂直向下（修改：去掉最后的水平连接，直接垂直向下）
        AddOrtho(鼓风机2_右, new Point(855, 180), FlowLineType.Aeration);
        AddOrtho(new Point(855, 180), MBR_上, FlowLineType.Aeration);
        
        // ========== 加药装置连接线（绿色虚线）==========
        // 加药装置下边点
        var 加药装置_下 = new Point(450, 250);
        // 缺氧池区域搅拌机下边点（第4列，在缺氧池上方）
        var 缺氧池搅拌机_下 = new Point(620, 350);
        
        // 底部绕行Y坐标（在设备下方，绕过所有中间设备）
        double 底部绕行Y = 330;
        
        // 加药装置 → 调节池（修改：最后直接垂直向下，不需要水平折线）
        // 从加药装置下点(450,250)垂直向下到(450,380)，然后水平向左到调节池上方(300,380)，最后垂直向下到调节池(300,420)
        AddOrtho(加药装置_下, new Point(450, 380), FlowLineType.WaterSupply, true);
        AddOrtho(new Point(450, 380), new Point(300, 380), FlowLineType.WaterSupply, true);
        AddOrtho(new Point(300, 380), 调节池_右边点, FlowLineType.WaterSupply, true);
        
        // 加药装置 → 缺氧池（修改：最后直接垂直向下，不需要水平折线）
        // 从加药装置下点(450,250)垂直向下到(450,400)，然后水平向右到缺氧池上方(645,400)，最后垂直向下到缺氧池(645,420)
        AddOrtho(加药装置_下, new Point(450, 400), FlowLineType.WaterSupply, true);
        AddOrtho(new Point(450, 400), new Point(645, 400), FlowLineType.WaterSupply, true);
        AddOrtho(new Point(645, 400), 缺氧池_上, FlowLineType.WaterSupply, true);
        
        // 缺氧池区域搅拌机 → 缺氧池（直接垂直连接，绿色虚线）
        // 路径：搅拌机下边(620,350) → 垂直向下到Y=420 → 水平向右到缺氧池上边(645,420)
        AddOrtho(缺氧池搅拌机_下, 缺氧池_上, FlowLineType.WaterSupply, true);
        
        // ========== 右侧加药装置连接线（绿色虚线）==========
        // 加药装置(右)下边点 → 中间水池上边点：水平向左 → 垂直向上（简洁的L形）
        var 加药装置右_下 = new Point(1290, 370);
        AddOrtho(加药装置右_下, new Point(中间水池_上.X, 370), FlowLineType.WaterSupply, true);
        AddOrtho(new Point(中间水池_上.X, 370), 中间水池_上, FlowLineType.WaterSupply, true);
        
        System.Diagnostics.Debug.WriteLine($"[流程图数据] 连接线加载完成，共 {lines.Count} 条");
        return lines;
    }
    
    /// <summary>
    /// 回退加载硬编码的连接线数据（兼容旧版本）
    /// </summary>
    private static ObservableCollection<FlowLineModel> LoadFlowLinesFallback()
    {
        // 现在直接使用新的硬编码布局
        return LoadFlowLines();
    }
    
    /// <summary>
    /// 创建处理单元辅助方法（兼容旧版本）
    /// </summary>
    private static ProcessUnitModel CreateUnit(string id, string title, Point position, IEnumerable<EquipmentItem> equipment)
    {
        var unit = ProcessUnitModel.CreateDefault(id, title, position);
        foreach (var item in equipment)
        {
            unit.Equipment.Add(item);
        }
        unit.ToolTip = $"点击查看{title}的详细信息";
        return unit;
    }
    
    
    /// <summary>
    /// 加载PLC点位映射配置
    /// </summary>
    public static List<PlcPointMapping> LoadPlcPointMappings()
    {
        return PlcPointMappingProvider.LoadDefaultMappings();
    }
    
    /// <summary>
    /// 获取单元相关的PLC点位映射
    /// </summary>
    public static List<PlcPointMapping> GetMappingsByUnitId(string unitId)
    {
        return PlcPointMappingProvider.GetMappingsByUnitId(unitId);
    }
    
    /// <summary>
    /// 保存处理单元坐标配置到文件
    /// </summary>
    /// <param name="units">处理单元集合</param>
    /// <returns>是否保存成功</returns>
    public static bool SaveCoordinatesConfig(ObservableCollection<ProcessUnitModel> units)
    {
        try
        {
            // 加载现有配置以保留非处理单元配置项（如设备）
            CoordinatesConfigCollection? existingConfig = LoadCoordinatesConfig();
            CoordinatesConfigCollection config = existingConfig ?? new CoordinatesConfigCollection();
            
            // 更新配置元数据
            config.Version = "1.0";
            config.LastUpdated = DateTime.Now;
            config.CanvasWidth = CanvasWidth;
            config.CanvasHeight = CanvasHeight;
            config.AlignUnitsHorizontally = false; // 保存时不强制对齐，保持用户拖放位置
            config.BaselineY = StartY;
            config.HorizontalSpacing = HorizontalSpacing;
            config.VerticalSpacing = VerticalSpacing;
            
            // 移除现有处理单元配置项（保留设备配置）
            config.Items.RemoveAll(item => item.ElementType == "ProcessUnit");
            
            // 添加所有处理单元配置
            foreach (var unit in units)
            {
                var unitConfig = CoordinatesConfig.CreateProcessUnitConfig(
                    unit.Id,
                    unit.Position.X,
                    unit.Position.Y,
                    unit.PositionComment ?? $"{unit.Title} - 位置保存于 {DateTime.Now:yyyy-MM-dd HH:mm}"
                );
                // 更新尺寸
                unitConfig.Width = unit.Size.Width;
                unitConfig.Height = unit.Size.Height;
                config.Items.Add(unitConfig);
            }
            
            // 确保Config目录存在
            string configDir = Path.GetDirectoryName(CoordinatesConfigPath);
            if (!Directory.Exists(configDir) && configDir != null)
            {
                Directory.CreateDirectory(configDir);
            }
            
            // 序列化为JSON并保存
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(CoordinatesConfigPath, json);
            
            System.Diagnostics.Debug.WriteLine($"[坐标配置] 成功保存配置到 {CoordinatesConfigPath}，包含 {config.Items.Count} 个配置项");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[坐标配置] 保存配置文件失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 获取工艺单元布局描述
    /// </summary>
    public static Dictionary<string, string> GetLayoutDescription()
    {
        return new Dictionary<string, string>
        {
            { "流程顺序", "格栅机 → 调节池 → 1#/2#提升泵 → 缺氧池 → MBR膜池 → 1#/2#产水泵 → 中间水池 → 1#/2#/3#回用泵 → 至用水单元" },
            { "补水路径", "自来水补水 → 电磁阀 → 调节池/中间水池" },
            { "加药路径", "加药装置 → 调节池/缺氧池/中间水池" },
            { "曝气系统", "1#/2#鼓风机 → MBR膜池" },
            { "反洗系统", "反洗罐 → 1#/2#反洗泵 → MBR膜池" },
            { "布局说明", "采用8列网格布局，所有设备硬编码，不依赖配置文件" },
            { "数据显示", "设备状态、关键参数、报警状态、液位/压力/流量等" }
        };
    }
}
