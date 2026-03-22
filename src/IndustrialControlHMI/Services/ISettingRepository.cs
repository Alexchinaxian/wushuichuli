using System.Collections.Generic;
using System.Threading.Tasks;
using IndustrialControlHMI.Models;

namespace IndustrialControlHMI.Services
{
    /// <summary>
    /// 设置存储库接口，提供配置项的持久化操作。
    /// </summary>
    public interface ISettingRepository
    {
        /// <summary>
        /// 根据分类和键获取设置值。
        /// </summary>
        Task<string?> GetSettingAsync(string category, string key);

        /// <summary>
        /// 设置或更新配置项。
        /// </summary>
        Task<bool> SetSettingAsync(string category, string key, string value);

        /// <summary>
        /// 获取指定分类的所有配置项。
        /// </summary>
        Task<IEnumerable<Setting>> GetSettingsByCategoryAsync(string category);

        /// <summary>
        /// 获取所有配置项。
        /// </summary>
        Task<IEnumerable<Setting>> GetAllAsync();

        /// <summary>
        /// 添加新配置项。
        /// </summary>
        Task AddAsync(Setting setting);

        /// <summary>
        /// 删除指定分类和键的配置项。
        /// </summary>
        Task<bool> DeleteSettingAsync(string category, string key);

        /// <summary>
        /// 删除所有配置项。
        /// </summary>
        Task DeleteAllSettingsAsync();

        /// <summary>
        /// 保存更改。
        /// </summary>
        Task SaveChangesAsync();
    }
}