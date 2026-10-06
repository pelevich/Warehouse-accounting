using Domain.Entities;

namespace Services.Interfaces.repository
{
    public interface IReceiptRepository
    {
        Task<Receipt?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Receipt>> GetAllAsync(CancellationToken ct = default);
        Task<Receipt> AddAsync(Receipt receiptItem, CancellationToken ct = default);
        Task Update(Receipt receiptItem, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
