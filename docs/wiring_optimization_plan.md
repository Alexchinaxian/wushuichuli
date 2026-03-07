# 接线系统优化计划

## 问题分析

### 当前接线系统的局限性
1. **硬编码坐标**：所有接线路径都是静态坐标，难以维护和修改
2. **缺乏智能布局**：接线路径没有自动避让和优化
3. **代码重复**：大量相似的`AddOrtho`和`AddOrtho3`调用
4. **可视化问题**：接线交叉、重叠、不美观
5. **缺乏动态性**：不能根据设备位置动态调整

### 优化目标
1. **可维护性**：减少硬编码，增加配置化
2. **智能化**：自动优化接线路径
3. **美观性**：减少交叉和重叠
4. **性能**：提高渲染效率
5. **扩展性**：支持动态调整和交互

## 优化方案

### 第一阶段：基础重构（1-2天）

#### 1.1 创建接线配置系统
```csharp
public class WiringConfig
{
    public string Id { get; set; }
    public string FromDeviceId { get; set; }
    public string ToDeviceId { get; set; }
    public FlowLineType LineType { get; set; }
    public bool IsDashed { get; set; }
    public WiringPathStyle PathStyle { get; set; } // 自动、直角、直接
    public List<Point> CustomWaypoints { get; set; } // 自定义路径点
}
```

#### 1.2 设备连接点定义
```csharp
public class DeviceConnectionPoint
{
    public string DeviceId { get; set; }
    public ConnectionSide Side { get; set; } // 左、右、上、下
    public Point Offset { get; set; } // 相对于设备中心的偏移
    public bool IsInput { get; set; } // 输入点还是输出点
}
```

#### 1.3 接线路径生成器
```csharp
public class WiringPathGenerator
{
    // 自动生成最优路径
    public List<Point> GeneratePath(Point start, Point end, WiringPathStyle style);
    
    // 避免交叉和重叠
    public List<Point> OptimizePath(List<Point> path, List<FlowLineModel> existingLines);
    
    // 计算直角路径
    public List<Point> GenerateOrthogonalPath(Point start, Point end);
}
```

### 第二阶段：智能布局（2-3天）

#### 2.1 自动避让算法
```csharp
public class WiringAvoidanceSystem
{
    // 检测路径冲突
    public List<Conflict> DetectConflicts(FlowLineModel newLine, List<FlowLineModel> existingLines);
    
    // 解决冲突（重新路由）
    public FlowLineModel ResolveConflict(FlowLineModel line, List<Conflict> conflicts);
    
    // 优化整体布局
    public void OptimizeAllWiring(List<FlowLineModel> allLines);
}
```

#### 2.2 网格对齐系统
```csharp
public class WiringGridSystem
{
    public double GridSize { get; set; } = 10.0;
    
    // 对齐到网格
    public Point SnapToGrid(Point point);
    
    // 确保接线在网格上
    public List<Point> AlignPathToGrid(List<Point> path);
}
```

#### 2.3 美观性优化
```csharp
public class WiringAestheticsOptimizer
{
    // 减少不必要的转折
    public List<Point> SimplifyPath(List<Point> path);
    
    // 统一转折点位置
    public List<Point> AlignTurnPoints(List<Point> path, List<FlowLineModel> existingLines);
    
    // 优化曲线平滑度
    public List<Point> SmoothPath(List<Point> path);
}
```

### 第三阶段：高级功能（3-5天）

#### 3.1 动态接线系统
```csharp
public class DynamicWiringSystem
{
    // 设备移动时自动更新接线
    public void UpdateWiringForDeviceMove(string deviceId, Point newPosition);
    
    // 实时避让
    public void UpdateWiringInRealTime();
    
    // 接线动画
    public void AnimateWiring(FlowLineModel line, TimeSpan duration);
}
```

