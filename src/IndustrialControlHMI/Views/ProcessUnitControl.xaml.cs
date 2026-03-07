using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlHMI.Models.Flowchart;
using IndustrialControlHMI.ViewModels;

namespace IndustrialControlHMI.Views;

/// <summary>
/// ProcessUnitControl.xaml 的交互逻辑
/// 设备位置固定，不支持拖动
/// </summary>
public partial class ProcessUnitControl : UserControl
{
    public static readonly DependencyProperty UnitModelProperty = 
        DependencyProperty.Register("UnitModel", typeof(ProcessUnitModel), typeof(ProcessUnitControl),
            new PropertyMetadata(null, OnUnitModelChanged));
    
    public static readonly DependencyProperty ClickCommandProperty =
        DependencyProperty.Register("ClickCommand", typeof(ICommand), typeof(ProcessUnitControl));
    
    public static readonly DependencyProperty ViewDetailsCommandProperty =
        DependencyProperty.Register("ViewDetailsCommand", typeof(ICommand), typeof(ProcessUnitControl));
    
    public static readonly DependencyProperty SimulateFaultCommandProperty =
        DependencyProperty.Register("SimulateFaultCommand", typeof(ICommand), typeof(ProcessUnitControl));
    
    public static readonly DependencyProperty ResetFaultCommandProperty =
        DependencyProperty.Register("ResetFaultCommand", typeof(ICommand), typeof(ProcessUnitControl));
    
    /// <summary>
    /// 获取或设置处理单元模型
    /// </summary>
    public ProcessUnitModel? UnitModel
    {
        get => (ProcessUnitModel?)GetValue(UnitModelProperty);
        set => SetValue(UnitModelProperty, value);
    }
    
    /// <summary>
    /// 获取或设置点击命令
    /// </summary>
    public ICommand? ClickCommand
    {
        get => (ICommand?)GetValue(ClickCommandProperty);
        set => SetValue(ClickCommandProperty, value);
    }
    
    /// <summary>
    /// 获取或设置查看详情命令
    /// </summary>
    public ICommand? ViewDetailsCommand
    {
        get => (ICommand?)GetValue(ViewDetailsCommandProperty);
        set => SetValue(ViewDetailsCommandProperty, value);
    }
    
    /// <summary>
    /// 获取或设置模拟故障命令
    /// </summary>
    public ICommand? SimulateFaultCommand
    {
        get => (ICommand?)GetValue(SimulateFaultCommandProperty);
        set => SetValue(SimulateFaultCommandProperty, value);
    }
    
    /// <summary>
    /// 获取或设置复位故障命令
    /// </summary>
    public ICommand? ResetFaultCommand
    {
        get => (ICommand?)GetValue(ResetFaultCommandProperty);
        set => SetValue(ResetFaultCommandProperty, value);
    }
    
    public ProcessUnitControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (UnitModel != null)
        {
            DataContext = UnitModel;
        }
    }
    
    private static void OnUnitModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ProcessUnitControl)d;
        control.DataContext = e.NewValue;
    }
    
    /// <summary>
    /// 鼠标点击事件处理 - 仅触发点击命令，设备位置固定
    /// </summary>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        
        if (UnitModel != null && ClickCommand != null && ClickCommand.CanExecute(UnitModel))
        {
            ClickCommand.Execute(UnitModel);
        }
    }
    
    /// <summary>
    /// 鼠标悬停时改变边框颜色
    /// </summary>
    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        
        if (UnitModel != null && !UnitModel.IsActive)
        {
            UnitModel.IsHovered = true;
        }
    }
    
    /// <summary>
    /// 鼠标离开时恢复边框颜色
    /// </summary>
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        
        if (UnitModel != null && !UnitModel.IsActive)
        {
            UnitModel.IsHovered = false;
        }
    }
}
