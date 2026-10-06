using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.repository;

namespace Infrastructure.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly AppDbContext _context;

        public InventoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Inventory?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Inventories.FindAsync(id, ct);
        }

        public async Task<IEnumerable<Inventory>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Inventories
                    .Include(f => f.Items)
                    .AsNoTracking()
                    .ToListAsync(ct);
        }

        public async Task<Inventory> AddAsync(Inventory Inventory, CancellationToken ct = default)
        {
            await _context.AddAsync(Inventory, ct);
            return Inventory;
        }

        public async Task Update(Inventory Inventory, CancellationToken ct = default)
        {
            _context.Inventories.Update(Inventory);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var file = await _context.Inventories.FindAsync(id);
            if (file != null)
            {
                _context.Inventories.Remove(file);
            }
        }

        public async Task<Inventory> GetMainInventoryAsync(CancellationToken ct = default)
        {
            return await _context.Inventories.FirstOrDefaultAsync(i => i.Name == "Main inventory", ct);
        }
    }
}
