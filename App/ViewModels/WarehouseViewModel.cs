using App.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace App.ViewModels
{
    public partial class WarehouseViewModel : ObservableObject
    {
        private readonly CatalogView _catalogView;
        private readonly StockView _stockView;
        private readonly ReceiptView _receiptView;

        private bool _initialized;

        public WarehouseViewModel(
            CatalogView catalogView, 
            StockView stockView, 
            ReceiptView receiptView)
        {
            _catalogView = catalogView;
            _stockView = stockView;
            _receiptView = receiptView;
            //CurrentView = _catalogView;
        }

        [ObservableProperty]
        public View currentView;

        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;

            CurrentView = _catalogView;
            await _catalogView.OnAppearingAsync();
        }

        [RelayCommand]
        private async Task ShowCatalog()
        {
            CurrentView = _catalogView;
            await _catalogView.OnAppearingAsync();
        }

        [RelayCommand]
        private async Task ShowStock()
        {
            CurrentView = _stockView;
            await _stockView.OnAppearingAsync();
        }

        [RelayCommand]
        private async Task ShowReceipt()
        {
            CurrentView = _receiptView;
            await _receiptView.OnAppearingAsync();
        }
    }
}