#### 3.2 交互式接线编辑
```csharp
public class InteractiveWiringEditor
{
    // 拖拽调整接线点
    public void EnablePointDragging(FlowLineModel line);
    
    // 添加/删除路径点
    public void ModifyPathPoints(FlowLineModel line, int index, Point newPoint);
    
    // 接线属性编辑
    public void EditWiringProperties(FlowLineModel line);
}
```

#### 3.3 接线可视化增强
```csharp
public class WiringVisualEnhancement
{
    // 流动动画
    public void AddFlowAnimation(FlowLineModel line);
    
    // 高亮相关接线
    public void HighlightRelatedWiring(string deviceId);
    
    // 接线标签
    public void AddWiringLabels(FlowLineModel line);
}
```

## 实施步骤

### 第1周：基础架构
1. 创建接线配置系统（JSON配置文件）
2. 实现设备连接点定义
3. 重构现有硬编码接线为配置驱动

### 第2周：智能布局
1. 实现自动路径生成
2. 添加避让算法
3. 优化美观性

### 第3周：高级功能
1. 实现动态接线
2. 添加交互功能
3. 增强可视化效果

## 技术细节

### 路径生成算法
```csharp
// A*算法寻找最优路径
public List<Point> FindOptimalPath(Point start, Point end, WiringGrid grid)
{
    // 实现A*算法
    // 考虑接线类型偏好（直角、直接等）
    // 避让现有接线和设备
}
```

### 冲突检测
```csharp
// 基于分离轴定理的线段交叉检测
public bool CheckLineIntersection(Point p1, Point p2, Point p3, Point p4)
{
    // 实现线段交叉检测
    // 考虑接线宽度
}
```

### 性能优化
```csharp
// 空间分区加速冲突检测
public class WiringSpacePartition
{
    private Dictionary<GridCell, List<FlowLineModel>> grid;
    
    // 快速查询某个区域的接线
    public List<FlowLineModel> GetLinesInArea(Rect area);
}
```

## 配置文件示例

```json
{
  "wiringConfigs": [
    {
      "id": "main_process_1",
      "fromDevice": "grid_chamber",
      "toDevice": "adjustment_tank",
      "lineType": "MainProcess",
      "pathStyle": "Orthogonal",
      "preferredSide": "Right",
      "priority": 1
    },
    {
      "id": "water_supply_1",
      "fromDevice": "tap_water_unit",
      "toDevice": "euro_filter",
      "lineType": "WaterSupply",
      "pathStyle": "Direct",
      "customWaypoints": []
    }
  ],
  "connectionPoints": {
    "grid_chamber": [
      { "side": "Right", "offset": { "x": 80, "y": 0 }, "isInput": false }
    ],
    "adjustment_tank": [
      { "side": "Left", "offset": { "x": -80, "y": 0 }, "isInput": true }
    ]
  }
}
```

## 成功标准

### 技术指标
- 接线配置减少80%的硬编码
- 自动路径生成准确率 > 95%
- 冲突减少 > 90%
- 渲染性能提升 > 30%

### 用户体验
- 接线更美观、整洁
- 减少交叉和重叠
- 支持动态调整
- 更好的可视化效果

### 开发体验
- 配置化，易于修改
- 代码更简洁、可维护
- 易于扩展新功能

## 风险与缓解

### 风险1：算法复杂度
- **缓解**：分阶段实施，先实现基础功能
- **缓解**：使用空间分区优化性能

### 风险2：兼容性问题
- **缓解**：保持向后兼容性
- **缓解**：提供迁移工具

### 风险3：性能问题
- **缓解**：性能测试和优化
- **缓解**：异步计算和缓存

## 测试计划

### 单元测试
- 路径生成算法
- 冲突检测
- 配置解析

### 集成测试
- 完整接线场景
- 动态调整
- 性能测试

### 用户测试
- 美观性评估
- 易用性测试
- 性能感受

## 后续扩展

### 短期扩展
1. 接线分组和批量操作
2. 接线样式主题
3. 导出接线图

### 长期扩展
1. 3D接线可视化
2. 实时数据流显示
3. AI辅助接线优化