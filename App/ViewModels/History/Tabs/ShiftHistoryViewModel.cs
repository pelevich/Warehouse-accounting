using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services.Interfaces.service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace App.ViewModels.Tabs;

public sealed class ShiftEmployeeOption
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = "";
}

public partial class ShiftHistoryRow : ObservableObject
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeName { get; init; } = "Не указан";
    public DateTime OpenedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public int SalesCount { get; init; }
    public int ReceiptsCount { get; init; }
    public decimal Revenue { get; init; }

    public string DateText =>
        OpenedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

    public string Summary =>
        ClosedAt.HasValue ? "Завершена" : "Открыта";

    public string OpenText =>
        $"Открыта: {OpenedAt.ToLocalTime():dd.MM.yyyy HH:mm}";

    public string CloseText =>
        ClosedAt.HasValue
            ? $"Закрыта: {ClosedAt.Value.ToLocalTime():dd.MM.yyyy HH:mm}"
            : "Смена ещё открыта";

    public string Details =>
        $"Продаж: {SalesCount}   ·   Приходов: {ReceiptsCount}   ·   Выручка: {Revenue:N2} ₽";

    // Во время генерации надпись меняется, сама кнопка остаётся видимой.
    public string DownloadButtonText =>
        IsDownloadingPdf ? "Формирование…" : "PDF";

    public bool CanDownloadPdf => !IsDownloadingPdf;

    public IAsyncRelayCommand DownloadPdfCommand { get; set; } = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Arrow))]
    private bool isExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DownloadButtonText))]
    [NotifyPropertyChangedFor(nameof(CanDownloadPdf))]
    private bool isDownloadingPdf;

    public string Arrow => IsExpanded ? "⌃" : "⌄";

    [RelayCommand]
    private void Toggle()
    {
        IsExpanded = !IsExpanded;
    }
}

public partial class ShiftHistoryViewModel : ObservableObject
{
    private readonly IWorkShiftService _shifts;
    private readonly IInventoryReportService _reportService;

    private List<ShiftHistoryRow> _all = new();
    private bool _loading;

    public ObservableCollection<ShiftHistoryRow> Records { get; } = new();
    public ObservableCollection<ShiftEmployeeOption> Employees { get; } = new();

    public IReadOnlyList<string> SortOptions { get; } =
        new[] { "Сначала новые", "Сначала старые" };

    [ObservableProperty]
    private ShiftEmployeeOption? selectedEmployee;

    [ObservableProperty]
    private string selectedSort = "Сначала новые";

    [ObservableProperty]
    private bool useDateFilter;

    [ObservableProperty]
    private DateTime dateFrom = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime dateTo = DateTime.Today;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = "";

    [ObservableProperty]
    private int visibleCount;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsEmpty => !IsBusy && Records.Count == 0;

    public ShiftHistoryViewModel(
        IWorkShiftService shifts,
        IInventoryReportService reportService)
    {
        _shifts = shifts;
        _reportService = reportService;
    }

    partial void OnSelectedEmployeeChanged(ShiftEmployeeOption? value) =>
        ApplyFilters();

    partial void OnSelectedSortChanged(string value) =>
        ApplyFilters();

    partial void OnUseDateFilterChanged(bool value) =>
        ApplyFilters();

    partial void OnDateFromChanged(DateTime value) =>
        ApplyFilters();

    partial void OnDateToChanged(DateTime value) =>
        ApplyFilters();

