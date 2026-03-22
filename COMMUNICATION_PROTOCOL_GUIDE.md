# 通讯协议架构指南

## 📡 概述

本指南介绍了上位机项目中新增的通讯协议架构。该架构支持多种工业通讯协议，包括 Modbus TCP、Modbus RTU 等，并提供了统一的接口进行管理。

## 🏗️ 架构设计

### 核心组件

1. **ProtocolBase** - 协议基类，定义所有协议的通用接口
2. **CommunicationManager** - 通讯管理器，管理多个协议实例
3. **ProtocolFactory** - 协议工厂，创建协议实例
4. **ProtocolConfigurationService** - 协议配置服务
5. **DataConverter** - 数据转换器
6. **CommunicationViewModel** - 通讯配置视图模型

### 支持的协议类型

- **Modbus TCP** - 基于以太网的 Modbus 协议
- **Modbus RTU** - 基于串口的 Modbus 协议
- **Modbus ASCII** - Modbus ASCII 模式
- **Siemens S7** - 西门子 S7 协议
- **Mitsubishi MC** - 三菱 MC 协议
- **Omron FINS** - 欧姆龙 FINS 协议
- **Profinet** - Profinet 协议
- **Ethernet/IP** - Ethernet/IP 协议
- **CANOpen** - CANOpen 协议
- **MQTT** - MQTT 协议
- **OPC UA** - OPC UA 协议
- **Custom** - 自定义协议

### 支持的接口类型

- **Ethernet** - 以太网接口
- **Serial** - 串口接口
- **USB** - USB 接口
- **CAN** - CAN 总线接口
- **Bluetooth** - 蓝牙接口
- **WiFi** - WiFi 接口

## 🚀 快速开始

### 1. 添加通讯服务

在 `App.xaml.cs` 中已经添加了通讯服务：

```csharp
// 在 ConfigureServices 方法中添加
services.AddCommunicationServices();
services.AddTransient<CommunicationViewModel>();
```

### 2. 使用通讯管理器

```csharp
// 获取通讯管理器
var communicationManager = serviceProvider.GetRequiredService<ICommunicationManager>();

// 创建 Modbus TCP 协议
var protocol = new ModbusTcpProtocol("192.168.1.100", 502, 1, 5000);

// 注册协议
communicationManager.RegisterProtocol("PLC1", protocol);

// 连接设备
await communicationManager.ConnectAsync("PLC1");

// 发送接收数据
var response = await communicationManager.SendReceiveAsync("PLC1", data);

// 断开连接
await communicationManager.DisconnectAsync("PLC1");
```

### 3. 使用协议工厂

```csharp
// 获取协议工厂
var factory = serviceProvider.GetRequiredService<IProtocolFactory>();

// 创建协议实例
var config = new ProtocolConfiguration
{
    ProtocolId = "PLC1",
    ProtocolType = ProtocolType.ModbusTCP,
    DeviceAddress = "192.168.1.100",
    Port = 502
};

var protocol = factory.CreateProtocol(config);
```

## 📋 配置管理

### 配置文件位置

通讯协议配置保存在：
```
Config/Communication/
├── ModbusTCP_Default.json
├── ModbusRTU_Default.json
└── ...
```

### 配置示例

```json
{
  "ProtocolId": "ModbusTCP_Default",
  "ProtocolType": "ModbusTCP",
  "InterfaceType": "Ethernet",
  "DeviceAddress": "192.168.1.100",
  "Port": 502,
  "SlaveId": 1,
  "Timeout": 5000,
  "RetryCount": 3,
  "AutoReconnect": true,
  "ReconnectInterval": 5000,
  "CustomSettings": {
    "Description": "默认Modbus TCP配置",
    "RegisterStartAddress": "40001",
    "RegisterCount": "100"
  }
}
```

## 🔧 扩展协议

### 创建新协议

1. 继承 `ProtocolBase` 基类
2. 实现抽象方法
3. 在协议工厂中注册

```csharp
public class CustomProtocol : ProtocolBase
{
    public override string ProtocolName => "Custom Protocol";
    public override string ProtocolVersion => "1.0";
    
    public override Task<bool> ConnectAsync() { /* 实现连接逻辑 */ }
    public override Task DisconnectAsync() { /* 实现断开逻辑 */ }
    public override Task<byte[]> SendReceiveAsync(byte[] data, int timeout) { /* 实现发送接收逻辑 */ }
    // ... 其他方法实现
}
```

### 在工厂中注册

```csharp
public ProtocolBase CreateProtocol(ProtocolConfiguration config)
{
    return config.ProtocolType switch
    {
        ProtocolType.Custom => new CustomProtocol(config.DeviceAddress, config.Port),
        // ... 其他协议类型
        _ => throw new NotSupportedException($"不支持的协议类型: {config.ProtocolType}")
    };
}
```

## 🎨 用户界面

### 通讯配置界面

通讯配置界面提供了以下功能：

1. **协议配置管理**
   - 创建、编辑、删除协议配置
   - 导入/导出配置
   - 测试连接

