using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services.Interfaces.service;

namespace App.ViewModels.Popups;

public partial class ManageShiftViewModel : ObservableObject
{
    private readonly IWorkShiftService _shifts;
    private Guid _shiftId;
    private decimal _expectedCash;
    private decimal _actualCash;

    public event Action? CloseRequested;

    [ObservableProperty] private string actualCashText = "0,00";
    [ObservableProperty] private string expectedCashText = "0,00 ₽";
    [ObservableProperty] private string actualCashReportText = "0,00 ₽";
    [ObservableProperty] private string differenceText = "0,00 ₽";
    [ObservableProperty] private Color differenceColor = Color.FromArgb("#21834D");
    [ObservableProperty] private string employeeName = "";
    [ObservableProperty] private int salesCount;
    [ObservableProperty] private int soldUnits;
    [ObservableProperty] private string totalRevenueText = "0,00 ₽";
    [ObservableProperty] private string cashRevenueText = "0,00 ₽";
    [ObservableProperty] private string cardRevenueText = "0,00 ₽";
    [ObservableProperty] private string errorMessage = "";
    [ObservableProperty] private bool isInputVisible = true;
    [ObservableProperty] private bool isReportVisible;
    [ObservableProperty] private bool isBusy;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool ShiftWasClosed { get; private set; }

    public ManageShiftViewModel(IWorkShiftService shifts) => _shifts = shifts;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsNotBusy));
    partial void OnActualCashTextChanged(string value)
    {
        if (HasError) ErrorMessage = "";
    }

    private static string Money(decimal value) => value.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var shift = await _shifts.GetOpenShiftAsync()
                ?? throw new InvalidOperationException("Нет открытой смены.");
            _shiftId = shift.Id;

            // В текущей модели нет начального остатка, внесений и изъятий.
            // Поэтому ожидаемые наличные = сумма наличных продаж смены.
            var report = await _shifts.GetShiftReportAsync(_shiftId);
            _expectedCash = Convert.ToDecimal(report.CashRevenue);
            ExpectedCashText = Money(_expectedCash);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (!IsBusy && !ShiftWasClosed) CloseRequested?.Invoke();
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (IsBusy) return;
        if (_shiftId == Guid.Empty)
        {
            ErrorMessage = "Данные смены не загружены.";
            return;
        }

        if (!TryParseCash(ActualCashText, out var actual))
        {
            ErrorMessage = "Введите корректную неотрицательную сумму (до 2 знаков после запятой).";
            return;
        }

        IsBusy = true;
        ErrorMessage = "";
        try
        {
            // В текущем интерфейсе CloseShiftAsync принимает только shiftId.
            // Фактическая сумма пока отображается в отчёте, но НЕ сохраняется в БД.
            var report = await _shifts.CloseShiftAsync(_shiftId);
            _actualCash = actual;
            ShiftWasClosed = true;

            EmployeeName = report.EmployeeFullName;
            SoldUnits = report.SoldUnits;
            SalesCount = report.SalesCount;
            TotalRevenueText = Money(Convert.ToDecimal(report.TotalRevenue));
            CashRevenueText = Money(Convert.ToDecimal(report.CashRevenue));
            CardRevenueText = Money(Convert.ToDecimal(report.CardRevenue));
            _expectedCash = Convert.ToDecimal(report.CashRevenue);
            ExpectedCashText = Money(_expectedCash);
            ActualCashReportText = Money(_actualCash);

            var difference = _actualCash - _expectedCash;
            DifferenceText = (difference > 0 ? "+" : "") + Money(difference);
            DifferenceColor = difference == 0
                ? Color.FromArgb("#21834D")
                : Color.FromArgb("#C07B32");

            IsInputVisible = false;
            IsReportVisible = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Не удалось закрыть смену: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Done()
    {
        if (ShiftWasClosed) CloseRequested?.Invoke();
    }

    private static bool TryParseCash(string? input, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var normalized = input.Trim().Replace(" ", "").Replace("\u00A0", "").Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out result)) return false;
        return result >= 0 && decimal.Round(result, 2) == result;
    }
}
