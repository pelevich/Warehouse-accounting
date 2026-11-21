using Domain.Entities;
using Services.Interfaces.repository;
using Services.Interfaces.service;

namespace Services.service
{
    public class SaleService : ISaleService
    {
        private IUnitOfWork _unitOfWork;
        private readonly ISessionService _session;

        public SaleService(IUnitOfWork unitOfWork, ISessionService session)
        {
            _unitOfWork = unitOfWork;
            _session = session;
        }

        public async Task AddAsync(Guid saleId, string paymentType, CancellationToken ct)
        {
            await _unitOfWork.SaleRepository.AddAsync(
                new Sale
                {
                    Id = saleId,
                    PaymentType = paymentType,
                    SoldAt = DateTime.UtcNow,
                    WorkShiftId = _session.CurrentShiftId
                },
                ct);
            await _unitOfWork.CommitAsync(ct);
        }

        public async Task AddItemAsync(string barcode, int count, Guid saleId, CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            if (count < 1)
                throw new ArgumentException("Количество добавляемого товара не может быть меньше 1", nameof(count));

            try
            {
                var product = await _unitOfWork.ProductRepository.GetByBarcodeAsync(barcode, ct)
                    ?? throw new ArgumentNullException(nameof(barcode), "Продукта с таким штрих-кодом нет");

                var inventoryItem = await _unitOfWork.InventoryItemRepository.GetByProductIdAsync(product.Id, ct)
                    ?? throw new ArgumentNullException($"Товара с штрих-кодом {barcode} на складе нет");

                var sale = await _unitOfWork.SaleRepository.GetByIdAsync(saleId, ct)
                    ?? throw new ArgumentNullException(nameof(saleId), "Чека (Sale) с таким id нет");

                var existingItem = sale.Items
                    .FirstOrDefault(i => i.Product.Id == product.Id);

                var totalNeeded = count + (existingItem?.Quantity ?? 0);

                if (inventoryItem.Quantity < totalNeeded)
                    throw new InvalidOperationException($"Недостаточно товара '{product.Name}' на складе. Запрошено: {count}, доступно: {inventoryItem.Quantity}");

                if (existingItem == null)
                {
                    var tempItem = new SaleItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        SaleId = sale.Id,
                        Quantity = count
                    };

                    sale.Items.Add(tempItem);
                    await _unitOfWork.SaleItemRepository.AddAsync(tempItem, ct);
                }
                else
                {
                    existingItem.Quantity += count;
                }

                await _unitOfWork.CommitAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw;
            }
        }

        public async Task UpdateQuantityAsync(Guid itemId, int count, CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            if (count < 1)
                throw new ArgumentException("Количество добавляемого товара не может быть меньше 1", nameof(count));

            var saleItem = await _unitOfWork.SaleItemRepository.GetByIdAsync(itemId)
                ?? throw new ArgumentNullException(nameof(itemId), "Такого товара в приходе нет");

            var inventoryItem = await _unitOfWork.InventoryItemRepository.GetByProductIdAsync(saleItem.Id, ct)
                ?? throw new ArgumentNullException($"Товара {saleItem.Product.Name} на складе нет");

            if (inventoryItem.Quantity < count)
                throw new InvalidOperationException($"Недостаточно товара '{saleItem.Product.Name}' на складе. Запрошено: {count}, доступно: {inventoryItem.Quantity}");

            saleItem.Quantity = count;

            await _unitOfWork.SaleItemRepository.Update(saleItem);
            await _unitOfWork.CommitAsync(ct);
        }

        public async Task DelItemAsync(Guid saleId, CancellationToken ct)
        {
            await _unitOfWork.SaleRepository.DeleteAsync(saleId, ct);
            await _unitOfWork.CommitAsync(ct);
        }

