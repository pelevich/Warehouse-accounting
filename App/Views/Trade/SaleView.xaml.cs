using App.ViewModels.Tabs;
using App.ViewModels.Trade.Tabs;

namespace App.Views.Trade;

public partial class SaleView : ContentView
{
    private readonly SaleViewModel _viewModel;

    public SaleView(SaleViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        InitializeComponent();
    }
}