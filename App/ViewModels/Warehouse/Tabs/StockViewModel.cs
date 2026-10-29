using App.Messages;
using App.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Services.Interfaces.service;
using System.Collections.ObjectModel;

namespace App.ViewModels.Tabs
{
    // Страница склада
    public partial class StockViewModel : ObservableObject, IRecipient<PushToStockMessage>
    {
        private readonly IInventoryItemService _inventoryItemService;
        private readonly IProductService _productService;
        private ILogger<StockViewModel> _logger;
        private bool _isLoaded = false;

        private readonly IMessenger _messenger;
        private readonly IDispatcher _dispatcher;

        [ObservableProperty] private bool isLoading;
        [ObservableProperty]
        private ObservableCollection<InventoryItemModel> inventoryItems = new();

        public StockViewModel(IInventoryItemService inventoryItemService,
            IProductService productService,
            ILogger<StockViewModel> logger,
            IMessenger messenger,
            IDispatcher dispatcher)
        {
            _inventoryItemService = inventoryItemService;
            _productService = productService;
            _logger = logger;

            _messenger = messenger;
            _dispatcher = dispatcher;

            _messenger.Register<PushToStockMessage>(this);
        }

        public async Task OnAppearingAsync()
        {
            if (!_isLoaded)
            {
                await LoadAsync();
            }
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                _logger.LogInformation("Загрузка склада...");

                var items = await _inventoryItemService.GetAllAsync();

                InventoryItems = new ObservableCollection<InventoryItemModel>();

                foreach (var item in items) 
                {
                    var product = await _productService.GetByIdAsync(item.ProductId.Value);

                    var i = new InventoryItemModel
                    {
                        Id = item.Id.Value,
                        Name = product.Name,
                        Barcode = product.Barcode,
                        Price = product.Price.Value,
                        Quantity = item.Quantity.Value,
                    };

                    InventoryItems.Add(i);
                }

                _isLoaded = true;
                _logger.LogInformation("Загружено: {Count}", InventoryItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки склада");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void Receive(PushToStockMessage message)
        {
            _dispatcher.Dispatch(() =>
            {
                try
                {
                    _ = LoadAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка не удается обновить остаток");
                }
            });
        }
    }
}
