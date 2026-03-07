# 连线箭头显示修改报告

## 修改概要
- **修改时间**: 2026年3月8日 03:34
- **修改执行者**: 经理 (AI助手)
- **修改状态**: ✅ 完成并测试通过
- **Git提交**: `1a6d3e4` - fix: 修改所有连线箭头显示逻辑，使所有连线都显示箭头（像自来水补水和电磁阀连线那样）

## 修改需求
根据老板的要求：
> "所有连线的箭头 都应想自来水补水和电磁阀连线那样"

即：所有连线都应该显示箭头，包括水平连线。

## 修改内容

### 1. 修改的文件
**`src/IndustrialControlHMI/Models/Flowchart/WiringConfigLoader.cs`**

### 2. 具体修改

#### 修改前：
```csharp
public static bool ShouldShowArrow(double deltaX, double deltaY)
{
    var config = GetConfig();
    var arrowSettings = config.VisualSettings.ArrowVisibility;
    
    // 如果是水平线（X变化远大于Y变化），不显示箭头
    if (deltaX > arrowSettings.HorizontalThreshold && 
        deltaY < arrowSettings.VerticalThreshold)
    {
        return false;
    }
    
    // 如果线段太短，也不显示箭头
    var segmentLength = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    if (segmentLength < arrowSettings.MinSegmentLength)
    {
        return false;
    }

    return true;
}
```

#### 修改后：
```csharp
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
```

## 修改影响

### 正面影响：
1. **统一箭头显示**：所有连线现在都会显示箭头，包括水平连线
2. **符合要求**：实现了"所有连线的箭头都应像自来水补水和电磁阀连线那样"的要求
3. **视觉一致性**：整个流程图的箭头显示更加一致

### 技术细节：
1. **简单实现**：通过直接返回`true`，绕过了原有的阈值判断逻辑
2. **可恢复性**：旧逻辑被注释保留，如果需要可以快速恢复
3. **向后兼容**：不影响其他功能，只修改箭头显示逻辑

## 测试验证
- ✅ 编译通过（0错误，4个警告）
- ✅ 程序可以正常运行
- ✅ Git提交成功

## 注意事项
1. 如果需要恢复原有的箭头显示逻辑（水平线不显示箭头），只需取消注释旧代码并删除`return true;`语句
2. 配置文件中的箭头阈值设置（`horizontalThreshold`, `verticalThreshold`, `minSegmentLength`）在当前修改下不再起作用
3. 如果未来需要更精细的控制，可以考虑修改阈值而不是完全禁用逻辑

## 相关文件
- `config/wiring_config.json` - 接线配置文件（箭头阈值设置在此文件中，但当前修改已禁用这些设置）

---
*修改完成，所有连线现在都会显示箭头。*