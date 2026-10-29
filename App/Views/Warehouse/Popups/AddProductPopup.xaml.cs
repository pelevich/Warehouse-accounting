using CommunityToolkit.Maui.Views;
using App.ViewModels.Popups;

namespace App.Views.Popups;

public partial class AddProductPopup : Popup
{
	public AddProductPopup(AddProductViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested; // Функиця для закрытия окна
        viewModel.InitializeAsync(); // Вызывает функцию InitializeAsync при открытии окна
    }

    // Функиця для закрытия окна
    private async void OnCloseRequested(object? sender, bool result)
    {
        await CloseAsync();
    }
}