using System.Text.Json;
using System.IO;
using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlHMI.Services.Communication;

namespace IndustrialControlHMI.ViewModels
{
    /// <summary>
    /// 通讯配置视图模型
    /// </summary>
    public partial class CommunicationViewModel : ObservableObject
    {
        private readonly ICommunicationManager _communicationManager;
        private readonly IProtocolFactory _protocolFactory;
        private readonly IProtocolConfigurationService _configurationService;
        private readonly IDataConverter _dataConverter;
        
        [ObservableProperty]
        private ObservableCollection<ProtocolConfiguration> _protocolConfigurations = new();
        
        [ObservableProperty]
        private ProtocolConfiguration? _selectedConfiguration;
        
        [ObservableProperty]
        private ObservableCollection<string> _availablePorts = new();
        
        [ObservableProperty]
        private ObservableCollection<ProtocolType> _protocolTypes = new();
        
        [ObservableProperty]
        private ObservableCollection<InterfaceType> _interfaceTypes = new();
        
        [ObservableProperty]
        private string _statusMessage = "就绪";
        
        [ObservableProperty]
        private bool _isConnected;
        
        [ObservableProperty]
        private string _sentData = string.Empty;
        
        [ObservableProperty]
        private string _receivedData = string.Empty;
        
        [ObservableProperty]
        private string _dataToSend = string.Empty;
        
        [ObservableProperty]
        private DataFormat _selectedDataFormat = DataFormat.Hex;
        
        [ObservableProperty]
        private bool _isSending;
        
        public CommunicationViewModel(
            ICommunicationManager communicationManager,
            IProtocolFactory protocolFactory,
            IProtocolConfigurationService configurationService,
            IDataConverter dataConverter)
        {
            _communicationManager = communicationManager ?? throw new ArgumentNullException(nameof(communicationManager));
            _protocolFactory = protocolFactory ?? throw new ArgumentNullException(nameof(protocolFactory));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _dataConverter = dataConverter ?? throw new ArgumentNullException(nameof(dataConverter));
            
            // 初始化命令
            LoadConfigurationsCommand = new AsyncRelayCommand(LoadConfigurationsAsync);
            SaveConfigurationCommand = new AsyncRelayCommand(SaveConfigurationAsync);
            DeleteConfigurationCommand = new AsyncRelayCommand(DeleteConfigurationAsync);
            ConnectCommand = new AsyncRelayCommand(ConnectAsync);
            DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
            SendDataCommand = new AsyncRelayCommand(SendDataAsync);
            RefreshPortsCommand = new AsyncRelayCommand(RefreshPortsAsync);
            ClearDataCommand = new RelayCommand(ClearData);
            
            // 初始化协议类型和接口类型
            InitializeEnums();
            
            // 订阅事件
            _communicationManager.CommunicationStatusChanged += OnCommunicationStatusChanged;
            _communicationManager.DataReceived += OnDataReceived;
            
            // 加载配置
            _ = LoadConfigurationsAsync();
            _ = RefreshPortsAsync();
        }
        
        /// <summary>
        /// 加载配置命令
        /// </summary>
        public ICommand LoadConfigurationsCommand { get; }
        
        /// <summary>
        /// 保存配置命令
        /// </summary>
        public ICommand SaveConfigurationCommand { get; }
        
        /// <summary>
        /// 删除配置命令
        /// </summary>
        public ICommand DeleteConfigurationCommand { get; }
        
        /// <summary>
        /// 连接命令
        /// </summary>
        public ICommand ConnectCommand { get; }
        
        /// <summary>
        /// 断开连接命令
        /// </summary>
        public ICommand DisconnectCommand { get; }
        
        /// <summary>
        /// 发送数据命令
        /// </summary>
        public ICommand SendDataCommand { get; }
        
        /// <summary>
        /// 刷新串口命令
        /// </summary>
        public ICommand RefreshPortsCommand { get; }
        
        /// <summary>
        /// 清空数据命令
        /// </summary>
        public ICommand ClearDataCommand { get; }
        
        /// <summary>
        /// 初始化枚举值
        /// </summary>
        private void InitializeEnums()
        {
            ProtocolTypes.Clear();
            foreach (ProtocolType type in Enum.GetValues(typeof(ProtocolType)))
            {
                ProtocolTypes.Add(type);
            }
            
            InterfaceTypes.Clear();
            foreach (InterfaceType type in Enum.GetValues(typeof(InterfaceType)))
            {
                InterfaceTypes.Add(type);
            }
        }
        
