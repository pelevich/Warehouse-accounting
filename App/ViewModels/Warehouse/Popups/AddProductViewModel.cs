
using App.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Services.DTOs;
using Services.Interfaces.service;
using System.Globalization;

namespace App.ViewModels.Popups
{
    public partial class AddProductViewModel : ObservableObject
    {
        public event EventHandler<bool>? CloseRequested;

        private readonly IBarcodeService _barcodeService;
        private readonly IProductService _productService;
        private readonly IProductCatalogService _productCatalogService;
        private readonly IMessenger _messenger;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string price = string.Empty;

        [ObservableProperty]
        private string barcode = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private string? nameError;

        [ObservableProperty]
        private string? priceError;

        [ObservableProperty]
        private string? barcodeError;

        [ObservableProperty]
        private string? descriptionError;

        public AddProductViewModel(
            IBarcodeService barcodeService,
            IProductService productService,
            IProductCatalogService productCatalogService,
            IMessenger messenger)
        {
            _barcodeService = barcodeService;
            _productService = productService;
            _productCatalogService = productCatalogService;
            _messenger = messenger;
        }

        public async Task InitializeAsync()
        {
            Barcode = await _barcodeService.GenerateBarcodeNumber();
        }

        [RelayCommand]
        private async Task GenerateBarcode()
        {
            Barcode = await _barcodeService.GenerateBarcodeNumber();
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, false);
        }

        [RelayCommand]
        private async Task Save()
        {
            // Очищаем предыдущие ошибки.
            NameError = null;
            PriceError = null;
            BarcodeError = null;
            DescriptionError = null;

            bool hasErrors = false;

            // Название товара.
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = "Обязательное поле";
                hasErrors = true;
            }

            // Описание товара.
            if (string.IsNullOrWhiteSpace(Description))
            {
                DescriptionError = "Обязательное поле";
                hasErrors = true;
            }

            // Цена товара.
            decimal parsedPrice = 0;

            if (string.IsNullOrWhiteSpace(Price))
            {
                PriceError = "Обязательное поле";
                hasErrors = true;
            }
            else
            {
                // Принимаем как 150,50, так и 150.50.
                string normalizedPrice = Price.Trim()
                    .Replace(',', '.');

                if (!decimal.TryParse(
                    normalizedPrice,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out parsedPrice))
                {
                    PriceError = "Введите корректную цену";
                    hasErrors = true;
                }
                else if (parsedPrice < 0)
                {
                    PriceError = "Цена не может быть отрицательной";
                    hasErrors = true;
                }
            }

            // Штрихкод.
            string trimmedBarcode = Barcode?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedBarcode))
            {
                BarcodeError = "Обязательное поле";
                hasErrors = true;
            }
            else if (trimmedBarcode.Length != 13 ||
                     !trimmedBarcode.All(c => c >= '0' && c <= '9'))
            {
                BarcodeError =
                    "Штрихкод должен содержать ровно 13 цифр";
                hasErrors = true;
            }
            else
            {
                bool isValid =
                    await _barcodeService.ValideCode(trimmedBarcode);

                if (!isValid)
                {
                    BarcodeError = "Недействительный штрихкод";
                    hasErrors = true;
                }
            }

            // Если есть ошибки, товар не добавляем.
            if (hasErrors)
                return;

            var product = new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = Name.Trim(),
                Price = parsedPrice,
                Barcode = trimmedBarcode,
                Description = Description.Trim()
            };

            // Получаем основной каталог.
            var catalog = await _productCatalogService
                .FindByNameAsync("Main catalog");

            if (catalog?.Id == null)
                throw new InvalidOperationException(
                    "Основной каталог не найден.");

            // Сохраняем товар.
            await _productService.AddProductAsync(
                product,
                catalog.Id.Value);

            // Уведомляем интерфейс об изменении каталога.
            _messenger.Send(new ProductAddedMessage(product));

            // Закрываем окно после успешного сохранения.
            CloseRequested?.Invoke(this, true);
        }
    }
}
