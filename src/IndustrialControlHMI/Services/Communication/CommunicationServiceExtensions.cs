using System.Net;
using System.Text;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Ports;

namespace IndustrialControlHMI.Services.Communication
{
    /// <summary>
    /// 通讯服务扩展方法
    /// </summary>
    public static class CommunicationServiceExtensions
    {
        /// <summary>
        /// 添加通讯服务
        /// </summary>
        public static IServiceCollection AddCommunicationServices(this IServiceCollection services)
        {
            // 注册通讯管理器
            services.AddSingleton<ICommunicationManager, CommunicationManager>();
            
            // 注册协议工厂
            services.AddSingleton<IProtocolFactory, ProtocolFactory>();
            
            // 注册协议配置服务
            services.AddSingleton<IProtocolConfigurationService, ProtocolConfigurationService>();
            
            // 注册数据转换服务
            services.AddSingleton<IDataConverter, DataConverter>();
            
            return services;
        }
    }
    
    /// <summary>
    /// 协议工厂接口
    /// </summary>
    public interface IProtocolFactory
    {
        /// <summary>
        /// 创建协议实例
        /// </summary>
        ProtocolBase CreateProtocol(ProtocolConfiguration config);
        
        /// <summary>
        /// 创建 Modbus TCP 协议
        /// </summary>
        ProtocolBase CreateModbusTcpProtocol(string ipAddress, int port = 502, int slaveId = 1, int timeout = 5000);
        
        /// <summary>
        /// 创建 Modbus RTU 协议
        /// </summary>
        ProtocolBase CreateModbusRtuProtocol(
            string portName,
            int slaveId = 1,
            int baudRate = 9600,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One,
            int timeout = 5000);
    }
    
    /// <summary>
    /// 协议工厂实现
    /// </summary>
    public class ProtocolFactory : IProtocolFactory
    {
        public ProtocolBase CreateProtocol(ProtocolConfiguration config)
        {
            return config.ProtocolType switch
            {
                ProtocolType.ModbusTCP => CreateModbusTcpProtocol(
                    config.DeviceAddress,
                    config.Port,
                    config.CustomSettings.TryGetValue("SlaveId", out var slaveIdStr) && int.TryParse(slaveIdStr, out var slaveId) ? slaveId : 1,
                    config.Timeout),
                
                ProtocolType.ModbusRTU => CreateModbusRtuProtocol(
                    config.DeviceAddress,
                    config.CustomSettings.TryGetValue("SlaveId", out var rtuSlaveIdStr) && int.TryParse(rtuSlaveIdStr, out var rtuSlaveId) ? rtuSlaveId : 1,
                    config.BaudRate,
                    ParseParity(config.Parity),
                    config.DataBits,
                    ParseStopBits(config.StopBits),
                    config.Timeout),
                
                _ => throw new NotSupportedException($"不支持的协议类型: {config.ProtocolType}")
            };
        }
        
        public ProtocolBase CreateModbusTcpProtocol(string ipAddress, int port = 502, int slaveId = 1, int timeout = 5000)
        {
            return new Protocols.ModbusTcpProtocol(ipAddress, port, slaveId, timeout);
        }
        
        public ProtocolBase CreateModbusRtuProtocol(
            string portName,
            int slaveId = 1,
            int baudRate = 9600,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One,
            int timeout = 5000)
        {
            return new Protocols.ModbusRtuProtocol(portName, slaveId, baudRate, parity, dataBits, stopBits, timeout);
        }
        
        private static Parity ParseParity(string parity)
        {
            return parity.ToLower() switch
            {
                "none" => Parity.None,
                "odd" => Parity.Odd,
                "even" => Parity.Even,
                "mark" => Parity.Mark,
                "space" => Parity.Space,
                _ => Parity.None
            };
        }
        
        private static StopBits ParseStopBits(string stopBits)
        {
            return stopBits.ToLower() switch
            {
                "none" => StopBits.None,
                "one" => StopBits.One,
                "two" => StopBits.Two,
                "onepointfive" => StopBits.OnePointFive,
                _ => StopBits.One
            };
        }
    }
    
    /// <summary>
    /// 协议配置服务接口
    /// </summary>
    public interface IProtocolConfigurationService
    {
        /// <summary>
        /// 加载所有协议配置
        /// </summary>
        Task<IEnumerable<ProtocolConfiguration>> LoadAllConfigurationsAsync();
        
        /// <summary>
        /// 保存协议配置
        /// </summary>
        Task SaveConfigurationAsync(ProtocolConfiguration config);
        
        /// <summary>
        /// 删除协议配置
        /// </summary>
        Task DeleteConfigurationAsync(string protocolId);
        
        /// <summary>
        /// 验证协议配置
        /// </summary>
        bool ValidateConfiguration(ProtocolConfiguration config);
    }
    
    /// <summary>
    /// 协议配置服务实现
    /// </summary>
    public class ProtocolConfigurationService : IProtocolConfigurationService
    {
        private readonly string _configDirectory;
        
        public ProtocolConfigurationService()
        {
            _configDirectory = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Config",
                "Communication"
            );
            
            System.IO.Directory.CreateDirectory(_configDirectory);
        }
        
