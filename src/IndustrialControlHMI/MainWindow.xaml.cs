using System;
using System.Diagnostics;
using System.Windows;
using IndustrialControlHMI.ViewModels;

namespace IndustrialControlHMI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            try
            {
                await vm.InitializeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"主窗口初始化失败: {ex.Message}");
            }
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // FlowchartView加载事件处理 - 已移除空实现
    // 如果需要添加加载完成后的逻辑，可以在这里添加
}
