using System;
using Microsoft.Extensions.Logging;

namespace IndustrialControlHMI.Services.Logging;

/// <summary>
/// 基于 Microsoft.Extensions.Logging 的日志实现。
/// </summary>
public sealed class AppLogger : IAppLogger
{
    private readonly ILogger<AppLogger> _logger;

    public AppLogger(ILogger<AppLogger> logger)
    {
        _logger = logger;
    }

    public void Info(string message) => _logger.LogInformation("{Message}", message);

    public void Warn(string message) => _logger.LogWarning("{Message}", message);

    public void Error(string message, Exception? exception = null)
        => _logger.LogError(exception, "{Message}", message);
}

