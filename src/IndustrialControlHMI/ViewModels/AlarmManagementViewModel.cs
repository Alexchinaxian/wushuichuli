using System.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlHMI.Models;
using IndustrialControlHMI.Services;

namespace IndustrialControlHMI.ViewModels
{
    /// <summary>
    /// 报警管理视图模型。
    /// </summary>
    public partial class AlarmManagementViewModel : ObservableObject
    {
        private readonly IAlarmRepository _alarmRepository;

        /// <summary>状态筛选下拉选项（与仓储状态一致：中文）。</summary>
        public static IReadOnlyList<string> StatusFilterOptions { get; } = new[] { "全部", "激活", "确认", "清除" };

        // 报警记录列表
        [ObservableProperty]
        private ObservableCollection<AlarmRecord> _alarmRecords;

        // 筛选条件
        [ObservableProperty]
        private string _filterStatus = "全部";

        [ObservableProperty]
        private string _filterParameter = "";

        [ObservableProperty]
        private DateTime _filterStartDate = DateTime.Now.AddDays(-7);

        [ObservableProperty]
        private DateTime _filterEndDate = DateTime.Now;

        // 选中项
        [ObservableProperty]
        private AlarmRecord _selectedAlarmRecord;

        // 统计信息
        [ObservableProperty]
        private int _totalAlarms;

        [ObservableProperty]
        private int _activeAlarms;

        [ObservableProperty]
        private int _acknowledgedAlarms;

        [ObservableProperty]
        private int _resolvedAlarms;

        /// <summary>
        /// 初始化报警管理视图模型。
        /// </summary>
        /// <param name="alarmRepository">报警存储库。</param>
        public AlarmManagementViewModel(IAlarmRepository alarmRepository)
        {
            _alarmRepository = alarmRepository ?? throw new ArgumentNullException(nameof(alarmRepository));
            AlarmRecords = new ObservableCollection<AlarmRecord>();

            // 初始化命令
            LoadAlarmsCommand = new AsyncRelayCommand(LoadAlarmsAsync);
            AcknowledgeAlarmCommand = new AsyncRelayCommand<int?>(AcknowledgeAlarmAsync);
            ResolveAlarmCommand = new AsyncRelayCommand<int?>(ResolveAlarmAsync);
            DeleteAlarmCommand = new AsyncRelayCommand<int?>(DeleteAlarmAsync);
            ApplyFilterCommand = new AsyncRelayCommand(ApplyFilterAsync);
            ClearFilterCommand = new AsyncRelayCommand(ClearFilterAsync);
            ExportAlarmsCommand = new AsyncRelayCommand(ExportAlarmsAsync);
        }

        /// <summary>
        /// 加载报警命令。
        /// </summary>
        public ICommand LoadAlarmsCommand { get; }

        /// <summary>
        /// 确认报警命令。
        /// </summary>
        public ICommand AcknowledgeAlarmCommand { get; }

        /// <summary>
        /// 解决报警命令。
        /// </summary>
        public ICommand ResolveAlarmCommand { get; }

        /// <summary>
        /// 删除报警命令。
        /// </summary>
        public ICommand DeleteAlarmCommand { get; }

        /// <summary>
        /// 应用筛选命令。
        /// </summary>
        public ICommand ApplyFilterCommand { get; }

        /// <summary>
        /// 清除筛选命令。
        /// </summary>
        public ICommand ClearFilterCommand { get; }

        /// <summary>
        /// 导出报警命令。
        /// </summary>
        public ICommand ExportAlarmsCommand { get; }

