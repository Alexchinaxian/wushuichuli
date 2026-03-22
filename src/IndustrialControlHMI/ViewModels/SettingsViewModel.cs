using System.Net;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlHMI.Models;
using IndustrialControlHMI.Services;

namespace IndustrialControlHMI.ViewModels
{
    /// <summary>
    /// 参数设置视图模型。
    /// </summary>
    public partial class SettingsViewModel : ObservableObject, IDisposable
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IModbusService _modbusService;
        private bool _isDisposed;

        // 配置分类
        [ObservableProperty]
        private ObservableCollection<SettingsCategory> _categories = new ObservableCollection<SettingsCategory>();

        [ObservableProperty]
        private SettingsCategory _selectedCategory;

        // 状态属性
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string _statusMessage = "就绪";

        [ObservableProperty]
        private bool _hasChanges;

        // Modbus设置
        [ObservableProperty]
        private string _modbusIpAddress = "192.168.1.100";

        [ObservableProperty]
        private int _modbusPort = 502;

        [ObservableProperty]
        private int _modbusSlaveId = 1;

        [ObservableProperty]
        private int _pollingInterval = 1000;

        [ObservableProperty]
        private int _readTimeout = 5000;

        [ObservableProperty]
        private int _writeTimeout = 5000;

        // 报警阈值设置
        [ObservableProperty]
        private ObservableCollection<AlarmThresholdSetting> _alarmThresholds = new ObservableCollection<AlarmThresholdSetting>();

        // 数据存储设置
        [ObservableProperty]
        private int _dataRetentionDays = 30;

        [ObservableProperty]
        private bool _enableAutoBackup = true;

        [ObservableProperty]
        private int _backupIntervalHours = 24;

        // UI设置
        [ObservableProperty]
        private string _theme = "Dark";

        [ObservableProperty]
        private int _chartHistoryPoints = 100;

        [ObservableProperty]
        private double _chartRefreshRate = 1.0;

