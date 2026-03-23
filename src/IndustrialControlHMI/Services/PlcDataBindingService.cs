using System.Timers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndustrialControlHMI.Models.Flowchart;
using IndustrialControlHMI.Services.Logging;

namespace IndustrialControlHMI.Services;

/// <summary>
/// PLC数据绑定服务，负责将PLC数据同步到流程图模型
/// </summary>
public class PlcDataBindingService : IDisposable
{
    private readonly IModbusService _modbusService;
    private readonly ConcurrentDictionary<string, PlcPointMapping> _pointMappings;
    private readonly ConcurrentDictionary<string, ProcessUnitModel> _processUnits;
    private System.Timers.Timer _pollingTimer;
    private bool _isPolling;
    private readonly int _pollingInterval = 2000; // 2秒
    
    /// <summary>
    /// 数据更新事件
    /// </summary>
    public event EventHandler<PlcDataUpdatedEventArgs> DataUpdated;
    
    /// <summary>
    /// 报警状态变化事件
    /// </summary>
    public event EventHandler<AlarmStatusChangedEventArgs> AlarmStatusChanged;
    
    public PlcDataBindingService(IModbusService modbusService)
    {
        _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));
        _pointMappings = new ConcurrentDictionary<string, PlcPointMapping>();
        _processUnits = new ConcurrentDictionary<string, ProcessUnitModel>();
        
        // 订阅Modbus服务事件
        _modbusService.DataReceived += OnModbusDataReceived;
        _modbusService.ConnectionStatusChanged += OnModbusConnectionStatusChanged;
        _modbusService.ErrorOccurred += OnModbusErrorOccurred;
    }
    
    /// <summary>
    /// 初始化PLC点位映射
    /// </summary>
    public void InitializeMappings(IEnumerable<PlcPointMapping> mappings)
    {
        _pointMappings.Clear();
        foreach (var mapping in mappings)
        {
            _pointMappings[mapping.Id] = mapping;
        }
    }
    
    /// <summary>
    /// 注册处理单元
    /// </summary>
    public void RegisterProcessUnits(IEnumerable<ProcessUnitModel> units)
    {
        _processUnits.Clear();
        foreach (var unit in units)
        {
            _processUnits[unit.Id] = unit;
            
            // 关联PLC点位
            var unitMappings = FlowchartDataProvider.GetMappingsByUnitId(unit.Id);
            foreach (var mapping in unitMappings)
            {
                unit.PlcPointIds.Add(mapping.Id);
            }
        }
    }
    
    /// <summary>
    /// 启动数据轮询
    /// </summary>
    public void StartPolling()
    {
        if (_isPolling) return;
        
        _isPolling = true;
        _pollingTimer = new System.Timers.Timer(_pollingInterval);
        _pollingTimer.Elapsed += async (sender, e) => await PollData();
        _pollingTimer.AutoReset = true;
        _pollingTimer.Enabled = true;
    }
    
    /// <summary>
    /// 停止数据轮询
    /// </summary>
    public void StopPolling()
    {
        _isPolling = false;
        _pollingTimer?.Dispose();
        _pollingTimer = null;
    }
    
    /// <summary>
    /// 手动触发数据读取
    /// </summary>
    public async Task RefreshDataAsync()
    {
        await PollDataAsync();
    }
    
    private async Task PollData()
    {
        await PollDataAsync();
    }
    
    private async Task PollDataAsync()
    {
        if (!_modbusService.IsConnected || !_isPolling)
            return;
        
        try
        {
            // 分组读取不同类型的点位以提高效率
            await ReadFaultStatusPoints();
            await ReadLevelDataPoints();
            await ReadControlOutputPoints();
            await ReadTimerParameterPoints();
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"PLC数据轮询错误: {ex.Message}", ex);
        }
    }
    
    private async Task ReadFaultStatusPoints()
    {
        var faultMappings = _pointMappings.Values
            .Where(m => m.Purpose == "故障状态")
            .ToList();
        
        foreach (var mapping in faultMappings)
        {
            try
            {
                // 解析寄存器地址
                if (TryParseRegisterAddress(mapping.RegisterAddress, out ushort address))
                {
                    // 读取故障状态（INTEGER类型）
                    short value = await _modbusService.ReadShortAsync(address);
                    
                    // 更新映射
                    mapping.CurrentValue = value;
                    mapping.LastUpdated = DateTime.Now;
                    mapping.IsAlarm = value == 1;
                    
                    // 更新对应的设备状态
                    UpdateEquipmentFromFaultMapping(mapping, value);
                    
                    // 触发数据更新事件
                    OnDataUpdated(mapping, value);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"读取故障状态点位失败 {mapping.VariableName}: {ex.Message}", ex);
            }
        }
    }
    
    private async Task ReadLevelDataPoints()
    {
        var levelMappings = _pointMappings.Values
            .Where(m => m.Purpose == "液位数据" || m.Purpose == "流量数据")
            .ToList();
        
        foreach (var mapping in levelMappings)
        {
            try
            {
                if (TryParseRegisterAddress(mapping.RegisterAddress, out ushort address))
                {
                    // 读取浮点数值
                    float value = await _modbusService.ReadFloatAsync(address);
                    
                    // 更新映射
                    mapping.CurrentValue = value;
                    mapping.LastUpdated = DateTime.Now;
                    
                    // 检查报警阈值
                    if (mapping.AlarmHigh.HasValue && value > mapping.AlarmHigh.Value)
                    {
                        mapping.IsAlarm = true;
                        OnAlarmStatusChanged(mapping, $"值{value:F1}超过高限{mapping.AlarmHigh.Value:F1}");
                    }
                    else if (mapping.AlarmLow.HasValue && value < mapping.AlarmLow.Value)
                    {
                        mapping.IsAlarm = true;
                        OnAlarmStatusChanged(mapping, $"值{value:F1}低于低限{mapping.AlarmLow.Value:F1}");
                    }
                    else
                    {
                        mapping.IsAlarm = false;
                    }
                    
                    // 更新对应的单元关键参数
                    UpdateUnitFromLevelMapping(mapping, value);
                    
                    // 触发数据更新事件
                    OnDataUpdated(mapping, value);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"读取液位数据点位失败 {mapping.VariableName}: {ex.Message}", ex);
            }
        }
    }
    
    private async Task ReadControlOutputPoints()
    {
        var controlMappings = _pointMappings.Values
            .Where(m => m.Purpose == "控制输出")
            .ToList();
        
        foreach (var mapping in controlMappings)
        {
            try
            {
                if (TryParseRegisterAddress(mapping.RegisterAddress, out ushort address))
                {
                    short value = await _modbusService.ReadShortAsync(address);
                    
                    mapping.CurrentValue = value;
                    mapping.LastUpdated = DateTime.Now;
                    
                    // 更新对应的设备状态
                    UpdateEquipmentFromControlMapping(mapping, value);
                    
                    OnDataUpdated(mapping, value);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"读取控制输出点位失败 {mapping.VariableName}: {ex.Message}", ex);
            }
        }
    }
    
    private async Task ReadTimerParameterPoints()
    {
        var timerMappings = _pointMappings.Values
            .Where(m => m.Purpose == "定时参数")
            .ToList();
        
        foreach (var mapping in timerMappings)
        {
            try
            {
                if (TryParseRegisterAddress(mapping.RegisterAddress, out ushort address))
                {
                    float value = await _modbusService.ReadFloatAsync(address);
                    
                    mapping.CurrentValue = value;
                    mapping.LastUpdated = DateTime.Now;
                    
                    OnDataUpdated(mapping, value);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"读取定时参数点位失败 {mapping.VariableName}: {ex.Message}", ex);
            }
        }
    }
    
    private void UpdateEquipmentFromFaultMapping(PlcPointMapping mapping, short faultValue)
    {
        if (!_processUnits.TryGetValue(mapping.UnitId, out var unit))
            return;
        
        // 查找对应的设备
        var equipment = unit.Equipment.FirstOrDefault(e => e.Name == mapping.EquipmentName);
        if (equipment != null)
        {
            equipment.UpdateFromPlcValue(faultValue, isFault: true);
            unit.UpdateAlarmStatus();
        }
    }
    
    private void UpdateEquipmentFromControlMapping(PlcPointMapping mapping, short controlValue)
    {
        if (!_processUnits.TryGetValue(mapping.UnitId, out var unit))
            return;
        
        var equipment = unit.Equipment.FirstOrDefault(e => e.Name == mapping.EquipmentName);
        if (equipment != null)
        {
            equipment.UpdateFromPlcValue(controlValue, isFault: false);
            unit.UpdateAlarmStatus();
        }
    }
    
    private void UpdateUnitFromLevelMapping(PlcPointMapping mapping, float value)
    {
        if (!_processUnits.TryGetValue(mapping.UnitId, out var unit))
            return;
        
        // 根据变量名更新关键参数
        string displayValue;
        if (mapping.VariableName.Contains("液位"))
        {
            displayValue = $"{value:F1}%";
            unit.AddKeyParameter("液位", displayValue);
            
            // 更新液位相关设备
            var levelEquipment = unit.Equipment.FirstOrDefault(e => e.Name.Contains("液位"));
            if (levelEquipment != null)
            {
                levelEquipment.NumericValue = value;
                levelEquipment.ValueUnit = "%";
                levelEquipment.LastUpdated = DateTime.Now;
            }
        }
        else if (mapping.VariableName.Contains("流量"))
        {
            displayValue = $"{value:F1} m³/h";
            unit.AddKeyParameter("流量", displayValue);
        }
        
        unit.LastUpdated = DateTime.Now;
    }
    
    private bool TryParseRegisterAddress(string registerAddress, out ushort address)
    {
        address = 0;
        
        if (string.IsNullOrEmpty(registerAddress))
            return false;
        
        // 处理不同类型的寄存器地址
        // 例如: "I000.0" -> 地址0, "VDF100" -> 地址100, "VWUB130" -> 地址130
        try
        {
            // 提取数字部分
            string numericPart = new string(registerAddress.Where(char.IsDigit).ToArray());
            if (ushort.TryParse(numericPart, out address))
            {
                return true;
            }
            
            // 对于VDFxxx格式，xxx是十进制地址
            if (registerAddress.StartsWith("VDF") || registerAddress.StartsWith("VWUB") || registerAddress.StartsWith("VBUB"))
            {
                string numberStr = registerAddress.Substring(3);
                if (ushort.TryParse(numberStr, out address))
                {
                    return true;
                }
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    private void OnModbusDataReceived(object sender, DataReceivedEventArgs e)
    {
        // 处理Modbus服务主动推送的数据
        // 可以根据需要实现实时数据更新
    }
    
    private void OnModbusConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
    {
        if (e.IsConnected)
        {
            StartPolling();
        }
        else
        {
            StopPolling();
        }
    }
    
    private void OnModbusErrorOccurred(object sender, ErrorOccurredEventArgs e)
    {
        OnErrorOccurred($"Modbus错误: {e.ErrorMessage}", e.Exception);
    }
    
    protected virtual void OnDataUpdated(PlcPointMapping mapping, object value)
    {
        DataUpdated?.Invoke(this, new PlcDataUpdatedEventArgs(mapping, value));
    }
    
    protected virtual void OnAlarmStatusChanged(PlcPointMapping mapping, string message)
    {
        AlarmStatusChanged?.Invoke(this, new AlarmStatusChangedEventArgs(mapping, message));
    }
    
    protected virtual void OnErrorOccurred(string errorMessage, Exception exception = null)
    {
        AppRuntimeLogger.Error($"PLC数据绑定服务错误: {errorMessage}", exception);
    }
    
    public void Dispose()
    {
        StopPolling();
        
        if (_modbusService != null)
        {
            _modbusService.DataReceived -= OnModbusDataReceived;
            _modbusService.ConnectionStatusChanged -= OnModbusConnectionStatusChanged;
            _modbusService.ErrorOccurred -= OnModbusErrorOccurred;
        }
    }
}

/// <summary>
/// PLC数据更新事件参数
/// </summary>
public class PlcDataUpdatedEventArgs : EventArgs
{
    public PlcPointMapping Mapping { get; }
    public object Value { get; }
    public DateTime Timestamp { get; }
    
    public PlcDataUpdatedEventArgs(PlcPointMapping mapping, object value)
    {
        Mapping = mapping;
        Value = value;
        Timestamp = DateTime.Now;
    }
}

/// <summary>
/// 报警状态变化事件参数
/// </summary>
public class AlarmStatusChangedEventArgs : EventArgs
{
    public PlcPointMapping Mapping { get; }
    public string Message { get; }
    public DateTime Timestamp { get; }
    
    public AlarmStatusChangedEventArgs(PlcPointMapping mapping, string message)
    {
        Mapping = mapping;
        Message = message;
        Timestamp = DateTime.Now;
    }
}