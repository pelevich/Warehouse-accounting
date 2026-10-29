using Services.DTO;
using Services.Interfaces.service;

namespace App.ViewModels.Tabs;

// Собственное состояние фильтров для вкладки Receipts.
public sealed class ReceiptsHistoryViewModel : BaseHistoryViewModel
{
    public ReceiptsHistoryViewModel(IHistoryService service) : base(service) { }

    public Task LoadAsync() => ShowAsync(HistoryKind.Receipts);
}
