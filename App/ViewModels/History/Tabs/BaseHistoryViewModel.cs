using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services.DTO;
using Services.Interfaces.service;
using System.Collections.ObjectModel;
using System.Globalization;

namespace App.ViewModels.Tabs;

public sealed class HistoryEmployeeOption
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = "";
}

public partial class HistoryRow : ObservableObject
{
    public Guid Id { get; init; }
    public DateTime? DateUtc { get; init; }
    public Guid? EmployeeId { get; init; }
    public string EmployeeName { get; init; } = "";
    public int TotalUnits { get; init; }
    public string AmountText { get; init; } = "";
    public string PaymentType { get; init; } = "";
    public string ShiftText { get; init; } = "";
    public string StatusText { get; init; } = "";
    public bool IsDraft { get; init; }
    public ObservableCollection<HistoryLineDto> Lines { get; } = new();

    public string DateText => DateUtc.HasValue
        ? DateUtc.Value.ToLocalTime().ToString("dd.MM.yyyy  HH:mm")
        : "Дата не указана";

    public string UnitsText => $"{TotalUnits} шт.";
    public string SummaryText => string.IsNullOrEmpty(AmountText)
        ? UnitsText : $"{UnitsText}  ·  {AmountText}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Arrow))]
    private bool isExpanded;

    public string Arrow => IsExpanded ? "⌃" : "⌄";

    [RelayCommand]
    private void Toggle() => IsExpanded = !IsExpanded;
}

public partial class BaseHistoryViewModel : ObservableObject
{
    private readonly IHistoryService _history;
    private HistoryKind _kind;
    private IReadOnlyList<HistoryRecordDto> _source = Array.Empty<HistoryRecordDto>();
    private bool _loading;

    public ObservableCollection<HistoryRow> Records { get; } = new();
    public ObservableCollection<HistoryEmployeeOption> Employees { get; } = new();
    public IReadOnlyList<string> SortOptions { get; } = new[]
    {
        "Сначала новые", "Сначала старые", "По сотруднику", "По количеству"
    };

    [ObservableProperty] private string title = "История операций";
    [ObservableProperty] private string subtitle = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = "";
    [ObservableProperty] private string emptyMessage = "Записей не найдено";
    [ObservableProperty] private int visibleCount;
    [ObservableProperty] private bool useDateFilter;
    [ObservableProperty] private DateTime dateFrom = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime dateTo = DateTime.Today;
    [ObservableProperty] private HistoryEmployeeOption? selectedEmployee;
    [ObservableProperty] private string selectedSort = "Сначала новые";
    [ObservableProperty] private bool showDrafts;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsEmpty => !IsBusy && Records.Count == 0;

    public BaseHistoryViewModel(IHistoryService history) => _history = history;

    partial void OnErrorMessageChanged(string value) =>
        OnPropertyChanged(nameof(HasError));

    partial void OnUseDateFilterChanged(bool value) => ApplyFilters();
    partial void OnDateFromChanged(DateTime value) => ApplyFilters();
    partial void OnDateToChanged(DateTime value) => ApplyFilters();
    partial void OnSelectedEmployeeChanged(HistoryEmployeeOption? value) => ApplyFilters();
    partial void OnSelectedSortChanged(string value) => ApplyFilters();
    partial void OnShowDraftsChanged(bool value) => ApplyFilters();

    public async Task ShowAsync(HistoryKind kind)
    {
        _kind = kind;
        Title = kind == HistoryKind.Receipts ? "История прихода" : "История продаж";
        Subtitle = kind == HistoryKind.Receipts
            ? "Поступления товаров, сотрудники и состав документов"
            : "Проведённые чеки, сотрудники и проданные товары";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_loading) return;
        _loading = true;
        IsBusy = true;
        ErrorMessage = "";
        OnPropertyChanged(nameof(IsEmpty));

        try
        {
            var employees = await _history.GetEmployeesAsync();
            _source = await _history.GetHistoryAsync(_kind);

            Employees.Clear();
            Employees.Add(new HistoryEmployeeOption { Id = null, Name = "Все сотрудники" });
            foreach (var employee in employees)
                Employees.Add(new HistoryEmployeeOption { Id = employee.Id, Name = employee.Name });

            var selectedId = SelectedEmployee?.Id;
            SelectedEmployee = Employees.FirstOrDefault(e => e.Id == selectedId) ?? Employees[0];
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Не удалось загрузить историю: " + ex.Message;
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
        ShowDrafts = false;
        SelectedSort = SortOptions[0];
        SelectedEmployee = Employees.FirstOrDefault();
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (UseDateFilter && DateFrom.Date > DateTo.Date)
        {
            ErrorMessage = "Дата «С» не может быть позже даты «По».";
            Records.Clear();
            VisibleCount = 0;
            OnPropertyChanged(nameof(IsEmpty));
            return;
        }

        ErrorMessage = "";

        IEnumerable<HistoryRecordDto> query = _source;
        if (!ShowDrafts)
            query = query.Where(r => r.IsCompleted != false);

        if (UseDateFilter)
        {
            // DatePicker задаёт локальные даты, даты в БД хранятся в UTC.
            var fromUtc = DateTime.SpecifyKind(DateFrom.Date, DateTimeKind.Local).ToUniversalTime();
            var toUtc = DateTime.SpecifyKind(DateTo.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();
            query = query.Where(r => r.DateUtc.HasValue
                && r.DateUtc.Value >= fromUtc && r.DateUtc.Value < toUtc);
        }

        if (SelectedEmployee?.Id is Guid id)
            query = query.Where(r => r.EmployeeId == id);

        query = SelectedSort switch
        {
            "Сначала старые" => query.OrderBy(r => r.DateUtc ?? DateTime.MinValue),
            "По сотруднику" => query.OrderBy(r => r.EmployeeName).ThenByDescending(r => r.DateUtc),
            "По количеству" => query.OrderByDescending(r => r.TotalUnits).ThenByDescending(r => r.DateUtc),
            _ => query.OrderByDescending(r => r.DateUtc ?? DateTime.MinValue)
        };

        Records.Clear();
        foreach (var item in query)
        {
            var row = new HistoryRow
            {
                Id = item.Id,
                DateUtc = item.DateUtc,
                EmployeeId = item.EmployeeId,
                EmployeeName = item.EmployeeName,
                TotalUnits = item.TotalUnits,
                AmountText = item.TotalAmount.HasValue
                    ? $"{item.TotalAmount.Value.ToString("N2", CultureInfo.GetCultureInfo("ru-RU"))} ₽"
                    : "",
                PaymentType = item.PaymentType,
                ShiftText = item.WorkShiftId.HasValue
                    ? item.WorkShiftId.Value.ToString("D")
                    : "Не привязана",
                StatusText = item.IsCompleted switch
                {
                    true => "Проведено",
                    false => "Черновик",
                    _ => "Статус неизвестен"
                },
                IsDraft = item.IsCompleted == false
            };
            foreach (var line in item.Lines) row.Lines.Add(line);
            Records.Add(row);
        }

        VisibleCount = Records.Count;
        OnPropertyChanged(nameof(IsEmpty));
    }
}
