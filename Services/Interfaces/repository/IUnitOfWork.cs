namespace Services.Interfaces.repository
{
    public interface IUnitOfWork : IDisposable
    {
        IInventoryItemRepository InventoryItemRepository { get; }
        IInventoryRepository InventoryRepository { get; }
        IProductCatalogRepository ProductCatalogRepository { get; }
        IProductRepository ProductRepository { get; }
        IReceiptRepository ReceiptRepository { get; }
        IReceiptItemRepository ReceiptItemRepository { get; }
        ISaleRepository SaleRepository { get; }
        ISaleItemRepository SaleItemRepository { get; }


        Task SaveChangesAsync(CancellationToken cancellationToken);
        Task BeginTransactionAsync(CancellationToken ct = default);
        Task CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
    }
}
