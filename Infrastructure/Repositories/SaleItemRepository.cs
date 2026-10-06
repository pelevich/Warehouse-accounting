using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.repository;

namespace Infrastructure.Repositories
{
    public class SaleItemRepository : ISaleItemRepository
    {
        private readonly AppDbContext _context;

        public SaleItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SaleItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.SaleItems.FindAsync(id, ct);
        }

        public async Task<IEnumerable<SaleItem>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.SaleItems
                    .AsNoTracking()
                    .ToListAsync(ct);
        }

        public async Task<SaleItem> AddAsync(SaleItem saleItem, CancellationToken ct = default)
        {
            await _context.AddAsync(saleItem, ct);
            return saleItem;
        }

        public async Task Update(SaleItem saleItem, CancellationToken ct = default)
        {
            _context.SaleItems.Update(saleItem);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var file = await _context.SaleItems.FindAsync(id);
            if (file != null)
            {
                _context.SaleItems.Remove(file);
            }
        }
    }
}
