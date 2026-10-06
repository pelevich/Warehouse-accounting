using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.repository;

namespace Infrastructure.Repositories
{
    public class InventoryItemRepository : IInventoryItemRepository
    {
        private readonly AppDbContext _context;

        public InventoryItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Items.FindAsync(id, ct);
        }

        public async Task<IEnumerable<InventoryItem>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Items
                    .AsNoTracking()
                    .ToListAsync(ct);
        }

        public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        {
            return await _context.Items
                .FirstOrDefaultAsync(i => i.ProductId == productId, ct);
        }

        public async Task<InventoryItem> AddAsync(InventoryItem inventoryItem, CancellationToken ct = default)
        {
            Console.WriteLine($"InventoryId = {inventoryItem.InventoryId}, exists = {await _context.Inventories.AnyAsync(i => i.Id == inventoryItem.InventoryId)}");
            Console.WriteLine($"ProductId = {inventoryItem.ProductId}, exists = {await _context.Products.AnyAsync(p => p.Id == inventoryItem.ProductId)}");
            await _context.AddAsync(inventoryItem, ct);
            return inventoryItem;
        }

        public async Task Update(InventoryItem inventoryItem, CancellationToken ct = default)
        {
            _context.Items.Update(inventoryItem);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var file = await _context.Items.FindAsync(id);
            if (file != null)
            {
                _context.Items.Remove(file);
            }
        }
    }
}
