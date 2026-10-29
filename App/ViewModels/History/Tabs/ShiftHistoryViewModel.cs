using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services.Interfaces.service;
using System.Collections.ObjectModel;

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

    public string DateText => OpenedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    public string Summary => ClosedAt.HasValue ? "Завершена" : "Открыта";
    public string OpenText => $"Открыта: {OpenedAt.ToLocalTime():dd.MM.yyyy HH:mm}";
    public string CloseText => ClosedAt.HasValue
        ? $"Закрыта: {ClosedAt.Value.ToLocalTime():dd.MM.yyyy HH:mm}"
        : "Смена ещё открыта";
    public string Details => $"Продаж: {SalesCount}   ·   Приходов: {ReceiptsCount}   ·   Выручка: {Revenue:N2} ₽";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Arrow))]
    private bool isExpanded;

    public string Arrow => IsExpanded ? "⌃" : "⌄";

    [RelayCommand]
    private void Toggle() => IsExpanded = !IsExpanded;
}

public partial class ShiftHistoryViewModel : ObservableObject
{
    private readonly IWorkShiftService _shifts;
    private List<ShiftHistoryRow> _all = new();
    private bool _loading;

    public ObservableCollection<ShiftHistoryRow> Records { get; } = new();
    public ObservableCollection<ShiftEmployeeOption> Employees { get; } = new();
    public IReadOnlyList<string> SortOptions { get; } = new[] { "Сначала новые", "Сначала старые" };

    [ObservableProperty] private ShiftEmployeeOption? selectedEmployee;
    [ObservableProperty] private string selectedSort = "Сначала новые";
    [ObservableProperty] private bool useDateFilter;
    [ObservableProperty] private DateTime dateFrom = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime dateTo = DateTime.Today;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = "";
    [ObservableProperty] private int visibleCount;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsEmpty => !IsBusy && Records.Count == 0;

    public ShiftHistoryViewModel(IWorkShiftService shifts) => _shifts = shifts;

    partial void OnSelectedEmployeeChanged(ShiftEmployeeOption? value) => ApplyFilters();
    partial void OnSelectedSortChanged(string value) => ApplyFilters();
    partial void OnUseDateFilterChanged(bool value) => ApplyFilters();
    partial void OnDateFromChanged(DateTime value) => ApplyFilters();
    partial void OnDateToChanged(DateTime value) => ApplyFilters();
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_loading) return;
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
                result.Add(new ShiftHistoryRow
                {
                    Id = shift.Id,
                    EmployeeId = shift.EmployeeId,
                    EmployeeName = report.EmployeeFullName,
                    OpenedAt = shift.OpenedAt,
                    ClosedAt = shift.ClosedAt,
                    SalesCount = report.SalesCount,
                    ReceiptsCount = report.ReceiptCount,
                    Revenue = report.TotalRevenue
                });
            }
            _all = result;
            var old = SelectedEmployee?.Id;
            Employees.Clear();
            Employees.Add(new ShiftEmployeeOption { Name = "Все сотрудники" });
            foreach (var emp in result.GroupBy(x => x.EmployeeId)
                                      .Select(x => new ShiftEmployeeOption { Id = x.Key, Name = x.First().EmployeeName })
                                      .OrderBy(x => x.Name))
                Employees.Add(emp);
            SelectedEmployee = Employees.FirstOrDefault(x => x.Id == old) ?? Employees[0];
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Ошибка загрузки истории смен: " + ex.Message;
        }
        finally
        {
            _loading = false;
            IsBusy = false;
            OnPropertyChanged(nameof(IsEmpty));
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
            query = query.Where(x => x.EmployeeId == employeeId);
        if (UseDateFilter)
        {
            if (DateFrom.Date > DateTo.Date)
            {
                ErrorMessage = "Дата «С» не может быть позже даты «По».";
                Records.Clear();
                VisibleCount = 0;
                OnPropertyChanged(nameof(IsEmpty));
                return;
            }
            var from = DateFrom.Date;
            var to = DateTo.Date;
            query = query.Where(x => x.OpenedAt.ToLocalTime().Date >= from && x.OpenedAt.ToLocalTime().Date <= to);
        }
        ErrorMessage = "";
        query = SelectedSort == "Сначала старые"
            ? query.OrderBy(x => x.OpenedAt)
            : query.OrderByDescending(x => x.OpenedAt);
        Records.Clear();
        foreach (var row in query) Records.Add(row);
        VisibleCount = Records.Count;
        OnPropertyChanged(nameof(IsEmpty));
    }
}
