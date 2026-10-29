using App.Messages;
using App.Model;
using App.Views.Popups;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Services.Interfaces.service;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace App.ViewModels.Tabs
{
    // Страница каталога
    public partial class CatalogViewModel : ObservableObject, IRecipient<ProductAddedMessage>
    {
        private readonly IProductService _productService;
        private readonly IBarcodeService _barcodeService;
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
            IBarcodeService barcodeService,
            ILogger<CatalogViewModel> logger,
            IMessenger messenger,
            IDispatcher dispatcher)
        {
            _productService = productService;
            _barcodeService = barcodeService;
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

        /// <summary>
        /// Функция для открытия popup редактирования существуещего товара. В идеале перенести в CatalogViewModel
        /// </summary>
        /// <returns>Открывает popup</returns>
        [RelayCommand]
        private async Task EditItem(CatalogItemModel item)
        {
            if (item is null)
                return;

            var services = Application.Current?
                .Handler?
                .MauiContext?
                .Services;

            if (services is null)
                return;

            var popup = services.GetRequiredService<EditProductPopup>();

            popup.Load(item);

            await Application.Current.MainPage.ShowPopupAsync(popup);
        }

        [RelayCommand]
        private async Task ShowBarcode(CatalogItemModel item) 
        {
            try
            {
                if (item.barcodePng is null)
                    await PreviewBarcode(item);

                var services = Application.Current?
                    .Handler?
                    .MauiContext?
                    .Services;

                if (services is null)
                    return;

                var popup = services.GetRequiredService<ShowBarcodeImgPopup>();

                popup.Load(item.BarcodeImage, item.barcodePng);

                await Application.Current.MainPage.ShowPopupAsync(popup);
            }
            catch (Exception ex) 
            {
                await Shell.Current.DisplayAlert(
                    "Ошибка генерации",
                    $"Не удалось создать штрихкод: {ex.Message} \n" +
                    $"Попробуйте обновить штрихкод",
                    "ОК"
                );
            }
        }

        private async Task PreviewBarcode(CatalogItemModel item)
        {
            using var image = _barcodeService.GenerateBarcodeCodeImg(item.Barcode);

            using var data = image.Encode(
                SKEncodedImageFormat.Png,
                100
            );

            item.barcodePng = data.ToArray();

            var bytes = item.barcodePng;

            item.BarcodeImage = ImageSource.FromStream(
                () => new MemoryStream(bytes, writable: false)
            );
        }

        [RelayCommand]
        private async Task SaveBarcodeImage(CatalogItemModel item)
        {
            if (item.barcodePng is null)
                await PreviewBarcode(item);

            using var stream = new MemoryStream(item.barcodePng);

            var result = await FileSaver.Default.SaveAsync(
                $"{item.name}_штрихкод_{item.barcode}.png",
                stream,
                CancellationToken.None
            );
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
