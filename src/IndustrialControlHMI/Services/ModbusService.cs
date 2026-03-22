using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace IndustrialControlHMI.Services
{
    /// <summary>
    /// Modbus通信服务接口。
    /// </summary>
    public interface IModbusService : IDisposable
    {
        // 连接管理
        Task<bool> ConnectAsync(string ipAddress, int port);
        Task<bool> ConnectAsync(); // 使用配置的连接参数
        void Disconnect();
        bool IsConnected { get; }
        
        // 数据读取
        Task<float> ReadFloatAsync(ushort registerAddress);
        Task<double> ReadDoubleAsync(ushort registerAddress);
        Task<short> ReadShortAsync(ushort registerAddress);
        Task<ushort> ReadUShortAsync(ushort registerAddress);
        Task<int> ReadInt32Async(ushort registerAddress);
        Task<uint> ReadUInt32Async(ushort registerAddress);
        Task<string> ReadStringAsync(ushort startAddress, int length);
        
        // 批量读取
        Task<IEnumerable<float>> ReadMultipleFloatsAsync(ushort startAddress, int count);
        Task<Dictionary<ushort, float>> ReadRegisterMapAsync(IEnumerable<ushort> addresses);
        
        // 数据写入
        Task<bool> WriteFloatAsync(ushort registerAddress, float value);
        Task<bool> WriteShortAsync(ushort registerAddress, short value);
        Task<bool> WriteMultipleRegistersAsync(ushort startAddress, ushort[] values);
        
        // 事件
        event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        event EventHandler<DataReceivedEventArgs> DataReceived;
        event EventHandler<ErrorOccurredEventArgs> ErrorOccurred;
    }

    /// <summary>
    /// Modbus配置接口。
    /// </summary>
    public interface IModbusConfig
    {
        string IpAddress { get; set; }
        int Port { get; set; }
        int SlaveId { get; set; }
        int ReadTimeout { get; set; }
        int WriteTimeout { get; set; }
        int RetryCount { get; set; }
        int RetryDelay { get; set; }
        int PollingInterval { get; set; }
    }

    /// <summary>
    /// 连接状态变更事件参数。
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string Message { get; }
        
        public ConnectionStatusChangedEventArgs(bool isConnected, string? message = null)
        {
            IsConnected = isConnected;
            Message = message;
        }
    }

    /// <summary>
    /// 数据接收事件参数。
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        public ushort RegisterAddress { get; }
        public object Value { get; }
        public DateTime Timestamp { get; }
        
        public DataReceivedEventArgs(ushort registerAddress, object value)
        {
            RegisterAddress = registerAddress;
            Value = value;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 错误发生事件参数。
    /// </summary>
    public class ErrorOccurredEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        public Exception Exception { get; }
        
        public ErrorOccurredEventArgs(string errorMessage, Exception? exception = null)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }

    /// <summary>
    /// Modbus服务实现（模拟版本，用于演示）。
    /// </summary>
    public class ModbusService : IModbusService
    {
        private readonly IModbusConfig _config;
        private TcpClient _tcpClient;
        private object _modbusMaster; // 模拟字段，实际不使用
        private bool _isConnected;
        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<ushort, float> _registerCache;
        private CancellationTokenSource _pollingCts;
        private Task _pollingTask;
        private readonly Random _random = new Random();
        
        public bool IsConnected => _isConnected;
        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<ErrorOccurredEventArgs> ErrorOccurred;
        
        public ModbusService(IModbusConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _registerCache = new ConcurrentDictionary<ushort, float>();
            InitializeCache();
        }
        
        private void InitializeCache()
        {
            // 初始化一些模拟数据
            for (ushort i = 0; i < 100; i++)
            {
                _registerCache[i] = (float)(_random.NextDouble() * 100);
            }
        }
        
        public async Task<bool> ConnectAsync(string ipAddress, int port)
        {
            try
            {
                Disconnect();
                
                string targetIp = ipAddress ?? _config.IpAddress;
                int targetPort = port;
                
                // 模拟连接延迟
                await Task.Delay(500);
                
                lock (_lock)
                {
                    // 在实际实现中，这里会创建TcpClient和ModbusMaster
                    // 这里仅模拟连接成功
                    _isConnected = true;
                    OnConnectionStatusChanged(true, $"成功连接到 {targetIp}:{targetPort}");
                    
                    // 启动轮询任务
                    StartPolling();
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"连接失败: {ex.Message}", ex);
                return false;
            }
        }
        
        public async Task<bool> ConnectAsync()
        {
            return await ConnectAsync(_config.IpAddress, _config.Port);
        }
        
        public void Disconnect()
        {
            lock (_lock)
            {
                StopPolling();
                
                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient = null;
                }
                
                _modbusMaster = null;
                
                if (_isConnected)
                {
                    _isConnected = false;
                    OnConnectionStatusChanged(false, "连接已断开");
                }
            }
        }
        
        private void StartPolling()
        {
            if (_pollingCts != null)
                return;
                
            _pollingCts = new CancellationTokenSource();
            _pollingTask = Task.Run(async () => await PollingLoop(_pollingCts.Token));
        }
        
        private void StopPolling()
        {
            if (_pollingCts != null)
            {
                _pollingCts.Cancel();
                _pollingCts = null;
            }
            
            if (_pollingTask != null)
            {
                try
                {
                    _pollingTask.Wait(1000);
                }
                catch (AggregateException) { }
                _pollingTask = null;
            }
        }
        
        private async Task PollingLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isConnected)
            {
                try
                {
                    await Task.Delay(_config.PollingInterval, cancellationToken);
                    
                    // 模拟读取一些寄存器
                    var addresses = new ushort[] { 0, 1, 2, 3, 4 };
                    foreach (var address in addresses)
                    {
                        // 模拟数据变化
                        float newValue = (float)(_random.NextDouble() * 100);
                        _registerCache[address] = newValue;
                        
                        OnDataReceived(address, newValue);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"轮询错误: {ex.Message}", ex);
                }
            }
        }
        
        public Task<float> ReadFloatAsync(ushort registerAddress)
        {
            if (!_isConnected)
                throw new InvalidOperationException("未连接到PLC");
                
            // 从缓存读取模拟值
            float value = _registerCache.GetOrAdd(registerAddress, _ => (float)(_random.NextDouble() * 100));
            return Task.FromResult(value);
        }
        
        public async Task<double> ReadDoubleAsync(ushort registerAddress)
        {
            float f = await ReadFloatAsync(registerAddress).ConfigureAwait(false);
            return (double)f;
        }
        
        public Task<short> ReadShortAsync(ushort registerAddress)
        {
            if (!_isConnected)
                throw new InvalidOperationException("未连接到PLC");
                
            short value = (short)_random.Next(-100, 100);
            return Task.FromResult(value);
        }
        
        public Task<ushort> ReadUShortAsync(ushort registerAddress)
        {
            if (!_isConnected)
                throw new InvalidOperationException("未连接到PLC");
                
            ushort value = (ushort)_random.Next(0, 100);
            return Task.FromResult(value);
        }
        
        public Task<int> ReadInt32Async(ushort registerAddress)
        {
            if (!_isConnected)
                throw new InvalidOperationException("未连接到PLC");
                
            int value = _random.Next(-1000, 1000);
            return Task.FromResult(value);
        }
        
        public Task<uint> ReadUInt32Async(ushort registerAddress)
        {
            if (!_isConnected)
                throw new InvalidOperationException("未连接到PLC");
                
            uint value = (uint)_random.Next(0, 1000);
            return Task.FromResult(value);
        }
        
        public Task<string> ReadStringAsync(ushort startAddress, int length)
        {
            if (!_isConnected)
                throw new InvalidOperationException("未连接到PLC");
                
            string value = $"模拟字符串 {startAddress}";
            return Task.FromResult(value);
        }
        
        public Task<IEnumerable<float>> ReadMultipleFloatsAsync(ushort startAddress, int count)
        {
            if (!_isConnected)
                throw new InvalidOperationException("未连接到PLC");
                
            var values = new List<float>();
            for (int i = 0; i < count; i++)
            {
                values.Add(_registerCache.GetOrAdd((ushort)(startAddress + i), _ => (float)(_random.NextDouble() * 100)));
            }
            
            return Task.FromResult(values.AsEnumerable());
        }
        
        public Task<Dictionary<ushort, float>> ReadRegisterMapAsync(IEnumerable<ushort> addresses)
        {
            if (!_isConnected)
                throw new InvalidOperationException("未连接到PLC");
                
            var result = new Dictionary<ushort, float>();
            foreach (var address in addresses)
            {
                result[address] = _registerCache.GetOrAdd(address, _ => (float)(_random.NextDouble() * 100));
            }
            
            return Task.FromResult(result);
        }
        
        public Task<bool> WriteFloatAsync(ushort registerAddress, float value)
        {
            if (!_isConnected)
                throw new InvalidOperationException("未连接到PLC");
                
            _registerCache[registerAddress] = value;
            OnDataReceived(registerAddress, value);
            return Task.FromResult(true);
        }
        
        public Task<bool> WriteShortAsync(ushort registerAddress, short value)
        {
            if (!_isConnected)
                throw new InvalidOperationException("未连接到PLC");
                
            // 模拟写入成功
            return Task.FromResult(true);
        }
        
        public Task<bool> WriteMultipleRegistersAsync(ushort startAddress, ushort[] values)
        {
            if (!_isConnected)
                throw new InvalidOperationException("未连接到PLC");
                
            // 模拟写入成功
            return Task.FromResult(true);
        }
        
        protected virtual void OnConnectionStatusChanged(bool isConnected, string message = null)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(isConnected, message));
        }
        
        protected virtual void OnDataReceived(ushort registerAddress, object value)
        {
            DataReceived?.Invoke(this, new DataReceivedEventArgs(registerAddress, value));
        }
        
        protected virtual void OnErrorOccurred(string errorMessage, Exception exception = null)
        {
            ErrorOccurred?.Invoke(this, new ErrorOccurredEventArgs(errorMessage, exception));
        }
        
        public void Dispose()
        {
            Disconnect();
        }
    }

    /// <summary>
    /// 默认Modbus配置。
    /// </summary>
    public class DefaultModbusConfig : IModbusConfig
    {
        public string IpAddress { get; set; } = "192.168.1.100";
        public int Port { get; set; } = 502;
        public int SlaveId { get; set; } = 1;
        public int ReadTimeout { get; set; } = 1000;
        public int WriteTimeout { get; set; } = 1000;
        public int RetryCount { get; set; } = 3;
        public int RetryDelay { get; set; } = 1000;
        public int PollingInterval { get; set; } = 500;
    }
}