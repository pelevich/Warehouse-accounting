using Domain.Entities;

namespace Services.Interfaces.service
{
    public interface IReceiptService
    {
        Task AddReceiptAsync(Guid receiptId, CancellationToken ct);
        Task DelReceiptAsync(Guid receiptId, CancellationToken ct);
        //Task UpdateQuantityAsync(Guid itemId, int count, CancellationToken ct);
        Task ConfirmAsync(Guid receiptId, Guid inventoryId, CancellationToken ct);
        Task<Receipt> GetByIdAsync (Guid id);
    }
}
