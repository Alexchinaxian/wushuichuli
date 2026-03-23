using System;
using System.Globalization;
using System.Windows.Data;
using IndustrialControlHMI.ViewModels;

namespace IndustrialControlHMI.Converters;

public sealed class HistoryExportFormatToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not HistoryDataExportFormat format)
            return value?.ToString() ?? string.Empty;

        return format switch
        {
            HistoryDataExportFormat.CSV => "CSV(GBK，Excel可能乱码)",
            HistoryDataExportFormat.CSVExcelAuto => "CSV(Excel自动识别)",
            HistoryDataExportFormat.Excel => "Excel(xlsx)",
            HistoryDataExportFormat.XML => "XML(GBK)",
            _ => format.ToString()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

