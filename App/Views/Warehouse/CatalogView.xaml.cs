using App.ViewModels.Tabs;

namespace App.Views;

public partial class CatalogView : ContentView
{
    private readonly CatalogViewModel _viewModel;

    public CatalogView(CatalogViewModel viewModel)
	{
        BindingContext = _viewModel = viewModel;
        InitializeComponent();
	}

    public async Task OnAppearingAsync()
    {
        await _viewModel.OnAppearingAsync();
    }
}