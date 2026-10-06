using App.ViewModels.Tabs;

namespace App.Views;

public partial class ReceiptView : ContentView
{
    private readonly ReceiptViewModel _viewModel;

    public ReceiptView(ReceiptViewModel viewModel)
	{
        BindingContext = _viewModel = viewModel;
        InitializeComponent();
    }

    public async Task OnAppearingAsync()
    {
        await _viewModel.OnAppearingAsync();
    }
}