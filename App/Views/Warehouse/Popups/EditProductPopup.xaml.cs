using App.Model;
using App.ViewModels.Popups;
using CommunityToolkit.Maui.Views;

namespace App.Views.Popups;

public partial class EditProductPopup : Popup
{
    private readonly EditProductViewModel _viewModel;

    public EditProductPopup(EditProductViewModel viewModel)
	{
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
    }

    public void Load(CatalogItemModel item) => _viewModel.Load(item);

    private async void OnCloseRequested(object? sender, bool result)
    {
        await CloseAsync();
    }
}