using App.Views;
using App.Views.Popups;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services.Interfaces.service;

namespace App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly SalesHistoryView _salesHistory;
    private readonly ReceiptsHistoryView _receiptsHistory;
    private readonly ShiftHistoryView _shiftsHistory;
    private readonly EmployeesView _employeesView;
    private int _currentTab = -1;

    public ISessionService Session { get; }

    [ObservableProperty]
    private View? currentView;

    [ObservableProperty]
    private bool isSalesSelected = true;

    [ObservableProperty]
    private bool isReceiptsSelected;

    [ObservableProperty]
    private bool isShiftsSelected;

    public HistoryViewModel(
        SalesHistoryView salesHistory,
        ReceiptsHistoryView receiptsHistory,
        ShiftHistoryView shiftsHistory,
        EmployeesView employeesView,
        ISessionService session)
    {
        _salesHistory = salesHistory;
        _receiptsHistory = receiptsHistory;
        _shiftsHistory = shiftsHistory;
        _employeesView = employeesView;
        Session = session;
    }

    public Task InitializeAsync() => SwitchAsync(0);

    private async Task SwitchAsync(int tab)
    {
        if (_currentTab == tab)
            return;

        _currentTab = tab;
        IsSalesSelected = tab == 0;
        IsReceiptsSelected = tab == 1;
        IsShiftsSelected = tab == 2;

        switch (tab)
        {
            case 0:
                CurrentView = _salesHistory;
                await _salesHistory.LoadAsync();
                break;
            case 1:
                CurrentView = _receiptsHistory;
                await _receiptsHistory.LoadAsync();
                break;
            case 2:
                CurrentView = _shiftsHistory;
                await _shiftsHistory.LoadAsync();
                break;
        }
    }

    [RelayCommand]
    private Task ShowSalesAsync() => SwitchAsync(0);

    [RelayCommand]
    private Task ShowReceiptsAsync() => SwitchAsync(1);

    [RelayCommand]
    private Task ShowShiftsAsync() => SwitchAsync(2);

    [RelayCommand]
    private Task GoWarehouseAsync() => Shell.Current.GoToAsync($"//{nameof(WarehousePage)}");

    [RelayCommand]
    private Task GoSaleAsync() => Shell.Current.GoToAsync($"//{nameof(SalePage)}");

    // Как в исходном HistoryPage: управление сменой осталось на странице склада.

    [RelayCommand]
    private void ShowEmployees()
    {
        _currentTab = -1;
        IsSalesSelected = false;
        IsReceiptsSelected = false;
        IsShiftsSelected = false;
        CurrentView = _employeesView;
    }

    [RelayCommand]
    private async Task GoShiftAsync()
    {
        var services = Application.Current?.Handler?.MauiContext?.Services;
        if (services == null) return;

        var popup = services.GetRequiredService<ManageShiftPopup>();

        await popup.InitializeAsync();

        await Shell.Current.ShowPopupAsync(popup);
    }
}
