using System;
using System.IO;

namespace IndustrialControlHMI.Infrastructure.Database;

/// <summary>
/// 中信污水项目 SQLite 文件路径与连接串。
/// </summary>
public static class ZhongxinSewageConnection
{
    private const string DbFileName = "zhongxin_sewage.db";

    /// <summary>
    /// %LocalAppData%\IndustrialControlHMI\zhongxin_sewage.db
    /// </summary>
    public static string GetDatabaseFilePath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IndustrialControlHMI");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, DbFileName);
    }

    public static string GetConnectionString() => $"Data Source={GetDatabaseFilePath()}";
}
