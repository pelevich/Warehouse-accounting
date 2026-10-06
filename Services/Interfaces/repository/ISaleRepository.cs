using Domain.Entities;

namespace Services.Interfaces.repository
{
    public interface ISaleRepository
    {
        Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Sale>> GetAllAsync(CancellationToken ct = default);
        Task<Sale> AddAsync(Sale saleItem, CancellationToken ct = default);
        Task Update(Sale saleItem, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
