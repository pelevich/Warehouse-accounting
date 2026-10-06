using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Services.Interfaces.repository;

namespace Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        private IInventoryItemRepository _inventoryItemRepository;
        private IInventoryRepository _inventoryRepository;
        private IProductCatalogRepository _productCatalogRepository;
        private IProductRepository _productRepository;
        private IReceiptRepository _receiptRepository;
        private IReceiptItemRepository _receiptItemRepository;
        private ISaleRepository _saleRepository;
        private ISaleItemRepository _saleItemRepository;

        private IDbContextTransaction _currentTransaction;
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWork(
            AppDbContext context,
            ILogger<UnitOfWork> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IInventoryItemRepository InventoryItemRepository
            => _inventoryItemRepository ??= new InventoryItemRepository(_context);

        public IInventoryRepository InventoryRepository
            => _inventoryRepository ??= new InventoryRepository(_context);

        public IProductCatalogRepository ProductCatalogRepository
            => _productCatalogRepository ??= new ProductCatalogRepository(_context);

        public IProductRepository ProductRepository
            => _productRepository ??= new ProductRepository(_context);

        public IReceiptRepository ReceiptRepository
            => _receiptRepository ??= new ReceiptRepository(_context);

        public IReceiptItemRepository ReceiptItemRepository
            => _receiptItemRepository ??= new ReceiptItemRepository(_context);

        public ISaleRepository SaleRepository
            => _saleRepository ??= new SaleRepository(_context);

        public ISaleItemRepository SaleItemRepository
            => _saleItemRepository ??= new SaleItemRepository(_context);

        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction != null)
            {
                var ex = new InvalidOperationException("Транзакция уже начата");
                _logger.LogError(ex, "Попытка начать новую транзакцию");
                throw ex;
            }

            _currentTransaction = await _context.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            try
            {
                await _context.SaveChangesAsync(ct);
                await (_currentTransaction?.CommitAsync(ct) ?? Task.CompletedTask);
                _logger.LogInformation("Commit");
            }
            catch
            {
                await RollbackAsync(ct);
                throw;
            }
            finally
            {
                _currentTransaction?.Dispose();
                _currentTransaction = null;
            }
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            try
            {
                await (_currentTransaction?.RollbackAsync(ct) ?? Task.CompletedTask);
                _logger.LogInformation("Roollback транзакции");
            }
            finally
            {
                _currentTransaction?.Dispose();
                _currentTransaction = null;
            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Изменение сохранены");

        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
