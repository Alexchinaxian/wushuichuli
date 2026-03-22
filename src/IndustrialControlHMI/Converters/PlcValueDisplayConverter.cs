using System;
using System.Globalization;
using System.Windows.Data;

namespace IndustrialControlHMI.Converters;

/// <summary>
/// PLC 点位当前值在表格中的友好显示（布尔/数值/null）。
/// </summary>
public class PlcValueDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "—";

        return value switch
        {
            bool b => b ? "1 (开)" : "0 (关)",
            float f => f.ToString("0.###", culture),
            double d => d.ToString("0.###", culture),
            byte by => by.ToString(culture),
            int i => i.ToString(culture),
            uint u => u.ToString(culture),
            _ => value.ToString() ?? "—"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
