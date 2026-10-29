using Services.DTOs;

namespace Services.Interfaces.service
{
    public interface IInventoryItemService
    {
        Task<IEnumerable<InventoryItemDto>> GetAllAsync();
        Task<InventoryItemDto?> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
        Task AddItemAsync(InventoryItemDto item, CancellationToken ct);
        Task UpdateItemAsync(InventoryItemDto item, CancellationToken ct);
    }
}
