using System.Timers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlHMI.Models.Flowchart;
using IndustrialControlHMI.Services;
using IndustrialControlHMI.Services.Logging;

namespace IndustrialControlHMI.ViewModels;

/// <summary>
/// 流程图视图模型，负责管理工艺流程可视化数据和PLC数据绑定
/// </summary>
public partial class FlowchartViewModel : ObservableObject, IDisposable
{
    private readonly System.Timers.Timer _simulationTimer;
    private readonly Random _random = new Random();
    private readonly PlcDataBindingService _plcDataBindingService;
    private readonly IModbusService _modbusService;
    private readonly IAppLogger? _logger;
    
    /// <summary>
    /// 所有处理单元的集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProcessUnitModel> _processUnits = new();
    
    /// <summary>
    /// 所有连接线的集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FlowLineModel> _flowLines = new();
    
    /// <summary>
    /// PLC点位映射集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PlcPointMapping> _plcPointMappings = new();
    
    /// <summary>
    /// 当前选中的处理单元
    /// </summary>
    [ObservableProperty]
    private ProcessUnitModel? _selectedUnit;
    
    /// <summary>
    /// 当前选中的PLC点位
    /// </summary>
    [ObservableProperty]
    private PlcPointMapping? _selectedPlcPoint;
    
    /// <summary>
    /// 是否启用自动演示（单元自动切换激活状态）
    /// </summary>
    [ObservableProperty]
    private bool _isAutoDemoEnabled = true;
    
    /// <summary>
    /// 是否启用PLC数据绑定
    /// </summary>
    [ObservableProperty]
    private bool _isPlcBindingEnabled = true;
    
    /// <summary>
    /// PLC连接状态
    /// </summary>
    [ObservableProperty]
    private bool _isPlcConnected;
    
    /// <summary>
    /// PLC连接状态文本
    /// </summary>
    [ObservableProperty]
    private string _plcConnectionStatus = "未连接";
    
    /// <summary>
    /// 流程图标题
    /// </summary>
    [ObservableProperty]
    private string _title = "MBR污水处理工艺流程可视化";
    
    /// <summary>
    /// 当前时间（用于状态栏显示）
    /// </summary>
    [ObservableProperty]
    private DateTime _currentTime = DateTime.Now;
    
    /// <summary>
    /// 报警数量
    /// </summary>
    [ObservableProperty]
    private int _alarmCount;
    
    /// <summary>
    /// 数据更新时间
    /// </summary>
    [ObservableProperty]
    private DateTime _lastDataUpdateTime = DateTime.Now;
    
    /// <summary>
    /// 保存状态消息
    /// </summary>
    [ObservableProperty]
    private string _saveStatusMessage = string.Empty;
    
    /// <summary>
    /// 是否显示保存状态
    /// </summary>
    [ObservableProperty]
    private bool _isSaveStatusVisible = false;
    
    /// <summary>
    /// 点击处理单元时触发的事件
    /// </summary>
    public event EventHandler<UnitClickedEventArgs>? UnitClicked;
    
    /// <summary>
    /// PLC数据更新时触发的事件
    /// </summary>
    public event EventHandler<PlcDataUpdatedEventArgs>? PlcDataUpdated;
    
    /// <summary>
    /// 报警状态变化时触发的事件
    /// </summary>
    public event EventHandler<AlarmStatusChangedEventArgs>? AlarmStatusChanged;
    
    /// <summary>
    /// 设计时使用的无参构造函数
    /// </summary>
    public FlowchartViewModel()
    {
        // 设计时直接加载设备数据
        ProcessUnits = FlowchartDataProvider.LoadProcessUnits();
        FlowLines = FlowchartDataProvider.LoadFlowLines();
        
        // 初始化命令（设计时空实现）
        UnitClickCommand = new RelayCommand<ProcessUnitModel>(_ => { });
        SavePositionsCommand = new RelayCommand(() => { });
        
        // 设计时默认选中第一个单元
        if (ProcessUnits.Count > 0)
        {
            SelectedUnit = ProcessUnits[0];
        }
    }
    
