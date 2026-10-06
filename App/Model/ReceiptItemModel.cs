using CommunityToolkit.Mvvm.ComponentModel;

namespace App.Model
{
    // Типо Dto для товара в приходе
    public partial class ReceiptItemModel : ObservableObject
    {
        // При открытии прихода создавать новый id, передавать id receipt'а в попам окно и добавлять товар и только в конце при нажатии кнопки добавить приход все загружать в БД
        public Guid Id { get; set; } // Здесь Id это Id receiptItem в БД
        public Guid ProductId { get; set; } // Id продукта

        [ObservableProperty] private string _name;
        [ObservableProperty] private string _quantity;

        // отвечает за изменение кнопки "-" на актив/неактив по условию
        public bool CanDecrement =>
            int.TryParse(Quantity, out var q) && q > 1;

        // свойство поля _quantity
        partial void OnQuantityChanged(string value)
        {
            OnPropertyChanged(nameof(CanDecrement));   // ← уведомить UI
        }

        //private CancellationTokenSource? _updateCts;

        //[RelayCommand]
        //private async Task EditItem()
        //{
        //    var services = Application.Current?.Handler?.MauiContext?.Services;
        //    if (services == null) return;
        //    // !!!!!!!!!!!!!
        //    //var popup = services.GetRequiredService<EditProductPopup>();
        //    //popup.Load(this);
        //    //var result = await Application.Current.MainPage.ShowPopupAsync(popup);
        //}

        //[RelayCommand]
        //private async Task IncrQuantity()
        //{
        //    Quantity = (int.Parse(Quantity) + 1).ToString();
        //    _ = DebouncedUpdateQuantityAsync();
        //}

        //[RelayCommand]
        //public async Task DecrQuantity() 
        //{
        //    if (int.Parse(Quantity)>=1)
        //    {
        //        Quantity = (int.Parse(Quantity) - 1).ToString();
        //        _ = DebouncedUpdateQuantityAsync();
        //    }
        //    else
        //    {
        //        // меняем цвет делаем кнопку не активной
        //    }
        //}

        //private async Task DebouncedUpdateQuantityAsync()
        //{
        //    _updateCts?.Cancel();
        //    _updateCts?.Dispose();
        //    _updateCts = new CancellationTokenSource();

        //    try
        //    {
        //        await Task.Delay(500, _updateCts.Token);

        //        await _receiptItemService.UpdateReceiptItemAsync(this.Id, int.Parse(Quantity), _updateCts.Token);
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        // штатная отмена — ничего не делаем
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Ошибка обновления количества");
        //    }
        //}
    }
}