2. **设备连接**
   - 连接/断开设备
   - 显示连接状态
   - 自动重连

3. **数据通讯**
   - 发送数据（支持多种格式）
   - 接收数据显示
   - 数据格式转换

4. **串口管理**
   - 自动检测可用串口
   - 串口参数配置

### 使用示例

```csharp
// 在视图模型中注入通讯服务
public CommunicationViewModel(
    ICommunicationManager communicationManager,
    IProtocolFactory protocolFactory,
    IProtocolConfigurationService configurationService,
    IDataConverter dataConverter)
{
    // 初始化
}
```

## 📊 数据格式

### 支持的数据格式

- **Binary** - 二进制格式
- **ASCII** - ASCII 文本格式
- **Hex** - 十六进制格式
- **JSON** - JSON 格式
- **XML** - XML 格式

### 数据转换

```csharp
// 获取数据转换器
var converter = serviceProvider.GetRequiredService<IDataConverter>();

// 字节数组转十六进制
var hex = converter.BytesToHex(data);

// 十六进制转字节数组
var bytes = converter.HexToBytes(hex);

// 字节数组转整数
var intValue = converter.BytesToInt32(data, isBigEndian: true);

// 浮点数转字节数组
var floatBytes = converter.FloatToBytes(3.14f, isBigEndian: true);
```

## 🔒 错误处理

### 错误类型

- **连接错误** - 设备连接失败
- **通讯错误** - 数据发送接收失败
- **配置错误** - 协议配置无效
- **超时错误** - 操作超时

### 错误处理示例

```csharp
try
{
    await communicationManager.ConnectAsync("PLC1");
}
catch (Exception ex)
{
    // 记录错误日志
    logger.LogError(ex, "连接设备失败");
    
    // 显示错误信息
    StatusMessage = $"连接失败: {ex.Message}";
    
    // 重试逻辑
    if (config.AutoReconnect)
    {
        await Task.Delay(config.ReconnectInterval);
        await ConnectAsync();
    }
}
```

## 📈 性能优化

### 批量操作

```csharp
// 批量发送数据
var requests = new Dictionary<string, byte[]>
{
    ["PLC1"] = data1,
    ["PLC2"] = data2,
    ["PLC3"] = data3
};

var responses = await communicationManager.SendReceiveBatchAsync(requests);
```

### 异步操作

所有通讯操作都支持异步，避免阻塞UI线程。

### 连接池

对于频繁连接的场景，可以实现连接池管理。

## 🧪 测试

### 单元测试

```csharp
[Test]
public async Task ModbusTcpProtocol_Connect_Success()
{
    // 准备
    var protocol = new ModbusTcpProtocol("192.168.1.100");
    
    // 执行
    var result = await protocol.ConnectAsync();
    
    // 断言
    Assert.IsTrue(result);
    Assert.IsTrue(protocol.IsConnected);
}
```

### 集成测试

```csharp
[Test]
public async Task CommunicationManager_MultipleProtocols_WorkCorrectly()
{
    // 准备
    var manager = new CommunicationManager();
    var tcpProtocol = new ModbusTcpProtocol("192.168.1.100");
    var rtuProtocol = new ModbusRtuProtocol("COM1");
    
    // 执行
    manager.RegisterProtocol("TCP", tcpProtocol);
    manager.RegisterProtocol("RTU", rtuProtocol);
    
    var tcpResult = await manager.ConnectAsync("TCP");
    var rtuResult = await manager.ConnectAsync("RTU");
    
    // 断言
    Assert.IsTrue(tcpResult);
    Assert.IsTrue(rtuResult);
}
```

## 📚 最佳实践

### 1. 配置验证

始终验证协议配置的有效性：

```csharp
if (!configurationService.ValidateConfiguration(config))
{
    throw new ArgumentException("协议配置无效");
}
```

### 2. 资源管理

及时释放协议资源：

```csharp
using var protocol = new ModbusTcpProtocol("192.168.1.100");
// 使用协议
```

### 3. 错误恢复

实现自动重连和错误恢复机制：

```csharp
public async Task ConnectWithRetryAsync(string protocolId, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await communicationManager.ConnectAsync(protocolId);
            return;
        }
        catch (Exception ex)
        {
            if (i == maxRetries - 1) throw;
            await Task.Delay(1000 * (i + 1));
        }
    }
}
```

### 4. 日志记录

记录重要的通讯事件：

```csharp
// 在事件处理中记录日志
private void OnDataReceived(object sender, CommunicationDataReceivedEventArgs e)
{
    logger.LogInformation("收到数据: {ProtocolId}, 长度: {Length}", 
        e.ProtocolId, e.RawData.Length);
}
```

## 🔄 更新日志

### v1.0.0 (2026-03-22)
- 初始版本发布
- 支持 Modbus TCP 和 Modbus RTU 协议
- 提供完整的通讯管理框架
- 包含配置管理和用户界面

## 🤝 贡献

欢迎提交 Issue 和 Pull Request 来改进通讯协议架构。

## 📄 许可证

本项目采用 MIT 许可证。

---
*最后更新: 2026-03-22*