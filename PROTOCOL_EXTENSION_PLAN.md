# 通讯协议扩展计划

## 🎯 扩展目标

### 第一阶段（1-2周）
1. **OPC UA 协议** - 现代工业标准
2. **S7 Protocol** - 西门子设备支持
3. **协议管理增强** - 统一配置界面

### 第二阶段（2-4周）
4. **Ethernet/IP** - 罗克韦尔设备
5. **MQTT** - IoT集成
6. **协议桥接** - 协议间数据转换

### 第三阶段（1-2月）
7. **Profinet**
8. **CANOpen**
9. **多协议并行支持**

## 📁 文件结构规划

```
src/IndustrialControlHMI/Services/Communication/
├── ProtocolBase.cs                          # 协议基类（已有）
├── CommunicationManager.cs                  # 通讯管理器（已有）
├── CommunicationServiceExtensions.cs        # 服务扩展（已有）
├── Protocols/
│   ├── ModbusTcpProtocol.cs                 # Modbus TCP（已有）
│   ├── SerialProtocol.cs                    # 串口基类（已有）
│   ├── ModbusRtuProtocol.cs                 # Modbus RTU（已有）
│   ├── OpcUaProtocol.cs                     # OPC UA（新增）
│   ├── S7Protocol.cs                        # 西门子S7（新增）
│   ├── EthernetIpProtocol.cs                # Ethernet/IP（新增）
│   ├── MqttProtocol.cs                      # MQTT（新增）
│   └── Common/
│       ├── DataConverter.cs                 # 数据转换工具（已有）
│       ├── ProtocolValidator.cs             # 协议验证器（新增）
│       └── ProtocolBridge.cs                # 协议桥接器（新增）
└── Configuration/
    ├── ProtocolConfiguration.cs             # 协议配置（已有）
    ├── OpcUaConfiguration.cs                # OPC UA配置（新增）
    ├── S7Configuration.cs                   # S7配置（新增）
    └── ProtocolConfigurationValidator.cs    # 配置验证器（新增）
```

## 🔧 具体实现步骤

### 步骤1：扩展 ProtocolType 枚举

```csharp
// 在 ProtocolType 枚举中添加
public enum ProtocolType
{
    ModbusTCP,      // 已有
    ModbusRTU,      // 已有
    ModbusASCII,    // 已有
    OpcUA,          // 新增
    S7,             // 新增
    EthernetIP,     // 新增
    MQTT,           // 新增
    Profinet,       // 新增
    CANOpen,        // 新增
    Custom          // 已有
}
```

### 步骤2：创建 OPC UA 协议实现

```csharp
public class OpcUaProtocol : ProtocolBase
{
    // OPC UA 特定属性
    private readonly string _serverUrl;
    private readonly string _username;
    private readonly string _password;
    private readonly string[] _securityPolicies;
    
    public OpcUaProtocol(string serverUrl, string username = null, string password = null)
    {
        _serverUrl = serverUrl;
        _username = username;
        _password = password;
    }
    
    public override string ProtocolName => "OPC UA";
    public override string ProtocolVersion => "1.04";
    
    // OPC UA 特定方法
    public async Task<object> ReadNodeAsync(string nodeId)
    {
        // 实现 OPC UA 节点读取
    }
    
    public async Task WriteNodeAsync(string nodeId, object value)
    {
        // 实现 OPC UA 节点写入
    }
    
    public async Task SubscribeAsync(string nodeId, Action<object> callback)
    {
        // 实现 OPC UA 订阅
    }
}
```

### 步骤3：创建 S7 协议实现

```csharp
public class S7Protocol : ProtocolBase
{
    // S7 特定属性
    private readonly string _ipAddress;
    private readonly int _rack;
    private readonly int _slot;
    private readonly int _connectionType;
    
    public S7Protocol(string ipAddress, int rack = 0, int slot = 1, int connectionType = 1)
    {
        _ipAddress = ipAddress;
        _rack = rack;
        _slot = slot;
        _connectionType = connectionType;
    }
    
    public override string ProtocolName => "Siemens S7";
    public override string ProtocolVersion => "S7Comm";
    
    // S7 特定方法
    public async Task<byte[]> ReadDataAsync(DataArea area, int dbNumber, int startByte, int length)
    {
        // 实现 S7 数据读取
    }
    
    public async Task WriteDataAsync(DataArea area, int dbNumber, int startByte, byte[] data)
    {
        // 实现 S7 数据写入
    }
    
    public enum DataArea
    {
        Inputs,
        Outputs,
        Flags,
        DataBlocks,
        Timers,
        Counters
    }
}
```

