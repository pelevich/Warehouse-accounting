using Services.DTOs;

namespace Services.Interfaces.service
{
    public interface IReceiptItemService
    {
        Task AddItemAsync(string barcode, int count, Guid receiptId, CancellationToken ct);
        Task DelReceiptItemAsync(Guid itemId, CancellationToken ct);
        Task UpdateReceiptItemAsync(Guid itemId, int count, CancellationToken ct);
        //Task<IEnumerable<ReceiptItemDto>> GetAllByReceiptId(Guid receiptId);
    }
}
