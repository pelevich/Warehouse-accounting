using Services.DTO;
using Services.Interfaces.service;

namespace App.ViewModels.Tabs;

// Собственное состояние фильтров для вкладки Sales.
public sealed class SalesHistoryViewModel : BaseHistoryViewModel
{
    public SalesHistoryViewModel(IHistoryService service) : base(service) { }

    public Task LoadAsync() => ShowAsync(HistoryKind.Sales);
}
