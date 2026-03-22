# IndustrialControlHMI - 工业控制上位机

基于C# WPF的污水处理系统监控软件。

## 功能特点
- PLC数据采集 (Modbus TCP)
- 工艺流程图实时显示
- 设备状态监控和报警
- 历史数据记录和查询
- 远程设备控制

## 快速开始
1. 使用Visual Studio打开 `IndustrialControlHMI.sln`
2. 恢复NuGet包依赖
3. 修改配置文件中的PLC连接参数
4. 编译运行

## 项目结构
- `src/IndustrialControlHMI/` - C# WPF源代码
- `config/` - 配置文件
- `参考资料/` - 技术文档和设计图

## 技术栈
- C# (.NET 8.0)
- WPF (Windows Presentation Foundation)
- SQLite + Entity Framework Core
- Modbus TCP通信
- LiveCharts数据可视化

## 配置文件
- `config/IndustrialControlHMI/CoordinatesConfig.json` - 设备坐标
- `config/wiring_config.json` - 管道连线配置

## GitHub仓库
- 地址: https://github.com/Alexchinaxian/wushuichuli
- SSH: git@github.com:Alexchinaxian/wushuichuli.git

## 许可证
MIT License - 详见 LICENSE 文件