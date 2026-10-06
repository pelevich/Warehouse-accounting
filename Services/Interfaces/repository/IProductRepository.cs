using Domain.Entities;

namespace Services.Interfaces.repository
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
        Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default);
        Task<Product> AddAsync(Product products, CancellationToken ct = default);
        Task Update(Product products, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Product>> SearchAsync(string query, CancellationToken ct = default);
    }
}
