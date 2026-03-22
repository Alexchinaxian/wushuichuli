using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IndustrialControlHMI.ViewModels;

namespace IndustrialControlHMI.Views
{
    /// <summary>
    /// 工艺流程图视图：绑定 FlowchartViewModel，显示 ProcessUnits 与 FlowLines。
    /// </summary>
    public partial class FlowchartView : UserControl
    {
        public FlowchartView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 若未通过绑定得到 DataContext，尝试从父级取 MainWindowViewModel.FlowchartViewModel
            if (DataContext is not FlowchartViewModel)
            {
                var parent = FindVisualParentWithDataContext(this);
                if (parent?.DataContext is MainWindowViewModel mainVm && mainVm.FlowchartViewModel != null)
                {
                    DataContext = mainVm.FlowchartViewModel;
                }
            }

            // 强制布局刷新，确保 ItemsControl 正确测量与排列
            if (ProcessUnitsControl != null)
            {
                ProcessUnitsControl.InvalidateMeasure();
                ProcessUnitsControl.UpdateLayout();
            }
            if (FlowLinesControl != null)
            {
                FlowLinesControl.InvalidateMeasure();
                FlowLinesControl.UpdateLayout();
            }
        }

        private static FrameworkElement? FindVisualParentWithDataContext(DependencyObject child)
        {
            while (child != null)
            {
                child = VisualTreeHelper.GetParent(child);
                if (child is FrameworkElement fe && fe.DataContext != null)
                    return fe;
            }
            return null;
        }
    }
}
