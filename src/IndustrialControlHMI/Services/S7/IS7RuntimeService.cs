using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IndustrialControlHMI.Models.Flowchart;

namespace IndustrialControlHMI.Services.S7;

/// <summary>
/// 中信污水项目 S7-200 SMART 运行时：连接、轮询、自动控制与故障报警。
/// </summary>
public interface IS7RuntimeService
{
    IReadOnlyList<PlcPointMapping> Mappings { get; }

    bool IsConnected { get; }

    bool IsPolling { get; }

    /// <summary>
    /// 是否允许上位机根据液位写 Q 区（默认 false，避免与 PLC 程序冲突）。
    /// </summary>
    bool AutomationEnabled { get; set; }

    string StatusMessage { get; }

    event EventHandler? SnapshotUpdated;

    void ReloadMappingsFromPointTable(string? filePath = null);

    Task<bool> ConnectAsync(string ip, short rack = 0, short slot = 1);

    Task DisconnectAsync();

    void StartPolling(int intervalMs = 1000);

    void StopPolling();

    IReadOnlyList<PlcPointMapping> GetMappingsSnapshot();
}