        /// <summary>
        /// 加载配置
        /// </summary>
        private async Task LoadConfigurationsAsync()
        {
            try
            {
                StatusMessage = "正在加载配置...";
                
                var configurations = await _configurationService.LoadAllConfigurationsAsync();
                ProtocolConfigurations.Clear();
                
                foreach (var config in configurations)
                {
                    ProtocolConfigurations.Add(config);
                }
                
                StatusMessage = $"已加载 {ProtocolConfigurations.Count} 个配置";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载配置失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 保存配置
        /// </summary>
        private async Task SaveConfigurationAsync()
        {
            if (SelectedConfiguration == null)
            {
                StatusMessage = "请选择或创建配置";
                return;
            }
            
            try
            {
                StatusMessage = "正在保存配置...";
                
                if (!_configurationService.ValidateConfiguration(SelectedConfiguration))
                {
                    StatusMessage = "配置无效，请检查参数";
                    return;
                }
                
                await _configurationService.SaveConfigurationAsync(SelectedConfiguration);
                
                // 重新加载配置
                await LoadConfigurationsAsync();
                
                StatusMessage = "配置保存成功";
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存配置失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 删除配置
        /// </summary>
        private async Task DeleteConfigurationAsync()
        {
            if (SelectedConfiguration == null)
            {
                StatusMessage = "请选择要删除的配置";
                return;
            }
            
            try
            {
                StatusMessage = "正在删除配置...";
                
                await _configurationService.DeleteConfigurationAsync(SelectedConfiguration.ProtocolId);
                
                // 重新加载配置
                await LoadConfigurationsAsync();
                
                StatusMessage = "配置删除成功";
            }
            catch (Exception ex)
            {
                StatusMessage = $"删除配置失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 连接设备
        /// </summary>
        private async Task ConnectAsync()
        {
            if (SelectedConfiguration == null)
            {
                StatusMessage = "请选择配置";
                return;
            }
            
            try
            {
                StatusMessage = "正在连接...";
                
                // 创建协议实例
                var protocol = _protocolFactory.CreateProtocol(SelectedConfiguration);
                
                // 注册协议
                if (!_communicationManager.RegisterProtocol(SelectedConfiguration.ProtocolId, protocol))
                {
                    StatusMessage = "协议注册失败";
                    return;
                }
                
                // 连接设备
                var result = await _communicationManager.ConnectAsync(SelectedConfiguration.ProtocolId);
                
                if (result)
                {
                    IsConnected = true;
                    StatusMessage = "连接成功";
                }
                else
                {
                    StatusMessage = "连接失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"连接失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        private async Task DisconnectAsync()
        {
            if (SelectedConfiguration == null)
            {
                StatusMessage = "请选择配置";
                return;
            }
            
            try
            {
                StatusMessage = "正在断开连接...";
                
                await _communicationManager.DisconnectAsync(SelectedConfiguration.ProtocolId);
                
                IsConnected = false;
                StatusMessage = "已断开连接";
            }
            catch (Exception ex)
            {
                StatusMessage = $"断开连接失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 发送数据
        /// </summary>
        private async Task SendDataAsync()
        {
            if (SelectedConfiguration == null)
            {
                StatusMessage = "请选择配置";
                return;
            }
            
            if (string.IsNullOrWhiteSpace(DataToSend))
            {
                StatusMessage = "请输入要发送的数据";
                return;
            }
            
            if (!IsConnected)
            {
                StatusMessage = "设备未连接";
                return;
            }
            
            IsSending = true;
            
            try
            {
                StatusMessage = "正在发送数据...";
                
                // 转换数据格式
                byte[] dataToSend = SelectedDataFormat switch
                {
                    DataFormat.Hex => _dataConverter.HexToBytes(DataToSend),
                    DataFormat.ASCII => _dataConverter.AsciiToBytes(DataToSend),
                    DataFormat.Binary => Convert.FromBase64String(DataToSend),
                    _ => _dataConverter.HexToBytes(DataToSend)
                };
                
                // 记录发送的数据
                SentData += $"[发送] {DateTime.Now:HH:mm:ss.fff}\n";
                SentData += $"{_dataConverter.BytesToHex(dataToSend)}\n\n";
                
                // 发送数据
                var response = await _communicationManager.SendReceiveAsync(
                    SelectedConfiguration.ProtocolId,
                    dataToSend);
                
                if (response != null)
                {
                    // 记录接收的数据
                    ReceivedData += $"[接收] {DateTime.Now:HH:mm:ss.fff}\n";
                    ReceivedData += $"{_dataConverter.BytesToHex(response)}\n\n";
                    
                    StatusMessage = "数据发送接收成功";
                }
                else
                {
                    StatusMessage = "未收到响应";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"发送数据失败: {ex.Message}";
            }
            finally
            {
                IsSending = false;
            }
        }
        
        /// <summary>
        /// 刷新串口列表
        /// </summary>
        private async Task RefreshPortsAsync()
        {
            try
            {
                AvailablePorts.Clear();
                
                // 获取可用串口
                var ports = await Task.Run(() => System.IO.Ports.SerialPort.GetPortNames());
                
                foreach (var port in ports.OrderBy(p => p))
                {
                    AvailablePorts.Add(port);
                }
                
                if (AvailablePorts.Count == 0)
                {
                    AvailablePorts.Add("无可用串口");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"刷新串口失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 清空数据
        /// </summary>
        private void ClearData()
        {
            SentData = string.Empty;
            ReceivedData = string.Empty;
            DataToSend = string.Empty;
            StatusMessage = "数据已清空";
        }
        
        /// <summary>
        /// 通讯状态变更事件处理
        /// </summary>
        private void OnCommunicationStatusChanged(object? sender, CommunicationStatusChangedEventArgs e)
        {
            // 在主线程更新UI
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"{e.ProtocolId}: {e.Message}";
                
                if (SelectedConfiguration?.ProtocolId == e.ProtocolId)
                {
                    IsConnected = e.IsConnected;
                }
            });
        }
        
        /// <summary>
        /// 数据接收事件处理
        /// </summary>
        private void OnDataReceived(object? sender, CommunicationDataReceivedEventArgs e)
        {
            // 在主线程更新UI
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (SelectedConfiguration?.ProtocolId == e.ProtocolId && e.Direction == DataDirection.Received)
                {
                    ReceivedData += $"[自动接收] {DateTime.Now:HH:mm:ss.fff}\n";
                    ReceivedData += $"{_dataConverter.BytesToHex(e.RawData)}\n\n";
                }
            });
        }
        
        /// <summary>
        /// 创建新配置
        /// </summary>
        [RelayCommand]
        private void CreateNewConfiguration()
        {
            var newConfig = new ProtocolConfiguration
            {
                ProtocolId = $"Protocol_{Guid.NewGuid():N}",
                ProtocolType = ProtocolType.ModbusTCP,
                InterfaceType = InterfaceType.Ethernet,
                DeviceAddress = "192.168.1.100",
                Port = 502,
                BaudRate = 9600,
                DataBits = 8,
                Parity = "None",
                StopBits = "One",
                Timeout = 5000,
                RetryCount = 3,
                AutoReconnect = true,
                ReconnectInterval = 5000
            };
            
            ProtocolConfigurations.Add(newConfig);
            SelectedConfiguration = newConfig;
            
            StatusMessage = "已创建新配置";
        }
        
        /// <summary>
        /// 复制配置
        /// </summary>
        [RelayCommand]
        private void CopyConfiguration()
        {
            if (SelectedConfiguration == null)
            {
                StatusMessage = "请选择要复制的配置";
                return;
            }
            
            var copy = new ProtocolConfiguration
            {
                ProtocolId = $"{SelectedConfiguration.ProtocolId}_Copy",
                ProtocolType = SelectedConfiguration.ProtocolType,
                InterfaceType = SelectedConfiguration.InterfaceType,
                DeviceAddress = SelectedConfiguration.DeviceAddress,
                Port = SelectedConfiguration.Port,
                BaudRate = SelectedConfiguration.BaudRate,
                DataBits = SelectedConfiguration.DataBits,
                Parity = SelectedConfiguration.Parity,
                StopBits = SelectedConfiguration.StopBits,
                Timeout = SelectedConfiguration.Timeout,
                RetryCount = SelectedConfiguration.RetryCount,
                AutoReconnect = SelectedConfiguration.AutoReconnect,
                ReconnectInterval = SelectedConfiguration.ReconnectInterval,
                CustomSettings = new Dictionary<string, string>(SelectedConfiguration.CustomSettings)
            };
            
            ProtocolConfigurations.Add(copy);
            SelectedConfiguration = copy;
            
            StatusMessage = "配置已复制";
        }
        
        /// <summary>
        /// 测试连接
        /// </summary>
        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            if (SelectedConfiguration == null)
            {
                StatusMessage = "请选择配置";
                return;
            }
            
            try
            {
                StatusMessage = "正在测试连接...";
                
                // 创建临时协议实例进行测试
                var protocol = _protocolFactory.CreateProtocol(SelectedConfiguration);
                
                var result = await protocol.ConnectAsync();
                
                if (result)
                {
                    await protocol.DisconnectAsync();
                    StatusMessage = "连接测试成功";
                }
                else
                {
                    StatusMessage = "连接测试失败";
                }
                
                protocol.Dispose();
            }
            catch (Exception ex)
            {
                StatusMessage = $"连接测试失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 导入配置
        /// </summary>
        [RelayCommand]
        private async Task ImportConfigurationAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    Title = "导入协议配置"
                };
                
                if (dialog.ShowDialog() == true)
                {
                    var json = await System.IO.File.ReadAllTextAsync(dialog.FileName);
                    var config = System.Text.Json.JsonSerializer.Deserialize<ProtocolConfiguration>(json);
                    
                    if (config != null)
                    {
                        ProtocolConfigurations.Add(config);
                        SelectedConfiguration = config;
                        StatusMessage = "配置导入成功";
                    }
                    else
                    {
                        StatusMessage = "配置文件格式错误";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"导入配置失败: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 导出配置
        /// </summary>
        [RelayCommand]
        private async Task ExportConfigurationAsync()
        {
            if (SelectedConfiguration == null)
            {
                StatusMessage = "请选择要导出的配置";
                return;
            }
            
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    FileName = $"{SelectedConfiguration.ProtocolId}.json",
                    Title = "导出协议配置"
                };
                
                if (dialog.ShowDialog() == true)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(SelectedConfiguration, 
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    
                    await System.IO.File.WriteAllTextAsync(dialog.FileName, json);
                    StatusMessage = "配置导出成功";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"导出配置失败: {ex.Message}";
            }
        }
    }
}