using System;
using System.Threading.Tasks;

namespace IndustrialControlHMI.Services
{
    /// <summary>
    /// 设置管理器接口，提供配置的获取、设置、导入导出等功能。
    /// </summary>
    public interface ISettingsManager
    {
        /// <summary>
        /// 获取字符串配置值。
        /// </summary>
        Task<string?> GetSettingAsync(string category, string key, string? defaultValue = null);

        /// <summary>
        /// 获取强类型配置值。
        /// </summary>
        Task<T?> GetSettingAsync<T>(string category, string key, T? defaultValue = default);

        /// <summary>
        /// 设置字符串配置值。
        /// </summary>
        Task<bool> SetSettingAsync(string category, string key, string value);

        /// <summary>
        /// 设置强类型配置值。
        /// </summary>
        Task<bool> SetSettingAsync<T>(string category, string key, T value);

        /// <summary>
        /// 重置所有设置为默认值。
        /// </summary>
        Task<bool> ResetToDefaultsAsync();

        /// <summary>
        /// 导出设置到文件。
        /// </summary>
        Task ExportSettingsAsync(string filePath);

        /// <summary>
        /// 从文件导入设置。
        /// </summary>
        Task ImportSettingsAsync(string filePath);

        /// <summary>
        /// 设置更改事件。
        /// </summary>
        event EventHandler<SettingChangedEventArgs> SettingChanged;
    }

    /// <summary>
    /// 设置更改事件参数。
    /// </summary>
    public class SettingChangedEventArgs : EventArgs
    {
        public string Category { get; }
        public string Key { get; }
        public object Value { get; }

        public SettingChangedEventArgs(string category, string key, object value)
        {
            Category = category;
            Key = key;
            Value = value;
        }
    }
}