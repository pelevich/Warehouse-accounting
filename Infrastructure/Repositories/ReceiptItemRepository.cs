using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.repository;

namespace Infrastructure.Repositories
{
    public class ReceiptItemRepository : IReceiptItemRepository
    {
        private readonly AppDbContext _context;

        public ReceiptItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ReceiptItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.ReceiptItems.FindAsync(id, ct);
        }

        public async Task<IEnumerable<ReceiptItem>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.ReceiptItems
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<ReceiptItem> AddAsync(ReceiptItem receiptItem, CancellationToken ct = default)
        {
            await _context.AddAsync(receiptItem, ct);
            return receiptItem;
        }

        public async Task Update(ReceiptItem receiptItem, CancellationToken ct = default)
        {
            _context.ReceiptItems.Update(receiptItem);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var file = await _context.ReceiptItems.FindAsync(id);
            if (file != null)
            {
                _context.ReceiptItems.Remove(file);
            }
        }
    }
}
