using System;
using System.Threading.Tasks;

namespace IndustrialControlHMI.Services.Communication
{
    /// <summary>
    /// 通讯协议基类，定义所有协议共有的接口
    /// </summary>
    public abstract class ProtocolBase : IDisposable
    {
        /// <summary>
        /// 协议名称
        /// </summary>
        public abstract string ProtocolName { get; }
        
        /// <summary>
        /// 协议版本
        /// </summary>
        public abstract string ProtocolVersion { get; }
        
        /// <summary>
        /// 是否已连接
        /// </summary>
        public abstract bool IsConnected { get; }
        
        /// <summary>
        /// 连接状态变更事件
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        
        /// <summary>
        /// 数据接收事件
        /// </summary>
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        
        /// <summary>
        /// 错误发生事件
        /// </summary>
        public event EventHandler<ErrorOccurredEventArgs> ErrorOccurred;
        
        /// <summary>
        /// 连接设备
        /// </summary>
        public abstract Task<bool> ConnectAsync();
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public abstract Task DisconnectAsync();
        
        /// <summary>
        /// 发送数据
        /// </summary>
        public abstract Task<byte[]> SendReceiveAsync(byte[] data, int timeout = 5000);
        
        /// <summary>
        /// 发送数据（无响应）
        /// </summary>
        public abstract Task SendAsync(byte[] data);
        
        /// <summary>
        /// 读取数据
        /// </summary>
        public abstract Task<byte[]> ReadAsync(int length, int timeout = 5000);
        
        /// <summary>
        /// 写入数据
        /// </summary>
        public abstract Task WriteAsync(byte[] data);
        
        /// <summary>
        /// 验证数据包
        /// </summary>
        protected abstract bool ValidatePacket(byte[] data);
        
        /// <summary>
        /// 计算校验和
        /// </summary>
        protected abstract byte[] CalculateChecksum(byte[] data);
        
        /// <summary>
        /// 触发连接状态变更事件
        /// </summary>
        protected virtual void OnConnectionStatusChanged(ConnectionStatusChangedEventArgs e)
        {
            ConnectionStatusChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// 触发数据接收事件
        /// </summary>
        protected virtual void OnDataReceived(DataReceivedEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }
        
        /// <summary>
        /// 触发错误事件
        /// </summary>
        protected virtual void OnErrorOccurred(ErrorOccurredEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public abstract void Dispose();
    }
    
    /// <summary>
    /// 连接状态变更事件参数
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string DeviceAddress { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 数据接收事件参数
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] RawData { get; set; } = Array.Empty<byte>();
        public string HexData { get; set; } = string.Empty;
        public string AsciiData { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Source { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 错误事件参数
    /// </summary>
    public class ErrorOccurredEventArgs : EventArgs
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
    }
    
    /// <summary>
    /// 错误严重程度
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
    
    /// <summary>
    /// 通讯协议类型
    /// </summary>
    public enum ProtocolType
    {
        ModbusTCP,
        ModbusRTU,
        ModbusASCII,
        SiemensS7,
        MitsubishiMC,
        OmronFINS,
        Profinet,
        EthernetIP,
        CANOpen,
        MQTT,
        OPCUA,
        Custom
    }
    
    /// <summary>
    /// 通讯接口类型
    /// </summary>
    public enum InterfaceType
    {
        Ethernet,
        Serial,
        USB,
        CAN,
        Bluetooth,
        WiFi
    }
    
    /// <summary>
    /// 数据格式
    /// </summary>
    public enum DataFormat
    {
        Binary,
        ASCII,
        Hex,
        JSON,
        XML
    }
}