using App.ViewModels.Popups;
using CommunityToolkit.Maui.Views;

namespace App.Views.Popups;

public partial class ManageShiftPopup : Popup
{
    private readonly ManageShiftViewModel _viewModel;

    public bool ShiftWasClosed => _viewModel.ShiftWasClosed;

    public ManageShiftPopup(ManageShiftViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.CloseRequested += OnCloseRequested;
    }

    public Task InitializeAsync() => _viewModel.InitializeAsync();

    private async void OnCloseRequested()
    {
        _viewModel.CloseRequested -= OnCloseRequested;
        await CloseAsync();
    }
}
