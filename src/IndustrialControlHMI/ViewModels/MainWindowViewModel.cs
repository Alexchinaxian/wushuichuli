using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlHMI.Models;
using IndustrialControlHMI.Services;
using IndustrialControlHMI.Services.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialControlHMI.ViewModels
{
    /// <summary>
    /// 主窗口视图模型，基于SVG图像设计实现。
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject, IDisposable
    {
        private readonly System.Timers.Timer _updateTimer;
        private readonly IModbusService _modbusService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAppLogger _logger;
        private bool _isDisposed;
        
        // 导航相关属性
        [ObservableProperty]
        private ObservableCollection<NavigationItem> _navigationItems;
        
        [ObservableProperty]
        private NavigationItem _selectedNavigationItem;
        
        [ObservableProperty]
        private object _currentViewModel;
        
        // 连接状态属性
        [ObservableProperty]
        private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;
        
        [ObservableProperty]
        private Color _connectionStatusColor = Colors.Gray;
        
        [ObservableProperty]
        private string _plcIpAddress = "192.168.1.100:502";
        
        // 设备状态属性
        [ObservableProperty]
        private DeviceStatus _deviceStatus = DeviceStatus.Offline;
        
        [ObservableProperty]
        private string _deviceStatusText = "离线";
        
        [ObservableProperty]
        private Brush _deviceStatusBrush = new SolidColorBrush(Color.FromRgb(142, 142, 147)); // #8E8E93
        
        // 数据更新属性
        [ObservableProperty]
        private DateTime _lastUpdateTime = DateTime.Now;
        
        [ObservableProperty]
        private DateTime _currentDateTime = DateTime.Now;
        
        // 报警状态属性
        [ObservableProperty]
        private AlarmStatus _alarmStatus = AlarmStatus.Normal;
        
        [ObservableProperty]
        private string _alarmStatusText = "正常";
        
        [ObservableProperty]
        private Color _alarmStatusColor = Colors.Green;
        
        // 参数监控属性
        [ObservableProperty]
        private ObservableCollection<ParameterItem> _parameterItems;
        
        [ObservableProperty]
        private ObservableCollection<KeyParameter> _keyParameters;
        
        /// <summary>
        /// 流程图视图模型，用于主内容区 MBR 工艺流程可视化。
        /// </summary>
        [ObservableProperty]
        private FlowchartViewModel _flowchartViewModel;
        
        /// <summary>
        /// 初始化主窗口视图模型。
        /// </summary>
        public MainWindowViewModel(IModbusService modbusService, IServiceProvider serviceProvider, IAppLogger logger)
        {
            _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // 流程图视图模型（供主内容区 FlowchartView 绑定显示）
            FlowchartViewModel = _serviceProvider.GetRequiredService<FlowchartViewModel>();
            
            // 初始化导航项
            InitializeNavigationItems();
            
            // 初始化参数项
            InitializeParameterItems();
            
            // 初始化关键参数
            InitializeKeyParameters();
            
            // 设置定时器（每秒更新一次）
            _updateTimer = new System.Timers.Timer(1000);
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
            _updateTimer.Start();
        }
        
        /// <summary>
        /// 异步初始化视图模型。
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // 设置默认视图
                await NavigateToDashboardAsync();
                
                // 尝试连接Modbus PLC
                bool connected = await _modbusService.ConnectAsync();
                if (connected)
                {
                    ConnectionStatus = ConnectionStatus.Connected;
                    ConnectionStatusColor = StatusColors.Connected;
                    PlcIpAddress = "192.168.1.100:502";
                    DeviceStatus = DeviceStatus.Online;
                    DeviceStatusText = "在线";
                    DeviceStatusBrush = new SolidColorBrush(StatusColors.Connected);
                }
                else
                {
                    ConnectionStatus = ConnectionStatus.Disconnected;
                    ConnectionStatusColor = StatusColors.Disconnected;
                    DeviceStatus = DeviceStatus.Offline;
                    DeviceStatusText = "离线";
                    DeviceStatusBrush = new SolidColorBrush(StatusColors.Offline);
                }
                
                // 启动定时器（如果未启动）
                if (!_updateTimer.Enabled)
                    _updateTimer.Start();
            }
            catch (Exception ex)
            {
                _logger.Error("主窗口视图模型初始化失败", ex);
                // 即使连接失败也继续运行（使用模拟数据）
                ConnectionStatus = ConnectionStatus.Disconnected;
                ConnectionStatusColor = StatusColors.Disconnected;
                DeviceStatus = DeviceStatus.Offline;
                DeviceStatusText = "离线";
                DeviceStatusBrush = new SolidColorBrush(StatusColors.Offline);
            }
        }
        
        /// <summary>
        /// 初始化导航菜单项。
        /// </summary>
        private void InitializeNavigationItems()
        {
            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem 
                { 
                    Id = "dashboard",
                    Icon = "",
                    Label = "仪表板",
                    Description = "工艺流程与关键参数",
                    ViewModelType = null // 暂留
                },
                new NavigationItem 
                { 
                    Id = "alarms",
                    Icon = "",
                    Label = "报警管理",
                    Description = "报警记录和规则设置",
                    ViewModelType = null
                },
                new NavigationItem 
                { 
                    Id = "history",
                    Icon = "",
                    Label = "历史数据",
                    Description = "历史记录查询和导出",
                    ViewModelType = null
                },
                new NavigationItem 
                { 
                    Id = "settings",
                    Icon = "",
                    Label = "参数设置",
                    Description = "系统参数配置",
                    ViewModelType = null
                }
            };
            
            SelectedNavigationItem = NavigationItems[0];
        }
        
        /// <summary>
        /// 初始化参数监控项。
        /// </summary>
        private void InitializeParameterItems()
        {
            ParameterItems = new ObservableCollection<ParameterItem>
            {
                new ParameterItem 
                { 
                    Name = "温度",
                    Value = 0.0,
                    Unit = "°C",
                    Timestamp = DateTime.Now,
                    Status = "正常",
                    StatusColor = "#4CD964",
                    MinValue = 0,
                    MaxValue = 100,
                    WarningThreshold = 80,
                    AlarmThreshold = 90
                },
                new ParameterItem 
                { 
                    Name = "压力",
                    Value = 0.0,
                    Unit = "MPa",
                    Timestamp = DateTime.Now,
                    Status = "正常",
                    StatusColor = "#4CD964",
                    MinValue = 0,
                    MaxValue = 10,
                    WarningThreshold = 8,
                    AlarmThreshold = 9
                },
                new ParameterItem 
                { 
                    Name = "流量",
                    Value = 0.0,
                    Unit = "L/min",
                    Timestamp = DateTime.Now,
                    Status = "正常",
                    StatusColor = "#4CD964",
                    MinValue = 0,
                    MaxValue = 1000,
                    WarningThreshold = 800,
                    AlarmThreshold = 900
                },
                new ParameterItem 
                { 
                    Name = "状态",
                    Value = 0,
                    Unit = "",
                    Timestamp = DateTime.Now,
                    Status = "离线",
                    StatusColor = "#8E8E93"
                }
            };
        }
        
        /// <summary>
        /// 初始化关键参数。
        /// </summary>
        private void InitializeKeyParameters()
        {
            KeyParameters = new ObservableCollection<KeyParameter>
            {
                new KeyParameter { Name = "当前温度", Value = 0.0, Unit = "°C" },
                new KeyParameter { Name = "当前压力", Value = 0.0, Unit = "MPa" },
                new KeyParameter { Name = "当前流量", Value = 0.0, Unit = "L/min" },
                new KeyParameter { Name = "设备状态", Value = 0, Unit = "" }
            };
        }
        
        /// <summary>
        /// 导航到仪表板命令。
        /// </summary>
        [RelayCommand]
        private async Task NavigateToDashboardAsync()
        {
            try
            {
                // 这里实现导航逻辑，暂时设置当前视图模型为null
                CurrentViewModel = null;
                await Task.Delay(10);
            }
            catch (Exception ex)
            {
                _logger.Error("导航到仪表板失败", ex);
            }
        }
        
        /// <summary>
        /// 导航到报警管理命令。
        /// </summary>
        [RelayCommand]
        private async Task NavigateToAlarmsAsync()
        {
            try
            {
                var alarmViewModel = _serviceProvider.GetRequiredService<AlarmManagementViewModel>();
                await alarmViewModel.InitializeAsync();
                CurrentViewModel = alarmViewModel;
            }
            catch (Exception ex)
            {
                _logger.Error("导航到报警管理失败", ex);
                CurrentViewModel = null;
            }
        }
        
        /// <summary>
        /// 导航到历史数据命令。
        /// </summary>
        [RelayCommand]
        private async Task NavigateToHistoryAsync()
        {
            try
            {
                var historyViewModel = _serviceProvider.GetRequiredService<HistoryDataViewModel>();
                await historyViewModel.InitializeAsync();
                CurrentViewModel = historyViewModel;
            }
            catch (Exception ex)
            {
                _logger.Error("导航到历史数据失败", ex);
                CurrentViewModel = null;
            }
        }
        
        /// <summary>
        /// 导航到参数设置命令。
        /// </summary>
        [RelayCommand]
        private async Task NavigateToSettingsAsync()
        {
            try
            {
                var settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
                // 进入参数设置时默认展示「通信设置」/ PLC（Modbus）连接配置
                var comm = settingsViewModel.Categories.FirstOrDefault(c => c.Id == "communication");
                if (comm != null)
                    settingsViewModel.SelectedCategory = comm;
                CurrentViewModel = settingsViewModel;
            }
            catch (Exception ex)
            {
                _logger.Error("导航到参数设置失败", ex);
                CurrentViewModel = null;
            }
        }

        /// <summary>
        /// 导航到 S7 监控（中信污水 PLC 点位轮询与报警）。
        /// </summary>
        [RelayCommand]
        private async Task NavigateToS7MonitorAsync()
        {
            try
            {
                var vm = _serviceProvider.GetRequiredService<S7MonitorViewModel>();
                CurrentViewModel = vm;
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error("打开 S7 监控失败", ex);
                CurrentViewModel = null;
            }
        }
        
        /// <summary>
        /// 定时器触发更新。
        /// </summary>
        private void OnUpdateTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // 在主线程更新UI属性
            App.Current.Dispatcher.Invoke(() =>
            {
                // 更新时间
                LastUpdateTime = DateTime.Now;
                CurrentDateTime = DateTime.Now;
                
                // 模拟数据更新
                UpdateSimulatedData();
            });
        }
        
        /// <summary>
        /// 更新模拟数据。
        /// </summary>
        private void UpdateSimulatedData()
        {
            var random = new Random();
            double newTemp = 20 + random.NextDouble() * 15;
            double newPressure = 5 + random.NextDouble() * 3;
            double newFlow = 500 + random.NextDouble() * 200;
            
            // 更新参数项
            if (ParameterItems.Count > 0)
            {
                var tempItem = ParameterItems[0];
                tempItem.Value = newTemp;
                tempItem.Timestamp = DateTime.Now;
                tempItem.Status = newTemp > 90 ? "报警" : newTemp > 80 ? "警告" : "正常";
                tempItem.StatusColor = newTemp > 90 ? "#FF3B30" : newTemp > 80 ? "#FF9500" : "#4CD964";
                
                var pressureItem = ParameterItems[1];
                pressureItem.Value = newPressure;
                pressureItem.Timestamp = DateTime.Now;
                pressureItem.Status = newPressure > 9 ? "报警" : newPressure > 8 ? "警告" : "正常";
                pressureItem.StatusColor = newPressure > 9 ? "#FF3B30" : newPressure > 8 ? "#FF9500" : "#4CD964";
                
                var flowItem = ParameterItems[2];
                flowItem.Value = newFlow;
                flowItem.Timestamp = DateTime.Now;
                flowItem.Status = newFlow > 900 ? "报警" : newFlow > 800 ? "警告" : "正常";
                flowItem.StatusColor = newFlow > 900 ? "#FF3B30" : newFlow > 800 ? "#FF9500" : "#4CD964";
                
                // 触发属性更改
                OnPropertyChanged(nameof(ParameterItems));
            }
            
            // 更新关键参数
            if (KeyParameters.Count >= 3)
            {
                KeyParameters[0].Value = newTemp;
                KeyParameters[1].Value = newPressure;
                KeyParameters[2].Value = newFlow;
                OnPropertyChanged(nameof(KeyParameters));
            }
        }
        
        /// <summary>
        /// 更新连接状态。
        /// </summary>
        private void UpdateConnectionStatus()
        {
            // 模拟连接状态切换
            if (ConnectionStatus == ConnectionStatus.Disconnected)
            {
                ConnectionStatus = ConnectionStatus.Connected;
                ConnectionStatusColor = StatusColors.Connected;
                DeviceStatus = DeviceStatus.Online;
                DeviceStatusText = "在线";
                DeviceStatusBrush = new SolidColorBrush(StatusColors.Connected);
            }
            else
            {
                ConnectionStatus = ConnectionStatus.Disconnected;
                ConnectionStatusColor = StatusColors.Disconnected;
                DeviceStatus = DeviceStatus.Offline;
                DeviceStatusText = "离线";
                DeviceStatusBrush = new SolidColorBrush(StatusColors.Offline);
            }
        }
        
        /// <summary>
        /// 启动数据更新。
        /// </summary>
        public void StartDataUpdates()
        {
            _updateTimer.Start();
        }
        
        /// <summary>
        /// 停止数据更新。
        /// </summary>
        public void StopDataUpdates()
        {
            _updateTimer.Stop();
        }
        
        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            FlowchartViewModel?.Dispose();
            
            _isDisposed = true;
        }
    }
}