### 步骤4：扩展协议工厂

```csharp
public class ProtocolFactory : IProtocolFactory
{
    public ProtocolBase CreateProtocol(ProtocolConfiguration config)
    {
        return config.ProtocolType switch
        {
            ProtocolType.ModbusTCP => CreateModbusTcpProtocol(config),
            ProtocolType.ModbusRTU => CreateModbusRtuProtocol(config),
            ProtocolType.OpcUA => CreateOpcUaProtocol(config),      // 新增
            ProtocolType.S7 => CreateS7Protocol(config),            // 新增
            ProtocolType.EthernetIP => CreateEthernetIpProtocol(config), // 新增
            ProtocolType.MQTT => CreateMqttProtocol(config),        // 新增
            _ => throw new NotSupportedException($"不支持的协议类型: {config.ProtocolType}")
        };
    }
    
    // 新增工厂方法
    public ProtocolBase CreateOpcUaProtocol(ProtocolConfiguration config)
    {
        return new OpcUaProtocol(
            config.DeviceAddress,
            config.CustomSettings.TryGetValue("Username", out var username) ? username : null,
            config.CustomSettings.TryGetValue("Password", out var password) ? password : null
        );
    }
    
    public ProtocolBase CreateS7Protocol(ProtocolConfiguration config)
    {
        return new S7Protocol(
            config.DeviceAddress,
            config.CustomSettings.TryGetValue("Rack", out var rackStr) && int.TryParse(rackStr, out var rack) ? rack : 0,
            config.CustomSettings.TryGetValue("Slot", out var slotStr) && int.TryParse(slotStr, out var slot) ? slot : 1
        );
    }
}
```

### 步骤5：扩展配置界面

在 `CommunicationSettingsView.xaml` 中添加新的协议配置选项：

```xml
<!-- OPC UA 配置 -->
<GroupBox Header="OPC UA 配置" 
          Visibility="{Binding SelectedConfiguration.ProtocolType, 
                         Converter={StaticResource ProtocolTypeToVisibilityConverter},
                         ConverterParameter=OpcUA}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <Label Grid.Row="0">服务器URL:</Label>
        <TextBox Grid.Row="0" Grid.Column="1" 
               Text="{Binding SelectedConfiguration.DeviceAddress}"/>
        
        <Label Grid.Row="1">用户名:</Label>
        <TextBox Grid.Row="1" Grid.Column="1" 
               Text="{Binding SelectedConfiguration.CustomSettings[Username]}"/>
        
        <Label Grid.Row="2">密码:</Label>
        <PasswordBox Grid.Row="2" Grid.Column="1" 
                   Password="{Binding SelectedConfiguration.CustomSettings[Password]}"/>
    </Grid>
</GroupBox>

<!-- S7 配置 -->
<GroupBox Header="S7 配置" 
          Visibility="{Binding SelectedConfiguration.ProtocolType, 
                         Converter={StaticResource ProtocolTypeToVisibilityConverter},
                         ConverterParameter=S7}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <Label Grid.Row="0">IP地址:</Label>
        <TextBox Grid.Row="0" Grid.Column="1" 
               Text="{Binding SelectedConfiguration.DeviceAddress}"/>
        
        <Label Grid.Row="1">Rack:</Label>
        <TextBox Grid.Row="1" Grid.Column="1" 
               Text="{Binding SelectedConfiguration.CustomSettings[Rack]}"/>
        
        <Label Grid.Row="2">Slot:</Label>
        <TextBox Grid.Row="2" Grid.Column="1" 
               Text="{Binding SelectedConfiguration.CustomSettings[Slot]}"/>
    </Grid>
</GroupBox>
```

## 📦 需要的 NuGet 包

### OPC UA 支持
```xml
<PackageReference Include="OPCFoundation.NetStandard.Opc.Ua" Version="1.4.368.58" />
<PackageReference Include="OPCFoundation.NetStandard.Opc.Ua.Client" Version="1.4.368.58" />
```

