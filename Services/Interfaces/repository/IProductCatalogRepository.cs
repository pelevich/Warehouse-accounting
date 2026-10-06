using Domain.Entities;

namespace Services.Interfaces.repository
{
    public interface IProductCatalogRepository
    {
        Task<ProductCatalog?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<ProductCatalog?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<IEnumerable<ProductCatalog>> GetAllAsync(CancellationToken ct = default);
        Task<ProductCatalog> AddAsync(ProductCatalog catalog, CancellationToken ct = default);
        Task Update(ProductCatalog catalog, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
