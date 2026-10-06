using App.Messages;
using App.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Services.Interfaces.service;
using System.Collections.ObjectModel;

namespace App.ViewModels.Popups
{
    public partial class AddReceiptItemViewModel : ObservableObject
    {
        public event EventHandler<bool>? CloseRequested;
        public readonly IProductService _productService;
        public ILogger<AddReceiptItemViewModel> _logger;
        public readonly IReceiptItemService _receiptItemService;
        public readonly IBarcodeService _barcodeService;
        public Guid receiptId;
        private readonly IMessenger _messenger;

        private CancellationTokenSource? _searchCts;

        [ObservableProperty] private string _nameText = string.Empty;
        [ObservableProperty] private string _barcodeText = string.Empty;
        [ObservableProperty] private bool   _hasSuggestions;
        [ObservableProperty] private string description;
        [ObservableProperty] private double price;
        [ObservableProperty] private string quantity;

        [ObservableProperty] private string? nameError;
        [ObservableProperty] private string? priceError;
        [ObservableProperty] private string? barcodeError;
        [ObservableProperty] private string? descriptionError;
        [ObservableProperty] private string? quantityError;

        private bool _suppressSearch = false;

        public ObservableCollection<ProductSuggestion> Suggestions { get; } = new();

        public AddReceiptItemViewModel(
            IProductService                  productService,
            ILogger<AddReceiptItemViewModel> logger,
            IReceiptItemService              receiptItemService,
            IBarcodeService                  barcodeService,
            IMessenger                       messenger)
        {
            _productService = productService;
            _logger = logger;
            _receiptItemService = receiptItemService;
            _barcodeService = barcodeService;
            _messenger = messenger;
            Quantity = "1";
        }

        public void Load(Guid? receiptId)
        {
            this.receiptId = receiptId.Value;
        }

        partial void OnNameTextChanged(string value)
        {
            if (_suppressSearch == true) return;
            _ = DebouncedSearchAsync(value);
        }
        partial void OnBarcodeTextChanged(string value) 
        {
            if (_suppressSearch == true) return;
            _ = DebouncedSearchAsync(value);
        }

        private async Task DebouncedSearchAsync(string value)
        {
            // Отменяем предыдущий поиск
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();

            try
            {
                // Ждём 300 мс — если пользователь продолжает печатать, поиск не запустится
                await Task.Delay(300, _searchCts.Token);

                await PerformSearchAsync(value, _searchCts.Token);
            }
            catch (TaskCanceledException) 
            {
                //nameFlag = barcodeFlag = false; !!!!!!!!! может на что-то повлиять наверно пока ХЗ
            }
        }

        private async Task PerformSearchAsync(string query, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                HasSuggestions = false;
                //nameFlag = barcodeFlag = false;         !!!!!!!!!!!!!!!!!!! может на что-то повлиять наверно пока ХЗ
                return;
            }

            try
            {
                var results = await _productService.SearchAsync(query.Trim(), ct);

                if (ct.IsCancellationRequested) return;

                Suggestions.Clear();
                foreach (var p in results)
                {
                    Suggestions.Add(new ProductSuggestion
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Barcode = p.Barcode,
                        Price = p.Price,
                        Description = p.Description,

                    });
                }
                HasSuggestions = Suggestions.Count > 0;

            }
            catch (OperationCanceledException) { /* ок */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска товара");
                HasSuggestions = false;
            }
        }

        [RelayCommand]
        private void SelectSuggestion(ProductSuggestion? suggestion)
        {
            if (suggestion is null) return;

            _suppressSearch = true;
            try
            {
                NameText = suggestion.Name;
                BarcodeText = suggestion.Barcode;
                Description = suggestion.Description;
                Price = suggestion.Price;
                HasSuggestions = false;
            }
            finally
            {
                _suppressSearch = false;
            }
        }

        [RelayCommand]
        private async Task Close()
        {
            CloseRequested?.Invoke(this, false);
        }

        [RelayCommand]
        private async Task Add()
        {
            NameError = BarcodeError = QuantityError = null;

            var hasErrors = false;

            if (new[] { NameText, BarcodeText }.Any(string.IsNullOrWhiteSpace) || !BarcodeText.All(char.IsDigit))
            {
                NameError = string.IsNullOrWhiteSpace(NameText) ? "Обязательное поле" : null;
                BarcodeError = string.IsNullOrWhiteSpace(BarcodeText) ? "Обязательное поле" : null;

                if (BarcodeText.All(char.IsDigit) && BarcodeText.Length == 13)
                {
                    var resultValid = await _barcodeService.ValideCode(BarcodeText);
                    if (resultValid == false)
                    {
                        BarcodeError = "Не валидный штрих-код";
                    }
                }
                else
                {
                    BarcodeError = "Штрих-код должен быть длиной 13 символо и состоять только из цифр";
                }

                hasErrors = true;
            }

            if(!(int.TryParse(Quantity, out var q) && q > 0))
            {
                QuantityError = "Количество должно быть целым числом больше 0";
                hasErrors = true; ;
            }

            if (hasErrors) return;

            await _receiptItemService.AddItemAsync(BarcodeText, int.Parse(Quantity), receiptId, default);
            _messenger.Send(new ReceiptItemAddedMessage());
            CloseRequested?.Invoke(this, true);
        }
    }
}
