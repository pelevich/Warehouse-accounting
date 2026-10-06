using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.repository;

namespace Infrastructure.Repositories
{
    public class ReceiptRepository : IReceiptRepository
    {
        private readonly AppDbContext _context;

        public ReceiptRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Receipt?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Receipts
                    .Include(r => r.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(r => r.Id == id, ct);
        }

        public async Task<IEnumerable<Receipt>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Receipts
                    .Include(f => f.Items)
                    .AsNoTracking()
                    .ToListAsync(ct);
        }

        public async Task<Receipt> AddAsync(Receipt receiptItem, CancellationToken ct = default)
        {
            await _context.AddAsync(receiptItem, ct);
            return receiptItem;
        }

        public async Task Update(Receipt receiptItem, CancellationToken ct = default)
        {
            _context.Receipts.Update(receiptItem);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var file = await _context.Receipts.FindAsync(id);
            if (file != null)
            {
                _context.Receipts.Remove(file);
            }
        }
    }
}
