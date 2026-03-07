using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using IndustrialControlHMI.Models;

namespace IndustrialControlHMI.Services
{
    /// <summary>
    /// 设置管理器，提供配置的缓存、验证、持久化和导入导出功能。
    /// </summary>
    public class SettingsManager : ISettingsManager
    {
        private readonly ISettingRepository _repository;
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();
        private readonly object _cacheLock = new object();

        public SettingsManager(ISettingRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc />
        public async Task<string> GetSettingAsync(string category, string key, string defaultValue = null)
        {
            string cacheKey = $"{category}.{key}";

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(cacheKey, out var cachedValue))
                {
                    return cachedValue as string ?? defaultValue;
                }
            }

            try
            {
                var setting = await _repository.GetSettingAsync(category, key);

                lock (_cacheLock)
                {
                    _cache[cacheKey] = setting ?? defaultValue;
                }

                return setting ?? defaultValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取设置失败: {category}.{key}, 错误: {ex.Message}");
                return defaultValue;
            }
        }

        /// <inheritdoc />
        public async Task<T> GetSettingAsync<T>(string category, string key, T defaultValue = default)
        {
            string cacheKey = $"{category}.{key}";

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(cacheKey, out var cachedValue))
                {
                    return cachedValue is T typedValue ? typedValue : defaultValue;
                }
            }

            try
            {
                var settingValue = await _repository.GetSettingAsync(category, key);

                if (string.IsNullOrEmpty(settingValue))
                {
                    lock (_cacheLock)
                    {
                        _cache[cacheKey] = defaultValue;
                    }
                    return defaultValue;
                }

                T value;

                // 根据类型转换
                if (typeof(T) == typeof(string))
                {
                    value = (T)(object)settingValue;
                }
                else if (typeof(T) == typeof(int))
                {
                    value = (T)(object)int.Parse(settingValue);
                }
                else if (typeof(T) == typeof(double))
                {
                    value = (T)(object)double.Parse(settingValue);
                }
                else if (typeof(T) == typeof(bool))
                {
                    value = (T)(object)bool.Parse(settingValue);
                }
                else if (typeof(T) == typeof(DateTime))
                {
                    value = (T)(object)DateTime.Parse(settingValue);
                }
                else
                {
                    // 尝试JSON反序列化
                    value = JsonSerializer.Deserialize<T>(settingValue);
                }

                lock (_cacheLock)
                {
                    _cache[cacheKey] = value;
                }

                return value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取设置失败: {category}.{key}, 类型: {typeof(T).Name}, 错误: {ex.Message}");
                return defaultValue;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetSettingAsync(string category, string key, string value)
        {
            string cacheKey = $"{category}.{key}";

            try
            {
                bool success = await _repository.SetSettingAsync(category, key, value);

                if (success)
                {
                    lock (_cacheLock)
                    {
                        _cache[cacheKey] = value;
                    }

                    // 触发设置更改事件
                    OnSettingChanged(new SettingChangedEventArgs(category, key, value));

                    System.Diagnostics.Debug.WriteLine($"设置已更新: {category}.{key} = {value}");
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新设置失败: {category}.{key}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetSettingAsync<T>(string category, string key, T value)
        {
            string stringValue;

            if (value is string str)
            {
                stringValue = str;
            }
            else if (value is int || value is double || value is bool || value is DateTime)
            {
                stringValue = value.ToString();
            }
            else
            {
                // 序列化为JSON
                stringValue = JsonSerializer.Serialize(value);
            }

            return await SetSettingAsync(category, key, stringValue);
        }

        /// <inheritdoc />
        public async Task<bool> ResetToDefaultsAsync()
        {
            try
            {
                // 清除缓存
                lock (_cacheLock)
                {
                    _cache.Clear();
                }

                // 删除所有设置
                await _repository.DeleteAllSettingsAsync();

                // 添加默认设置
                var defaultSettings = GetDefaultSettings();
                foreach (var setting in defaultSettings)
                {
                    await _repository.AddAsync(setting);
                }

                System.Diagnostics.Debug.WriteLine("设置已重置为默认值");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重置设置为默认值失败: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task ExportSettingsAsync(string filePath)
        {
            try
            {
                var settings = await _repository.GetAllAsync();

                var exportData = new SettingsExport
                {
                    ExportTime = DateTime.Now,
                    Version = "1.0",
                    Settings = settings.Select(s => new SettingExportItem
                    {
                        Category = s.Category,
                        Key = s.Key,
                        Value = s.Value,
                        DataType = s.DataType,
                        Description = s.Description
                    }).ToList()
                };

                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".json")
                {
                    string json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(filePath, json);
                }
                else if (extension == ".xml")
                {
                    // XML序列化
                    var serializer = new XmlSerializer(typeof(SettingsExport));
                    using var writer = new StreamWriter(filePath);
                    serializer.Serialize(writer, exportData);
                }
                else
                {
                    throw new NotSupportedException($"不支持的文件格式: {extension}");
                }

                System.Diagnostics.Debug.WriteLine($"设置已导出到: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出设置失败: {filePath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ImportSettingsAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"配置文件不存在: {filePath}");

                SettingsExport importData;
                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".json")
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    importData = JsonSerializer.Deserialize<SettingsExport>(json);
                }
                else if (extension == ".xml")
                {
                    var serializer = new XmlSerializer(typeof(SettingsExport));
                    using var reader = new StreamReader(filePath);
                    importData = (SettingsExport)serializer.Deserialize(reader);
                }
                else
                {
                    throw new NotSupportedException($"不支持的文件格式: {extension}");
                }

                // 验证导入数据
                if (importData == null || importData.Settings == null)
                    throw new InvalidDataException("配置文件格式无效");

                // 清空现有设置
                await _repository.DeleteAllSettingsAsync();

                // 导入新设置
                foreach (var item in importData.Settings)
                {
                    var setting = new Setting
                    {
                        Category = item.Category,
                        Key = item.Key,
                        Value = item.Value,
                        DataType = item.DataType,
                        Description = item.Description,
                        LastModified = DateTime.Now,
                        ModifiedBy = "Import"
                    };

                    await _repository.AddAsync(setting);
                }

                // 清除缓存
                lock (_cacheLock)
                {
                    _cache.Clear();
                }

                System.Diagnostics.Debug.WriteLine($"设置已从 {filePath} 导入");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导入设置失败: {filePath}, 错误: {ex.Message}");
                throw;
            }
        }

        private List<Setting> GetDefaultSettings()
        {
            return new List<Setting>
            {
                new Setting { Category = "Modbus", Key = "IpAddress", Value = "192.168.1.100", DataType = DataType.String, Description = "PLC IP地址" },
                new Setting { Category = "Modbus", Key = "Port", Value = "502", DataType = DataType.Integer, Description = "Modbus端口" },
                new Setting { Category = "Modbus", Key = "SlaveId", Value = "1", DataType = DataType.Integer, Description = "从站ID" },
                new Setting { Category = "Modbus", Key = "PollingInterval", Value = "1000", DataType = DataType.Integer, Description = "轮询间隔(ms)" },
                new Setting { Category = "Modbus", Key = "ReadTimeout", Value = "5000", DataType = DataType.Integer, Description = "读取超时(ms)" },
                new Setting { Category = "Modbus", Key = "WriteTimeout", Value = "5000", DataType = DataType.Integer, Description = "写入超时(ms)" },

                new Setting { Category = "Alarm.Temperature", Key = "HighHigh", Value = "90", DataType = DataType.Float, Description = "温度高高报警阈值(°C)" },
                new Setting { Category = "Alarm.Temperature", Key = "High", Value = "80", DataType = DataType.Float, Description = "温度高报警阈值(°C)" },
                new Setting { Category = "Alarm.Temperature", Key = "Low", Value = "10", DataType = DataType.Float, Description = "温度低报警阈值(°C)" },
                new Setting { Category = "Alarm.Temperature", Key = "LowLow", Value = "5", DataType = DataType.Float, Description = "温度低低报警阈值(°C)" },

                new Setting { Category = "Data", Key = "RetentionDays", Value = "30", DataType = DataType.Integer, Description = "数据保留天数" },
                new Setting { Category = "Data", Key = "EnableAutoBackup", Value = "true", DataType = DataType.Boolean, Description = "启用自动备份" },
                new Setting { Category = "Data", Key = "BackupIntervalHours", Value = "24", DataType = DataType.Integer, Description = "备份间隔(小时)" },

                new Setting { Category = "UI", Key = "Theme", Value = "Dark", DataType = DataType.String, Description = "界面主题" },
                new Setting { Category = "UI", Key = "ChartHistoryPoints", Value = "100", DataType = DataType.Integer, Description = "图表历史点数" },
                new Setting { Category = "UI", Key = "ChartRefreshRate", Value = "1.0", DataType = DataType.Float, Description = "图表刷新率(秒)" }
            };
        }

        protected virtual void OnSettingChanged(SettingChangedEventArgs e)
        {
            SettingChanged?.Invoke(this, e);
        }

        /// <inheritdoc />
        public event EventHandler<SettingChangedEventArgs> SettingChanged;
    }

    /// <summary>
    /// 设置导出数据模型。
    /// </summary>
    public class SettingsExport
    {
        public DateTime ExportTime { get; set; }
        public string Version { get; set; } = "1.0";
        public List<SettingExportItem> Settings { get; set; } = new List<SettingExportItem>();
    }

    /// <summary>
    /// 设置导出项。
    /// </summary>
    public class SettingExportItem
    {
        public string Category { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = IndustrialControlHMI.Models.DataType.String;
        public string Description { get; set; } = string.Empty;
    }
}