### S7 协议支持
```xml
<PackageReference Include="S7NetPlus" Version="1.0.2" />
<!-- 或 -->
<PackageReference Include="Sharp7" Version="1.1.0" />
```

### MQTT 支持
```xml
<PackageReference Include="MQTTnet" Version="4.3.7.1207" />
```

### Ethernet/IP 支持
```xml
<PackageReference Include="EthernetIP" Version="1.0.0" />
<!-- 或 -->
<PackageReference Include="LibPlcTag" Version="1.0.0" />
```

## 🚀 实施计划

### 第1天：准备阶段
1. 添加必要的 NuGet 包
2. 扩展 ProtocolType 枚举
3. 更新协议工厂

### 第2-3天：实现 OPC UA
1. 创建 OpcUaProtocol 类
2. 实现基本连接和读写
3. 添加配置界面

### 第4-5天：实现 S7 协议
1. 创建 S7Protocol 类
2. 实现 S7 通讯
3. 添加配置界面

### 第6-7天：测试和集成
1. 编写单元测试
2. 集成到主界面
3. 文档更新

## 🔍 测试策略

### 单元测试
```csharp
[Fact]
public async Task OpcUaProtocol_Connect_Success()
{
    // 使用模拟服务器测试
    var protocol = new OpcUaProtocol("opc.tcp://localhost:4840");
    var result = await protocol.ConnectAsync();
    Assert.True(result);
}

[Fact]
public async Task S7Protocol_ReadData_Success()
{
    // 使用模拟PLC测试
    var protocol = new S7Protocol("192.168.1.100");
    await protocol.ConnectAsync();
    var data = await protocol.ReadDataAsync(S7Protocol.DataArea.DataBlocks, 1, 0, 10);
    Assert.NotNull(data);
}
```

### 集成测试
1. 连接真实设备测试
2. 多协议并行测试
3. 长时间运行稳定性测试

## 📚 文档更新

需要更新的文档：
1. `COMMUNICATION_PROTOCOL_GUIDE.md` - 添加新协议说明
2. `CONNECTION_SETTINGS_GUIDE.md` - 更新配置指南
3. `API_DOCUMENTATION.md` - 添加新协议API文档

## 💡 最佳实践

### 1. 错误处理
```csharp
public async Task<OperationResult> SafeConnectAsync()
{
    try
    {
        var connected = await ConnectAsync();
        return OperationResult.Success(connected);
    }
    catch (OpcUaException ex) when (ex.StatusCode == StatusCodes.BadCertificateInvalid)
    {
        return OperationResult.Failure("证书无效，请检查证书配置");
    }
    catch (SocketException ex)
    {
        return OperationResult.Failure($"网络错误: {ex.Message}");
    }
}
```

### 2. 性能优化
- 使用连接池管理多个连接
- 实现数据缓存机制
- 支持批量读写操作

### 3. 安全性
- 支持证书认证
- 加密数据传输
- 访问控制列表

## 🎯 成功标准

### 技术标准
- [ ] 所有新协议实现 ProtocolBase 接口
- [ ] 通过单元测试
- [ ] 通过集成测试
- [ ] 性能满足要求（<100ms响应时间）

### 功能标准
- [ ] 支持配置界面
- [ ] 支持连接状态显示
- [ ] 支持数据读写
- [ ] 支持错误处理

### 质量标准
- [ ] 代码注释完整
- [ ] 文档更新
- [ ] 向后兼容
- [ ] 易于扩展

## 🔄 向后兼容性

确保：
1. 现有 Modbus 协议不受影响
2. 配置格式兼容
3. API 接口兼容
4. 界面布局兼容

## 📞 支持资源

### 学习资源
- OPC UA 规范：https://opcfoundation.org/
- S7 协议文档：https://support.industry.siemens.com/
- MQTT 规范：http://mqtt.org/

### 开发工具
- OPC UA 模拟服务器：Prosys OPC UA Simulation Server
- S7 模拟器：PLCSIM Advanced
- MQTT 测试工具：MQTT.fx

### 社区支持
- OPC Foundation 论坛
- Siemens 开发者社区
- MQTT GitHub 仓库

---

**开始建议**：从 OPC UA 开始，因为它是现代工业标准，有成熟的 .NET 库支持，且通用性强。