using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.repository;

namespace Infrastructure.Repositories
{
    public class ProductCatalogRepository : IProductCatalogRepository
    {
        private readonly AppDbContext _context;

        public ProductCatalogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProductCatalog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Catalogs.FindAsync(id, ct);
        }

        public async Task<ProductCatalog?> GetByNameAsync(string name, CancellationToken ct = default)
        {
            return await _context.Catalogs
                .FirstOrDefaultAsync(c => c.Name == name, ct);
        }

        public async Task<IEnumerable<ProductCatalog>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Catalogs
                    .Include(f => f.Products)
                    .AsNoTracking()
                    .ToListAsync(ct);
        }

        public async Task<ProductCatalog> AddAsync(ProductCatalog catalog, CancellationToken ct = default)
        {
            await _context.AddAsync(catalog, ct);
            return catalog;
        }

        public async Task Update(ProductCatalog catalog, CancellationToken ct = default)
        {
            _context.Catalogs.Update(catalog);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var file = await _context.Catalogs.FindAsync(id);
            if (file != null)
            {
                _context.Catalogs.Remove(file);
            }
        }
    }
}
