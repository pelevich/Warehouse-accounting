using Domain.Entities;

namespace Services.Interfaces.repository
{
    public interface IInventoryRepository
    {
        Task<Inventory?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Inventory>> GetAllAsync(CancellationToken ct = default);
        Task<Inventory> AddAsync(Inventory inventory, CancellationToken ct = default);
        Task Update(Inventory inventory, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task<Inventory> GetMainInventoryAsync(CancellationToken ct = default);
    }
}