    public FlowchartViewModel(IModbusService modbusService, IAppLogger logger)
    {
        _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // 初始化PLC数据绑定服务
        _plcDataBindingService = new PlcDataBindingService(_modbusService);
        _plcDataBindingService.DataUpdated += OnPlcDataUpdated;
        _plcDataBindingService.AlarmStatusChanged += OnAlarmStatusChanged;
        
        // 加载初始数据
        LoadFlowchartData();
        
        // 初始化PLC点位映射
        InitializePlcMappings();
        
        // 初始化定时器 - 30Hz刷新率（约33ms）
        _simulationTimer = new System.Timers.Timer(33.33); // 约30Hz
        _simulationTimer.Elapsed += OnSimulationTimerElapsed;
        _simulationTimer.Start();
        
        // 初始化命令
        UnitClickCommand = new RelayCommand<ProcessUnitModel>(OnUnitClick);
        ToggleAutoDemoCommand = new RelayCommand(ToggleAutoDemo);
        TogglePlcBindingCommand = new RelayCommand(TogglePlcBinding);
        RefreshPlcDataCommand = new RelayCommand(RefreshPlcData);
        ConnectPlcCommand = new AsyncRelayCommand(ConnectPlcAsync);
        SavePositionsCommand = new RelayCommand(SavePositions);
        
        // 初始化PLC连接状态
        UpdatePlcConnectionStatus();
    }
    
    /// <summary>
    /// 处理单元点击命令
    /// </summary>
    public ICommand UnitClickCommand { get; }
    
    /// <summary>
    /// 切换自动演示命令
    /// </summary>
    public ICommand ToggleAutoDemoCommand { get; }
    
    /// <summary>
    /// 切换PLC数据绑定命令
    /// </summary>
    public ICommand TogglePlcBindingCommand { get; }
    
    /// <summary>
    /// 刷新PLC数据命令
    /// </summary>
    public ICommand RefreshPlcDataCommand { get; }
    
    /// <summary>
    /// 连接PLC命令
    /// </summary>
    public ICommand ConnectPlcCommand { get; }
    
    /// <summary>
    /// 保存位置命令
    /// </summary>
    public ICommand SavePositionsCommand { get; }
    
    /// <summary>
    /// 加载流程图数据
    /// </summary>
    private void LoadFlowchartData()
    {
        ProcessUnits = FlowchartDataProvider.LoadProcessUnits();
        FlowLines = FlowchartDataProvider.LoadFlowLines();
        
        // 注册处理单元到PLC数据绑定服务
        _plcDataBindingService.RegisterProcessUnits(ProcessUnits);
        
        _logger?.Info($"FlowchartViewModel 已加载处理单元 {ProcessUnits.Count} 个，连接线 {FlowLines.Count} 条");
        
        // 设置初始选中单元（第一个）
        if (ProcessUnits.Count > 0)
        {
            SelectedUnit = ProcessUnits[0];
            ProcessUnits[0].IsActive = true;
        }
    }
    
    /// <summary>
    /// 初始化PLC点位映射
    /// </summary>
    private void InitializePlcMappings()
    {
        var mappings = FlowchartDataProvider.LoadPlcPointMappings();
        PlcPointMappings = new ObservableCollection<PlcPointMapping>(mappings);
        
        // 注册映射到PLC数据绑定服务
        _plcDataBindingService.InitializeMappings(mappings);
    }
    
    /// <summary>
    /// 处理单元点击事件
    /// </summary>
    private void OnUnitClick(ProcessUnitModel? unit)
    {
        if (unit == null) return;
        
        // 清除其他单元的激活状态
        foreach (var u in ProcessUnits)
        {
            u.IsActive = false;
        }
        
        // 设置当前单元为激活状态
        unit.IsActive = true;
        SelectedUnit = unit;
        
        // 触发事件，通知外部（例如主窗口）
        UnitClicked?.Invoke(this, new UnitClickedEventArgs(unit.Id, unit.Title));
        
        _logger?.Info($"流程图点击单元: {unit.Title} ({unit.Id})");
    }
    
    /// <summary>
    /// 切换自动演示状态
    /// </summary>
    private void ToggleAutoDemo()
    {
        IsAutoDemoEnabled = !IsAutoDemoEnabled;
        
        if (IsAutoDemoEnabled)
        {
            _simulationTimer.Start();
            _logger?.Info("流程图自动演示已启用");
        }
        else
        {
            _simulationTimer.Stop();
            _logger?.Info("流程图自动演示已禁用");
        }
    }
    
