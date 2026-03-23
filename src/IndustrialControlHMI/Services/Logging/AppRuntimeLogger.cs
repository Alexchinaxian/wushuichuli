using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialControlHMI.Services.Logging;

/// <summary>
/// 运行时静态日志桥接，供非 DI 创建对象使用。
/// </summary>
public static class AppRuntimeLogger
{
    public static void Info(string message)
    {
        var logger = TryGetLogger();
        if (logger != null) logger.Info(message);
        else Trace.WriteLine($"[INFO] {message}");
    }

    public static void Warn(string message)
    {
        var logger = TryGetLogger();
        if (logger != null) logger.Warn(message);
        else Trace.WriteLine($"[WARN] {message}");
    }

    public static void Error(string message, Exception? ex = null)
    {
        var logger = TryGetLogger();
        if (logger != null) logger.Error(message, ex);
        else Trace.WriteLine($"[ERROR] {message} {ex?.Message}");
    }

    private static IAppLogger? TryGetLogger()
    {
        var app = System.Windows.Application.Current as App;
        return app?.ServiceProvider.GetService<IAppLogger>();
    }
}