        /// <summary>
        /// 初始化设置视图模型。
        /// </summary>
        public SettingsViewModel(ISettingsManager settingsManager, IModbusService modbusService)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));

            InitializeCategories();
            _ = LoadSettingsAsync();
        }

        /// <summary>
        /// 初始化配置分类。
        /// </summary>
        private void InitializeCategories()
        {
            Categories.Clear();

            Categories.Add(new SettingsCategory
            {
                Id = "communication",
                Name = "通信设置",
                Icon = "", // Network icon
                Description = "配置Modbus TCP通信参数"
            });

            Categories.Add(new SettingsCategory
            {
                Id = "alarms",
                Name = "报警设置",
                Icon = "", // Alarm icon
                Description = "配置报警阈值和通知"
            });

            Categories.Add(new SettingsCategory
            {
                Id = "data",
                Name = "数据存储",
                Icon = "", // Database icon
                Description = "配置数据存储和备份"
            });

            Categories.Add(new SettingsCategory
            {
                Id = "ui",
                Name = "界面设置",
                Icon = "", // UI icon
                Description = "配置用户界面外观"
            });

            SelectedCategory = Categories[0];
        }

        /// <summary>
        /// 加载设置命令。
        /// </summary>
        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            IsLoading = true;
            StatusMessage = "正在加载配置...";

            try
            {
                // 加载Modbus设置
                ModbusIpAddress = await _settingsManager.GetSettingAsync("Modbus", "IpAddress", "192.168.1.100");
                ModbusPort = await _settingsManager.GetSettingAsync<int>("Modbus", "Port", 502);
                ModbusSlaveId = await _settingsManager.GetSettingAsync<int>("Modbus", "SlaveId", 1);
                PollingInterval = await _settingsManager.GetSettingAsync<int>("Modbus", "PollingInterval", 1000);
                ReadTimeout = await _settingsManager.GetSettingAsync<int>("Modbus", "ReadTimeout", 5000);
                WriteTimeout = await _settingsManager.GetSettingAsync<int>("Modbus", "WriteTimeout", 5000);

                // 加载报警阈值
                await LoadAlarmThresholdsAsync();

                // 加载数据存储设置
                DataRetentionDays = await _settingsManager.GetSettingAsync<int>("Data", "RetentionDays", 30);
                EnableAutoBackup = await _settingsManager.GetSettingAsync<bool>("Data", "EnableAutoBackup", true);
                BackupIntervalHours = await _settingsManager.GetSettingAsync<int>("Data", "BackupIntervalHours", 24);

                // 加载UI设置
                Theme = await _settingsManager.GetSettingAsync("UI", "Theme", "Dark");
                ChartHistoryPoints = await _settingsManager.GetSettingAsync<int>("UI", "ChartHistoryPoints", 100);
                ChartRefreshRate = await _settingsManager.GetSettingAsync<double>("UI", "ChartRefreshRate", 1.0);

                HasChanges = false;
                StatusMessage = "配置加载完成";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载配置失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 保存设置命令。
        /// </summary>
        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (!HasChanges)
            {
                StatusMessage = "没有需要保存的更改";
                return;
            }

            IsSaving = true;
            StatusMessage = "正在保存配置...";

            try
            {
                // 验证设置
                if (!ValidateSettings())
                {
                    StatusMessage = "配置验证失败，请检查输入";
                    return;
                }

                // 保存Modbus设置
                await _settingsManager.SetSettingAsync("Modbus", "IpAddress", ModbusIpAddress);
                await _settingsManager.SetSettingAsync("Modbus", "Port", ModbusPort);
                await _settingsManager.SetSettingAsync("Modbus", "SlaveId", ModbusSlaveId);
                await _settingsManager.SetSettingAsync("Modbus", "PollingInterval", PollingInterval);
                await _settingsManager.SetSettingAsync("Modbus", "ReadTimeout", ReadTimeout);
                await _settingsManager.SetSettingAsync("Modbus", "WriteTimeout", WriteTimeout);

                // 保存报警阈值
                await SaveAlarmThresholdsAsync();

                // 保存数据存储设置
                await _settingsManager.SetSettingAsync("Data", "RetentionDays", DataRetentionDays);
                await _settingsManager.SetSettingAsync("Data", "EnableAutoBackup", EnableAutoBackup);
                await _settingsManager.SetSettingAsync("Data", "BackupIntervalHours", BackupIntervalHours);

                // 保存UI设置
                await _settingsManager.SetSettingAsync("UI", "Theme", Theme);
                await _settingsManager.SetSettingAsync("UI", "ChartHistoryPoints", ChartHistoryPoints);
                await _settingsManager.SetSettingAsync("UI", "ChartRefreshRate", ChartRefreshRate);

                HasChanges = false;
                StatusMessage = "配置保存成功";
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存配置失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// 重置为默认值命令。
        /// </summary>
        [RelayCommand]
        private async Task ResetToDefaultsAsync()
        {
            try
            {
                await _settingsManager.ResetToDefaultsAsync();
                await LoadSettingsAsync();
                StatusMessage = "已重置为默认配置";
            }
            catch (Exception ex)
            {
                StatusMessage = $"重置配置失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"重置配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试Modbus连接命令。
        /// </summary>
        [RelayCommand]
        private async Task TestModbusConnectionAsync()
        {
            StatusMessage = "正在测试Modbus连接...";
            try
            {
                bool connected = await _modbusService.ConnectAsync(ModbusIpAddress, ModbusPort);
                StatusMessage = connected
                    ? $"Modbus连接测试成功: {ModbusIpAddress}:{ModbusPort}"
                    : $"Modbus连接测试失败: {ModbusIpAddress}:{ModbusPort}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"连接测试异常: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Modbus连接测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载报警阈值。
        /// </summary>
        private async Task LoadAlarmThresholdsAsync()
        {
            try
            {
                AlarmThresholds.Clear();

                var parameters = new[] { "Temperature", "Pressure", "FlowRate" };

                foreach (var param in parameters)
                {
                    var threshold = new AlarmThresholdSetting
                    {
                        ParameterName = param,
                        HighHigh = await _settingsManager.GetSettingAsync<double>($"Alarm.{param}", "HighHigh", 0),
                        High = await _settingsManager.GetSettingAsync<double>($"Alarm.{param}", "High", 0),
                        Low = await _settingsManager.GetSettingAsync<double>($"Alarm.{param}", "Low", 0),
                        LowLow = await _settingsManager.GetSettingAsync<double>($"Alarm.{param}", "LowLow", 0),
                        Unit = param switch
                        {
                            "Temperature" => "°C",
                            "Pressure" => "MPa",
                            "FlowRate" => "L/min",
                            _ => ""
                        }
                    };

                    AlarmThresholds.Add(threshold);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载报警阈值失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存报警阈值。
        /// </summary>
        private async Task SaveAlarmThresholdsAsync()
        {
            try
            {
                foreach (var threshold in AlarmThresholds)
                {
                    await _settingsManager.SetSettingAsync($"Alarm.{threshold.ParameterName}", "HighHigh", threshold.HighHigh);
                    await _settingsManager.SetSettingAsync($"Alarm.{threshold.ParameterName}", "High", threshold.High);
                    await _settingsManager.SetSettingAsync($"Alarm.{threshold.ParameterName}", "Low", threshold.Low);
                    await _settingsManager.SetSettingAsync($"Alarm.{threshold.ParameterName}", "LowLow", threshold.LowLow);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存报警阈值失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 验证设置。
        /// </summary>
        private bool ValidateSettings()
        {
            // 验证IP地址格式
            if (!System.Net.IPAddress.TryParse(ModbusIpAddress, out _))
            {
                System.Windows.MessageBox.Show("IP地址格式无效", "验证错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }

            // 验证端口范围
            if (ModbusPort < 1 || ModbusPort > 65535)
            {
                System.Windows.MessageBox.Show("端口必须在1-65535范围内", "验证错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }

            // 验证轮询间隔
            if (PollingInterval < 100 || PollingInterval > 10000)
            {
                System.Windows.MessageBox.Show("轮询间隔必须在100-10000ms范围内", "验证错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }

            // 验证数据保留天数
            if (DataRetentionDays < 1 || DataRetentionDays > 365)
            {
                System.Windows.MessageBox.Show("数据保留天数必须在1-365范围内", "验证错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 属性更改时检查变化。
        /// </summary>
        partial void OnModbusIpAddressChanged(string value) => CheckForChanges();
        partial void OnModbusPortChanged(int value) => CheckForChanges();
        partial void OnModbusSlaveIdChanged(int value) => CheckForChanges();
        partial void OnPollingIntervalChanged(int value) => CheckForChanges();
        partial void OnReadTimeoutChanged(int value) => CheckForChanges();
        partial void OnWriteTimeoutChanged(int value) => CheckForChanges();
        partial void OnDataRetentionDaysChanged(int value) => CheckForChanges();
        partial void OnEnableAutoBackupChanged(bool value) => CheckForChanges();
        partial void OnBackupIntervalHoursChanged(int value) => CheckForChanges();
        partial void OnThemeChanged(string value) => CheckForChanges();
        partial void OnChartHistoryPointsChanged(int value) => CheckForChanges();
        partial void OnChartRefreshRateChanged(double value) => CheckForChanges();

        /// <summary>
        /// 检查是否有更改。
        /// </summary>
        private void CheckForChanges()
        {
            // 简化：设置更改后标记为有更改
            HasChanges = true;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            // 清理资源
        }
    }
}