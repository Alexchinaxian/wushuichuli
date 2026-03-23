using System;

namespace IndustrialControlHMI.Services.Logging;

/// <summary>
/// 应用日志抽象，便于统一记录和后续替换日志实现。
/// </summary>
public interface IAppLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
}

