using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace IndustrialControlHMI.Services;

/// <summary>
/// 配置管理器，负责应用程序配置的加载、保存和验证
/// </summary>
public class ConfigurationManager : IConfigurationManager
{
    private readonly IConfiguration _configuration;
    private readonly string _configDirectory;
    
    /// <summary>
    /// 配置变更事件
    /// </summary>
    public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    
    public ConfigurationManager()
    {
        // 配置目录：项目根目录下的config文件夹
        _configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        
        // 确保配置目录存在
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
        }
        
        // 构建配置
        _configuration = new ConfigurationBuilder()
            .SetBasePath(_configDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("CoordinatesConfig.json", optional: false, reloadOnChange: true)
            .AddJsonFile("wiring_config.json", optional: false, reloadOnChange: true)
            .Build();
            
        // 监听配置变更
        SetupConfigurationChangeHandlers();
    }
    
    /// <summary>
    /// 获取配置值
    /// </summary>
    public string GetString(string key, string defaultValue = "")
    {
        return _configuration[key] ?? defaultValue;
    }
    
    /// <summary>
    /// 获取整数配置值
    /// </summary>
    public int GetInt(string key, int defaultValue = 0)
    {
        if (int.TryParse(_configuration[key], out int result))
        {
            return result;
        }
        return defaultValue;
    }
    
    /// <summary>
    /// 获取布尔配置值
    /// </summary>
    public bool GetBool(string key, bool defaultValue = false)
    {
        if (bool.TryParse(_configuration[key], out bool result))
        {
            return result;
        }
        return defaultValue;
    }
    
    /// <summary>
    /// 获取连接字符串
    /// </summary>
    public string GetConnectionString(string name)
    {
        return _configuration.GetConnectionString(name) ?? string.Empty;
    }
    
    /// <summary>
    /// 获取Modbus配置
    /// </summary>
    public ModbusConfiguration GetModbusConfiguration()
    {
        return new ModbusConfiguration
        {
            IpAddress = GetString("Modbus:IpAddress", "192.168.1.100"),
            Port = GetInt("Modbus:Port", 502),
            SlaveId = GetInt("Modbus:SlaveId", 1),
            Timeout = GetInt("Modbus:Timeout", 5000),
            RetryCount = GetInt("Modbus:RetryCount", 3)
        };
    }
    
    /// <summary>
    /// 保存配置
    /// </summary>
    public async Task SaveConfigurationAsync<T>(string configName, T configuration)
    {
        try
        {
            var filePath = Path.Combine(_configDirectory, configName);
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await File.WriteAllTextAsync(filePath, json);
            
            // 触发配置变更事件
            OnConfigurationChanged(new ConfigurationChangedEventArgs
            {
                ConfigName = configName,
                ChangeType = ConfigurationChangeType.Updated
            });
        }
        catch (Exception ex)
        {
            throw new ConfigurationException($"保存配置失败: {configName}", ex);
        }
    }
    
    /// <summary>
    /// 加载配置
    /// </summary>
    public async Task<T> LoadConfigurationAsync<T>(string configName)
    {
        try
        {
            var filePath = Path.Combine(_configDirectory, configName);
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"配置文件不存在: {configName}", filePath);
            }
            
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("配置反序列化失败");
        }
        catch (Exception ex)
        {
            throw new ConfigurationException($"加载配置失败: {configName}", ex);
        }
    }
    
    /// <summary>
    /// 验证配置
    /// </summary>
    public bool ValidateConfiguration<T>(T configuration)
    {
        // 这里可以添加具体的验证逻辑
        // 例如：检查必填字段、范围验证等
        return configuration != null;
    }
    
    /// <summary>
    /// 获取配置目录路径
    /// </summary>
    public string GetConfigDirectory() => _configDirectory;
    
    /// <summary>
    /// 获取所有配置文件列表
    /// </summary>
    public string[] GetConfigurationFiles()
    {
        return Directory.GetFiles(_configDirectory, "*.json");
    }
    
    private void SetupConfigurationChangeHandlers()
    {
        // 这里可以添加配置变更的监听逻辑
        // 例如：当配置文件被修改时重新加载
    }
    
    private void OnConfigurationChanged(ConfigurationChangedEventArgs e)
    {
        ConfigurationChanged?.Invoke(this, e);
    }
}

/// <summary>
/// 配置管理器接口
/// </summary>
public interface IConfigurationManager
{
    event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    
    string GetString(string key, string defaultValue = "");
    int GetInt(string key, int defaultValue = 0);
    bool GetBool(string key, bool defaultValue = false);
    string GetConnectionString(string name);
    ModbusConfiguration GetModbusConfiguration();
    
    Task SaveConfigurationAsync<T>(string configName, T configuration);
    Task<T> LoadConfigurationAsync<T>(string configName);
    bool ValidateConfiguration<T>(T configuration);
    
    string GetConfigDirectory();
    string[] GetConfigurationFiles();
}

/// <summary>
/// Modbus配置
/// </summary>
public class ModbusConfiguration
{
    public string IpAddress { get; set; } = "192.168.1.100";
    public int Port { get; set; } = 502;
    public int SlaveId { get; set; } = 1;
    public int Timeout { get; set; } = 5000;
    public int RetryCount { get; set; } = 3;
}

/// <summary>
/// 配置变更事件参数
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public string ConfigName { get; set; } = string.Empty;
    public ConfigurationChangeType ChangeType { get; set; }
}

/// <summary>
/// 配置变更类型
/// </summary>
public enum ConfigurationChangeType
{
    Created,
    Updated,
    Deleted,
    Reloaded
}

/// <summary>
/// 配置异常
/// </summary>
public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}