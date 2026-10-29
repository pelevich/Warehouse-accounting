using App.ViewModels;

namespace App.Views;

public partial class EmployeesView : ContentView
{
	public EmployeesView(EmployeesViewModel viewModel)
	{
        BindingContext = viewModel;
        InitializeComponent();
	}
}