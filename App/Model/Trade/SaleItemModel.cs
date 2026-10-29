using CommunityToolkit.Mvvm.ComponentModel;

namespace App.Model.Trade
{
    public partial class SaleItemModel : ObservableObject
    {
        public Guid Id { get; set; }
        public string Barcode { get; set; }
        [ObservableProperty] public string name;
        [ObservableProperty] public int quantity;
        public int totalQuantity;
        [ObservableProperty] public decimal price;
        [ObservableProperty] public int totalAmount;
    }
}
