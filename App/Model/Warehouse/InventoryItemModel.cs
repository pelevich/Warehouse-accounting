using CommunityToolkit.Mvvm.ComponentModel;

namespace App.Model
{
    public partial class InventoryItemModel : ObservableObject
    {
        public Guid Id { get; set; }

        [ObservableProperty] 
        public string name;

        [ObservableProperty] 
        public string barcode;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalAmount))] 
        public int _quantity;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalAmount))] 
        public decimal _price;
        public decimal TotalAmount => Price * Quantity;
    }
}
