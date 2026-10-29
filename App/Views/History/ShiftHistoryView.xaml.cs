using App.ViewModels.Tabs;

namespace App.Views;

public partial class ShiftHistoryView : ContentView
{
    private readonly ShiftHistoryViewModel _viewModel;

    public ShiftHistoryView(ShiftHistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public Task LoadAsync() => _viewModel.LoadAsync();
}
