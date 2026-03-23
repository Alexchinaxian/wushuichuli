using System.Text.Json;
using System.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using IndustrialControlHMI.Infrastructure.Config;
using IndustrialControlHMI.Services.Logging;
using Microsoft.Extensions.Configuration;

namespace IndustrialControlHMI.Services.Communication
{
    /// <summary>
    /// 通讯管理器，负责管理多个通讯协议实例
    /// </summary>
    public class CommunicationManager : ICommunicationManager
    {
        private sealed class ProtocolRuntimeStats
        {
            public long TotalBytesSent;
            public long TotalBytesReceived;
            public int TotalErrors;
            public DateTime LastActivity = DateTime.Now;
            public DateTime? ConnectedAt;
        }

        private readonly ConcurrentDictionary<string, ProtocolBase> _protocols;
        private readonly ConcurrentDictionary<string, ProtocolRuntimeStats> _stats;
        private readonly IConfiguration _configuration;
        private readonly IAppLogger _logger;
        private readonly IProtocolFactory _protocolFactory;
        private readonly string _configDirectory;
        
        /// <summary>
        /// 通讯状态变更事件
        /// </summary>
        public event EventHandler<CommunicationStatusChangedEventArgs> CommunicationStatusChanged;
        
        /// <summary>
        /// 数据接收事件
        /// </summary>
        public event EventHandler<CommunicationDataReceivedEventArgs> DataReceived;
        
        public CommunicationManager(IConfiguration configuration, IAppLogger logger, IProtocolFactory protocolFactory)
        {
            _protocols = new ConcurrentDictionary<string, ProtocolBase>();
            _stats = new ConcurrentDictionary<string, ProtocolRuntimeStats>();
            _configuration = configuration;
            _logger = logger;
            _protocolFactory = protocolFactory;
            _configDirectory = Path.Combine(AppConfigPaths.GetConfigDirectory(), "Communication");
            
            // 确保配置目录存在
            System.IO.Directory.CreateDirectory(_configDirectory);
            
            // 加载配置
            LoadConfiguration();
        }
        
        /// <summary>
        /// 注册协议实例
        /// </summary>
        public bool RegisterProtocol(string protocolId, ProtocolBase protocol)
        {
            if (string.IsNullOrEmpty(protocolId))
                throw new ArgumentException("协议ID不能为空", nameof(protocolId));
            
            if (protocol == null)
                throw new ArgumentNullException(nameof(protocol));
            
            var added = _protocols.TryAdd(protocolId, protocol);
            if (added)
            {
                _stats.TryAdd(protocolId, new ProtocolRuntimeStats());
                protocol.ConnectionStatusChanged += OnProtocolConnectionStatusChanged;
                protocol.DataReceived += OnProtocolDataReceived;
                protocol.ErrorOccurred += OnProtocolErrorOccurred;
            }

            return added;
        }
        
