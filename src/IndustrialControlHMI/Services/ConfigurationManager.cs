using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using IndustrialControlHMI.Infrastructure.Config;
using IndustrialControlHMI.Services.Logging;
using Microsoft.Extensions.Configuration;

namespace IndustrialControlHMI.Services;

/// <summary>
/// 配置管理器，负责应用程序配置的加载、保存和验证
/// </summary>
public class ConfigurationManager : IConfigurationManager
{
    private readonly IConfiguration _configuration;
    private readonly string _configDirectory;
    private readonly IAppLogger _logger;
    private FileSystemWatcher? _configWatcher;
    
    /// <summary>
    /// 配置变更事件
    /// </summary>
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    
    public ConfigurationManager(IAppLogger logger)
    {
        _logger = logger;
        // 配置目录优先读取环境变量 INDUSTRIAL_HMI_CONFIG_DIR，否则使用应用目录下 Config
        _configDirectory = AppConfigPaths.GetConfigDirectory();
        EnsureDefaultConfigFiles();
        
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
            var safeFileName = NormalizeConfigFileName(configName);
            var filePath = Path.Combine(_configDirectory, safeFileName);
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await File.WriteAllTextAsync(filePath, json);
            
            // 触发配置变更事件
            OnConfigurationChanged(new ConfigurationChangedEventArgs
            {
                ConfigName = safeFileName,
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
            var safeFileName = NormalizeConfigFileName(configName);
            var filePath = Path.Combine(_configDirectory, safeFileName);
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"配置文件不存在: {safeFileName}", filePath);
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
        if (configuration == null)
            return false;

        // 仅对已知配置类型做最小校验，避免错误配置进入运行时。
        if (configuration is ModbusConfiguration modbus)
        {
            if (string.IsNullOrWhiteSpace(modbus.IpAddress))
                return false;
            if (modbus.Port <= 0 || modbus.Port > 65535)
                return false;
            if (modbus.SlaveId <= 0 || modbus.SlaveId > 247)
                return false;
            if (modbus.Timeout <= 0 || modbus.RetryCount < 0)
                return false;
        }

        return true;
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
        _configWatcher?.Dispose();
        _configWatcher = new FileSystemWatcher(_configDirectory, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };

        _configWatcher.Changed += (_, e) => RaiseConfigChanged(e);
        _configWatcher.Created += (_, e) => RaiseConfigChanged(e);
        _configWatcher.Deleted += (_, e) => RaiseConfigChanged(e);
        _configWatcher.Renamed += (_, e) => RaiseConfigChanged(e);
        _configWatcher.EnableRaisingEvents = true;
    }

    private void RaiseConfigChanged(FileSystemEventArgs e)
    {
        var changeType = e.ChangeType switch
        {
            WatcherChangeTypes.Created => ConfigurationChangeType.Created,
            WatcherChangeTypes.Deleted => ConfigurationChangeType.Deleted,
            _ => ConfigurationChangeType.Updated
        };

        OnConfigurationChanged(new ConfigurationChangedEventArgs
        {
            ConfigName = Path.GetFileName(e.FullPath),
            ChangeType = changeType
        });
    }

    private void EnsureDefaultConfigFiles()
    {
        EnsureJsonFile("appsettings.json", "{}");
        EnsureJsonFile("CoordinatesConfig.json", "{\"version\":\"1.0\",\"items\":[],\"flowLines\":[]}");
        EnsureJsonFile("wiring_config.json", "{\"version\":\"1.0\",\"connectionPoints\":{},\"wiringRules\":[]}");
    }

    private void EnsureJsonFile(string fileName, string defaultJson)
    {
        var path = Path.Combine(_configDirectory, NormalizeConfigFileName(fileName));
        if (File.Exists(path))
            return;

        File.WriteAllText(path, defaultJson);
        _logger.Info($"已创建默认配置文件: {path}");
    }

    private static string NormalizeConfigFileName(string configName)
    {
        if (string.IsNullOrWhiteSpace(configName))
            throw new ArgumentException("配置文件名不能为空。", nameof(configName));

        var fileName = Path.GetFileName(configName.Trim());
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("仅支持 .json 配置文件。", nameof(configName));
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("配置文件名包含非法字符。", nameof(configName));

        return fileName;
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