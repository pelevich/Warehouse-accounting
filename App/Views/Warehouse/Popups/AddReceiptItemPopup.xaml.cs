using App.ViewModels.Popups;
using CommunityToolkit.Maui.Views;

namespace App.Views.Popups;

public partial class AddReceiptItemPopup : Popup
{
    private readonly AddReceiptItemViewModel _viewModel;

    public AddReceiptItemPopup(AddReceiptItemViewModel viewModel)
	{
		BindingContext = viewModel;
		InitializeComponent();
        viewModel.CloseRequested += OnCloseRequested;
        _viewModel = viewModel;
    }

    public void Load(Guid? receiptId) => _viewModel.Load(receiptId);

    private async void OnCloseRequested(object? sender, bool result)
    {
        await CloseAsync();
    }
}