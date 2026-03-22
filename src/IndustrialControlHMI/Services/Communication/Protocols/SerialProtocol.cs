using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace IndustrialControlHMI.Services.Communication.Protocols
{
    /// <summary>
    /// 串口通讯协议基类
    /// </summary>
    public abstract class SerialProtocol : ProtocolBase
    {
        protected SerialPort? _serialPort;
        protected readonly string _portName;
        protected readonly int _baudRate;
        protected readonly Parity _parity;
        protected readonly int _dataBits;
        protected readonly StopBits _stopBits;
        protected readonly int _timeout;
        protected readonly SemaphoreSlim _semaphore = new(1, 1);
        
        protected SerialProtocol(
            string portName,
            int baudRate = 9600,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One,
            int timeout = 5000)
        {
            _portName = portName ?? throw new ArgumentNullException(nameof(portName));
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _timeout = timeout;
        }
        
        public override bool IsConnected => _serialPort?.IsOpen == true;
        
        /// <summary>
        /// 连接设备
        /// </summary>
        public override async Task<bool> ConnectAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (IsConnected)
                    return true;
                
                _serialPort = new SerialPort(_portName, _baudRate, _parity, _dataBits, _stopBits)
                {
                    ReadTimeout = _timeout,
                    WriteTimeout = _timeout,
                    Handshake = Handshake.None,
                    RtsEnable = true,
                    DtrEnable = true
                };
                
                _serialPort.Open();
                
                OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs
                {
                    IsConnected = true,
                    DeviceAddress = _portName,
                    Message = $"串口连接成功: {_portName}@{_baudRate}bps"
                });
                
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorOccurredEventArgs
                {
                    ErrorCode = "SERIAL_CONNECT_FAILED",
                    ErrorMessage = $"串口连接失败: {ex.Message}",
                    Exception = ex,
                    Severity = ErrorSeverity.Error
                });
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public override async Task DisconnectAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;
                }
                
                OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs
                {
                    IsConnected = false,
                    DeviceAddress = _portName,
                    Message = "串口已断开连接"
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorOccurredEventArgs
                {
                    ErrorCode = "SERIAL_DISCONNECT_FAILED",
                    ErrorMessage = $"串口断开连接失败: {ex.Message}",
                    Exception = ex,
                    Severity = ErrorSeverity.Warning
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// 发送接收数据
        /// </summary>
        public override async Task<byte[]> SendReceiveAsync(byte[] data, int timeout = 5000)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!IsConnected || _serialPort == null)
                    throw new InvalidOperationException("串口未连接");
                
                // 清空接收缓冲区
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                
                // 发送数据
                await _serialPort.BaseStream.WriteAsync(data, 0, data.Length);
                await _serialPort.BaseStream.FlushAsync();
                
                OnDataReceived(new DataReceivedEventArgs
                {
                    RawData = data,
                    HexData = BitConverter.ToString(data).Replace("-", " "),
                    Source = "Sent",
                    Timestamp = DateTime.Now
                });
                
                // 接收响应
                var response = await ReadResponseAsync(timeout);
                
                OnDataReceived(new DataReceivedEventArgs
                {
                    RawData = response,
                    HexData = BitConverter.ToString(response).Replace("-", " "),
                    Source = "Received",
                    Timestamp = DateTime.Now
                });
                
                return response;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorOccurredEventArgs
                {
                    ErrorCode = "SERIAL_SEND_RECEIVE_FAILED",
                    ErrorMessage = $"串口发送接收失败: {ex.Message}",
                    Exception = ex,
                    Severity = ErrorSeverity.Error
                });
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// 发送数据（无响应）
        /// </summary>
        public override async Task SendAsync(byte[] data)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!IsConnected || _serialPort == null)
                    throw new InvalidOperationException("串口未连接");
                
                await _serialPort.BaseStream.WriteAsync(data, 0, data.Length);
                await _serialPort.BaseStream.FlushAsync();
                
                OnDataReceived(new DataReceivedEventArgs
                {
                    RawData = data,
                    HexData = BitConverter.ToString(data).Replace("-", " "),
                    Source = "Sent",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorOccurredEventArgs
                {
                    ErrorCode = "SERIAL_SEND_FAILED",
                    ErrorMessage = $"串口发送失败: {ex.Message}",
                    Exception = ex,
                    Severity = ErrorSeverity.Error
                });
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// 读取数据
        /// </summary>
        public override async Task<byte[]> ReadAsync(int length, int timeout = 5000)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!IsConnected || _serialPort == null)
                    throw new InvalidOperationException("串口未连接");
                
                var buffer = new byte[length];
                var bytesRead = await _serialPort.BaseStream.ReadAsync(buffer, 0, length);
                
                if (bytesRead == 0)
                    throw new IOException("串口连接已关闭");
                
                var response = new byte[bytesRead];
                Array.Copy(buffer, response, bytesRead);
                
                OnDataReceived(new DataReceivedEventArgs
                {
                    RawData = response,
                    HexData = BitConverter.ToString(response).Replace("-", " "),
                    Source = "Received",
                    Timestamp = DateTime.Now
                });
                
                return response;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorOccurredEventArgs
                {
                    ErrorCode = "SERIAL_READ_FAILED",
                    ErrorMessage = $"串口读取失败: {ex.Message}",
                    Exception = ex,
                    Severity = ErrorSeverity.Error
                });
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// 写入数据
        /// </summary>
        public override Task WriteAsync(byte[] data)
        {
            return SendAsync(data);
        }
        
        /// <summary>
        /// 读取响应（需要子类实现具体协议）
        /// </summary>
        protected abstract Task<byte[]> ReadResponseAsync(int timeout);
        
        /// <summary>
        /// 获取可用串口列表
        /// </summary>
        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Dispose()
        {
            _semaphore?.Dispose();
            _serialPort?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
    
    /// <summary>
    /// Modbus RTU 协议
    /// </summary>
    public class ModbusRtuProtocol : SerialProtocol
    {
        private readonly int _slaveId;
        private ushort _crc;
        
        public ModbusRtuProtocol(
            string portName,
            int slaveId = 1,
            int baudRate = 9600,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One,
            int timeout = 5000)
            : base(portName, baudRate, parity, dataBits, stopBits, timeout)
        {
            _slaveId = slaveId;
        }
        
        public override string ProtocolName => "Modbus RTU";
        public override string ProtocolVersion => "1.0";
        
        /// <summary>
        /// 读取保持寄存器
        /// </summary>
        public async Task<byte[]> ReadHoldingRegistersAsync(ushort startAddress, ushort quantity)
        {
            var pdu = new byte[5];
            pdu[0] = (byte)_slaveId;
            pdu[1] = 0x03; // 功能码: 读取保持寄存器
            pdu[2] = (byte)(startAddress >> 8);
            pdu[3] = (byte)(startAddress & 0xFF);
            pdu[4] = (byte)(quantity >> 8);
            pdu[5] = (byte)(quantity & 0xFF);
            
            var crc = CalculateCRC16(pdu, 0, 6);
            pdu[6] = (byte)(crc & 0xFF);
            pdu[7] = (byte)(crc >> 8);
            
            return await SendReceiveAsync(pdu);
        }
        
        /// <summary>
        /// 写入单个寄存器
        /// </summary>
        public async Task<byte[]> WriteSingleRegisterAsync(ushort address, ushort value)
        {
            var pdu = new byte[6];
            pdu[0] = (byte)_slaveId;
            pdu[1] = 0x06; // 功能码: 写入单个寄存器
            pdu[2] = (byte)(address >> 8);
            pdu[3] = (byte)(address & 0xFF);
            pdu[4] = (byte)(value >> 8);
            pdu[5] = (byte)(value & 0xFF);
            
            var crc = CalculateCRC16(pdu, 0, 6);
            pdu[6] = (byte)(crc & 0xFF);
            pdu[7] = (byte)(crc >> 8);
            
            return await SendReceiveAsync(pdu);
        }
        
        /// <summary>
        /// 读取响应
        /// </summary>
        protected override async Task<byte[]> ReadResponseAsync(int timeout)
        {
            if (_serialPort == null)
                throw new InvalidOperationException("串口未初始化");
            
            // 读取从站ID和功能码
            var header = new byte[2];
            var bytesRead = await _serialPort.BaseStream.ReadAsync(header, 0, 2);
            
            if (bytesRead != 2)
                throw new IOException("读取响应头部失败");
            
            // 检查错误响应
            var functionCode = header[1];
            if ((functionCode & 0x80) != 0)
            {
                // 错误响应，读取错误码
                var errorData = new byte[3]; // 错误码 + CRC
                bytesRead = await _serialPort.BaseStream.ReadAsync(errorData, 0, 3);
                
                if (bytesRead != 3)
                    throw new IOException("读取错误响应失败");
                
                var errorCode = errorData[0];
                var errorMessage = GetModbusErrorMessage(errorCode);
                throw new InvalidOperationException($"Modbus RTU 错误: {errorMessage} (代码: {errorCode})");
            }
            
            // 正常响应，根据功能码确定数据长度
            int dataLength = functionCode switch
            {
                0x03 => await ReadHoldingRegistersResponse(),
                0x06 => await WriteSingleRegisterResponse(),
                _ => throw new NotSupportedException($"不支持的功能码: {functionCode}")
            };
            
            // 读取CRC
            var crcBytes = new byte[2];
            bytesRead = await _serialPort.BaseStream.ReadAsync(crcBytes, 0, 2);
            
            if (bytesRead != 2)
                throw new IOException("读取CRC失败");
            
            // 构建完整响应
            var response = new byte[2 + dataLength + 2];
            Array.Copy(header, 0, response, 0, 2);
            // 这里需要根据实际读取的数据填充 response[2..]
            Array.Copy(crcBytes, 0, response, 2 + dataLength, 2);
            
            return response;
        }
        
        /// <summary>
        /// 读取保持寄存器响应
        /// </summary>
        private async Task<int> ReadHoldingRegistersResponse()
        {
            if (_serialPort == null)
                return 0;
            
            // 读取字节数
            var byteCountBuffer = new byte[1];
            var bytesRead = await _serialPort.BaseStream.ReadAsync(byteCountBuffer, 0, 1);
            
            if (bytesRead != 1)
                throw new IOException("读取字节数失败");
            
            var byteCount = byteCountBuffer[0];
            
            // 读取寄存器数据
            var dataBuffer = new byte[byteCount];
            bytesRead = await _serialPort.BaseStream.ReadAsync(dataBuffer, 0, byteCount);
            
            if (bytesRead != byteCount)
                throw new IOException("读取寄存器数据失败");
            
            return 1 + byteCount; // 字节数 + 数据
        }
        
        /// <summary>
        /// 写入单个寄存器响应
        /// </summary>
        private async Task<int> WriteSingleRegisterResponse()
        {
            if (_serialPort == null)
                return 0;
            
            // 写入单个寄存器的响应与请求相同
            var dataBuffer = new byte[4]; // 地址(2) + 值(2)
            var bytesRead = await _serialPort.BaseStream.ReadAsync(dataBuffer, 0, 4);
            
            if (bytesRead != 4)
                throw new IOException("读取写入响应失败");
            
            return 4;
        }
        
        /// <summary>
        /// 计算 CRC16 校验码
        /// </summary>
        private ushort CalculateCRC16(byte[] data, int offset, int length)
        {
            ushort crc = 0xFFFF;
            
            for (int i = offset; i < offset + length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            
            return crc;
        }
        
        /// <summary>
        /// 获取 Modbus 错误消息
        /// </summary>
        private string GetModbusErrorMessage(byte errorCode)
        {
            return errorCode switch
            {
                0x01 => "非法功能码",
                0x02 => "非法数据地址",
                0x03 => "非法数据值",
                0x04 => "从站设备故障",
                0x05 => "确认",
                0x06 => "从站设备忙",
                0x07 => "负确认",
                0x08 => "存储奇偶性差错",
                0x0A => "网关路径不可用",
                0x0B => "网关目标设备响应失败",
                _ => $"未知错误 (代码: {errorCode})"
            };
        }
        
        /// <summary>
        /// 验证数据包
        /// </summary>
        protected override bool ValidatePacket(byte[] data)
        {
            if (data.Length < 4) // 从站ID + 功能码 + 至少2字节数据 + 2字节CRC
                return false;
            
            // 验证CRC
            var crc = CalculateCRC16(data, 0, data.Length - 2);
            var receivedCrc = (ushort)((data[data.Length - 1] << 8) | data[data.Length - 2]);
            
            return crc == receivedCrc;
        }
        
        /// <summary>
        /// 计算校验和
        /// </summary>
        protected override byte[] CalculateChecksum(byte[] data)
        {
            var crc = CalculateCRC16(data, 0, data.Length);
            return new byte[] { (byte)(crc & 0xFF), (byte)(crc >> 8) };
        }
    }
}