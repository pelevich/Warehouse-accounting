using Services.DTOs;

namespace Services.Interfaces.service
{
    public interface IInventoryItemService
    {
        Task<IEnumerable<InventoryItemDto>> GetAllAsync();
        Task AddItemAsync(InventoryItemDto item, CancellationToken ct);
        Task UpdateItemAsync(InventoryItemDto item, CancellationToken ct);
    }
}
