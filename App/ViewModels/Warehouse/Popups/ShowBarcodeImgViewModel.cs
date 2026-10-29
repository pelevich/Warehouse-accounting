using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace App.ViewModels.Warehouse.Popups
{
    public partial class ShowBarcodeImgViewModel : ObservableObject
    {
        public event EventHandler<bool>? CloseRequested;
        [ObservableProperty]
        private ImageSource? barcodeImage;
        private byte[]? barcodePng;

        public void Load(ImageSource? barcodeImage, byte[]? barcodePng)
        {
            BarcodeImage = barcodeImage;
            this.barcodePng = barcodePng;
        }

        [RelayCommand]
        private async Task SaveBarcodeImage()
        {
            using var stream = new MemoryStream(barcodePng);

            var result = await FileSaver.Default.SaveAsync(
                $"Штрихкод_{barcodePng}.png",
                stream,
                CancellationToken.None
            );
        }

        [RelayCommand]
        private async Task Close()
        {
            CloseRequested?.Invoke(this, false);
        }
    }
}
