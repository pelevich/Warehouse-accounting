using App.ViewModels;

namespace App.Views;

public partial class WarehousePage : ContentPage
{
	public WarehousePage(WarehouseViewModel viewModel)
	{
		BindingContext = viewModel;
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is WarehouseViewModel vm)
            await vm.InitializeAsync();
    }
}