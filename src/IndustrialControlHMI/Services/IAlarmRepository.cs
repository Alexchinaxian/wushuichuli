using System.Collections.Generic;
using System.Threading.Tasks;
using IndustrialControlHMI.Models;

namespace IndustrialControlHMI.Services;

/// <summary>
/// 报警记录存储库接口，定义了对报警记录的CRUD操作。
/// </summary>
public interface IAlarmRepository
{
    /// <summary>
    /// 获取所有报警记录。
    /// </summary>
    /// <returns>报警记录列表。</returns>
    Task<IEnumerable<AlarmRecord>> GetAllAsync();

    /// <summary>
    /// 根据ID获取单个报警记录。
    /// </summary>
    /// <param name="id">报警记录ID。</param>
    /// <returns>报警记录，如果不存在则返回null。</returns>
    Task<AlarmRecord?> GetByIdAsync(int id);

    /// <summary>
    /// 获取未确认的报警记录。
    /// </summary>
    /// <returns>未确认的报警记录列表。</returns>
    Task<IEnumerable<AlarmRecord>> GetUnacknowledgedAsync();

    /// <summary>
    /// 获取指定状态的报警记录。
    /// </summary>
    /// <param name="status">报警状态（如Active、Acknowledged、Resolved）。</param>
    /// <returns>匹配状态的报警记录列表。</returns>
    Task<IEnumerable<AlarmRecord>> GetByStatusAsync(string status);

    /// <summary>
    /// 添加新的报警记录。
    /// </summary>
    /// <param name="record">要添加的报警记录。</param>
    /// <returns>添加后的记录（包含生成的ID）。</returns>
    Task<AlarmRecord> AddAsync(AlarmRecord record);

    /// <summary>
    /// 更新现有报警记录。
    /// </summary>
    /// <param name="record">要更新的报警记录。</param>
    /// <returns>更新后的记录。</returns>
    Task<AlarmRecord> UpdateAsync(AlarmRecord record);

    /// <summary>
    /// 删除指定ID的报警记录。
    /// </summary>
    /// <param name="id">要删除的报警记录ID。</param>
    /// <returns>是否成功删除。</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// 确认报警（将状态改为Acknowledged）。
    /// </summary>
    /// <param name="id">要确认的报警记录ID。</param>
    /// <returns>更新后的记录。</returns>
    Task<AlarmRecord?> AcknowledgeAsync(int id);

    /// <summary>
    /// 解决报警（将状态改为Resolved）。
    /// </summary>
    /// <param name="id">要解决的报警记录ID。</param>
    /// <returns>更新后的记录。</returns>
    Task<AlarmRecord?> ResolveAsync(int id);

    /// <summary>
    /// 获取最近N条报警记录。
    /// </summary>
    /// <param name="count">要获取的记录数量。</param>
    /// <returns>最近的报警记录列表。</returns>
    Task<IEnumerable<AlarmRecord>> GetRecentAsync(int count = 50);

    /// <summary>
    /// 检查是否存在未解决的报警。
    /// </summary>
    /// <returns>是否存在未解决的报警。</returns>
    Task<bool> HasActiveAlarmsAsync();
}