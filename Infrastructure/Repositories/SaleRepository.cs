using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.repository;

namespace Infrastructure.Repositories
{
    public class SaleRepository : ISaleRepository
    {
        private readonly AppDbContext _context;

        public SaleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Sales
                .Include(r => r.Items)
                    .FirstOrDefaultAsync(r => r.Id == id, ct);
        }

        public async Task<IEnumerable<Sale>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Sales
                    .Include(f => f.Items)
                    .AsNoTracking()
                    .ToListAsync(ct);
        }

        public async Task<Sale> AddAsync(Sale saleItem, CancellationToken ct = default)
        {
            await _context.AddAsync(saleItem, ct);
            return saleItem;
        }

        public async Task Update(Sale saleItem, CancellationToken ct = default)
        {
            _context.Sales.Update(saleItem);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var file = await _context.Sales.FindAsync(id);
            if (file != null)
            {
                _context.Sales.Remove(file);
            }
        }
    }
}
