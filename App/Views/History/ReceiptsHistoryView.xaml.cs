using App.ViewModels.Tabs;

namespace App.Views;

public partial class ReceiptsHistoryView : ContentView
{
    private readonly ReceiptsHistoryViewModel _viewModel;

    public ReceiptsHistoryView(ReceiptsHistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public Task LoadAsync() => _viewModel.LoadAsync();
}
