using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlHMI.Models.Flowchart;
using IndustrialControlHMI.Services.S7;

namespace IndustrialControlHMI.ViewModels;

/// <summary>
/// S7 点位监控：连接 SMART、轮询、可选液位自动控制。
/// </summary>
public partial class S7MonitorViewModel : ObservableObject, IDisposable
{
    private readonly IS7RuntimeService _s7;

    [ObservableProperty]
    private string _plcIp = "192.168.0.1";

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _automationEnabled;

    [ObservableProperty]
    private ObservableCollection<PlcPointMapping> _faultPoints = new();

    [ObservableProperty]
    private ObservableCollection<PlcPointMapping> _valuePoints = new();

    [ObservableProperty]
    private ObservableCollection<PlcPointMapping> _controlPoints = new();

    /// <summary>
    /// 按变量名筛选（不区分大小写）。
    /// </summary>
    [ObservableProperty]
    private string _filterText = string.Empty;

    /// <summary>
    /// 各分区筛选后数量 / 全量数量。
    /// </summary>
    [ObservableProperty]
    private string _pointCountLabel = "故障 0/0 · 数值 0/0 · 控制 0/0";

    private List<PlcPointMapping> _allPoints = new();

    public S7MonitorViewModel(IS7RuntimeService s7Runtime)
    {
        _s7 = s7Runtime ?? throw new ArgumentNullException(nameof(s7Runtime));
        _s7.SnapshotUpdated += OnSnapshotUpdated;
        AutomationEnabled = _s7.AutomationEnabled;
        RefreshFromService();
        StatusText = _s7.StatusMessage;
    }

    private void OnSnapshotUpdated(object? sender, EventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(RefreshFromService);
    }

    private void RefreshFromService()
    {
        StatusText = _s7.StatusMessage;
        _allPoints = _s7.GetMappingsSnapshot().ToList();
        ApplyFilter();
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<PlcPointMapping> q = _allPoints;
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var key = FilterText.Trim();
            q = q.Where(m =>
                (m.VariableName?.Contains(key, StringComparison.OrdinalIgnoreCase) == true) ||
                (m.RegisterAddress?.Contains(key, StringComparison.OrdinalIgnoreCase) == true) ||
                (m.Purpose?.Contains(key, StringComparison.OrdinalIgnoreCase) == true));
        }

        var list = q.ToList();
        var faults = list.Where(IsFaultPoint).ToList();
        var controls = list.Where(IsControlPoint).ToList();
        var values = list.Where(m => !IsFaultPoint(m) && !IsControlPoint(m)).ToList();

        FaultPoints = new ObservableCollection<PlcPointMapping>(faults);
        ValuePoints = new ObservableCollection<PlcPointMapping>(values);
        ControlPoints = new ObservableCollection<PlcPointMapping>(controls);

        int tf = _allPoints.Count(IsFaultPoint);
        int tc = _allPoints.Count(IsControlPoint);
        int tv = _allPoints.Count - tf - tc;
        PointCountLabel = $"故障 {faults.Count}/{tf} · 数值 {values.Count}/{tv} · 控制 {controls.Count}/{tc}";
    }

    private static bool IsFaultPoint(PlcPointMapping m) =>
        string.Equals(m.Purpose, "故障状态", StringComparison.Ordinal);

    private static bool IsControlPoint(PlcPointMapping m) =>
        string.Equals(m.Purpose, "控制输出", StringComparison.Ordinal);

    partial void OnAutomationEnabledChanged(bool value)
    {
        _s7.AutomationEnabled = value;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        StatusText = "正在连接...";
        var ok = await _s7.ConnectAsync(PlcIp.Trim());
        StatusText = _s7.StatusMessage;
        if (ok)
            _s7.StartPolling(1000);
        RefreshFromService();
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _s7.DisconnectAsync();
        StatusText = _s7.StatusMessage;
        RefreshFromService();
    }

    [RelayCommand]
    private void ReloadPointTable()
    {
        _s7.ReloadMappingsFromPointTable();
        StatusText = _s7.StatusMessage;
        RefreshFromService();
    }

    public void Dispose()
    {
        _s7.SnapshotUpdated -= OnSnapshotUpdated;
    }
}
