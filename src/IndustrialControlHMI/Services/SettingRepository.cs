using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IndustrialControlHMI.Infrastructure;
using IndustrialControlHMI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndustrialControlHMI.Services
{
    /// <summary>
    /// 设置存储库实现，使用Entity Framework Core操作SQLite数据库。
    /// </summary>
    public class SettingRepository : ISettingRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// 初始化设置存储库。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        public SettingRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public async Task<string?> GetSettingAsync(string category, string key)
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Category == category && s.Key == key);
            return setting?.Value;
        }

        /// <inheritdoc />
        public async Task<bool> SetSettingAsync(string category, string key, string value)
        {
            try
            {
                var existing = await _context.Settings
                    .FirstOrDefaultAsync(s => s.Category == category && s.Key == key);

                if (existing != null)
                {
                    existing.Value = value;
                    existing.LastModified = DateTime.Now;
                    existing.ModifiedBy = "System";
                }
                else
                {
                    var newSetting = new Setting
                    {
                        Category = category,
                        Key = key,
                        Value = value,
                        DataType = DataType.String,
                        Description = string.Empty,
                        LastModified = DateTime.Now,
                        ModifiedBy = "System"
                    };
                    await _context.Settings.AddAsync(newSetting);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Setting>> GetSettingsByCategoryAsync(string category)
        {
            return await _context.Settings
                .Where(s => s.Category == category)
                .OrderBy(s => s.Key)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Setting>> GetAllAsync()
        {
            return await _context.Settings
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Key)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task AddAsync(Setting setting)
        {
            await _context.Settings.AddAsync(setting);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<bool> DeleteSettingAsync(string category, string key)
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Category == category && s.Key == key);
            if (setting == null)
                return false;

            _context.Settings.Remove(setting);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc />
        public async Task DeleteAllSettingsAsync()
        {
            var allSettings = await _context.Settings.ToListAsync();
            _context.Settings.RemoveRange(allSettings);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}