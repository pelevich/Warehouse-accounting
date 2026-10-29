using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Infrastructure.Startup;
using Services.Interfaces.service;

namespace App.ViewModels;

public sealed class EmployeeOption
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = "";
    public static EmployeeOption From(Employee employee) => new()
    {
        Id = employee.Id,
        FullName = string.Join(" ", new[]
        {
            employee.LastName, employee.FirstName, employee.MiddleName
        }.Where(s => !string.IsNullOrWhiteSpace(s)))
    };
}

public partial class WelcomeViewModel : ObservableObject
{
    private readonly IStartupTask _startup;
    private readonly IEmployeeService _employees;
    private readonly IWorkShiftService _shifts;
    private readonly IAuthService _auth;
    private readonly ISessionService _session;
    private bool _initialized;

    public ObservableCollection<EmployeeOption> Employees { get; } = new();
    [ObservableProperty] private EmployeeOption? selectedEmployee;
    [ObservableProperty] private string openingCashText = "0";
    [ObservableProperty] private string login = "admin";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private bool showAdminLogin;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool hasOpenShift;
    [ObservableProperty] private string openShiftText = "";
    [ObservableProperty] private string errorMessage = "";
    public bool CanOpenNew => !HasOpenShift;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public ISessionService Session => _session;

    public WelcomeViewModel(
        IStartupTask startup,
        IEmployeeService employees,
        IWorkShiftService shifts,
        IAuthService auth,
        ISessionService session
        )
    {
        _startup = startup;
        _employees = employees;
        _shifts = shifts;
        _auth = auth;
        _session = session;
    }

    partial void OnHasOpenShiftChanged(bool value) => OnPropertyChanged(nameof(CanOpenNew));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    public async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            //if (!_initialized)
            //{
            //    await _startup.ExecuteAsync();
            //    _initialized = true;
            //}
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Не удалось запустить систему: " + ex.Message;
        }
        finally { IsBusy = false; }
    }

    private async Task RefreshAsync()
    {
        var list = await _employees.GetActiveAsync();
        Employees.Clear();
        foreach (var employee in list) Employees.Add(EmployeeOption.From(employee));
        SelectedEmployee = Employees.FirstOrDefault();
        var open = await _shifts.GetOpenShiftAsync();
        HasOpenShift = open is not null;
        OpenShiftText = open is null ? "" :
            $"Открыта смена: {open.Employee.LastName} {open.Employee.FirstName}, " +
            $"{open.OpenedAt.ToLocalTime():dd.MM.yyyy HH:mm}";
    }

    [RelayCommand]
    private void ToggleAdminLogin()
    {
        ErrorMessage = "";
        ShowAdminLogin = !ShowAdminLogin;
    }

    [RelayCommand]
    private async Task LoginAdminAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            await _auth.LoginAdminAsync(Login, Password);
            Password = "";
            ShowAdminLogin = false;
            await Shell.Current.GoToAsync("//WarehousePage");
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorMessage = ex.Message;
            Password = "";
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось выполнить вход. Попробуйте ещё раз.";
            Password = "";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenShiftAsync()
    {
        if (IsBusy) return;
        ErrorMessage = "";
        if (SelectedEmployee is null)
        {
            ErrorMessage = "Выберите сотрудника.";
            return;
        }
        if (!TryMoney(OpeningCashText, out var cash))
        {
            ErrorMessage = "Введите корректный начальный остаток наличных.";
            return;
        }
        IsBusy = true;
        try
        {
            var shift = await _shifts.OpenShiftAsync(SelectedEmployee.Id); //cash
            if (_session.IsAdmin) _session.SetCurrentShift(shift.Id);
            else _session.StartCashier(shift.EmployeeId, shift.Id);
            await Shell.Current.GoToAsync("//WarehousePage");
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ContinueShiftAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var shift = await _shifts.GetOpenShiftAsync()
                ?? throw new InvalidOperationException("Открытая смена не найдена.");
            if (_session.IsAdmin) _session.SetCurrentShift(shift.Id);
            else _session.StartCashier(shift.EmployeeId, shift.Id);
            await Shell.Current.GoToAsync("//WarehousePage");
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    public static bool TryMoney(string? text, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = text.Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture, out result) &&
               result >= 0 && decimal.Round(result, 2) == result;
    }
}