        /// <summary>
        /// 移除协议实例
        /// </summary>
        public bool UnregisterProtocol(string protocolId)
        {
            if (_protocols.TryRemove(protocolId, out var protocol))
            {
                _stats.TryRemove(protocolId, out _);
                protocol.ConnectionStatusChanged -= OnProtocolConnectionStatusChanged;
                protocol.DataReceived -= OnProtocolDataReceived;
                protocol.ErrorOccurred -= OnProtocolErrorOccurred;
                protocol.Dispose();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 获取协议实例
        /// </summary>
        public ProtocolBase? GetProtocol(string protocolId)
        {
            _protocols.TryGetValue(protocolId, out var protocol);
            return protocol;
        }
        
        /// <summary>
        /// 获取所有协议
        /// </summary>
        public IEnumerable<KeyValuePair<string, ProtocolBase>> GetAllProtocols()
        {
            return _protocols.ToArray();
        }
        
        /// <summary>
        /// 连接指定协议
        /// </summary>
        public async Task<bool> ConnectAsync(string protocolId)
        {
            var protocol = GetProtocol(protocolId);
            if (protocol == null)
            {
                OnErrorOccurred(new CommunicationErrorEventArgs
                {
                    ProtocolId = protocolId,
                    ErrorMessage = $"协议未注册: {protocolId}",
                    Severity = ErrorSeverity.Error
                });
                return false;
            }
            
            try
            {
                var result = await protocol.ConnectAsync();
                
                OnCommunicationStatusChanged(new CommunicationStatusChangedEventArgs
                {
                    ProtocolId = protocolId,
                    IsConnected = result,
                    Message = result ? "连接成功" : "连接失败",
                    Timestamp = DateTime.Now
                });
                
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new CommunicationErrorEventArgs
                {
                    ProtocolId = protocolId,
                    ErrorMessage = $"连接失败: {ex.Message}",
                    Exception = ex,
                    Severity = ErrorSeverity.Error
                });
                return false;
            }
        }
        
        /// <summary>
        /// 断开指定协议连接
        /// </summary>
        public async Task DisconnectAsync(string protocolId)
        {
            var protocol = GetProtocol(protocolId);
            if (protocol == null)
                return;
            
            try
            {
                await protocol.DisconnectAsync();
                
                OnCommunicationStatusChanged(new CommunicationStatusChangedEventArgs
                {
                    ProtocolId = protocolId,
                    IsConnected = false,
                    Message = "已断开连接",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new CommunicationErrorEventArgs
                {
                    ProtocolId = protocolId,
                    ErrorMessage = $"断开连接失败: {ex.Message}",
                    Exception = ex,
                    Severity = ErrorSeverity.Warning
                });
            }
        }
        
        /// <summary>
        /// 发送数据到指定协议
        /// </summary>
        public async Task<byte[]?> SendReceiveAsync(string protocolId, byte[] data, int timeout = 5000)
        {
            var protocol = GetProtocol(protocolId);
            if (protocol == null)
            {
                OnErrorOccurred(new CommunicationErrorEventArgs
                {
                    ProtocolId = protocolId,
                    ErrorMessage = $"协议未注册: {protocolId}",
                    Severity = ErrorSeverity.Error
                });
                return null;
            }
            
            if (!protocol.IsConnected)
            {
                OnErrorOccurred(new CommunicationErrorEventArgs
                {
                    ProtocolId = protocolId,
                    ErrorMessage = "协议未连接",
                    Severity = ErrorSeverity.Error
                });
                return null;
            }
            
            try
            {
                var response = await protocol.SendReceiveAsync(data, timeout);
                
                OnDataReceived(new CommunicationDataReceivedEventArgs
                {
                    ProtocolId = protocolId,
                    RawData = response,
                    Direction = DataDirection.Received,
                    Timestamp = DateTime.Now
                });
                
                return response;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new CommunicationErrorEventArgs
                {
                    ProtocolId = protocolId,
                    ErrorMessage = $"发送接收失败: {ex.Message}",
                    Exception = ex,
                    Severity = ErrorSeverity.Error
                });
                return null;
            }
        }
        
        /// <summary>
        /// 批量发送数据
        /// </summary>
        public async Task<Dictionary<string, byte[]?>> SendReceiveBatchAsync(
            Dictionary<string, byte[]> requests, 
            int timeout = 5000)
        {
            var results = new Dictionary<string, byte[]?>();
            var tasks = new List<Task<KeyValuePair<string, byte[]?>>>();
            
            foreach (var request in requests)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var response = await SendReceiveAsync(request.Key, request.Value, timeout);
                    return new KeyValuePair<string, byte[]?>(request.Key, response);
                }));
            }
            
            var completedTasks = await Task.WhenAll(tasks);
            foreach (var task in completedTasks)
            {
                results[task.Key] = task.Value;
            }
            
            return results;
        }
        
        /// <summary>
        /// 保存协议配置
        /// </summary>
        public async Task SaveProtocolConfigurationAsync(string protocolId, ProtocolConfiguration config)
        {
            try
            {
                var normalizedId = NormalizeProtocolId(protocolId);
                var filePath = Path.Combine(_configDirectory, $"{normalizedId}.json");
                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await System.IO.File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new CommunicationErrorEventArgs
                {
                    ProtocolId = protocolId,
                    ErrorMessage = $"保存配置失败: {ex.Message}",
                    Exception = ex,
                    Severity = ErrorSeverity.Error
                });
            }
        }
        
        /// <summary>
        /// 加载协议配置
        /// </summary>
        public async Task<ProtocolConfiguration?> LoadProtocolConfigurationAsync(string protocolId)
        {
            try
            {
                var normalizedId = NormalizeProtocolId(protocolId);
                var filePath = Path.Combine(_configDirectory, $"{normalizedId}.json");
                
                if (!System.IO.File.Exists(filePath))
                    return null;
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                return System.Text.Json.JsonSerializer.Deserialize<ProtocolConfiguration>(json);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new CommunicationErrorEventArgs
                {
                    ProtocolId = protocolId,
                    ErrorMessage = $"加载配置失败: {ex.Message}",
                    Exception = ex,
                    Severity = ErrorSeverity.Warning
                });
                return null;
            }
        }
        
        /// <summary>
        /// 获取通讯统计信息
        /// </summary>
        public CommunicationStatistics GetStatistics(string protocolId)
        {
            var stats = _stats.GetOrAdd(protocolId, _ => new ProtocolRuntimeStats());
            var connectionTime = stats.ConnectedAt.HasValue
                ? DateTime.Now - stats.ConnectedAt.Value
                : TimeSpan.Zero;

            return new CommunicationStatistics
            {
                ProtocolId = protocolId,
                TotalBytesSent = stats.TotalBytesSent,
                TotalBytesReceived = stats.TotalBytesReceived,
                TotalErrors = stats.TotalErrors,
                ConnectionTime = connectionTime,
                LastActivity = stats.LastActivity
            };
        }
        
        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfiguration()
        {
            var files = Directory.Exists(_configDirectory)
                ? Directory.GetFiles(_configDirectory, "*.json")
                : Array.Empty<string>();

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var config = JsonSerializer.Deserialize<ProtocolConfiguration>(json);
                    if (config == null || string.IsNullOrWhiteSpace(config.ProtocolId))
                        continue;

                    var protocol = _protocolFactory.CreateProtocol(config);
                    RegisterProtocol(config.ProtocolId, protocol);
                    _logger.Info($"自动加载通信协议配置: {config.ProtocolId} ({config.ProtocolType})");
                }
                catch (Exception ex)
                {
                    _logger.Warn($"跳过无效通信配置文件: {Path.GetFileName(file)}，原因: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 触发通讯状态变更事件
        /// </summary>
        protected virtual void OnCommunicationStatusChanged(CommunicationStatusChangedEventArgs e)
        {
            CommunicationStatusChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// 触发数据接收事件
        /// </summary>
        protected virtual void OnDataReceived(CommunicationDataReceivedEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }
        
        /// <summary>
        /// 触发错误事件
        /// </summary>
        protected virtual void OnErrorOccurred(CommunicationErrorEventArgs e)
        {
            var stats = _stats.GetOrAdd(e.ProtocolId, _ => new ProtocolRuntimeStats());
            stats.TotalErrors++;
            stats.LastActivity = e.Timestamp;

            var message = $"通信错误 [{e.ProtocolId}] {e.ErrorMessage}";
            if (e.Severity == ErrorSeverity.Warning)
                _logger.Warn(message);
            else
                _logger.Error(message, e.Exception);

            CommunicationStatusChanged?.Invoke(this, new CommunicationStatusChangedEventArgs
            {
                ProtocolId = e.ProtocolId,
                IsConnected = false,
                Message = e.ErrorMessage,
                Timestamp = e.Timestamp
            });
        }

        private void OnProtocolConnectionStatusChanged(object? sender, ConnectionStatusChangedEventArgs e)
        {
            var protocolId = FindProtocolId(sender as ProtocolBase);
            var stats = _stats.GetOrAdd(protocolId, _ => new ProtocolRuntimeStats());
            stats.LastActivity = e.Timestamp;
            stats.ConnectedAt = e.IsConnected ? e.Timestamp : null;
            OnCommunicationStatusChanged(new CommunicationStatusChangedEventArgs
            {
                ProtocolId = protocolId,
                IsConnected = e.IsConnected,
                Message = e.Message,
                Timestamp = e.Timestamp
            });
        }

        private void OnProtocolDataReceived(object? sender, DataReceivedEventArgs e)
        {
            var protocolId = FindProtocolId(sender as ProtocolBase);
            var stats = _stats.GetOrAdd(protocolId, _ => new ProtocolRuntimeStats());
            if (e.Source == "Sent")
                stats.TotalBytesSent += e.RawData?.Length ?? 0;
            else
                stats.TotalBytesReceived += e.RawData?.Length ?? 0;
            stats.LastActivity = e.Timestamp;
            OnDataReceived(new CommunicationDataReceivedEventArgs
            {
                ProtocolId = protocolId,
                RawData = e.RawData,
                Direction = e.Source == "Sent" ? DataDirection.Sent : DataDirection.Received,
                Timestamp = e.Timestamp
            });
        }

        private void OnProtocolErrorOccurred(object? sender, ErrorOccurredEventArgs e)
        {
            var protocolId = FindProtocolId(sender as ProtocolBase);
            OnErrorOccurred(new CommunicationErrorEventArgs
            {
                ProtocolId = protocolId,
                ErrorMessage = e.ErrorMessage,
                Exception = e.Exception,
                Severity = e.Severity,
                Timestamp = e.Timestamp
            });
        }

        private string FindProtocolId(ProtocolBase? protocol)
        {
            if (protocol == null)
                return string.Empty;

            foreach (var pair in _protocols)
            {
                if (ReferenceEquals(pair.Value, protocol))
                    return pair.Key;
            }

            return string.Empty;
        }

        private static string NormalizeProtocolId(string protocolId)
        {
            if (string.IsNullOrWhiteSpace(protocolId))
                throw new ArgumentException("协议ID不能为空。", nameof(protocolId));

            var trimmed = protocolId.Trim();
            if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("协议ID包含非法文件名字符。", nameof(protocolId));
            if (trimmed.Contains("..", StringComparison.Ordinal))
                throw new ArgumentException("协议ID不允许包含目录上跳。", nameof(protocolId));

            return trimmed;
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            foreach (var protocol in _protocols.Values)
            {
                protocol.Dispose();
            }
            _protocols.Clear();
            GC.SuppressFinalize(this);
        }
    }
    
    /// <summary>
    /// 通讯管理器接口
    /// </summary>
    public interface ICommunicationManager : IDisposable
    {
        event EventHandler<CommunicationStatusChangedEventArgs> CommunicationStatusChanged;
        event EventHandler<CommunicationDataReceivedEventArgs> DataReceived;
        
        bool RegisterProtocol(string protocolId, ProtocolBase protocol);
        bool UnregisterProtocol(string protocolId);
        ProtocolBase? GetProtocol(string protocolId);
        IEnumerable<KeyValuePair<string, ProtocolBase>> GetAllProtocols();
        
        Task<bool> ConnectAsync(string protocolId);
        Task DisconnectAsync(string protocolId);
        Task<byte[]?> SendReceiveAsync(string protocolId, byte[] data, int timeout = 5000);
        Task<Dictionary<string, byte[]?>> SendReceiveBatchAsync(
            Dictionary<string, byte[]> requests, 
            int timeout = 5000);
        
        Task SaveProtocolConfigurationAsync(string protocolId, ProtocolConfiguration config);
        Task<ProtocolConfiguration?> LoadProtocolConfigurationAsync(string protocolId);
        
        CommunicationStatistics GetStatistics(string protocolId);
    }
    
    /// <summary>
    /// 通讯状态变更事件参数
    /// </summary>
    public class CommunicationStatusChangedEventArgs : EventArgs
    {
        public string ProtocolId { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// 通讯数据接收事件参数
    /// </summary>
    public class CommunicationDataReceivedEventArgs : EventArgs
    {
        public string ProtocolId { get; set; } = string.Empty;
        public byte[] RawData { get; set; } = Array.Empty<byte>();
        public DataDirection Direction { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// 通讯错误事件参数
    /// </summary>
    public class CommunicationErrorEventArgs : EventArgs
    {
        public string ProtocolId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public ErrorSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 数据方向
    /// </summary>
    public enum DataDirection
    {
        Sent,
        Received
    }
    
    /// <summary>
    /// 协议配置
    /// </summary>
    public class ProtocolConfiguration
    {
        public string ProtocolId { get; set; } = string.Empty;
        public ProtocolType ProtocolType { get; set; }
        public InterfaceType InterfaceType { get; set; }
        public string DeviceAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public string Parity { get; set; } = "None";
        public string StopBits { get; set; } = "One";
        public int Timeout { get; set; } = 5000;
        public int RetryCount { get; set; } = 3;
        public bool AutoReconnect { get; set; } = true;
        public int ReconnectInterval { get; set; } = 5000;
        public Dictionary<string, string> CustomSettings { get; set; } = new();
    }
    
    /// <summary>
    /// 通讯统计信息
    /// </summary>
    public class CommunicationStatistics
    {
        public string ProtocolId { get; set; } = string.Empty;
        public long TotalBytesSent { get; set; }
        public long TotalBytesReceived { get; set; }
        public int TotalErrors { get; set; }
        public TimeSpan ConnectionTime { get; set; }
        public DateTime LastActivity { get; set; }
        public double BytesPerSecond => 
            ConnectionTime.TotalSeconds > 0 ? 
            (TotalBytesSent + TotalBytesReceived) / ConnectionTime.TotalSeconds : 0;
    }
}