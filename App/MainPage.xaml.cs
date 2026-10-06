using App.ViewModels;

namespace App
{
    public partial class MainPage : ContentPage
    {
        public MainPage(WarehouseViewModel warehouseViewModel)
        {
            BindingContext = warehouseViewModel;
            InitializeComponent();
        }


        public MainPage()
        {
            InitializeComponent();
        }
    }
}
