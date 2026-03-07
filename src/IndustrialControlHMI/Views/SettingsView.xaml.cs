using System.Windows.Controls;

namespace IndustrialControlHMI.Views;

/// <summary>
/// SettingsView.xaml 的交互逻辑。
/// DataContext 由主窗口通过 ContentControl + DataTemplate 注入。
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
}
