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

    private void FlowchartView_Loaded(object sender, RoutedEventArgs e)
    {

    }
}
