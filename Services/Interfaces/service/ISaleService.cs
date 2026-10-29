namespace Services.Interfaces.service
{
    public interface ISaleService
    {
        Task AddAsync(Guid saleId, string paymentType, CancellationToken ct);
        Task AddItemAsync(string barcode, int count, Guid saleId, CancellationToken ct);
        Task UpdateQuantityAsync(Guid itemId, int count, CancellationToken ct);
        Task DelItemAsync(Guid saleId, CancellationToken ct);
        Task ConfirmAsync(Guid saleId, CancellationToken ct);
        Task CompleteSaleAsync(Guid saleId, string paymentType, IReadOnlyList<(string Barcode, int Quantity)> lines, CancellationToken ct = default);
    }
}
