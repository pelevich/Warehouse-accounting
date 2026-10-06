using App.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Services.DTOs;
using Services.Interfaces.service;

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
                Id = Guid.NewGuid(),
                Name = Name,
                Price = priceDouble,
                Barcode = Barcode,
                Description = Description
            };

            var catalog = await _productCatalogService.FindByNameAsync("Main catalog");
            await _productService.AddProductAsync(product, catalog.Id.Value);
            _messenger.Send(new ProductAddedMessage(product));
            CloseRequested?.Invoke(this, true);
        }
    }
}