    partial void OnErrorMessageChanged(string value) =>
        OnPropertyChanged(nameof(HasError));

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_loading)
            return;

        _loading = true;
        IsBusy = true;
        ErrorMessage = "";
        OnPropertyChanged(nameof(IsEmpty));

        try
        {
            var shifts = await _shifts.GetRecentShiftsAsync(200);
            var result = new List<ShiftHistoryRow>();

            foreach (var shift in shifts)
            {
                var report = await _shifts.GetShiftReportAsync(shift.Id);

                var row = new ShiftHistoryRow
                {
                    Id = shift.Id,
                    EmployeeId = shift.EmployeeId,
                    EmployeeName = report.EmployeeFullName,
                    OpenedAt = shift.OpenedAt,
                    ClosedAt = shift.ClosedAt,
                    SalesCount = report.SalesCount,
                    ReceiptsCount = report.ReceiptCount,
                    Revenue = report.TotalRevenue
                };

                // Команда создаётся отдельно для каждой карточки смены.
                row.DownloadPdfCommand =
                    new AsyncRelayCommand(() => DownloadPdfAsync(row));

                result.Add(row);
            }

            _all = result;

            var previouslySelectedEmployeeId = SelectedEmployee?.Id;

            Employees.Clear();
            Employees.Add(new ShiftEmployeeOption
            {
                Name = "Все сотрудники"
            });

            foreach (var employee in result
                .GroupBy(row => row.EmployeeId)
                .Select(group => new ShiftEmployeeOption
                {
                    Id = group.Key,
                    Name = group.First().EmployeeName
                })
                .OrderBy(employee => employee.Name))
            {
                Employees.Add(employee);
            }

            SelectedEmployee =
                Employees.FirstOrDefault(employee =>
                    employee.Id == previouslySelectedEmployeeId)
                ?? Employees[0];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            ErrorMessage =
                "Ошибка загрузки истории смен: " + ex.Message;
        }
        finally
        {
            _loading = false;
            IsBusy = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private async Task DownloadPdfAsync(ShiftHistoryRow row)
    {
        if (row.IsDownloadingPdf)
            return;

        row.IsDownloadingPdf = true;

        try
        {
            // Формируем PDF только для выбранной смены.
            var pdfBytes =
                await _reportService.GenerateShiftPdfAsync(row.Id);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "Генератор вернул пустой PDF-файл.");
            }

            var fileName =
                $"Отчёт_смена_{row.OpenedAt.ToLocalTime():yyyyMMdd_HHmm}.pdf";

            using var stream = new MemoryStream(pdfBytes);

            var saveResult = await FileSaver.Default.SaveAsync(
                fileName,
                stream,
                CancellationToken.None);

            if (saveResult.IsSuccessful)
            {
                await Shell.Current.DisplayAlert(
                    "Отчёт сохранён",
                    $"PDF-файл сохранён:\n{saveResult.FilePath}",
                    "OK");
            }
            else if (saveResult.Exception is not null)
            {
                await Shell.Current.DisplayAlert(
                    "Ошибка сохранения",
                    saveResult.Exception.Message,
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Не удалось сформировать отчёт",
                ex.Message,
                "OK");
        }
        finally
        {
            // Возвращаем кнопку в обычное состояние в любом случае.
            row.IsDownloadingPdf = false;
        }
    }

    [RelayCommand]
    private void ResetFilters()
    {
        UseDateFilter = false;
        SelectedSort = SortOptions[0];
        SelectedEmployee = Employees.FirstOrDefault();
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<ShiftHistoryRow> query = _all;

        if (SelectedEmployee?.Id is Guid employeeId)
        {
            query = query.Where(row =>
                row.EmployeeId == employeeId);
        }

        if (UseDateFilter)
        {
            if (DateFrom.Date > DateTo.Date)
            {
                ErrorMessage =
                    "Дата «С» не может быть позже даты «По».";

                Records.Clear();
                VisibleCount = 0;
                OnPropertyChanged(nameof(IsEmpty));
                return;
            }

            var from = DateFrom.Date;
            var to = DateTo.Date;

            query = query.Where(row =>
                row.OpenedAt.ToLocalTime().Date >= from &&
                row.OpenedAt.ToLocalTime().Date <= to);
        }

        ErrorMessage = "";

        query = SelectedSort == "Сначала старые"
            ? query.OrderBy(row => row.OpenedAt)
            : query.OrderByDescending(row => row.OpenedAt);

        Records.Clear();

        foreach (var row in query)
        {
            Records.Add(row);
        }

        VisibleCount = Records.Count;
        OnPropertyChanged(nameof(IsEmpty));
    }
}
