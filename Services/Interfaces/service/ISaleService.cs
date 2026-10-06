namespace Services.Interfaces.service
{
    public interface ISaleService
    {
        Task AddItemAsync(string barcode, int count, Guid saleId, CancellationToken ct);
        Task UpdateQuantityAsync(Guid itemId, int count, CancellationToken ct);
        Task DelItemAsync(Guid saleId, CancellationToken ct);
        Task ConfirmAsync(Guid saleId, Guid inventoryId, CancellationToken ct);
    }
}