    /// <summary>
    /// 切换PLC数据绑定状态
    /// </summary>
    private void TogglePlcBinding()
    {
        IsPlcBindingEnabled = !IsPlcBindingEnabled;
        
        if (IsPlcBindingEnabled)
        {
            _plcDataBindingService.StartPolling();
            _logger?.Info("流程图 PLC 数据绑定已启用");
        }
        else
        {
            _plcDataBindingService.StopPolling();
            _logger?.Info("流程图 PLC 数据绑定已禁用");
        }
    }
    
    /// <summary>
    /// 刷新PLC数据
    /// </summary>
    private async void RefreshPlcData()
    {
        if (!IsPlcConnected)
        {
            _logger?.Warn("流程图 PLC 未连接，无法刷新数据");
            return;
        }
        
        try
        {
            await _plcDataBindingService.RefreshDataAsync();
            LastDataUpdateTime = DateTime.Now;
            _logger?.Info("流程图 PLC 数据已手动刷新");
        }
        catch (Exception ex)
        {
            _logger?.Error("流程图刷新 PLC 数据失败", ex);
        }
    }
    
    /// <summary>
    /// 连接PLC
    /// </summary>
    private async Task ConnectPlcAsync()
    {
        if (IsPlcConnected)
        {
            _modbusService.Disconnect();
            IsPlcConnected = false;
            PlcConnectionStatus = "已断开";
            _plcDataBindingService.StopPolling();
            return;
        }
        
        try
        {
            _logger?.Info("流程图正在连接 PLC...");
            PlcConnectionStatus = "连接中...";
            
            bool connected = await _modbusService.ConnectAsync();
            
            if (connected)
            {
                IsPlcConnected = true;
                PlcConnectionStatus = "已连接";
                
                // 启动PLC数据轮询
                if (IsPlcBindingEnabled)
                {
                    _plcDataBindingService.StartPolling();
                }
                
                _logger?.Info("流程图 PLC 连接成功");
            }
            else
            {
                IsPlcConnected = false;
                PlcConnectionStatus = "连接失败";
                _logger?.Warn("流程图 PLC 连接失败");
            }
        }
        catch (Exception ex)
        {
            IsPlcConnected = false;
            PlcConnectionStatus = $"连接错误: {ex.Message}";
            _logger?.Error("流程图 PLC 连接异常", ex);
        }
    }
    
    /// <summary>
    /// 定时器事件处理 - 模拟数据更新和自动演示
    /// </summary>
    private void OnSimulationTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // 更新当前时间（需要在UI线程上执行）
        Task.Run(() =>
        {
            CurrentTime = DateTime.Now;
        });
        
        // 如果启用了自动演示，切换激活单元
        if (IsAutoDemoEnabled && ProcessUnits.Count > 0)
        {
            Task.Run(() =>
            {
                // 随机选择一个单元激活（模拟流程演示）
                var randomIndex = _random.Next(ProcessUnits.Count);
                
                // 清除所有激活状态
                foreach (var unit in ProcessUnits)
                {
                    unit.IsActive = false;
                }
                
                // 设置随机选中的单元为激活状态
                ProcessUnits[randomIndex].IsActive = true;
                SelectedUnit = ProcessUnits[randomIndex];
                
                // 如果没有启用PLC数据绑定，模拟设备状态变化
                if (!IsPlcBindingEnabled || !IsPlcConnected)
                {
                    SimulateEquipmentStatusChanges();
                }
            });
        }
        
