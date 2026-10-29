using App.ViewModels.Warehouse.Popups;
using CommunityToolkit.Maui.Views;

namespace App.Views.Popups;

public partial class ShowBarcodeImgPopup : Popup
{
    private readonly ShowBarcodeImgViewModel _viewModel;
    public ShowBarcodeImgPopup(
        ShowBarcodeImgViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;

        viewModel.CloseRequested += OnCloseRequested;

        _viewModel = viewModel;
    }

    public void Load(ImageSource? barcodeImage, byte[]? barcodePng) => _viewModel.Load(barcodeImage, barcodePng);

    private async void OnCloseRequested(
        object? sender,
        bool result)
    {
        await CloseAsync();
    }
}