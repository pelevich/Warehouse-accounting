using App.Views;
using App.Views.Popups;
using App.Views.Trade;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services.DTO;
using Services.Interfaces.service;

namespace App.ViewModels
{
    public partial class TradeViewModel : ObservableObject
    {

        private readonly SaleView _saleView;
        private readonly EmployeesView _employeesView;
        //private readonly HistoryView _salesHistoryView;
        public ISessionService Session { get; }

        [ObservableProperty]
        public View currentView;


        public TradeViewModel(SaleView saleView,
            ISessionService session,
            EmployeesView employeesView
            )
        {
            _saleView = saleView;
            CurrentView = saleView;
            Session = session;
            _employeesView = employeesView;
        }

        [RelayCommand]
        private async Task ManageShift()
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services == null) return;

            var popup = services.GetRequiredService<ManageShiftPopup>();

            await popup.InitializeAsync();

            await Shell.Current.ShowPopupAsync(popup);
        }

        [RelayCommand]
        public async Task WarehousePage() => await Shell.Current.GoToAsync($"//{nameof(WarehousePage)}");

        [RelayCommand]
        public async Task HistoryPage() =>
            await Shell.Current.GoToAsync($"//{nameof(HistoryPage)}");

        [RelayCommand]
        private async Task ShowEmployees()
        {
            CurrentView = _employeesView;
        }

        //[RelayCommand]
        //private async Task ShowSalesHistory()
        //{
        //    CurrentView = _salesHistoryView;

        //    await _salesHistoryView.ShowAsync(
        //        HistoryKind.Sales);
        //}
    }
}