        // 更新报警计数
        UpdateAlarmCount();
    }
    
    /// <summary>
    /// 模拟设备状态变化（用于测试）
    /// </summary>
    private void SimulateEquipmentStatusChanges()
    {
        foreach (var unit in ProcessUnits)
        {
            foreach (var equipment in unit.Equipment)
            {
                // 随机改变设备状态（小概率）
                if (_random.NextDouble() < 0.05) // 5%概率
                {
                    var statuses = Enum.GetValues(typeof(EquipmentStatus));
                    equipment.Status = (EquipmentStatus)statuses.GetValue(_random.Next(statuses.Length))!;
                }
            }
            
            // 更新单元报警状态
            unit.UpdateAlarmStatus();
        }
        
        UpdateAlarmCount();
    }
    
    /// <summary>
    /// 更新报警计数
    /// </summary>
    private void UpdateAlarmCount()
    {
        int count = 0;
        foreach (var unit in ProcessUnits)
        {
            if (unit.IsAlarm) count++;
            foreach (var equipment in unit.Equipment)
            {
                if (equipment.IsAlarm) count++;
            }
        }
        
        foreach (var mapping in PlcPointMappings)
        {
            if (mapping.IsAlarm) count++;
        }
        
        AlarmCount = count;
    }
    
    /// <summary>
    /// PLC数据更新事件处理
    /// </summary>
    private void OnPlcDataUpdated(object sender, PlcDataUpdatedEventArgs e)
    {
        LastDataUpdateTime = DateTime.Now;
        
        // 触发外部事件
        PlcDataUpdated?.Invoke(this, e);
        
        // 更新UI相关的数据
        UpdateAlarmCount();
    }
    
    /// <summary>
    /// 报警状态变化事件处理
    /// </summary>
    private void OnAlarmStatusChanged(object sender, AlarmStatusChangedEventArgs e)
    {
        // 触发外部事件
        AlarmStatusChanged?.Invoke(this, e);
        
        // 更新报警计数
        UpdateAlarmCount();
    }
    
    /// <summary>
    /// 更新PLC连接状态
    /// </summary>
    private void UpdatePlcConnectionStatus()
    {
        IsPlcConnected = _modbusService.IsConnected;
        PlcConnectionStatus = IsPlcConnected ? "已连接" : "未连接";
    }
    
    /// <summary>
    /// 手动触发单元点击（用于外部调用）
    /// </summary>
    public void SimulateUnitClick(string unitId)
    {
        var unit = ProcessUnits.FirstOrDefault(u => u.Id == unitId);
        if (unit != null)
        {
            OnUnitClick(unit);
        }
    }
    
    /// <summary>
    /// 根据单元ID获取PLC点位映射
    /// </summary>
    public ObservableCollection<PlcPointMapping> GetPlcMappingsByUnitId(string unitId)
    {
        var mappings = PlcPointMappings.Where(m => m.UnitId == unitId).ToList();
        return new ObservableCollection<PlcPointMapping>(mappings);
    }
    
    /// <summary>
    /// 保存处理单元位置到配置文件
    /// </summary>
    private void SavePositions()
    {
        try
        {
            bool success = FlowchartDataProvider.SaveCoordinatesConfig(ProcessUnits);
            if (success)
            {
                SaveStatusMessage = $"位置已保存 ({DateTime.Now:HH:mm:ss})";
                _logger?.Info("流程图位置保存成功");
            }
            else
            {
                SaveStatusMessage = "保存失败，请检查日志";
                _logger?.Warn("流程图位置保存失败");
            }
        }
        catch (Exception ex)
        {
            SaveStatusMessage = $"保存出错: {ex.Message}";
            _logger?.Error("流程图保存位置异常", ex);
        }
        
        // 显示保存状态，3秒后自动隐藏
        IsSaveStatusVisible = true;
        Task.Delay(3000).ContinueWith(_ =>
        {
            IsSaveStatusVisible = false;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        _simulationTimer?.Stop();
        _simulationTimer?.Dispose();
        
        _plcDataBindingService?.Dispose();
    }
}

/// <summary>
/// 单元点击事件参数
/// </summary>
public class UnitClickedEventArgs : EventArgs
{
    public string UnitId { get; }
    public string UnitTitle { get; }
    
    public UnitClickedEventArgs(string unitId, string unitTitle)
    {
        UnitId = unitId;
        UnitTitle = unitTitle;
    }
}

/// <summary>
/// 异步RelayCommand实现
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool> _canExecute;
    private bool _isExecuting;
    
    public event EventHandler CanExecuteChanged;
    
    public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }
    
    public bool CanExecute(object parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke() ?? true);
    }
    
    public async void Execute(object parameter)
    {
        if (CanExecute(parameter))
        {
            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
    }
    
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}