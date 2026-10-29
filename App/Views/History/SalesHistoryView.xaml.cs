using App.ViewModels.Tabs;

namespace App.Views;

public partial class SalesHistoryView : ContentView
{
    private readonly SalesHistoryViewModel _viewModel;

    public SalesHistoryView(SalesHistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public Task LoadAsync() => _viewModel.LoadAsync();
}
