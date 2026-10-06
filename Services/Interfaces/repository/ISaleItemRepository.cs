using Domain.Entities;

namespace Services.Interfaces.repository
{
    public interface ISaleItemRepository
    {
        Task<SaleItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<SaleItem>> GetAllAsync(CancellationToken ct = default);
        Task<SaleItem> AddAsync(SaleItem saleItem, CancellationToken ct = default);
        Task Update(SaleItem saleItem, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