        public async Task ConfirmAsync(Guid saleId, CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var sale = await _unitOfWork.SaleRepository.GetByIdAsync(saleId, ct)
                    ?? throw new ArgumentNullException(nameof(saleId), "Чек не найден");

                foreach (var saleItem in sale.Items)
                {
                    // ✅ Загружаем товар для обновления (с трекингом, без AsNoTracking)
                    var inventoryItem = await _unitOfWork.InventoryItemRepository.GetByProductIdAsync(saleItem.ProductId, ct)
                        ?? throw new InvalidOperationException($"Товара с ID {saleItem.ProductId} на складе нет");

                    if (inventoryItem.Quantity < saleItem.Quantity)
                        throw new InvalidOperationException($"Недостаточно товара на складе");

                    inventoryItem.Quantity -= saleItem.Quantity;

                    // Update не нужен, если сущность отслеживается! EF Core сам сохранит изменения при SaveChanges
                    // Но если ваш UnitOfWork требует вызова Update, оставьте его.
                    // await _unitOfWork.InventoryItemRepository.Update(inventoryItem, ct); 
                }

                await _unitOfWork.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync(ct);
                throw new Exception($"Подтверждение продажи не прошло: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Атомарно записывает продажу, её позиции и списывает товар со склада.
        /// Вызывать ТОЛЬКО после подтверждения оплаты.
        /// </summary>
        public async Task CompleteSaleAsync(
            Guid saleId,
            string paymentType,
            IReadOnlyList<(string Barcode, int Quantity)> lines,
            CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(paymentType);
            ArgumentNullException.ThrowIfNull(lines);

            if (saleId == Guid.Empty)
                throw new ArgumentException("Неверный идентификатор продажи.", nameof(saleId));

            if (lines.Count == 0)
                throw new InvalidOperationException("Нельзя провести пустой чек.");

            if (lines.Any(x => string.IsNullOrWhiteSpace(x.Barcode) || x.Quantity <= 0))
                throw new InvalidOperationException("В чеке есть товар без штрихкода или с неверным количеством.");

            // Объединяем одинаковые товары, чтобы складской остаток
            // проверялся для всей продажи, а не для каждой строки отдельно.
            var positions = lines
                .GroupBy(x => x.Barcode, StringComparer.Ordinal)
                .Select(g => (Barcode: g.Key, Quantity: g.Sum(x => (long)x.Quantity)))
                .ToArray();

            if (positions.Any(x => x.Quantity > int.MaxValue))
                throw new InvalidOperationException("Превышено допустимое количество товара.");

            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var sale = new Sale
                {
                    Id = saleId,
                    PaymentType = paymentType,
                    SoldAt = DateTime.UtcNow,
                    WorkShiftId = _session.CurrentShiftId
                };

                // Репозиторий добавляет сущность в DbContext,
                // но НЕ вызывает SaveChanges сам.
                await _unitOfWork.SaleRepository.AddAsync(sale, ct);

                foreach (var (barcode, quantityLong) in positions)
                {
                    int quantity = checked((int)quantityLong);
                    var product = await _unitOfWork.ProductRepository.GetByBarcodeAsync(barcode, ct)
                        ?? throw new InvalidOperationException($"Товар со штрихкодом {barcode} не найден.");

                    // GetByProductIdAsync отслеживает InventoryItem в EF Core.
                    var stockItem = await _unitOfWork.InventoryItemRepository
                        .GetByProductIdAsync(product.Id, ct)
                        ?? throw new InvalidOperationException($"Товар '{product.Name}' отсутствует на складе.");

                    if (stockItem.Quantity < quantity)
                        throw new InvalidOperationException(
                            $"Недостаточно товара '{product.Name}'. Требуется: {quantity}, " +
                            $"на складе: {stockItem.Quantity}.");

                    stockItem.Quantity -= quantity;

                    await _unitOfWork.SaleItemRepository.AddAsync(new SaleItem
                    {
                        Id = Guid.NewGuid(),
                        SaleId = saleId,
                        ProductId = product.Id,
                        Quantity = quantity
                    }, ct);
                }

                // Один SaveChanges и один COMMIT на весь чек.
                await _unitOfWork.CommitAsync(ct);
            }
            catch
            {
                // UnitOfWork.CommitAsync может уже откатить транзакцию.
                // Если она всё ещё активна, откатываем здесь.
                await _unitOfWork.RollbackAsync(ct);
                throw;
            }
        }
    }
}
