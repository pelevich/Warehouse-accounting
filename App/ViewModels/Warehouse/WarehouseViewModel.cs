using App.Views;
using App.Views.Popups;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services.DTO;
using Services.Interfaces.service;

namespace App.ViewModels
{
    public partial class WarehouseViewModel : ObservableObject
    {
        private readonly CatalogView _catalogView;
        private readonly StockView _stockView;
        private readonly ReceiptView _receiptView;
        private readonly EmployeesView _employeesView;
        //private readonly HistoryView _receiptHistoryView;
        public ISessionService Session { get; }
        private bool _initialized;

        [ObservableProperty]
        private View? currentView;

        [ObservableProperty]
        private bool isCatalogSelected = true;

        [ObservableProperty]
        private bool isStockSelected;

        [ObservableProperty]
        private bool isReceiptSelected;

        public WarehouseViewModel(
            CatalogView catalogView,
            StockView stockView,
            ReceiptView receiptView,
            ISessionService session,
            EmployeesView employeesView)
        {
            _catalogView = catalogView;
            _stockView = stockView;
            _receiptView = receiptView;
            Session = session;
            _employeesView = employeesView;
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;

            CurrentView = _catalogView;
            SetActiveTab(0);
            await _catalogView.OnAppearingAsync();
        }

        private void SetActiveTab(int index)
        {
            IsCatalogSelected = index == 0;
            IsStockSelected = index == 1;
            IsReceiptSelected = index == 2;
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



        //[RelayCommand]
        //private Task SignOut() => _shiftUi.SignOutAsync();

        [RelayCommand]
        public async Task SalePage() =>
            await Shell.Current.GoToAsync($"//{nameof(SalePage)}");

        [RelayCommand]
        public async Task HistoryPage() =>
            await Shell.Current.GoToAsync($"//{nameof(HistoryPage)}");

        [RelayCommand]
        private async Task ShowCatalog()
        {
            CurrentView = _catalogView;
            SetActiveTab(0);
            await _catalogView.OnAppearingAsync();
        }

        [RelayCommand]
        private async Task ShowStock()
        {
            CurrentView = _stockView;
            SetActiveTab(1);
            await _stockView.OnAppearingAsync();
        }

        [RelayCommand]
        private async Task ShowReceipt()
        {
            CurrentView = _receiptView;
            SetActiveTab(2);
            await _receiptView.OnAppearingAsync();
        }

        [RelayCommand]
        private async Task ShowEmployees()
        {
            CurrentView = _employeesView;
            SetActiveTab(3);
        }

        //[RelayCommand]
        //private async Task ShowReceiptHistory()
        //{
        //    CurrentView = _receiptHistoryView;

        //    await _receiptHistoryView.ShowAsync(
        //        HistoryKind.Receipts);
        //}
    }
}