        /// <summary>
        /// 异步加载报警记录。
        /// </summary>
        private async Task LoadAlarmsAsync()
        {
            try
            {
                var records = await _alarmRepository.GetAllAsync();
                AlarmRecords.Clear();
                foreach (var record in records)
                {
                    AlarmRecords.Add(record);
                }
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载报警记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步确认报警。
        /// </summary>
        /// <param name="alarmId">报警ID。</param>
        private async Task AcknowledgeAlarmAsync(int? alarmId)
        {
            if (!alarmId.HasValue) return;

            try
            {
                var updated = await _alarmRepository.AcknowledgeAsync(alarmId.Value);
                if (updated != null)
                {
                    await LoadAlarmsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"确认报警失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步解决报警。
        /// </summary>
        /// <param name="alarmId">报警ID。</param>
        private async Task ResolveAlarmAsync(int? alarmId)
        {
            if (!alarmId.HasValue) return;

            try
            {
                var updated = await _alarmRepository.ResolveAsync(alarmId.Value);
                if (updated != null)
                {
                    await LoadAlarmsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解决报警失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步删除报警。
        /// </summary>
        /// <param name="alarmId">报警ID。</param>
        private async Task DeleteAlarmAsync(int? alarmId)
        {
            if (!alarmId.HasValue) return;

            try
            {
                bool success = await _alarmRepository.DeleteAsync(alarmId.Value);
                if (success)
                {
                    await LoadAlarmsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除报警失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步应用筛选条件。
        /// </summary>
        private async Task ApplyFilterAsync()
        {
            try
            {
                IEnumerable<AlarmRecord> filtered;
                if (FilterStatus == "全部")
                {
                    filtered = await _alarmRepository.GetAllAsync();
                }
                else
                {
                    filtered = await _alarmRepository.GetByStatusAsync(FilterStatus);
                }

                // 按参数名称筛选（如果提供了）
                if (!string.IsNullOrWhiteSpace(FilterParameter))
                {
                    filtered = filtered.Where(a => a.ParameterName.Contains(FilterParameter, StringComparison.OrdinalIgnoreCase));
                }

                // 按时间范围筛选
                filtered = filtered.Where(a => a.OccurrenceTime >= FilterStartDate && a.OccurrenceTime <= FilterEndDate);

                AlarmRecords.Clear();
                foreach (var record in filtered.OrderByDescending(a => a.OccurrenceTime))
                {
                    AlarmRecords.Add(record);
                }
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用筛选失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步清除筛选条件。
        /// </summary>
        private async Task ClearFilterAsync()
        {
            FilterStatus = "全部";
            FilterParameter = "";
            FilterStartDate = DateTime.Now.AddDays(-7);
            FilterEndDate = DateTime.Now;
            await LoadAlarmsAsync();
        }

        /// <summary>
        /// 异步导出报警记录。
        /// </summary>
        private async Task ExportAlarmsAsync()
        {
            try
            {
                // 使用 SaveFileDialog 保存为 CSV 或 Excel
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV文件 (*.csv)|*.csv|Excel文件 (*.xlsx)|*.xlsx",
                    DefaultExt = ".csv",
                    FileName = $"报警记录_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    var records = AlarmRecords.ToList();
                    if (records.Count == 0)
                    {
                        System.Windows.MessageBox.Show("没有可导出的报警记录。", "导出", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        return;
                    }

                    // 根据扩展名选择导出方式
                    if (dialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        await ExportToCsvAsync(dialog.FileName, records);
                    }
                    else if (dialog.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        await ExportToExcelAsync(dialog.FileName, records);
                    }

                    System.Windows.MessageBox.Show($"报警记录已导出到: {dialog.FileName}", "导出成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出报警记录失败: {ex.Message}");
                System.Windows.MessageBox.Show($"导出失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出为CSV文件。
        /// </summary>
        private async Task ExportToCsvAsync(string filePath, IEnumerable<AlarmRecord> records)
        {
            var lines = new List<string>
            {
                "ID,参数名称,报警类型,阈值,实际值,消息,状态,发生时间,确认时间,清除时间"
            };

            foreach (var record in records)
            {
                lines.Add($"\"{record.Id}\",\"{record.ParameterName}\",\"{record.AlarmType}\",\"{record.Threshold}\",\"{record.ActualValue}\",\"{record.Message}\",\"{record.Status}\",\"{record.OccurrenceTime}\",\"{record.AcknowledgedTime}\",\"{record.ClearedTime}\"");
            }

            await File.WriteAllLinesAsync(filePath, lines, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 导出为Excel文件（占位符）。
        /// </summary>
        private async Task ExportToExcelAsync(string filePath, IEnumerable<AlarmRecord> records)
        {
            // 在实际项目中，可以使用EPPlus或ClosedXML库生成Excel
            // 这里简化为生成CSV格式（但使用.xlsx扩展名）
            await ExportToCsvAsync(filePath, records);
        }

        /// <summary>
        /// 更新统计信息。
        /// </summary>
        private void UpdateStatistics()
        {
            TotalAlarms = AlarmRecords.Count;
            ActiveAlarms = AlarmRecords.Count(r => r.Status == "激活");
            AcknowledgedAlarms = AlarmRecords.Count(r => r.Status == "确认");
            ResolvedAlarms = AlarmRecords.Count(r => r.Status == "清除");
        }

        /// <summary>
        /// 初始化视图模型。
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadAlarmsAsync();
        }
    }
}