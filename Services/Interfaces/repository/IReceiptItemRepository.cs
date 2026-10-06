using Domain.Entities;

namespace Services.Interfaces.repository
{
    public interface IReceiptItemRepository
    {
        Task<ReceiptItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<ReceiptItem>> GetAllAsync(CancellationToken ct = default);
        Task<ReceiptItem> AddAsync(ReceiptItem receiptItem, CancellationToken ct = default);
        Task Update(ReceiptItem receiptItem, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
