using CommunityToolkit.Mvvm.ComponentModel;

namespace IndustrialControlHMI.Models;

/// <summary>
/// 多选下拉/列表项模型（用于绑定 CheckBox 的 IsSelected）。
/// </summary>
public sealed partial class SelectableOption : ObservableObject
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;
}

