using App.Messages;
using App.Model;
using App.Views.Popups;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Services.DTOs;
using Services.Interfaces.service;
using System.Collections.ObjectModel;

namespace App.ViewModels.Tabs
{
    // Страница прихода
    public partial class ReceiptViewModel : ObservableObject, IRecipient<ReceiptItemAddedMessage>
    {
        private Guid? receiptId = null;
        private bool _isLoaded = false;
        private IReceiptService _receiptService;
        private readonly IReceiptItemService _receiptItemService;
        private readonly IInventoryItemService _inventoryItemService;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<ReceiptViewModel> _logger;
        private readonly IDispatcher _dispatcher;
        private readonly IMessenger _messenger;
        private bool _receiptCreated;
        private CancellationTokenSource? _updateCts;

        [ObservableProperty] private bool isLoading;

        [ObservableProperty]
        private ObservableCollection<ReceiptItemModel> receiptItemModels = new();

        public ReceiptViewModel(
            IReceiptService receiptService,
            ILogger<ReceiptViewModel> logger,
            IReceiptItemService receiptItemService,
            IInventoryItemService inventoryItemService,
            IInventoryService inventoryService,
            IDispatcher dispatcher,
            IMessenger messenger)
        {
            receiptId = receiptId ?? Guid.NewGuid();
            //receiptService.AddReceiptAsync(receiptId.Value, default);
            _receiptService = receiptService;
            _receiptItemService = receiptItemService;
            _inventoryItemService = inventoryItemService;
            _inventoryService = inventoryService;
            _dispatcher = dispatcher;
            _logger = logger;
            messenger.Register<ReceiptItemAddedMessage>(this);
            _messenger = messenger;
        }

        private async Task EnsureReceiptCreatedAsync()
        {
            if (_receiptCreated)
                return;

            if (!receiptId.HasValue)
                receiptId = Guid.NewGuid();

            await _receiptService.AddReceiptAsync(
                receiptId.Value,
                CancellationToken.None);

            _receiptCreated = true;
        }

        public async Task OnAppearingAsync()
        {
            await EnsureReceiptCreatedAsync();

            if (!_isLoaded)
                await LoadAsync();
        }

        [RelayCommand]
        private async Task AddReceiptItem()
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services == null) return;

            var popup = services.GetRequiredService<AddReceiptItemPopup>();
            popup.Load(this.receiptId);
            var result = await Application.Current.MainPage.ShowPopupAsync(popup);

        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                _logger.LogInformation("Загрузка прихода...");

                var receipt = await _receiptService.GetByIdAsync(receiptId.Value);
                var receiptItems = receipt.Items;

                ReceiptItemModels = new ObservableCollection<ReceiptItemModel>(
                    receiptItems.Select(p => new ReceiptItemModel
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        Name = p.Product.Name,
                        Quantity = p.Quantity.ToString()
                    }));

                _isLoaded = true;
                _logger.LogInformation("Загружено: {Count}", receiptItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки прихода");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Обновляет приход, когда добавляется новый товар, запрос приход из AddReceiptItemPopup
        /// </summary>
        /// <param name="message">пустой</param>
        public void Receive(ReceiptItemAddedMessage message)
        {
            _dispatcher.Dispatch(() =>
            {
                try
                {
                    _ = LoadAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки ProductAddedMessage");
                }
            });
        }

        [RelayCommand]
        private async Task IncrQuantity(ReceiptItemModel? item)
        {
            item.Quantity = (int.Parse(item.Quantity) + 1).ToString();
            _ = DebouncedUpdateQuantityAsync(item);
        }

        [RelayCommand]
        public async Task DecrQuantity(ReceiptItemModel? item)
        {
            item.Quantity = (int.Parse(item.Quantity) - 1).ToString();
            _ = DebouncedUpdateQuantityAsync(
                
                item);
        }

        private async Task DebouncedUpdateQuantityAsync(ReceiptItemModel? item)
        {
            _updateCts?.Cancel();
            _updateCts?.Dispose();
            _updateCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(500, _updateCts.Token);

                await _receiptItemService.UpdateReceiptItemAsync(item.Id, int.Parse(item.Quantity), _updateCts.Token);
            }
            catch (OperationCanceledException)
            {
                // штатная отмена — ничего не делаем
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка обновления количества, {item.Id}, {item.Name}");
            }
        }

        [RelayCommand]
        public async Task DeleteItem(ReceiptItemModel? item)
        {
            await _receiptItemService.DelReceiptItemAsync(item.Id, default);
            ReceiptItemModels.Remove(item);
        }

        [RelayCommand]
        public async Task PushToStock()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                _receiptCreated = true;

                if (!receiptId.HasValue)
                    throw new InvalidOperationException(
                        "Не создан документ прихода.");

                var ct = CancellationToken.None;

                var inventoryId =
                    await _inventoryService.GetIdMainInventoryAsync();

                // Проводим приход, обновляем остатки
                // и записываем SoldAt внутри ConfirmAsync.
                await _receiptService.ConfirmAsync(
                    receiptId.Value,
                    inventoryId,
                    ct);

                // Очищаем интерфейс только после успешного проведения.
                ReceiptItemModels.Clear();

                // Создаём следующий документ прихода.
                receiptId = Guid.NewGuid();

                await _receiptService.AddReceiptAsync(
                    receiptId.Value,
                    ct);

                _messenger.Send(new PushToStockMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Не удалось провести приход {ReceiptId}",
                    receiptId);

                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
