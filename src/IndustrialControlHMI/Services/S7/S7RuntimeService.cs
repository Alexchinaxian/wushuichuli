using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using IndustrialControlHMI.Models;
using IndustrialControlHMI.Models.Flowchart;
using S7.Net;

namespace IndustrialControlHMI.Services.S7;

/// <summary>
/// S7-200 SMART 连接、I/Q/V/M 轮询、液位自动控制（可选）、故障入库报警。
/// </summary>
public sealed class S7RuntimeService : IS7RuntimeService, IDisposable
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly object _plcLock = new();
    private Plc? _plc;
    private System.Timers.Timer? _pollTimer;
    private List<PlcPointMapping> _mappings = new();
    private readonly ConcurrentDictionary<string, bool> _faultActive = new();
    private bool _disposed;

    public S7RuntimeService(IAlarmRepository alarmRepository)
    {
        _alarmRepository = alarmRepository;
        var defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "中信国际污水项目PLC点位.md");
        if (File.Exists(defaultPath))
            ReloadMappingsFromPointTable(defaultPath);
        else
            _mappings = PlcPointMappingProvider.LoadDefaultMappings().ToList();
    }

    public IReadOnlyList<PlcPointMapping> Mappings => _mappings;

    public bool IsConnected
    {
        get
        {
            lock (_plcLock)
                return _plc is { IsConnected: true };
        }
    }

    public bool IsPolling => _pollTimer?.Enabled == true;

    public bool AutomationEnabled { get; set; }

    public string StatusMessage { get; private set; } = "就绪";

    public event EventHandler? SnapshotUpdated;

    public void ReloadMappingsFromPointTable(string? filePath = null)
    {
        var path = filePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "中信国际污水项目PLC点位.md");
        if (!File.Exists(path))
        {
            StatusMessage = $"点位表不存在: {path}，已使用内置默认映射";
            _mappings = PlcPointMappingProvider.LoadDefaultMappings().ToList();
            return;
        }

        _mappings = PlcPointTableParser.ParseFromFile(path).ToList();
        try
        {
            var json = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "plc_mappings_generated.json");
            PlcPointTableParser.SaveMappingsToJson(_mappings, json);
        }
        catch
        {
            // 忽略导出失败
        }

        StatusMessage = $"已加载点位 {_mappings.Count} 条";
    }

    public async Task<bool> ConnectAsync(string ip, short rack = 0, short slot = 1)
    {
        await Task.Yield();
        await DisconnectAsync();

        try
        {
            var plc = new Plc(CpuType.S7200, ip, rack, slot);
            await Task.Run(() => plc.Open());
            lock (_plcLock)
            {
                _plc = plc;
            }

            StatusMessage = $"S7 已连接 {ip} (S7200)";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"连接失败: {ex.Message}";
            return false;
        }
    }

    public Task DisconnectAsync()
    {
        StopPolling();
        lock (_plcLock)
        {
            try
            {
                _plc?.Close();
            }
            catch
            {
                // ignored
            }

            _plc = null;
        }

        StatusMessage = "已断开";
        return Task.CompletedTask;
    }

    public void StartPolling(int intervalMs = 1000)
    {
        StopPolling();
        _pollTimer = new System.Timers.Timer(Math.Max(200, intervalMs));
        _pollTimer.Elapsed += OnPollElapsed;
        _pollTimer.AutoReset = true;
        _pollTimer.Start();
        StatusMessage = "轮询已启动";
    }

    public void StopPolling()
    {
        if (_pollTimer != null)
        {
            _pollTimer.Stop();
            _pollTimer.Elapsed -= OnPollElapsed;
            _pollTimer.Dispose();
            _pollTimer = null;
        }
    }

    private void OnPollElapsed(object? sender, ElapsedEventArgs e)
    {
        Plc? plc;
        lock (_plcLock)
            plc = _plc;

        if (plc is not { IsConnected: true })
            return;

        List<PlcPointMapping> snapshot;
        lock (_plcLock)
            snapshot = _mappings.ToList();

        foreach (var m in snapshot)
        {
            if (!S7AddressInterpreter.TryRead(plc, m, out var val, out _))
                continue;

            m.CurrentValue = val;
            m.LastUpdated = DateTime.Now;

            if (m.Purpose == "故障状态")
            {
                var fault = ToBool(val);
                m.IsAlarm = fault;
                Task.Run(() => HandleFaultTransitionAsync(m, fault));
            }
            else
            {
                m.IsAlarm = false;
            }
        }

        if (AutomationEnabled)
            ApplyLevelAutomation(plc);

        if (Application.Current?.Dispatcher != null)
            Application.Current.Dispatcher.Invoke(() => SnapshotUpdated?.Invoke(this, EventArgs.Empty));
        else
            SnapshotUpdated?.Invoke(this, EventArgs.Empty);
    }

    private static bool ToBool(object? val) => val switch
    {
        true => true,
        false => false,
        byte b => b != 0,
        int i => i != 0,
        long l => l != 0,
        _ => false
    };

    private async Task HandleFaultTransitionAsync(PlcPointMapping mapping, bool fault)
    {
        var name = mapping.VariableName;
        _faultActive.TryGetValue(name, out var was);

        if (fault && !was)
        {
            _faultActive[name] = true;
            try
            {
                await _alarmRepository.AddAsync(new AlarmRecord
                {
                    ParameterName = name,
                    AlarmType = "故障",
                    Threshold = 0,
                    ActualValue = 1,
                    Message = $"S7 故障位: {mapping.RegisterAddress}",
                    Status = "激活",
                    OccurrenceTime = DateTime.Now
                });
            }
            catch
            {
                // 忽略数据库错误
            }
        }
        else if (!fault && was)
            _faultActive[name] = false;
    }

    /// <summary>
    /// 示例：根据调节池液位与阈值写提升泵 Q0.0（需在 PLC 允许联锁的前提下启用 AutomationEnabled）。
    /// </summary>
    private void ApplyLevelAutomation(Plc plc)
    {
        float? level = ReadVdf(plc, "VDF100");
        float? high = ReadVdf(plc, "VDF114");
        float? low = ReadVdf(plc, "VDF118");
        if (level == null || high == null || low == null)
            return;

        bool pumpOn = level > high;
        bool pumpOff = level < low;
        if (pumpOn)
            S7AddressInterpreter.TryWriteBit(plc, 'Q', 0, 0, true, out _);
        else if (pumpOff)
            S7AddressInterpreter.TryWriteBit(plc, 'Q', 0, 0, false, out _);
    }

    private static float? ReadVdf(Plc plc, string addr)
    {
        var m = new PlcPointMapping { RegisterAddress = addr, DataType = "SINGLE" };
        return S7AddressInterpreter.TryRead(plc, m, out var v, out _) && v is float f ? f : null;
    }

    public IReadOnlyList<PlcPointMapping> GetMappingsSnapshot()
    {
        lock (_plcLock)
            return _mappings.Select(CloneMapping).ToList();
    }

    private static PlcPointMapping CloneMapping(PlcPointMapping m) => new()
    {
        Id = m.Id,
        VariableName = m.VariableName,
        RegisterAddress = m.RegisterAddress,
        DataType = m.DataType,
        UnitId = m.UnitId,
        EquipmentName = m.EquipmentName,
        Purpose = m.Purpose,
        CurrentValue = m.CurrentValue,
        LastUpdated = m.LastUpdated,
        IsAlarm = m.IsAlarm,
        AlarmHigh = m.AlarmHigh,
        AlarmLow = m.AlarmLow
    };

    public void Dispose()
    {
        if (_disposed) return;
        StopPolling();
        lock (_plcLock)
        {
            try
            {
                _plc?.Close();
            }
            catch
            {
                // ignored
            }

            _plc = null;
        }

        _disposed = true;
    }
}
