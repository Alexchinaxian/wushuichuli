# IndustrialControlHMI 依赖注入注册说明

本文档说明 `App.xaml.cs` 中 `ConfigureServices` 的注册约定、生命周期和用途，便于后续维护与排障。

## 生命周期约定

- `Singleton`：应用全局单例，适合配置、仓储、通信管理器等无界面状态服务。
- `Transient`：按需创建，适合各页面 ViewModel，避免页面切换时状态串扰。

## 当前主要注册项

### 基础与配置

- `IConfigurationManager -> ConfigurationManager`（Singleton）
- `IAppLogger -> AppLogger`（Singleton）
- `IModbusConfig -> DefaultModbusConfig`（Singleton）

### 数据与仓储

- `AppDbContext`（Singleton）
- `IAlarmRepository -> AlarmRepository`（Singleton）
- `ISettingRepository -> SettingRepository`（Singleton）
- `IPointHistoryRepository -> PointHistoryRepository`（Singleton）
- `ISettingsManager -> SettingsManager`（Singleton）

### 通信能力

- `ICommunicationManager -> CommunicationManager`（Singleton）
- `IProtocolFactory -> ProtocolFactory`（Singleton）
- `IProtocolConfigurationService -> ProtocolConfigurationService`（Singleton）
- `IDataConverter -> DataConverter`（Singleton）
- `IModbusService -> ModbusService`（Singleton）
- `IS7RuntimeService -> S7RuntimeService`（Singleton）

### ViewModel

- `MainWindowViewModel`（Singleton）
- `AlarmManagementViewModel`（Transient）
- `HistoryDataViewModel`（Transient）
- `SettingsViewModel`（Transient）
- `FlowchartViewModel`（Transient）
- `CommunicationViewModel`（Transient）
- `S7MonitorViewModel`（Transient）

## 新增服务接入规范

1. 优先面向接口注册：`IYourService -> YourService`。
2. 服务需区分“无状态/有状态”后再选生命周期。
3. 任何新服务若涉及异常处理，统一使用 `IAppLogger` 记录错误上下文。
4. 修改注册后必须执行一次 `dotnet build`，并验证导航/启动页是否可正常解析依赖。

