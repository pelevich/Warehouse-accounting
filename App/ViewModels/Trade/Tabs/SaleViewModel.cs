using App.Model.Trade;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Services.Interfaces.service;
using System.Collections.ObjectModel;
using System.Globalization;

namespace App.ViewModels.Trade.Tabs
{
    public partial class SaleViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly ISaleService _saleService;
        private readonly IInventoryItemService _inventoryItemService;
        private readonly ILogger<SaleViewModel> _logger;

        // Товары в чеке и результаты поиска.
        [ObservableProperty]
        private ObservableCollection<SaleItemModel> saleItemModels = new();

        [ObservableProperty]
        private ObservableCollection<SearchItemModel> searchItemModels = new();

        [ObservableProperty]
        private string selectedOption = "Наличный способ";

        [ObservableProperty]
        private string change = string.Empty;

        // Вводимая кассиром сумма. Строка нужна, чтобы пользователь мог
        // спокойно набрать число, включая промежуточные состояния вроде "100,".
        [ObservableProperty]
        private string paidAmountText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPaymentError))]
        private string paymentError = string.Empty;

        [ObservableProperty]
        private bool canCompleteSale;

        [ObservableProperty]
        private bool isSaleInProgress;

        public bool IsCashPayment => SelectedOption == "Наличный способ";
        public bool HasPaymentError => !string.IsNullOrEmpty(PaymentError);

        [ObservableProperty]
        private string searchLine = string.Empty;

        [ObservableProperty]
        private bool hasSuggestions;

        [ObservableProperty]
        private SearchItemModel? selectedSearchItem;

        [ObservableProperty]
        private decimal selectedPrice;

        [ObservableProperty]
        private string requestedQuantityText = "1";

        [ObservableProperty]
        private int availableQuantity;

        [ObservableProperty]
        private decimal selectedLineTotal;

        [ObservableProperty]
        private bool canAddToCart;

        [ObservableProperty]
        private decimal totalAmount;

        private CancellationTokenSource? _searchCts;

        // Ключ - Id товара. Значение - его позиция в текущем чеке.
        private readonly Dictionary<object, SaleItemModel> _cartByProductId = new();

        // Обратный индекс нужен кнопкам +/- и удалению позиции из чека.
        private readonly Dictionary<SaleItemModel, object> _productIdByCartItem = new();

        // Последний полученный складской остаток по каждому товару.
        private readonly Dictionary<object, int> _stockByProductId = new();

        public List<string> Options { get; } = new()
        {
            "Наличный способ",
            "Безналичный способ"
        };

        public SaleViewModel(
            IProductService productService,
            IInventoryItemService inventoryItemService,
            ISaleService saleService,
            ILogger<SaleViewModel> logger)
        {
            _productService = productService;
            _inventoryItemService = inventoryItemService;
            _saleService = saleService;
            _logger = logger;

            SaleItemModels.CollectionChanged += (_, _) =>
            {
                CleanupCartIndexes();
                RecalculateCartTotal();
                UpdateSelectedProductInfo();
            };

            StartCatalogQuery(string.Empty, false);
        }

        private void StartCatalogQuery(string? value, bool debounce)
        {
            _searchCts?.Cancel();

            var cts = new CancellationTokenSource();
            _searchCts = cts;

            _ = LoadCatalogAsync(value?.Trim() ?? string.Empty, debounce, cts);
        }

        private async Task LoadCatalogAsync(string query, bool debounce,
            CancellationTokenSource cts)
        {
            try
            {
                if (debounce)
                    await Task.Delay(300, cts.Token);

                // Пустой запрос должен возвращать ВСЕ товары из сервиса.
                // Ниже в PerformSearchAsync отфильтровываются нулевые остатки.
                await PerformSearchAsync(query, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Запущен следующий поиск.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки каталога товаров");
            }
        }

        partial void OnSearchLineChanged(string value)
        {
            StartCatalogQuery(value, true);
        }

        partial void OnSelectedOptionChanged(string value)
        {
            OnPropertyChanged(nameof(IsCashPayment));
            UpdatePaymentState();
        }

        partial void OnPaidAmountTextChanged(string value)
        {
            UpdatePaymentState();
        }

        partial void OnTotalAmountChanged(decimal value)
        {
            UpdatePaymentState();
        }

        private void UpdatePaymentState()
        {
            // При безналичной оплате сумму полученных наличных не запрашиваем.
            if (!IsCashPayment)
            {
                PaymentError = string.Empty;
                Change = string.Empty;
                CanCompleteSale = SaleItemModels.Count > 0 && !IsSaleInProgress;
                return;
            }

            Change = "0,00 ₽";
            CanCompleteSale = false;

            if (SaleItemModels.Count == 0)
            {
                PaymentError = string.Empty;
                return;
            }

            if (string.IsNullOrWhiteSpace(PaidAmountText))
            {
                PaymentError = "Укажите сумму, полученную от покупателя.";
                return;
            }

            // Допускаем как запятую, так и точку для десятичных копеек.
            string normalized = PaidAmountText.Trim().Replace(',', '.');
            bool parsed = decimal.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal paid);

            if (!parsed || paid < 0m || decimal.Round(paid, 2) != paid)
            {
                PaymentError = "Введите корректную сумму (не более 2 знаков после запятой).";
                return;
            }

            if (paid < TotalAmount)
            {
                PaymentError = $"Не хватает {TotalAmount - paid:N2} ₽ до суммы чека.";
                return;
            }

            PaymentError = string.Empty;
            Change = $"{paid - TotalAmount:N2} ₽";
            CanCompleteSale = !IsSaleInProgress;
        }

        private async Task DebouncedSearchAsync(string query, CancellationTokenSource cts)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (cts.IsCancellationRequested) return;
                        SearchItemModels.Clear();
                        HasSuggestions = false;
                        SelectedSearchItem = null;
                    });
                    return;
                }

                await Task.Delay(300, cts.Token);
                await PerformSearchAsync(query.Trim(), cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Новый ввод отменил предыдущий поиск.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска товара");
            }
            finally
            {
                if (ReferenceEquals(_searchCts, cts))
                    _searchCts = null;

                cts.Dispose();
            }
        }

        private async Task PerformSearchAsync(string query, CancellationToken ct)
        {
            var products = await _productService.SearchAsync(query, ct);
            ct.ThrowIfCancellationRequested();

            // Сначала собираем результаты, потом одним проходом обновляем UI.
            var foundItems = new List<SearchItemModel>();

            foreach (var product in products)
            {
                ct.ThrowIfCancellationRequested();

                var invItem = await _inventoryItemService.GetByProductIdAsync(product.Id, ct);
                ct.ThrowIfCancellationRequested();

                if (invItem is null || invItem.Quantity <= 0)
                    continue;

                // В исходном сервисе Quantity nullable (использовалось .Value).
                // Для штучных товаров берём только целое неотрицательное количество.
                int stock = invItem?.Quantity is { } quantity
                    ? Math.Max(0, (int)Math.Min(int.MaxValue,
                        Math.Floor(Convert.ToDecimal(quantity, CultureInfo.InvariantCulture))))
                    : 0;

                foundItems.Add(new SearchItemModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Barcode = product.Barcode,
                    Price = product.Price,
                    Quantity = stock
                });
            }

            if (ct.IsCancellationRequested) return;

            SearchItemModels.Clear();

            foreach (var item in foundItems)
            {
                SearchItemModels.Add(item);
                _stockByProductId[item.Id] = GetStockFromSearchItem(item);
            }

            HasSuggestions = SearchItemModels.Count > 0;

            // Остатки могли измениться после обновления каталога.
            IncrementCartItemCommand.NotifyCanExecuteChanged();

            // Не оставляем выбранным объект от старого набора результатов.
            if (SelectedSearchItem is { } oldSelection)
            {
                SelectedSearchItem = SearchItemModels.FirstOrDefault(
                    item => Equals(item.Id, oldSelection.Id));
            }
        }

        partial void OnSelectedSearchItemChanged(SearchItemModel? value)
        {
            RequestedQuantityText = "1";
            UpdateSelectedProductInfo();
        }

        partial void OnRequestedQuantityTextChanged(string value)
        {
            if (SelectedSearchItem is not null &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out int entered) &&
                entered > AvailableQuantity)
            {
                // В поле нельзя оставить число больше доступного остатка.
                RequestedQuantityText = AvailableQuantity.ToString(CultureInfo.CurrentCulture);
                return;
            }

            UpdateSelectedProductInfo();
        }

        private void UpdateSelectedProductInfo()
        {
            if (SelectedSearchItem is not { } product)
            {
                SelectedPrice = 0m;
                AvailableQuantity = 0;
                SelectedLineTotal = 0m;
                CanAddToCart = false;
                return;
            }

            SelectedPrice = Convert.ToDecimal(product.Price, CultureInfo.InvariantCulture);
            int stock = GetStock(product);
            int alreadyInCart = FindCartItem(product) is { } cartItem
                ? GetCartQuantity(cartItem)
                : 0;

            AvailableQuantity = Math.Max(0, stock - alreadyInCart);

            bool isValid = int.TryParse(RequestedQuantityText,
                NumberStyles.Integer, CultureInfo.CurrentCulture, out int quantity)
                && quantity > 0;

            if (isValid && quantity > AvailableQuantity)
            {
                RequestedQuantityText = AvailableQuantity.ToString(CultureInfo.CurrentCulture);
                return;
            }

            CanAddToCart = isValid && quantity <= AvailableQuantity;
            SelectedLineTotal = CanAddToCart ? SelectedPrice * quantity : 0m;
        }

        [RelayCommand]
        private void SelectProduct(SearchItemModel? product)
        {
            SelectedSearchItem = product;
        }

        [RelayCommand]
        private void AddSelectedToCart()
        {
            if (SelectedSearchItem is not { } product || !CanAddToCart)
                return;

            if (int.TryParse(RequestedQuantityText, NumberStyles.Integer,
                    CultureInfo.CurrentCulture, out int quantity))
            {
                AddToCart(product, quantity);
            }
        }

        [RelayCommand]
        private void QuickAddProduct(SearchItemModel? product)
        {
            if (product is null) return;

            SelectedSearchItem = product;
            AddToCart(product, 1);
        }

        private void AddToCart(SearchItemModel product, int quantity)
        {
            if (quantity <= 0) return;

            var existing = FindCartItem(product);
            int inCart = existing?.Quantity ?? 0;

            int remaining = Math.Max(0, GetStock(product) - inCart);
            if (quantity > remaining) return;

            if (existing is not null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                // Поля SaleItemModel предполагаются такими же, как в разметке чека.
                var newItem = new SaleItemModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    Barcode = product.Barcode
                };

                _cartByProductId[product.Id] = newItem;
                _productIdByCartItem[newItem] = product.Id;
                SaleItemModels.Add(newItem);
            }

            RecalculateCartTotal();
            UpdateSelectedProductInfo();
        }

        // Кнопка «+» доступна, только пока в чеке меньше товара, чем на складе.
        private bool CanIncrementCartItem(SaleItemModel? item)
        {
            if (item is null || !SaleItemModels.Contains(item)) return false;
            if (!_productIdByCartItem.TryGetValue(item, out var productId)) return false;
            if (!_stockByProductId.TryGetValue(productId, out int stock)) return false;

            return GetCartQuantity(item) < stock;
        }

        [RelayCommand(CanExecute = nameof(CanIncrementCartItem))]
        private void IncrementCartItem(SaleItemModel? item)
        {
            if (!CanIncrementCartItem(item) || item is null) return;

            item.Quantity += 1;
            RecalculateCartTotal();
            UpdateSelectedProductInfo();
        }

        [RelayCommand]
        private void DecrementCartItem(SaleItemModel? item)
        {
            if (item is null || !SaleItemModels.Contains(item)) return;
            if (GetCartQuantity(item) <= 1) return;

            item.Quantity -= 1;
            RecalculateCartTotal();
            UpdateSelectedProductInfo();
        }

        [RelayCommand]
        private void RemoveCartItem(SaleItemModel? item)
        {
            if (item is null) return;
            SaleItemModels.Remove(item);
        }

        //[RelayCommand]
        //private async Task Sale()
        //{
        //    if (IsSaleInProgress) return;

        //    // Повторяем проверку даже при неактивной кнопке в интерфейсе.
        //    UpdatePaymentState();
        //    if (SaleItemModels.Count == 0 || !CanCompleteSale)
        //    {
        //        await Shell.Current.DisplayAlert(
        //            "Продажа невозможна",
        //            IsCashPayment && !string.IsNullOrWhiteSpace(PaymentError)
        //                ? PaymentError
        //                : "Проверьте состав чека и способ оплаты.",
        //            "ОК");
        //        return;
        //    }

        //    // Сдача вычисляется из введённой суммы, а не из строки Change.
        //    decimal changeAmount = 0m;
        //    if (IsCashPayment)
        //    {
        //        var text = PaidAmountText.Trim().Replace(',', '.');
        //        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint,
        //                CultureInfo.InvariantCulture, out var paid)
        //            || paid < TotalAmount || paid < 0m
        //            || decimal.Round(paid, 2) != paid)
        //        {
        //            UpdatePaymentState();
        //            await Shell.Current.DisplayAlert(
        //                "Недостаточная оплата",
        //                "Полученная сумма не должна быть меньше стоимости чека.",
        //                "ОК");
        //            return;
        //        }
        //        changeAmount = paid - TotalAmount;
        //    }

        //    IsSaleInProgress = true;
        //    UpdatePaymentState();

        //    try
        //    {
        //        // Запоминаем позиции. Не изменяем коллекцию во время сохранения.
        //        var items = SaleItemModels.ToArray();
        //        var saleId = Guid.NewGuid();

        //        await _saleService.AddAsync(saleId, SelectedOption, CancellationToken.None);
        //        foreach (var item in items)
        //        {
        //            await _saleService.AddItemAsync(
        //                item.Barcode, item.Quantity, saleId, CancellationToken.None);
        //        }
        //        await _saleService.ConfirmAsync(saleId, CancellationToken.None);

        //        SaleItemModels.Clear();
        //        PaidAmountText = string.Empty;
        //        Change = string.Empty;
        //        SelectedSearchItem = null;
        //        RequestedQuantityText = "1";
        //        _stockByProductId.Clear();
        //        StartCatalogQuery(string.Empty, false);

        //        await Shell.Current.DisplayAlert(
        //            "Продажа завершена",
        //            IsCashPayment
        //                ? $"Продажа проведена. Сдача: {changeAmount:N2} ₽"
        //                : "Безналичная продажа проведена.",
        //            "ОК");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Не удалось провести продажу");
        //        // Чек не очищается: его можно проверить и не потерять позиции.
        //        await Shell.Current.DisplayAlert(
        //            "Ошибка продажи",
        //            "Операция не завершена. Проверьте сохранённые продажи и остатки " +
        //            "перед повторным нажатием: часть записи могла сохраниться.\n\n" +
        //            ex.Message,
        //            "ОК");
        //    }
        //    finally
        //    {
        //        IsSaleInProgress = false;
        //        UpdatePaymentState();
        //    }
        //}

        private SaleItemModel? FindCartItem(SearchItemModel product)
        {
            return _cartByProductId.TryGetValue(product.Id, out var item) &&
                   SaleItemModels.Contains(item)
                ? item
                : null;
        }

        private int GetStock(SearchItemModel product)
        {
            return _stockByProductId.TryGetValue(product.Id, out var stock)
                ? stock
                : GetStockFromSearchItem(product);
        }

        private static int GetStockFromSearchItem(SearchItemModel item)
        {
            return Math.Max(0, (int)Math.Min(int.MaxValue,
                Math.Floor(Convert.ToDecimal(item.Quantity, CultureInfo.InvariantCulture))));
        }

        private static int GetCartQuantity(SaleItemModel item)
        {
            return Math.Max(0, Convert.ToInt32(item.Quantity, CultureInfo.InvariantCulture));
        }

        private void CleanupCartIndexes()
        {
            foreach (var entry in _productIdByCartItem.ToArray())
            {
                if (SaleItemModels.Contains(entry.Key)) continue;

                _productIdByCartItem.Remove(entry.Key);
                _cartByProductId.Remove(entry.Value);
            }
        }

        private void RecalculateCartTotal()
        {
            TotalAmount = SaleItemModels.Sum(item =>
                Convert.ToDecimal(item.Price, CultureInfo.InvariantCulture)
                * GetCartQuantity(item));

            // После добавления, изменения количества или удаления позиции
            // MAUI повторно проверит CanExecute для кнопок «+» в чеке.
            IncrementCartItemCommand.NotifyCanExecuteChanged();
            UpdatePaymentState();
        }

        [RelayCommand]
        private async Task Sale()
        {
            if (IsSaleInProgress) return;
            UpdatePaymentState();

            if (SaleItemModels.Count == 0 || !CanCompleteSale)
            {
                await Shell.Current.DisplayAlert(
                    "Продажа невозможна",
                    IsCashPayment && !string.IsNullOrWhiteSpace(PaymentError)
                        ? PaymentError
                        : "Проверьте товары в чеке и способ оплаты.",
                    "ОК");
                return;
            }

            // Блокировка от повторного нажатия распространяется на ВСЁ:
            // подтверждение платежа, запись продажи и очистку чека.
            IsSaleInProgress = true;
            UpdatePaymentState();
            try
            {
                if (IsCashPayment)
                    await SaleCashAsync();
                else
                    await SaleCardAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось провести продажу");
                await Shell.Current.DisplayAlert(
                    "Ошибка продажи",
                    "Сохранение не завершено. Если оплата уже принята, " +
                    "не принимайте её повторно: сначала проверьте платёж и запись продажи.\n\n" +
                    ex.Message,
                    "ОК");
            }
            finally
            {
                IsSaleInProgress = false;
                UpdatePaymentState();
            }
        }

        private async Task SaleCashAsync()
        {
            string normalized = PaidAmountText.Trim().Replace(',', '.');
            bool parsed = decimal.TryParse(normalized,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal paidAmount);

            if (!parsed || paidAmount < 0m || paidAmount < TotalAmount ||
                decimal.Round(paidAmount, 2) != paidAmount)
            {
                await Shell.Current.DisplayAlert(
                    "Недостаточная оплата",
                    "Укажите сумму наличных не меньше итога чека.", "ОК");
                return;
            }

            await FinalizeSaleAsync("Наличный способ", paidAmount - TotalAmount);
        }

        private async Task SaleCardAsync()
        {
            await Shell.Current.DisplayAlert(
                    "Ошибка продажи",
                    "Оплата картой пока не реализована",
                    "ОК");
        }

        //private async Task CompleteSaleAsync(string paymentType, decimal changeAmount)
        //{
        //    if (IsSaleInProgress)
        //        return;

        //    IsSaleInProgress = true;
        //    UpdatePaymentState();

        //    try
        //    {
        //        var items = SaleItemModels.ToArray();
        //        var saleId = Guid.NewGuid();

        //        await _saleService.AddAsync(
        //            saleId,
        //            paymentType,
        //            CancellationToken.None);

        //        foreach (var item in items)
        //        {
        //            await _saleService.AddItemAsync(
        //                item.Barcode,
        //                item.Quantity,
        //                saleId,
        //                CancellationToken.None);
        //        }

        //        await _saleService.ConfirmAsync(
        //            saleId,
        //            CancellationToken.None);

        //        // Очищаем чек после успешной продажи
        //        SaleItemModels.Clear();

        //        PaidAmountText = string.Empty;
        //        Change = string.Empty;
        //        SelectedSearchItem = null;
        //        RequestedQuantityText = "1";

        //        _stockByProductId.Clear();
        //        StartCatalogQuery(string.Empty, false);

        //        string message = paymentType == "Наличный способ"
        //            ? $"Продажа проведена.\nСдача: {changeAmount:N2} ₽"
        //            : "Безналичная продажа проведена.";

        //        await Shell.Current.DisplayAlert(
        //            "Продажа завершена",
        //            message,
        //            "ОК");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(
        //            ex,
        //            "Не удалось провести продажу");

        //        await Shell.Current.DisplayAlert(
        //            "Ошибка продажи",
        //            "Продажа не завершена. Проверьте сохранённые " +
        //            "чеки и остатки перед повторной попыткой.\n\n" +
        //            ex.Message,
        //            "ОК");
        //    }
        //    finally
        //    {
        //        IsSaleInProgress = false;
        //        UpdatePaymentState();
        //    }
        //}

        private async Task FinalizeSaleAsync(string paymentType, decimal changeAmount)
        {
            var lines = SaleItemModels
                .Select(item => (item.Barcode, item.Quantity))
                .ToArray();

            var saleId = Guid.NewGuid();
            await _saleService.CompleteSaleAsync(
                saleId, paymentType, lines, CancellationToken.None);

            // Очищаем интерфейс только после успешного коммита.
            SaleItemModels.Clear();
            PaidAmountText = string.Empty;
            Change = string.Empty;
            SelectedSearchItem = null;
            RequestedQuantityText = "1";
            _stockByProductId.Clear();
            StartCatalogQuery(string.Empty, false);

            string message = paymentType == "Наличный способ"
                ? $"Продажа проведена. Сдача: {changeAmount:N2} ₽"
                : "Оплата подтверждена кассиром. Продажа проведена.";

            await Shell.Current.DisplayAlert("Продажа завершена", message, "ОК");
        }
    }
}
