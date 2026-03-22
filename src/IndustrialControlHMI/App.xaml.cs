using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using IndustrialControlHMI.Infrastructure;
using IndustrialControlHMI.Services;
using IndustrialControlHMI.ViewModels;

namespace IndustrialControlHMI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 服务提供者，用于依赖注入。
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    /// 应用程序启动时触发。
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 配置依赖注入容器
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // 确保数据库已创建
        var dbContext = ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.EnsureDatabaseCreated();

        // 创建主窗口并显示
        var mainWindow = new MainWindow
        {
            DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>()
        };
        mainWindow.Show();
    }

    /// <summary>
    /// 配置依赖注入服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    private void ConfigureServices(IServiceCollection services)
    {
        // 配置管理
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        
        // Modbus配置
        services.AddSingleton<IModbusConfig, DefaultModbusConfig>();

        // Modbus服务
        services.AddSingleton<IModbusService, ModbusService>();

        // 数据库上下文（使用单例，因为SQLite内存数据库需要共享连接）
        services.AddSingleton<AppDbContext>();

        // 存储库
        services.AddSingleton<IAlarmRepository, AlarmRepository>();
        services.AddSingleton<ISettingRepository, SettingRepository>();

        // 设置管理器
        services.AddSingleton<ISettingsManager, SettingsManager>();

        // 视图模型
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<AlarmManagementViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<FlowchartViewModel>();
        
        // 流程图相关服务
        services.AddSingleton<PlcDataBindingService>();
    }
}
