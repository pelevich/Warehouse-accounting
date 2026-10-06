using Domain.Entities;

namespace Services.Interfaces.repository
{
    public interface IInventoryItemRepository
    {
        Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<InventoryItem>> GetAllAsync(CancellationToken ct = default);
        Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
        Task<InventoryItem> AddAsync(InventoryItem inventoryItem, CancellationToken ct = default);
        Task Update(InventoryItem inventoryItem, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
