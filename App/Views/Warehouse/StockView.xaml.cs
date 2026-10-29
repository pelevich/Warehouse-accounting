using App.ViewModels.Tabs;

namespace App.Views;

public partial class StockView : ContentView
{
    private StockViewModel _viewModel;
    public StockView(StockViewModel viewModel)
	{
        BindingContext = _viewModel = viewModel;
        InitializeComponent();
	}

    public async Task OnAppearingAsync()
    {
        await _viewModel.OnAppearingAsync();
    }
}