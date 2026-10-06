using App.Messages;
using App.Model;
using App.Views.Popups;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Services.Interfaces.service;
using System.Collections.ObjectModel;

namespace App.ViewModels.Tabs
{
    // Страница каталога
    public partial class CatalogViewModel : ObservableObject, IRecipient<ProductAddedMessage>
    {
        private readonly IProductService _productService;
        private readonly ILogger<CatalogViewModel> _logger;
        private readonly IMessenger _messenger;
        private readonly IDispatcher _dispatcher;
        private bool _isLoaded = false;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private ObservableCollection<CatalogItemModel> catalogItems = new();

        public CatalogViewModel(
            IProductService productService, 
            ILogger<CatalogViewModel> logger,
            IMessenger messenger,
            IDispatcher dispatcher)
        {
            _productService = productService;
            _logger = logger;
            _messenger = messenger;
            _dispatcher = dispatcher;

            _messenger.Register<ProductAddedMessage>(this);
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
                _logger.LogInformation("Загрузка каталога...");

                var products = await _productService.GetAllAsync();

                CatalogItems = new ObservableCollection<CatalogItemModel>(
                    products.Select(p => new CatalogItemModel
                    {
                        Id = p.Id.Value,
                        Name = p.Name,
                        Barcode = p.Barcode,
                        Price = p.Price.ToString(), //Вместо цены нули
                        Description = p.Description
                    }));

                _isLoaded = true;
                _logger.LogInformation("Загружено: {Count}", CatalogItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки каталога");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddItem()
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services == null) return;

            var popup = services.GetRequiredService<AddProductPopup>();

            var result = await Application.Current.MainPage.ShowPopupAsync(popup);
        }

        public void Receive(ProductAddedMessage message)
        {
            _dispatcher.Dispatch(() =>
            {
                try
                {
                    //var item = new CatalogItemModel 
                    //{
                    //    Id = message.product.Id.Value,
                    //    Price = message.product.Price.ToString(),
                    //    Barcode = message.product.Barcode,
                    //    Description = message.product.Description
                    //};

                    //CatalogItems.Add(item);

                     _ = LoadAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки ProductAddedMessage");
                }
            });
        }
    }
}
