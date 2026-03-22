# IndustrialControlHMI - 工业控制人机界面

[![GitHub](https://img.shields.io/github/license/Alexchinaxian/wushuichuli)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Windows%20Presentation%20Foundation-purple)](https://docs.microsoft.com/zh-cn/dotnet/desktop/wpf/)

## 📋 项目简介

IndustrialControlHMI 是一个基于C# WPF的工业控制上位机软件，专门用于污水处理系统的设备控制、数据采集和实时监控。

### 主要功能
- ✅ **实时数据监控** - PLC数据采集和显示
- ✅ **流程图可视化** - 工业流程动态展示
- ✅ **报警管理系统** - 异常状态监控和记录
- ✅ **设备控制** - 远程设备操作和控制
- ✅ **数据记录** - 历史数据存储和查询
- ✅ **报表生成** - 运行数据分析和报告

## 🏗️ 技术栈

### 核心技术
- **编程语言**: C# 11.0
- **UI框架**: WPF (Windows Presentation Foundation)
- **目标框架**: .NET 8.0 Windows
- **数据库**: SQLite (Entity Framework Core 9.0)
- **通信协议**: Modbus TCP (NModbus库)
- **图表库**: LiveCharts.Wpf
- **MVVM框架**: CommunityToolkit.Mvvm

### 依赖包
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
<PackageReference Include="LiveCharts.Wpf" Version="0.9.7" />
<PackageReference Include="NModbus" Version="4.0.0-alpha010" />
```

## 📁 项目结构

```
上位机开发/
├── src/                           # 源代码
│   └── IndustrialControlHMI/      # C# WPF主项目
│       ├── Models/                # 数据模型
│       ├── Views/                 # 界面视图
│       ├── ViewModels/            # MVVM视图模型
│       ├── Services/              # 业务服务
│       ├── Infrastructure/        # 基础设施
│       └── MainWindow.xaml        # 主窗口
├── config/                        # 配置文件
│   ├── IndustrialControlHMI/      # 项目配置文件
│   └── project_config.json        # 项目配置模板
├── docs/                          # 文档目录
├── 参考资料/                       # 参考文档
├── build/                         # 构建输出
├── tests/                         # 测试目录
├── tools/                         # 开发工具
└── lib/                           # 第三方库
```

## 🚀 快速开始

### 环境要求
- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 或更高版本

### 克隆仓库
```bash
git clone git@github.com:Alexchinaxian/wushuichuli.git
cd wushuichuli
```

### 打开项目
```bash
# 使用Visual Studio
start IndustrialControlHMI.sln

# 或使用命令行
cd src/IndustrialControlHMI
dotnet restore
dotnet build
dotnet run
```

### 配置项目
1. 修改 `config/IndustrialControlHMI/CoordinatesConfig.json` 配置设备坐标
2. 修改 `config/wiring_config.json` 配置连线路径
3. 配置PLC连接参数

## 🎯 功能模块

### 1. Modbus通信模块
- 西门子S7-Smart200以太网通信
- 实时数据采集和解析
- 故障状态监控

### 2. 流程图显示模块
- 工业流程动态可视化
- 设备状态实时显示
- 管道连接线绘制
- 水位指示灯显示

### 3. 报警管理模块
- 异常状态检测
- 报警记录存储
- 历史报警查询
- 报警通知

### 4. 数据管理模块
- 实时数据绑定
- 历史数据存储
- 数据报表生成
- 趋势分析

### 5. 系统设置模块
- 参数配置管理
- 通信参数设置
- 设备参数调整
- 用户权限管理

## 📊 应用场景

### 污水处理系统
项目应用于**中信国际污水项目**，包含以下设备：

**主要工艺单元**:
- 调节池、缺氧池、MBR膜池、中间水池
- 格栅机、提升泵、加药装置
- 鼓风机、反洗泵、产水泵、回用泵
- 电磁阀、搅拌器、计量泵

**控制系统**:
- 西门子S7-Smart200 PLC
- Modbus TCP通信协议
- 自动/手动控制模式
- 故障检测和报警系统

## 🔧 开发指南

### 代码结构
项目采用MVVM架构模式：
- **Models**: 数据模型和实体类
- **Views**: WPF界面和用户控件
- **ViewModels**: 业务逻辑和命令处理
- **Services**: 业务服务和数据访问
- **Infrastructure**: 基础设施和工具类

### 添加新设备
1. 在 `config/IndustrialControlHMI/CoordinatesConfig.json` 中添加设备坐标
2. 在 `Models/Flowchart/EquipmentItem.cs` 中添加设备模型
3. 在 `Views/ProcessUnitControl.xaml` 中添加界面控件
4. 在 `ViewModels/FlowchartViewModel.cs` 中添加业务逻辑

### 修改连线路径
1. 修改 `config/wiring_config.json` 中的连线配置
2. 在 `Services/FlowchartRenderer.cs` 中调整绘制逻辑
3. 在 `Views/FlowLineControl.xaml` 中修改连线样式

## 📝 文档

### 项目文档
- [PROJECT_STATUS.md](PROJECT_STATUS.md) - 项目状态报告
- [README.md](README.md) - 项目说明文档
- [MIGRATION_REPORT.md](MIGRATION_REPORT.md) - 迁移报告
- [VALIDATION_REPORT.md](VALIDATION_REPORT.md) - 验证报告

### 修改报告
项目包含50+个详细修改报告，记录所有界面优化和功能改进：
- [UI_MODIFICATION_REPORT.md](UI_MODIFICATION_REPORT.md) - 界面修改报告
- [VERSION_REPORT_20260309.md](VERSION_REPORT_20260309.md) - 版本报告
- 其他详细修改报告见项目根目录

### 参考资料
- [中信国际污水项目PLC点位.md](参考资料/中信国际污水项目PLC点位.md) - PLC点位文档
- [流程图实际图.png](参考资料/流程图实际图.png) - 流程图设计图
- [未命名绘图.drawio](参考资料/未命名绘图.drawio) - Draw.io设计文件

## 🤝 贡献指南

### 开发流程
1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

### 提交规范
使用有意义的提交信息：
```
类型: 简要描述

详细描述：
- 修改了哪些内容
- 为什么修改
- 可能的影响

类型包括：
- feat: 新功能
- fix: 修复bug
- docs: 文档更新
- style: 代码格式调整
- refactor: 代码重构
- test: 测试相关
- chore: 构建过程或辅助工具变动
```

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 📞 联系方式

- **项目负责人**: 老板
- **技术经理**: 经理 (AI助手)
- **GitHub**: [Alexchinaxian](https://github.com/Alexchinaxian)
- **仓库地址**: [wushuichuli](https://github.com/Alexchinaxian/wushuichuli)

## 🙏 致谢

感谢所有为项目做出贡献的开发者和测试人员。

---

**项目状态**: ✅ 开发中，功能完善  
**最后更新**: 2026年3月22日  
**版本**: 1.0.0