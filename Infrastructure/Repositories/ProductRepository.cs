using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.repository;

namespace Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Products.FindAsync(id, ct);
        }

        public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
        {
            return await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Barcode == barcode, ct);
        }

        public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Products
                    .AsNoTracking()
                    .OrderBy(f => f.Name)
                    .ToListAsync(ct);
        }

        public async Task<Product> AddAsync(Product Product, CancellationToken ct = default)
        {
            await _context.AddAsync(Product, ct);
            return Product;
        }

        public async Task Update(Product Product, CancellationToken ct = default)
        {
            _context.Products.Update(Product);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var file = await _context.Products.FindAsync(id);
            if (file != null)
            {
                _context.Products.Remove(file);
            }
        }

        public async Task<IReadOnlyList<Product>> SearchAsync(string query, CancellationToken ct = default)
        {

            var q = query.Trim().ToLower();

            return await _context.Products
                .AsNoTracking()
                .Where(p => (EF.Functions.Like(p.Name.ToLower(), $"%{q}%") ||
                             p.Barcode.StartsWith(q)))
                .OrderBy(p => p.Name)
                .Take(20).ToListAsync(ct);
        }
    }
}
