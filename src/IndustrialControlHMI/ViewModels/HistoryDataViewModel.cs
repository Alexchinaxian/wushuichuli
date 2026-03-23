using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlHMI.Models;
using IndustrialControlHMI.Models.Database;
using IndustrialControlHMI.Services.Database;
using IndustrialControlHMI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IndustrialControlHMI.ViewModels;

public enum HistoryDataExportFormat
{
    CSV,
    CSVExcelAuto,
    Excel,
    XML
}

public partial class HistoryDataViewModel : ObservableObject, IDisposable
{
    private const int PreviewMaxRowsDefault = 100;
    private const int ExportSimulatedMaxRowsDefault = 2000;

    private readonly AppDbContext _db;
    private readonly IPointHistoryRepository _pointHistoryRepository;

    private CancellationTokenSource? _exportCts;

    public HistoryDataViewModel(AppDbContext db, IPointHistoryRepository pointHistoryRepository)
    {
        _db = db;
        _pointHistoryRepository = pointHistoryRepository;

        ExportFormatOptions = new ObservableCollection<HistoryDataExportFormat>(
            Enum.GetValues(typeof(HistoryDataExportFormat)).Cast<HistoryDataExportFormat>());

        PreviewRows = new ObservableCollection<HistoryDataRow>();
        ProcessUnitOptions = new ObservableCollection<SelectableOption>();
        ParameterOptions = new ObservableCollection<SelectableOption>();

        LoadOptionsCommand = new AsyncRelayCommand(LoadOptionsAsync);
        QueryPreviewCommand = new AsyncRelayCommand(QueryPreviewAsync);
        ExportDataCommand = new AsyncRelayCommand(ExportDataAsync);
        CancelExportCommand = new RelayCommand(CancelExport, () => IsExporting);

        SelectAllProcessUnitsCommand = new RelayCommand(() => SetAllSelections(ProcessUnitOptions, true));
        InvertProcessUnitsCommand = new RelayCommand(() => InvertSelections(ProcessUnitOptions));
        ClearProcessUnitsCommand = new RelayCommand(() => SetAllSelections(ProcessUnitOptions, false));

        SelectAllParametersCommand = new RelayCommand(() => SetAllSelections(ParameterOptions, true));
        InvertParametersCommand = new RelayCommand(() => InvertSelections(ParameterOptions));
        ClearParametersCommand = new RelayCommand(() => SetAllSelections(ParameterOptions, false));
    }

    public ObservableCollection<HistoryDataExportFormat> ExportFormatOptions { get; }

    [ObservableProperty]
    private ObservableCollection<string> _dataTypeOptions = new();

    [ObservableProperty]
    private string _selectedDataType = "液位数据";

    [ObservableProperty]
    private ObservableCollection<SelectableOption> _processUnitOptions;

    [ObservableProperty]
    private ObservableCollection<SelectableOption> _parameterOptions;

    [ObservableProperty]
    private int _selectedProcessUnitCount;

    [ObservableProperty]
    private int _totalProcessUnitCount;

    [ObservableProperty]
    private int _selectedParameterCount;

    [ObservableProperty]
    private int _totalParameterCount;

    [ObservableProperty]
    private DateTime _filterStartDate = DateTime.Now.AddDays(-7);

    [ObservableProperty]
    private DateTime _filterEndDate = DateTime.Now;

    [ObservableProperty]
    private HistoryDataExportFormat _selectedExportFormat = HistoryDataExportFormat.CSVExcelAuto;

    [ObservableProperty]
    private bool _compressToZip;

    [ObservableProperty]
    private bool _includeHeader = true;

    [ObservableProperty]
    private int _previewMaxRows = PreviewMaxRowsDefault;

    [ObservableProperty]
    private int _maxSamplesPerPoint = 50000;

    [ObservableProperty]
    private ObservableCollection<HistoryDataRow> _previewRows;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private int _exportProgress;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private string _exportNoDataMessage = string.Empty;

