using BarcodeStandard;
using Domain.Entities;
using Services.Interfaces.repository;
using Services.Interfaces.service;

namespace Services.service
{
    public class SaleService : ISaleService
    {
        private IUnitOfWork _unitOfWork;

        public SaleService(IUnitOfWork unitOfWork) 
        {
            _unitOfWork = unitOfWork;
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
                    .FirstOrDefault(i => i.Product.Barcode == barcode);

                var totalNeeded = count + (existingItem?.Quantity ?? 0);

                if (inventoryItem.Quantity < totalNeeded)
                    throw new InvalidOperationException($"Недостаточно товара '{product.Name}' на складе. Запрошено: {count}, доступно: {inventoryItem.Quantity}");

                if (existingItem == null)
                {
                    var tempItem = new SaleItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        Product = product,
                        SaleId = sale.Id,
                        Sale = sale,
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
            await _unitOfWork.SaleRepository.DeleteAsync(saleId);
            await _unitOfWork.CommitAsync();
        }

        public async Task ConfirmAsync(Guid saleId, Guid inventoryId, CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {    
                var inventoryItems = await _unitOfWork.InventoryItemRepository.GetAllAsync(ct);
                var sale = await _unitOfWork.SaleRepository.GetByIdAsync(saleId, ct);

                foreach (var saleItem in sale.Items)
                {
                    var inventoryItem = inventoryItems.First(i => i.Product == saleItem.Product);
                    if (inventoryItem != null)
                    {
                        inventoryItem.Quantity -= saleItem.Quantity;
                        await _unitOfWork.InventoryItemRepository.Update(inventoryItem, ct);
                    }
                    else
                    {
                        throw new ArgumentNullException($"Товара {saleItem.Product.Name} на складе нет");
                    }
                }

                await _unitOfWork.CommitAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw new Exception("Подтверждение продажи не прошло");
            }
        }
    }
}
