using App.Messages;
using App.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Services.DTOs;
using Services.Interfaces.service;

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

        double priceDouble;

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
            if (new[] { Name, Barcode, Price, Description }.Any(string.IsNullOrWhiteSpace) || !Barcode.All(char.IsDigit))
            {
                NameError = string.IsNullOrWhiteSpace(Name) ? "Обязательное поле" : null;
                DescriptionError = string.IsNullOrWhiteSpace(Description) ? "Обязательное поле" : null;
                BarcodeError = string.IsNullOrWhiteSpace(Barcode) ? "Обязательное поле" : null;
                if (double.TryParse(Price, out var priceDouble))
                {
                    priceDouble = double.Parse(Price);
                }
                else
                {
                    PriceError = "Обязательное поле";
                }

                if (Barcode.All(char.IsDigit) && Barcode.Length == 13)
                {
                    var resultValid = await _barcodeService.ValideCode(Barcode);
                    if (resultValid == false)
                    {
                        BarcodeError = "Не валидный штрих-код";
                    }
                }
                else
                {
                    BarcodeError = "Штрих-код должен быть длиной 13 символо и состоять только из цифр";
                }

                return;
            }

            var product = new ProductDto
            {
                Id = id,
                Name = Name,
                Price = priceDouble,
                Barcode = Barcode,
                Description = Description
            };

            await _productService.UpdateProductAsync(product);
            _messenger.Send(new ProductAddedMessage(product));
            CloseRequested?.Invoke(this, true);
        }
    }
}
