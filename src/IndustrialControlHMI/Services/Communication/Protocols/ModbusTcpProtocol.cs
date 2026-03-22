using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace IndustrialControlHMI.Services.Communication.Protocols
{
    /// <summary>
    /// Modbus TCP 协议实现
    /// </summary>
    public class ModbusTcpProtocol : ProtocolBase
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly int _slaveId;
        private readonly int _timeout;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private ushort _transactionId = 0;
        
        public ModbusTcpProtocol(string ipAddress, int port = 502, int slaveId = 1, int timeout = 5000)
        {
            _ipAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            _port = port;
            _slaveId = slaveId;
            _timeout = timeout;
        }
        
        public override string ProtocolName => "Modbus TCP";
        public override string ProtocolVersion => "1.0";
        public override bool IsConnected => _tcpClient?.Connected == true;
        
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
                
                _tcpClient = new TcpClient
                {
                    SendTimeout = _timeout,
                    ReceiveTimeout = _timeout
                };
                
                var connectTask = _tcpClient.ConnectAsync(_ipAddress, _port);
                var timeoutTask = Task.Delay(_timeout);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    OnErrorOccurred(new ErrorOccurredEventArgs
                    {
                        ErrorCode = "CONNECT_TIMEOUT",
                        ErrorMessage = $"连接超时: {_ipAddress}:{_port}",
                        Severity = ErrorSeverity.Error
                    });
                    return false;
                }
                
                await connectTask;
                _networkStream = _tcpClient.GetStream();
                
                OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs
                {
                    IsConnected = true,
                    DeviceAddress = $"{_ipAddress}:{_port}",
                    Message = "Modbus TCP 连接成功"
                });
                
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorOccurredEventArgs
                {
                    ErrorCode = "CONNECT_FAILED",
                    ErrorMessage = $"连接失败: {ex.Message}",
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
                if (_networkStream != null)
                {
                    await _networkStream.FlushAsync();
                    _networkStream.Close();
                    _networkStream = null;
                }
                
                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient = null;
                }
                
                OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs
                {
                    IsConnected = false,
                    DeviceAddress = $"{_ipAddress}:{_port}",
                    Message = "Modbus TCP 已断开连接"
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorOccurredEventArgs
                {
                    ErrorCode = "DISCONNECT_FAILED",
                    ErrorMessage = $"断开连接失败: {ex.Message}",
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
                if (!IsConnected || _networkStream == null)
                    throw new InvalidOperationException("未连接到设备");
                
                // 构建 Modbus TCP 帧
                var request = BuildModbusFrame(data);
                
                // 发送数据
                await _networkStream.WriteAsync(request, 0, request.Length);
                
                OnDataReceived(new DataReceivedEventArgs
                {
                    RawData = request,
                    HexData = BitConverter.ToString(request).Replace("-", " "),
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
                
                // 解析 Modbus TCP 响应
                return ParseModbusResponse(response);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorOccurredEventArgs
                {
                    ErrorCode = "SEND_RECEIVE_FAILED",
                    ErrorMessage = $"发送接收失败: {ex.Message}",
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
                if (!IsConnected || _networkStream == null)
                    throw new InvalidOperationException("未连接到设备");
                
                var request = BuildModbusFrame(data);
                await _networkStream.WriteAsync(request, 0, request.Length);
                
                OnDataReceived(new DataReceivedEventArgs
                {
                    RawData = request,
                    HexData = BitConverter.ToString(request).Replace("-", " "),
                    Source = "Sent",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorOccurredEventArgs
                {
                    ErrorCode = "SEND_FAILED",
                    ErrorMessage = $"发送失败: {ex.Message}",
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
                if (!IsConnected || _networkStream == null)
                    throw new InvalidOperationException("未连接到设备");
                
                var buffer = new byte[length];
                var bytesRead = await _networkStream.ReadAsync(buffer, 0, length);
                
                if (bytesRead == 0)
                    throw new IOException("连接已关闭");
                
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
                    ErrorCode = "READ_FAILED",
                    ErrorMessage = $"读取失败: {ex.Message}",
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
        /// 读取保持寄存器
        /// </summary>
        public async Task<byte[]> ReadHoldingRegistersAsync(ushort startAddress, ushort quantity)
        {
            var pdu = new byte[5];
            pdu[0] = 0x03; // 功能码: 读取保持寄存器
            pdu[1] = (byte)(startAddress >> 8);
            pdu[2] = (byte)(startAddress & 0xFF);
            pdu[3] = (byte)(quantity >> 8);
            pdu[4] = (byte)(quantity & 0xFF);
            
            return await SendReceiveAsync(pdu);
        }
        
        /// <summary>
        /// 写入单个寄存器
        /// </summary>
        public async Task<byte[]> WriteSingleRegisterAsync(ushort address, ushort value)
        {
            var pdu = new byte[5];
            pdu[0] = 0x06; // 功能码: 写入单个寄存器
            pdu[1] = (byte)(address >> 8);
            pdu[2] = (byte)(address & 0xFF);
            pdu[3] = (byte)(value >> 8);
            pdu[4] = (byte)(value & 0xFF);
            
            return await SendReceiveAsync(pdu);
        }
        
        /// <summary>
        /// 写入多个寄存器
        /// </summary>
        public async Task<byte[]> WriteMultipleRegistersAsync(ushort startAddress, ushort[] values)
        {
            var byteCount = values.Length * 2;
            var pdu = new byte[6 + byteCount];
            
            pdu[0] = 0x10; // 功能码: 写入多个寄存器
            pdu[1] = (byte)(startAddress >> 8);
            pdu[2] = (byte)(startAddress & 0xFF);
            pdu[3] = (byte)(values.Length >> 8);
            pdu[4] = (byte)(values.Length & 0xFF);
            pdu[5] = (byte)byteCount;
            
            for (int i = 0; i < values.Length; i++)
            {
                pdu[6 + i * 2] = (byte)(values[i] >> 8);
                pdu[7 + i * 2] = (byte)(values[i] & 0xFF);
            }
            
            return await SendReceiveAsync(pdu);
        }
        
        /// <summary>
        /// 构建 Modbus TCP 帧
        /// </summary>
        private byte[] BuildModbusFrame(byte[] pdu)
        {
            var transactionId = _transactionId++;
            var frame = new byte[7 + pdu.Length];
            
            // MBAP 头部
            frame[0] = (byte)(transactionId >> 8);      // 事务标识符高字节
            frame[1] = (byte)(transactionId & 0xFF);    // 事务标识符低字节
            frame[2] = 0x00;                            // 协议标识符高字节
            frame[3] = 0x00;                            // 协议标识符低字节
            frame[4] = (byte)((pdu.Length + 1) >> 8);   // 长度高字节
            frame[5] = (byte)((pdu.Length + 1) & 0xFF); // 长度低字节
            frame[6] = (byte)_slaveId;                  // 单元标识符
            
            // PDU
            Array.Copy(pdu, 0, frame, 7, pdu.Length);
            
            return frame;
        }
        
        /// <summary>
        /// 读取响应
        /// </summary>
        private async Task<byte[]> ReadResponseAsync(int timeout)
        {
            if (_networkStream == null)
                throw new InvalidOperationException("网络流未初始化");
            
            // 读取 MBAP 头部 (7字节)
            var header = new byte[7];
            var bytesRead = await _networkStream.ReadAsync(header, 0, 7);
            
            if (bytesRead != 7)
                throw new IOException("读取响应头部失败");
            
            // 获取数据长度
            var length = (header[4] << 8) | header[5];
            
            // 读取剩余数据
            var data = new byte[length - 1]; // 减去单元标识符
            bytesRead = await _networkStream.ReadAsync(data, 0, data.Length);
            
            if (bytesRead != data.Length)
                throw new IOException("读取响应数据失败");
            
            // 合并头部和数据
            var response = new byte[7 + data.Length];
            Array.Copy(header, 0, response, 0, 7);
            Array.Copy(data, 0, response, 7, data.Length);
            
            return response;
        }
        
        /// <summary>
        /// 解析 Modbus 响应
        /// </summary>
        private byte[] ParseModbusResponse(byte[] response)
        {
            if (response.Length < 9) // MBAP头部(7) + 功能码(1) + 至少1字节数据
                throw new ArgumentException("响应数据长度不足");
            
            // 检查错误响应
            var functionCode = response[7];
            if ((functionCode & 0x80) != 0)
            {
                var errorCode = response[8];
                var errorMessage = GetModbusErrorMessage(errorCode);
                throw new InvalidOperationException($"Modbus 错误: {errorMessage} (代码: {errorCode})");
            }
            
            // 返回 PDU 部分
            var pduLength = response.Length - 7;
            var pdu = new byte[pduLength];
            Array.Copy(response, 7, pdu, 0, pduLength);
            
            return pdu;
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
            if (data.Length < 7)
                return false;
            
            // 检查 MBAP 头部
            var length = (data[4] << 8) | data[5];
            return data.Length == length + 6; // 长度字段包含单元标识符
        }
        
        /// <summary>
        /// 计算校验和（Modbus TCP 不需要）
        /// </summary>
        protected override byte[] CalculateChecksum(byte[] data)
        {
            return Array.Empty<byte>();
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Dispose()
        {
            _semaphore?.Dispose();
            _networkStream?.Dispose();
            _tcpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}