        public async Task<IEnumerable<ProtocolConfiguration>> LoadAllConfigurationsAsync()
        {
            var configurations = new List<ProtocolConfiguration>();
            
            if (!System.IO.Directory.Exists(_configDirectory))
                return configurations;
            
            var files = System.IO.Directory.GetFiles(_configDirectory, "*.json");
            
            foreach (var file in files)
            {
                try
                {
                    var json = await System.IO.File.ReadAllTextAsync(file);
                    var config = System.Text.Json.JsonSerializer.Deserialize<ProtocolConfiguration>(json);
                    
                    if (config != null)
                        configurations.Add(config);
                }
                catch (Exception)
                {
                    // 忽略无效的配置文件
                }
            }
            
            return configurations;
        }
        
        public async Task SaveConfigurationAsync(ProtocolConfiguration config)
        {
            if (!ValidateConfiguration(config))
                throw new ArgumentException("协议配置无效");
            
            var filePath = System.IO.Path.Combine(_configDirectory, $"{config.ProtocolId}.json");
            var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }
        
        public async Task DeleteConfigurationAsync(string protocolId)
        {
            var filePath = System.IO.Path.Combine(_configDirectory, $"{protocolId}.json");
            
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
            
            await Task.CompletedTask;
        }
        
        public bool ValidateConfiguration(ProtocolConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.ProtocolId))
                return false;
            
            if (string.IsNullOrWhiteSpace(config.DeviceAddress))
                return false;
            
            // 根据协议类型进行验证
            return config.ProtocolType switch
            {
                ProtocolType.ModbusTCP => ValidateModbusTcpConfig(config),
                ProtocolType.ModbusRTU => ValidateModbusRtuConfig(config),
                _ => true // 其他协议类型暂时不验证
            };
        }
        
        private bool ValidateModbusTcpConfig(ProtocolConfiguration config)
        {
            if (config.Port <= 0 || config.Port > 65535)
                return false;
            
            // 验证IP地址格式
            if (!System.Net.IPAddress.TryParse(config.DeviceAddress, out _))
                return false;
            
            return true;
        }
        
        private bool ValidateModbusRtuConfig(ProtocolConfiguration config)
        {
            if (config.BaudRate <= 0)
                return false;
            
            if (config.DataBits < 5 || config.DataBits > 8)
                return false;
            
            return true;
        }
    }
    
    /// <summary>
    /// 数据转换器接口
    /// </summary>
    public interface IDataConverter
    {
        /// <summary>
        /// 字节数组转十六进制字符串
        /// </summary>
        string BytesToHex(byte[] data, string separator = " ");
        
        /// <summary>
        /// 十六进制字符串转字节数组
        /// </summary>
        byte[] HexToBytes(string hex);
        
        /// <summary>
        /// 字节数组转ASCII字符串
        /// </summary>
        string BytesToAscii(byte[] data);
        
        /// <summary>
        /// ASCII字符串转字节数组
        /// </summary>
        byte[] AsciiToBytes(string ascii);
        
        /// <summary>
        /// 字节数组转整数
        /// </summary>
        int BytesToInt32(byte[] data, bool isBigEndian = true);
        
        /// <summary>
        /// 整数转字节数组
        /// </summary>
        byte[] Int32ToBytes(int value, bool isBigEndian = true);
        
        /// <summary>
        /// 字节数组转浮点数
        /// </summary>
        float BytesToFloat(byte[] data, bool isBigEndian = true);
        
        /// <summary>
        /// 浮点数转字节数组
        /// </summary>
        byte[] FloatToBytes(float value, bool isBigEndian = true);
    }
    
    /// <summary>
    /// 数据转换器实现
    /// </summary>
    public class DataConverter : IDataConverter
    {
        public string BytesToHex(byte[] data, string separator = " ")
        {
            return BitConverter.ToString(data).Replace("-", separator);
        }
        
        public byte[] HexToBytes(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", "");
            
            if (hex.Length % 2 != 0)
                throw new ArgumentException("十六进制字符串长度必须为偶数");
            
            var bytes = new byte[hex.Length / 2];
            
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            
            return bytes;
        }
        
        public string BytesToAscii(byte[] data)
        {
            return System.Text.Encoding.ASCII.GetString(data);
        }
        
        public byte[] AsciiToBytes(string ascii)
        {
            return System.Text.Encoding.ASCII.GetBytes(ascii);
        }
        
        public int BytesToInt32(byte[] data, bool isBigEndian = true)
        {
            if (data.Length < 4)
                throw new ArgumentException("数据长度不足4字节");
            
            if (isBigEndian)
            {
                return (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
            }
            else
            {
                return (data[3] << 24) | (data[2] << 16) | (data[1] << 8) | data[0];
            }
        }
        
        public byte[] Int32ToBytes(int value, bool isBigEndian = true)
        {
            var bytes = new byte[4];
            
            if (isBigEndian)
            {
                bytes[0] = (byte)(value >> 24);
                bytes[1] = (byte)(value >> 16);
                bytes[2] = (byte)(value >> 8);
                bytes[3] = (byte)value;
            }
            else
            {
                bytes[3] = (byte)(value >> 24);
                bytes[2] = (byte)(value >> 16);
                bytes[1] = (byte)(value >> 8);
                bytes[0] = (byte)value;
            }
            
            return bytes;
        }
        
        public float BytesToFloat(byte[] data, bool isBigEndian = true)
        {
            if (data.Length < 4)
                throw new ArgumentException("数据长度不足4字节");
            
            if (isBigEndian != BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            
            return BitConverter.ToSingle(data, 0);
        }
        
        public byte[] FloatToBytes(float value, bool isBigEndian = true)
        {
            var bytes = BitConverter.GetBytes(value);
            
            if (isBigEndian != BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            
            return bytes;
        }
    }
}