using CommunityToolkit.Mvvm.ComponentModel;

namespace App.Model
{
    public partial class CatalogItemModel : ObservableObject
    {
        public Guid Id { get; set; }

        [ObservableProperty]
        public string name;

        [ObservableProperty]
        public string barcode;

        [ObservableProperty]
        public string price;

        [ObservableProperty]
        public string description;

        [ObservableProperty]
        public ImageSource? barcodeImage;

        public byte[]? barcodePng;
    }
}
