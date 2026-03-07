# 坐标配置文件填写指南

## 概述

此配置文件用于调整污水处理工艺流程图中的单元位置坐标。通过修改 `CoordinatesConfig.json` 文件，您可以精确控制每个处理单元和设备在画布上的显示位置，避免重叠并优化布局。

## 文件结构

配置文件包含以下主要部分：

1. **全局设置** - 控制画布尺寸和对齐方式
2. **坐标项列表** - 每个单元的具体坐标和属性

## 全局设置说明

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Version` | string | "1.0" | 配置版本号，用于兼容性检查 |
| `LastUpdated` | string | ISO 8601时间戳 | 最后更新时间，自动生成 |
| `CanvasWidth` | number | 1400 | 画布总宽度（像素） |
| `CanvasHeight` | number | 700 | 画布总高度（像素） |
| `AlignUnitsHorizontally` | boolean | true | 是否将所有处理单元在同一水平线对齐 |
| `BaselineY` | number | 150 | 水平基准线Y坐标（当AlignUnitsHorizontally=true时生效） |
| `HorizontalSpacing` | number | 180 | 水平间距参考值（用于自动布局） |
| `VerticalSpacing` | number | 150 | 垂直间距参考值（用于自动布局） |

## 坐标项配置说明

每个坐标项（`Items`数组中的对象）包含以下字段：

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ElementId` | string | 必需 | 元素唯一标识符（必须与代码中的ID一致） |
| `ElementType` | string | "ProcessUnit"或"Equipment" | 元素类型：ProcessUnit（处理单元）或Equipment（设备） |
| `X` | number | 必需 | 元素左上角在Canvas中的X坐标（像素） |
| `Y` | number | 必需 | 元素左上角在Canvas中的Y坐标（像素） |
| `Width` | number | 160（单元）/80（设备） | 元素的显示宽度（像素） |
| `Height` | number | 120（单元）/60（设备） | 元素的显示高度（像素） |
| `Comment` | string | 可选 | 坐标备注信息，用于描述位置用途 |
| `IsVisible` | boolean | true | 元素是否可见 |
| `ParentUnitId` | string | "" | 所属处理单元ID（对于设备类型） |

## 可配置的单元列表

### 主工艺流程单元（ElementType: ProcessUnit）

1. **格栅机** - `bar-screen`
2. **调节池** - `regulating-tank`
3. **缺氧池** - `anoxic-tank`
4. **MBR膜池** - `mbr-tank`
5. **产水泵** - `production-pump`
6. **消毒池** - `disinfection-tank`
7. **出水** - `outlet`
8. **碳源加药** - `dosing-carbon`
9. **反洗系统** - `backwash-system`
10. **中间水池** - `intermediate-tank`

### 独立设备单元（ElementType: ProcessUnit）

11. **1#提升泵** - `lift-pump-1`
12. **2#提升泵** - `lift-pump-2`
13. **搅拌机** - `regulating-mixer`
14. **除臭设备** - `deodorizer`
15. **缺氧池搅拌机** - `anoxic-mixer`
16. **1#鼓风机** - `blower-1`
17. **2#鼓风机** - `blower-2`
18. **1#回流泵** - `reflux-pump-1`
19. **2#回流泵** - `reflux-pump-2`
20. **反洗泵** - `backwash-pump`
21. **反洗罐** - `backwash-tank`

## 坐标调整指南

### 1. 避免重叠
- 确保每个单元的边界矩形不与其他单元相交
- 使用以下公式检查：`Rect1.IntersectsWith(Rect2) == false`
- 建议保持至少20像素的间距

### 2. 布局建议
- **主工艺流程**：建议保持在同一水平线（Y=150）上，从左到右顺序排列
- **辅助单元**：建议放在主流程单元下方（Y=300区域）
- **独立设备**：建议按功能分组，放置在对应处理单元下方

### 3. 坐标系统
- 原点 (0,0) 在画布左上角
- X轴向右为正，Y轴向下为正
- 典型画布尺寸：1400×700像素

## 操作步骤

### 步骤1：复制模板文件
将 `CoordinatesConfig_template.json` 复制为 `CoordinatesConfig.json`（覆盖原文件）。

### 步骤2：修改坐标值
使用文本编辑器或JSON编辑器打开 `CoordinatesConfig.json`，修改需要调整的单元的 `X` 和 `Y` 值。

### 步骤3：验证JSON格式
确保JSON格式正确，可以使用在线JSON验证工具检查。

### 步骤4：重启应用程序
保存文件后，重新启动应用程序以应用新的坐标配置。

## 示例：调整格栅机位置

```json
{
  "ElementId": "bar-screen",
  "ElementType": "ProcessUnit",
  "X": 100,  // 将X坐标从50改为100
  "Y": 120,  // 将Y坐标从150改为120
  "Width": 160,
  "Height": 120,
  "Comment": "格栅机 - 向右下移动以避免重叠",
  "IsVisible": true,
  "ParentUnitId": ""
}
```

## 故障排除

### 问题1：坐标修改后无变化
- 检查文件路径是否正确：`WinFormsApp1/Config/CoordinatesConfig.json`
- 检查JSON格式是否正确
- 确保应用程序有权限读取该文件

### 问题2：单元重叠
- 使用更大的间距值
- 调整 `HorizontalSpacing` 和 `VerticalSpacing` 参数
- 将 `AlignUnitsHorizontally` 设为 `false` 允许更灵活的布局

### 问题3：单元显示不全
- 检查坐标是否在画布范围内：`0 ≤ X ≤ CanvasWidth-Width`, `0 ≤ Y ≤ CanvasHeight-Height`
- 适当调整 `CanvasWidth` 和 `CanvasHeight` 值

## 注意事项

1. **备份原配置**：修改前建议备份原 `CoordinatesConfig.json` 文件
2. **增量修改**：每次只修改少量单元，逐步调整到满意布局
3. **测试验证**：每次修改后重启应用程序查看效果
4. **注释说明**：在 `Comment` 字段记录修改原因，便于后续维护

## 联系支持

如遇到配置问题，请提供：
- 修改后的 `CoordinatesConfig.json` 文件内容
- 应用程序的错误日志
- 期望的布局效果描述