    public ICommand LoadOptionsCommand { get; }
    public ICommand QueryPreviewCommand { get; }
    public ICommand ExportDataCommand { get; }
    public ICommand CancelExportCommand { get; }

    public ICommand SelectAllProcessUnitsCommand { get; }
    public ICommand InvertProcessUnitsCommand { get; }
    public ICommand ClearProcessUnitsCommand { get; }

    public ICommand SelectAllParametersCommand { get; }
    public ICommand InvertParametersCommand { get; }
    public ICommand ClearParametersCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadOptionsAsync();
        await QueryPreviewAsync();
    }

    private void AttachOptionChanged(SelectableOption option)
    {
        option.PropertyChanged += OnSelectableOptionPropertyChanged;
    }

    private void DetachOptionChanged(SelectableOption option)
    {
        option.PropertyChanged -= OnSelectableOptionPropertyChanged;
    }

    // CommunityToolkit 的 ObservableObject 会实现 INotifyPropertyChanged；这里只关心 IsSelected。
    private void OnSelectableOptionPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableOption.IsSelected))
            UpdateSelectionCounts();
    }

    private void UpdateSelectionCounts()
    {
        TotalProcessUnitCount = ProcessUnitOptions?.Count ?? 0;
        SelectedProcessUnitCount = ProcessUnitOptions?.Count(o => o.IsSelected) ?? 0;

        TotalParameterCount = ParameterOptions?.Count ?? 0;
        SelectedParameterCount = ParameterOptions?.Count(o => o.IsSelected) ?? 0;
    }

    private void SetAllSelections(ObservableCollection<SelectableOption> options, bool value)
    {
        foreach (var o in options)
            o.IsSelected = value;
        UpdateSelectionCounts();
    }

    private void InvertSelections(ObservableCollection<SelectableOption> options)
    {
        foreach (var o in options)
            o.IsSelected = !o.IsSelected;
        UpdateSelectionCounts();
    }

    private async Task LoadOptionsAsync()
    {
        IsLoading = true;
        ExportNoDataMessage = string.Empty;
        try
        {
            // 从点位映射表里提取可用 Purpose（例如：液位数据 / 流量数据 / 定时参数 ...）
            var purposes = await _db.PointMappings
                .AsNoTracking()
                .Select(p => p.Purpose)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            DataTypeOptions = new ObservableCollection<string>(purposes);
            if (DataTypeOptions.Count > 0 && !DataTypeOptions.Contains(SelectedDataType))
                SelectedDataType = DataTypeOptions[0];

            await RefreshProcessUnitsAsync(resetSelection: true);
            await RefreshParametersAsync(resetSelection: true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedDataTypeChanged(string value)
    {
        // 数据类型变化后，重建可选的单元与参数
        if (IsLoading)
            return;
        _ = RefreshUnitsAndParametersAsync(resetSelection: true);
    }

    private async Task RefreshUnitsAndParametersAsync(bool resetSelection)
    {
        await RefreshProcessUnitsAsync(resetSelection);
        await RefreshParametersAsync(resetSelection);
    }

    private async Task RefreshProcessUnitsAsync(bool resetSelection)
    {
        if (ProcessUnitOptions != null)
        {
            foreach (var opt in ProcessUnitOptions)
                DetachOptionChanged(opt);
        }
        ProcessUnitOptions.Clear();

        var unitIds = await _db.PointMappings
            .AsNoTracking()
            .Where(p => p.Purpose == SelectedDataType)
            .Select(p => p.UnitId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync();

        if (unitIds.Count == 0)
        {
            UpdateSelectionCounts();
            return;
        }

        // 为展示使用 UnitTitle（优先使用 Category=单元 或 UnitTitle 非空项）
        var equipments = await _db.Equipments
            .AsNoTracking()
            .Where(e => unitIds.Contains(e.UnitId))
            .ToListAsync();

        string GetTitle(string unitId)
        {
            var exact = equipments.FirstOrDefault(e => e.UnitId == unitId && !string.IsNullOrWhiteSpace(e.UnitTitle));
            if (exact != null)
                return exact.UnitTitle ?? unitId;
            return unitId;
        }

        foreach (var unitId in unitIds)
        {
            var option = new SelectableOption
            {
                Id = unitId,
                DisplayName = GetTitle(unitId),
                IsSelected = resetSelection
            };

            AttachOptionChanged(option);
            ProcessUnitOptions.Add(option);
        }

        UpdateSelectionCounts();
    }

    private async Task RefreshParametersAsync(bool resetSelection)
    {
        if (ParameterOptions != null)
        {
            foreach (var opt in ParameterOptions)
                DetachOptionChanged(opt);
        }
        ParameterOptions.Clear();

        var selectedUnitIds = ProcessUnitOptions.Where(o => o.IsSelected).Select(o => o.Id).ToList();
        if (selectedUnitIds.Count == 0)
        {
            UpdateSelectionCounts();
            return;
        }

        var paramVariableNames = await _db.PointMappings
            .AsNoTracking()
            .Where(p => p.Purpose == SelectedDataType && selectedUnitIds.Contains(p.UnitId))
            .Select(p => p.VariableName)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync();

        foreach (var variableName in paramVariableNames)
        {
            var option = new SelectableOption
            {
                Id = variableName,
                DisplayName = variableName,
                IsSelected = resetSelection
            };
            AttachOptionChanged(option);
            ParameterOptions.Add(option);
        }

        UpdateSelectionCounts();
    }

    private IReadOnlyList<string> GetSelectedUnitIds()
        => ProcessUnitOptions.Where(o => o.IsSelected).Select(o => o.Id).ToList();

    private IReadOnlyList<string> GetSelectedParameterIds()
        => ParameterOptions.Where(o => o.IsSelected).Select(o => o.Id).ToList();

    private async Task<IReadOnlyList<(long PointMappingId, string UnitId, string VariableName)>> ResolvePointMappingsAsync()
    {
        var selectedUnitIds = GetSelectedUnitIds();
        var selectedParameterIds = GetSelectedParameterIds();

        // 严格模式：不选则不导出/不预览（与“清空/取消勾选”按钮语义一致）
        if (selectedUnitIds.Count == 0 || selectedParameterIds.Count == 0)
            return Array.Empty<(long, string, string)>();

        var mappings = await _db.PointMappings.AsNoTracking()
            .Where(p => p.Purpose == SelectedDataType
                        && selectedUnitIds.Contains(p.UnitId)
                        && selectedParameterIds.Contains(p.VariableName))
            .Select(p => new { p.Id, p.UnitId, p.VariableName })
            .ToListAsync();

        return mappings.Select(m => (m.Id, m.UnitId, m.VariableName)).ToList();
    }

    private async Task<IDictionary<string, string>> LoadUnitTitleMapAsync(IEnumerable<string> unitIds)
    {
        var ids = unitIds.ToList();
        var equipments = await _db.Equipments.AsNoTracking()
            .Where(e => ids.Contains(e.UnitId))
            .ToListAsync();

        string GetTitle(string unitId)
        {
            var title = equipments
                .Where(e => e.UnitId == unitId)
                .Select(e => e.UnitTitle)
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
            return title ?? unitId;
        }

        return ids.ToDictionary(id => id, id => GetTitle(id));
    }

    private static double SimulateValue(string parameterName, Random rnd)
    {
        // 依据参数名称简单猜测量纲，生成更“像”的数值
        var name = parameterName ?? string.Empty;
        if (name.Contains("液位", StringComparison.OrdinalIgnoreCase))
            return Math.Round(10 + rnd.NextDouble() * 80, 2);
        if (name.Contains("流量", StringComparison.OrdinalIgnoreCase))
            return Math.Round(1 + rnd.NextDouble() * 25, 2);
        if (name.Contains("时间", StringComparison.OrdinalIgnoreCase) || name.Contains("延时", StringComparison.OrdinalIgnoreCase))
            return Math.Round(rnd.NextDouble() * 3600, 1);

        // 默认：模拟为 0~100
        return Math.Round(rnd.NextDouble() * 100, 2);
    }

    private static IReadOnlyList<DateTime> BuildTimeSeries(DateTime fromUtc, DateTime toUtc, int count)
    {
        if (count <= 0) return Array.Empty<DateTime>();
        if (count == 1) return new[] { fromUtc };
        if (toUtc <= fromUtc) return new[] { fromUtc };

        var totalTicks = (toUtc - fromUtc).Ticks;
        var step = totalTicks / (count - 1);
        if (step <= 0) step = 1;

        var list = new DateTime[count];
        for (int i = 0; i < count; i++)
        {
            list[i] = fromUtc.AddTicks(step * i);
        }
        return list;
    }

    private async Task QueryPreviewAsync()
    {
        IsLoading = true;
        ExportNoDataMessage = string.Empty;
        PreviewRows.Clear();

        try
        {
            if (FilterEndDate < FilterStartDate)
            {
                MessageBox.Show("结束时间不能早于开始时间。", "参数错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fromUtc = FilterStartDate.ToUniversalTime();
            var toUtc = FilterEndDate.ToUniversalTime();

            var mappings = await ResolvePointMappingsAsync();
            if (mappings.Count == 0)
            {
                StatusMessage = "无法预览：未选择处理单元/导出参数";
                ExportNoDataMessage = "未选择处理单元或导出参数：请先勾选后再预览。";
                return;
            }

            var unitTitleMap = await LoadUnitTitleMapAsync(mappings.Select(m => m.UnitId).Distinct());

            // 性能优化：预览阶段避免 N+1 查询
            // 一次批量拿到每个 PointMappingId 前 PreviewMaxRows 条真实历史样本
            var distinctPointMappingIds = mappings.Select(m => m.PointMappingId).Distinct().ToList();
            var allRealSamples = await _pointHistoryRepository.QueryRangeManyAsync(
                distinctPointMappingIds,
                fromUtc,
                toUtc,
                takeLimit: PreviewMaxRows);

            var samplesByPointMappingId = allRealSamples
                .GroupBy(s => s.PointMappingId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var previewRemaining = PreviewMaxRows;
            var anyRealData = false;
            var anySimulated = false;
            var simPerMapping = Math.Max(1, PreviewMaxRows / Math.Max(1, mappings.Count));
            var simTimes = BuildTimeSeries(fromUtc, toUtc, simPerMapping).ToArray();

            foreach (var mapping in mappings)
            {
                if (previewRemaining <= 0) break;

                if (samplesByPointMappingId.TryGetValue(mapping.PointMappingId, out var samples) && samples.Count > 0)
                {
                    anyRealData = true;
                    foreach (var s in samples)
                    {
                        PreviewRows.Add(new HistoryDataRow
                        {
                            TimestampLocal = s.TimestampUtc.ToLocalTime(),
                            ProcessUnitTitle = unitTitleMap.TryGetValue(mapping.UnitId, out var title) ? title : mapping.UnitId,
                            ParameterName = mapping.VariableName,
                            Value = s.ValueReal,
                            Quality = s.Quality,
                            IsSimulated = false
                        });

                        previewRemaining--;
                        if (previewRemaining <= 0) break;
                    }
                }
                else
                {
                    // 该点位无真实数据：生成模拟数据
                    anySimulated = true;
                    var seed = HashCode.Combine(mapping.VariableName, mapping.UnitId, fromUtc.Ticks, toUtc.Ticks);
                    var rnd = new Random(seed);
                    foreach (var t in simTimes)
                    {
                        PreviewRows.Add(new HistoryDataRow
                        {
                            TimestampLocal = t.ToLocalTime(),
                            ProcessUnitTitle = unitTitleMap.TryGetValue(mapping.UnitId, out var title) ? title : mapping.UnitId,
                            ParameterName = mapping.VariableName,
                            Value = SimulateValue(mapping.VariableName, rnd),
                            Quality = 1,
                            IsSimulated = true
                        });

                        previewRemaining--;
                        if (previewRemaining <= 0) break;
                    }
                }
            }

            if (!anyRealData && anySimulated)
                ExportNoDataMessage = "历史数据为空：已生成模拟数据用于预览（导出同样可包含模拟数据）。";
            else if (anyRealData && anySimulated)
                ExportNoDataMessage = "部分点位无历史数据，已对缺失点位生成模拟数据用于预览。";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CancelExport()
    {
        if (!IsExporting) return;
        _exportCts?.Cancel();
    }

    private static string CsvEscape(string? value)
    {
        value ??= string.Empty;
        return value.Replace("\"", "\"\"");
    }

    private static void EnsureDirectoryForFile(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private static Encoding GetGbkEncoding()
    {
        // Windows 上常用 GBK 代码页为 936。
        // 在 .NET（尤其跨平台/精简运行时）里需要注册编码提供器，否则会出现：
        // "No data is available for encoding 936"
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        catch
        {
            // ignore: 如果 provider 注册失败，后面仍会尝试直接 GetEncoding
        }

        try
        {
            // 有些环境支持按名称解析
            return Encoding.GetEncoding("GBK");
        }
        catch
        {
            // 回退到代码页 936（GBK）
            return Encoding.GetEncoding(936);
        }
    }

    private static Encoding GetExcelAutoCsvEncoding()
    {
        // Excel 对 UTF-16LE(带 BOM) 的 CSV 通常能自动识别编码，不需要用户手动选 936/GBK
        return new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
    }

    private async Task ExportDataAsync()
    {
        if (IsExporting)
            return;

        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = SelectedExportFormat switch
                {
                    HistoryDataExportFormat.CSV => "CSV文件 (*.csv)|*.csv",
                    HistoryDataExportFormat.CSVExcelAuto => "CSV文件(给Excel自动识别) (*.csv)|*.csv",
                    HistoryDataExportFormat.Excel => "Excel文件 (*.xlsx)|*.xlsx",
                    HistoryDataExportFormat.XML => "XML文件 (*.xml)|*.xml",
                    _ => "CSV文件 (*.csv)|*.csv"
                },
                DefaultExt = SelectedExportFormat switch
                {
                    HistoryDataExportFormat.CSV => ".csv",
                    HistoryDataExportFormat.CSVExcelAuto => ".csv",
                    HistoryDataExportFormat.Excel => ".xlsx",
                    HistoryDataExportFormat.XML => ".xml",
                    _ => ".csv"
                },
                FileName = BuildDefaultExportFileName()
            };

            if (dialog.ShowDialog() != true)
                return;

            var filePath = dialog.FileName;
            EnsureDirectoryForFile(filePath);

            // 建立取消控制
            _exportCts = new CancellationTokenSource();
            var token = _exportCts.Token;

            IsExporting = true;
            ExportProgress = 0;
            StatusMessage = "开始导出...";
            ExportNoDataMessage = string.Empty;

            var fromUtc = FilterStartDate.ToUniversalTime();
            var toUtc = FilterEndDate.ToUniversalTime();

            var mappings = await ResolvePointMappingsAsync();
            if (mappings.Count == 0)
            {
                StatusMessage = "未选择处理单元或导出参数。";
                ExportNoDataMessage = "请先勾选需要导出的内容，再点击导出。";
                return;
            }

            var unitTitleMap = await LoadUnitTitleMapAsync(mappings.Select(m => m.UnitId).Distinct());

            if (SelectedExportFormat == HistoryDataExportFormat.CSV)
            {
                await ExportCsvAsync(filePath, mappings, fromUtc, toUtc, unitTitleMap, GetGbkEncoding(), token);
            }
            else if (SelectedExportFormat == HistoryDataExportFormat.CSVExcelAuto)
            {
                await ExportCsvAsync(filePath, mappings, fromUtc, toUtc, unitTitleMap, GetExcelAutoCsvEncoding(), token);
            }
            else if (SelectedExportFormat == HistoryDataExportFormat.Excel)
            {
                await ExportExcelAsync(filePath, mappings, fromUtc, toUtc, unitTitleMap, token);
            }
            else
            {
                await ExportXmlAsync(filePath, mappings, fromUtc, toUtc, unitTitleMap, token);
            }

            // 可选压缩
            if (CompressToZip)
                ZipExportResult(filePath);

            StatusMessage = $"导出完成：{Path.GetFileName(filePath)}";
            MessageBox.Show("导出成功。", "导出", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "导出已取消。";
            MessageBox.Show("导出已取消。", "导出取消", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败：{ex.Message}";
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsExporting = false;
            ExportProgress = 0;
        }
    }

    private string BuildDefaultExportFileName()
    {
        var safeType = string.IsNullOrWhiteSpace(SelectedDataType) ? "History" : SelectedDataType;
        return $"历史数据_{safeType}_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    private void ZipExportResult(string filePath)
    {
        var zipPath = filePath + ".zip";
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        zip.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
        // 不删除原文件，避免用户拿不到未压缩版本
    }

    private async Task ExportCsvAsync(
        string filePath,
        IReadOnlyList<(long PointMappingId, string UnitId, string VariableName)> mappings,
        DateTime fromUtc,
        DateTime toUtc,
        IDictionary<string, string> unitTitleMap,
        Encoding encoding,
        CancellationToken token)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        // CSV 文本按指定编码写入
        using var writer = new StreamWriter(stream, encoding);

        if (IncludeHeader)
        {
            writer.WriteLine("时间(本地),处理单元,参数名称,值,质量");
        }

        var mappingCount = mappings.Count;
        var writtenRows = 0;

        var anyRealData = false;
        var anySimulated = false;
        var simRowsPerMapping = Math.Max(1, Math.Min(ExportSimulatedMaxRowsDefault / Math.Max(1, mappingCount), MaxSamplesPerPoint));
        var simTimes = BuildTimeSeries(fromUtc, toUtc, simRowsPerMapping).ToArray();

        for (int i = 0; i < mappingCount; i++)
        {
            token.ThrowIfCancellationRequested();
            var m = mappings[i];

            var samples = await _pointHistoryRepository.QueryRangeAsync(
                m.PointMappingId,
                fromUtc,
                toUtc,
                takeLimit: MaxSamplesPerPoint,
                cancellationToken: token);

            if (samples.Count > 0)
            {
                anyRealData = true;
                foreach (var s in samples)
                {
                    writer.WriteLine(
                        $"\"{s.TimestampUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}\",\"{CsvEscape(unitTitleMap[m.UnitId])}\",\"{CsvEscape(m.VariableName)}\",\"{s.ValueReal.ToString(CultureInfo.InvariantCulture)}\",\"{s.Quality}\"");
                    writtenRows++;
                }
            }
            else
            {
                anySimulated = true;
                var seed = HashCode.Combine(m.VariableName, m.UnitId, fromUtc.Ticks, toUtc.Ticks, "csv");
                var rnd = new Random(seed);
                foreach (var t in simTimes)
                {
                    var value = SimulateValue(m.VariableName, rnd);
                    var quality = 1;
                    writer.WriteLine(
                        $"\"{t.ToLocalTime():yyyy-MM-dd HH:mm:ss}\",\"{CsvEscape(unitTitleMap[m.UnitId])}\",\"{CsvEscape(m.VariableName)}\",\"{value.ToString(CultureInfo.InvariantCulture)}\",\"{quality}\"");
                    writtenRows++;
                }
            }

            ExportProgress = (i + 1) * 100 / mappingCount;
            StatusMessage = $"正在导出... {ExportProgress}% 已写入 {writtenRows} 行";
            await writer.FlushAsync();
        }

        if (!anyRealData && anySimulated)
            ExportNoDataMessage = "历史数据为空：已生成模拟数据用于导出。";
        else if (anyRealData && anySimulated)
            ExportNoDataMessage = "部分点位无历史数据：已对缺失点位生成模拟数据用于导出。";
    }

    private async Task ExportExcelAsync(
        string filePath,
        IReadOnlyList<(long PointMappingId, string UnitId, string VariableName)> mappings,
        DateTime fromUtc,
        DateTime toUtc,
        IDictionary<string, string> unitTitleMap,
        CancellationToken token)
    {
        // EPPlus 许可：按非商业用途设置（可按你的实际情况调整）
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("History");

        int rowIndex = 1;
        if (IncludeHeader)
        {
            sheet.Cells[rowIndex, 1].Value = "时间(本地)";
            sheet.Cells[rowIndex, 2].Value = "处理单元";
            sheet.Cells[rowIndex, 3].Value = "参数名称";
            sheet.Cells[rowIndex, 4].Value = "值";
            sheet.Cells[rowIndex, 5].Value = "质量";
            rowIndex++;
        }

        var mappingCount = mappings.Count;
        var writtenRows = 0;

        var anyRealData = false;
        var anySimulated = false;
        var simRowsPerMapping = Math.Max(1, Math.Min(ExportSimulatedMaxRowsDefault / Math.Max(1, mappingCount), MaxSamplesPerPoint));
        var simTimes = BuildTimeSeries(fromUtc, toUtc, simRowsPerMapping).ToArray();

        for (int i = 0; i < mappingCount; i++)
        {
            token.ThrowIfCancellationRequested();
            var m = mappings[i];

            var samples = await _pointHistoryRepository.QueryRangeAsync(
                m.PointMappingId,
                fromUtc,
                toUtc,
                takeLimit: MaxSamplesPerPoint,
                cancellationToken: token);

            if (samples.Count > 0)
            {
                anyRealData = true;
                foreach (var s in samples)
                {
                    sheet.Cells[rowIndex, 1].Value = s.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                    sheet.Cells[rowIndex, 2].Value = unitTitleMap[m.UnitId];
                    sheet.Cells[rowIndex, 3].Value = m.VariableName;
                    sheet.Cells[rowIndex, 4].Value = s.ValueReal;
                    sheet.Cells[rowIndex, 5].Value = s.Quality;
                    rowIndex++;
                    writtenRows++;
                }
            }
            else
            {
                anySimulated = true;
                var seed = HashCode.Combine(m.VariableName, m.UnitId, fromUtc.Ticks, toUtc.Ticks, "excel");
                var rnd = new Random(seed);
                foreach (var t in simTimes)
                {
                    sheet.Cells[rowIndex, 1].Value = t.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                    sheet.Cells[rowIndex, 2].Value = unitTitleMap[m.UnitId];
                    sheet.Cells[rowIndex, 3].Value = m.VariableName;
                    sheet.Cells[rowIndex, 4].Value = SimulateValue(m.VariableName, rnd);
                    sheet.Cells[rowIndex, 5].Value = 1;
                    rowIndex++;
                    writtenRows++;
                }
            }

            ExportProgress = (i + 1) * 100 / mappingCount;
            StatusMessage = $"正在导出Excel... {ExportProgress}% 已写入 {writtenRows} 行";
        }

        sheet.Cells.AutoFitColumns();
        await package.SaveAsAsync(new FileInfo(filePath), token);

        if (!anyRealData && anySimulated)
            ExportNoDataMessage = "历史数据为空：已生成模拟数据用于导出。";
        else if (anyRealData && anySimulated)
            ExportNoDataMessage = "部分点位无历史数据：已对缺失点位生成模拟数据用于导出。";
    }

    private async Task ExportXmlAsync(
        string filePath,
        IReadOnlyList<(long PointMappingId, string UnitId, string VariableName)> mappings,
        DateTime fromUtc,
        DateTime toUtc,
        IDictionary<string, string> unitTitleMap,
        CancellationToken token)
    {
        var root = new XElement("WasteWaterHistoryExport",
            new XAttribute("generatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            new XAttribute("dataType", SelectedDataType),
            new XAttribute("from", FilterStartDate.ToString("yyyy-MM-dd HH:mm:ss")),
            new XAttribute("to", FilterEndDate.ToString("yyyy-MM-dd HH:mm:ss")));

        int mappingCount = mappings.Count;
        var anyRealData = false;
        var anySimulated = false;
        var simRowsPerMapping = Math.Max(1, Math.Min(ExportSimulatedMaxRowsDefault / Math.Max(1, mappingCount), MaxSamplesPerPoint));
        var simTimes = BuildTimeSeries(fromUtc, toUtc, simRowsPerMapping).ToArray();

        for (int i = 0; i < mappingCount; i++)
        {
            token.ThrowIfCancellationRequested();
            var m = mappings[i];
            var unitTitle = unitTitleMap.TryGetValue(m.UnitId, out var title) ? title : m.UnitId;

            IEnumerable<PointHistorySampleLike> rows;
            var samples = await _pointHistoryRepository.QueryRangeAsync(
                m.PointMappingId,
                fromUtc,
                toUtc,
                takeLimit: MaxSamplesPerPoint,
                cancellationToken: token);

            if (samples.Count > 0)
            {
                anyRealData = true;
                rows = samples.Select(s => new PointHistorySampleLike
                {
                    TimestampUtc = s.TimestampUtc,
                    Value = s.ValueReal,
                    Quality = s.Quality
                }).ToArray();
            }
            else
            {
                anySimulated = true;
                var seed = HashCode.Combine(m.VariableName, m.UnitId, fromUtc.Ticks, toUtc.Ticks, "xml");
                var rnd = new Random(seed);
                rows = simTimes.Select(t => new PointHistorySampleLike
                {
                    TimestampUtc = t,
                    Value = SimulateValue(m.VariableName, rnd),
                    Quality = (byte)1
                }).ToArray();
            }

            foreach (var r in rows)
            {
                root.Add(new XElement("Row",
                    new XAttribute("timeLocal", r.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")),
                    new XAttribute("unit", unitTitle),
                    new XAttribute("parameter", m.VariableName),
                    new XAttribute("value", r.Value.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("quality", r.Quality)));
            }

            ExportProgress = (i + 1) * 100 / mappingCount;
            StatusMessage = $"正在导出XML... {ExportProgress}%";
        }

        var doc = new XDocument(root);
        var gbk = GetGbkEncoding();
        await Task.Run(() =>
        {
            var settings = new System.Xml.XmlWriterSettings
            {
                Encoding = gbk,
                Indent = true,
                NewLineOnAttributes = false
            };

            using var writer = System.Xml.XmlWriter.Create(filePath, settings);
            doc.Save(writer);
        }, token);

        if (!anyRealData && anySimulated)
            ExportNoDataMessage = "历史数据为空：已生成模拟数据用于导出。";
        else if (anyRealData && anySimulated)
            ExportNoDataMessage = "部分点位无历史数据：已对缺失点位生成模拟数据用于导出。";
    }

    private sealed class PointHistorySampleLike
    {
        public DateTime TimestampUtc { get; set; }
        public double Value { get; set; }
        public byte Quality { get; set; }
    }

    public void Dispose()
    {
        try
        {
            _exportCts?.Cancel();
            _exportCts?.Dispose();
        }
        catch
        {
            // ignored
        }
    }
}

