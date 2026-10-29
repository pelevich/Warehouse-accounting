using App.Messages;
using App.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Services.DTOs;
using Services.Interfaces.service;
using System.Diagnostics;

namespace App.ViewModels.Popups
{
    public partial class EditProductViewModel : ObservableObject
    {
        public event EventHandler<bool>? CloseRequested;
        private readonly IBarcodeService _barcodeService;
        private readonly IProductService _productService;
        private readonly IMessenger _messenger;

        private Guid id;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string price;

        [ObservableProperty]
        private string barcode;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private string? nameError;

        [ObservableProperty]
        private string? priceError;

        [ObservableProperty]
        private string? barcodeError;

        [ObservableProperty]
        private string? descriptionError;

        decimal priceDecimal;

        public EditProductViewModel(
            IBarcodeService barcodeService,
            IProductService productService,
            IMessenger messenger)
        {
            _barcodeService = barcodeService;
            _productService = productService;
            _messenger = messenger;
        }

        public void Load(CatalogItemModel item)
        {
            id = item.Id;
            Name = item.Name;
            Barcode = item.Barcode;
            Price = item.Price;
            Description = item.Description;
        }

        [RelayCommand]
        private async Task GenerateBarcode()
        {
            Barcode = await _barcodeService.GenerateBarcodeNumber();
        }

        [RelayCommand]
        private async Task Close()
        {
            CloseRequested?.Invoke(this, false);
        }


        [RelayCommand]
        private async Task Save()
        {
            // Очищаем предыдущие ошибки
            NameError = null;
            PriceError = null;
            BarcodeError = null;
            DescriptionError = null;

            bool hasErrors = false;

            // Проверка названия
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = "Обязательное поле";
                hasErrors = true;
            }

            // Проверка описания
            if (string.IsNullOrWhiteSpace(Description))
            {
                DescriptionError = "Обязательное поле";
                hasErrors = true;
            }

            // Проверка цены
            if (string.IsNullOrWhiteSpace(Price))
            {
                PriceError = "Обязательное поле";
                hasErrors = true;
            }
            else if (!decimal.TryParse(Price, out var parsedPrice))
            {
                PriceError = "Введите корректную цену";
                hasErrors = true;
            }
            else if (parsedPrice < 0)
            {
                PriceError = "Цена не может быть отрицательной";
                hasErrors = true;
            }

            // Проверка штрихкода
            if (string.IsNullOrWhiteSpace(Barcode))
            {
                BarcodeError = "Обязательное поле";
                hasErrors = true;
            }
            else if (Barcode.Length != 13 ||
                     !Barcode.All(char.IsDigit))
            {
                BarcodeError =
                    "Штрихкод должен состоять из 13 цифр";
                hasErrors = true;
            }
            else
            {
                bool isValid =
                    await _barcodeService.ValideCode(Barcode);

                if (!isValid)
                {
                    BarcodeError = "Недействительный штрихкод";
                    hasErrors = true;
                }
            }

            if (hasErrors)
                return;

            // Преобразуем цену независимо от остальных проверок
            decimal priceDecimal = decimal.Parse(Price);

            var product = new ProductDto
            {
                Id = id,
                Name = Name.Trim(),
                Price = priceDecimal,
                Barcode = Barcode.Trim(),
                Description = Description.Trim()
            };

            await _productService.UpdateProductAsync(product);

            // Уведомляем каталог об изменении товара
            _messenger.Send(new ProductAddedMessage(product));

            CloseRequested?.Invoke(this, true);
        }

    }
}
