using App.Views.Popups;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace App.Model
{
    public partial class CatalogItemModel : ObservableObject
    {
        public Guid Id { get; set; }

        [ObservableProperty]
        public string name;

        [ObservableProperty]
        public string barcode;

        [ObservableProperty]
        public string price;

        [ObservableProperty]
        public string description;

        /// <summary>
        /// Функция для открытия popup редактирования существуещего товара. В идеале перенести в CatalogViewModel
        /// </summary>
        /// <returns>Открывает popup</returns>
        [RelayCommand]
        private async Task EditItem()
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services == null) return;

            var popup = services.GetRequiredService<EditProductPopup>();
            popup.Load(this);
            var result = await Application.Current.MainPage.ShowPopupAsync(popup);
        }

    }
}
