using System;

namespace IndustrialControlHMI.Models
{
    /// <summary>
    /// 导航项数据模型。
    /// </summary>
    public class NavigationItem
    {
        /// <summary>
        /// 导航项唯一标识。
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 图标字符（Segoe MDL2 Assets）。
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// 显示文本。
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 描述信息。
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 对应视图模型类型。
        /// </summary>
        public Type ViewModelType { get; set; } = null!;
    }
}