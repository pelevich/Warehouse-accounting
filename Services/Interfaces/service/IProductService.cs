using Domain.Entities;
using Services.DTOs;

namespace Services.Interfaces.service
{
    public interface IProductService
    {
        public Task AddProductAsync(ProductDto dto, Guid CatalogId);
        public Task DeleteProductAsync(Guid id);
        public Task UpdateProductAsync(ProductDto dto);
        public Task<IEnumerable<ProductDto>> GetAllAsync();
        public Task<ProductDto> GetByIdAsync(Guid Id);
        Task<IReadOnlyList<Product>> SearchAsync(string query, CancellationToken ct = default);
    }
}
