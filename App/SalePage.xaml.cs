using App.ViewModels;

namespace App.Views;

public partial class SalePage : ContentPage
{
	public SalePage(TradeViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}