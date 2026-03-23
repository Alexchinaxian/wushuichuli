using System;
using System.IO;

namespace IndustrialControlHMI.Infrastructure.Config;

/// <summary>
/// 应用配置路径统一入口，支持环境变量覆盖。
/// </summary>
public static class AppConfigPaths
{
    private const string ConfigDirEnvName = "INDUSTRIAL_HMI_CONFIG_DIR";

    public static string GetConfigDirectory()
    {
        var envDir = Environment.GetEnvironmentVariable(ConfigDirEnvName);
        var configDir = !string.IsNullOrWhiteSpace(envDir)
            ? envDir
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

        Directory.CreateDirectory(configDir);
        return configDir;
    }

    public static string GetConfigFilePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("配置文件名不能为空。", nameof(fileName));

        return Path.Combine(GetConfigDirectory(), fileName);
    }
}

