using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using IndustrialControlHMI.Infrastructure;
using IndustrialControlHMI.Services;
using IndustrialControlHMI.Services.Communication;
using IndustrialControlHMI.Services.Database;
using IndustrialControlHMI.Services.S7;
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

        var dbContext = ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.EnsureDatabaseCreated();
        ZhongxinSewageDatabaseInitializer.Initialize(dbContext);

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

        services.AddSingleton<AppDbContext>();

        services.AddSingleton<IAlarmRepository, AlarmRepository>();
        services.AddSingleton<ISettingRepository, SettingRepository>();
        services.AddSingleton<IPointHistoryRepository, PointHistoryRepository>();

        // 设置管理器
        services.AddSingleton<ISettingsManager, SettingsManager>();

        // 通讯服务
        services.AddCommunicationServices();

        // 视图模型
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<AlarmManagementViewModel>();
        services.AddTransient<HistoryDataViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<FlowchartViewModel>();
        services.AddTransient<CommunicationViewModel>();
        services.AddTransient<S7MonitorViewModel>();
        
        // 流程图相关服务
        services.AddSingleton<PlcDataBindingService>();

        // 西门子 S7（中信污水点位表）
        services.AddSingleton<IS7RuntimeService, S7RuntimeService>();
    }
}
