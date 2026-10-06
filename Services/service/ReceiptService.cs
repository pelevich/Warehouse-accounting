using Domain.Entities;
using Services.Interfaces.repository;
using Services.Interfaces.service;

namespace Services.service
{
    public class ReceiptService : IReceiptService
    {
        private IUnitOfWork _unitOfWork;

        public ReceiptService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task AddReceiptAsync(Guid receiptId, CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var receipt_t = new Receipt
                {
                    Id = receiptId,
                };

                await _unitOfWork.ReceiptRepository.AddAsync(receipt_t);

                await _unitOfWork.CommitAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw;
            }
        }

        //public async Task UpdateQuantityAsync(Guid itemId, int count, CancellationToken ct)
        //{
        //    if (count < 1)
        //        throw new ArgumentException("Количество добавляемого товара не может быть меньше 1", nameof(count));

        //    await _unitOfWork.BeginTransactionAsync();

        //    var receiptItem = await _unitOfWork.ReceiptItemRepository.GetByIdAsync(itemId)
        //        ?? throw new ArgumentNullException(nameof(itemId), "Такого товара в приходе нет");

        //    receiptItem.Quantity = count;

        //    await _unitOfWork.ReceiptItemRepository.Update(receiptItem);
        //    await _unitOfWork.CommitAsync();
        //}

        public async Task DelReceiptAsync(Guid receiptId, CancellationToken ct)
        {
            await _unitOfWork.ReceiptRepository.DeleteAsync(receiptId, ct);
            await _unitOfWork.CommitAsync();
        }

        public async Task ConfirmAsync(Guid receiptId, Guid inventoryId, CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var inventoryItems = await _unitOfWork.InventoryItemRepository.GetAllAsync(ct);
                var receipt = await _unitOfWork.ReceiptRepository.GetByIdAsync(receiptId, ct);

                foreach (var receiptItem in receipt.Items)
                {
                    var inventoryItem = inventoryItems.First(i => i.Product == receiptItem.Product);
                    if (inventoryItem != null)
                    {
                        inventoryItem.Quantity += receiptItem.Quantity;
                        await _unitOfWork.InventoryItemRepository.Update(inventoryItem, ct);
                    }
                    else
                    {
                        var inventoryTemp = await _unitOfWork.InventoryRepository.GetByIdAsync(inventoryId, ct);
                        var inventoryItemTemp = new InventoryItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = receiptItem.ProductId,
                            Product = receiptItem.Product,
                            InventoryId = inventoryTemp.Id,
                            Inventory = inventoryTemp,
                            Quantity = receiptItem.Quantity
                        };
                        await _unitOfWork.InventoryItemRepository.AddAsync(inventoryItemTemp, ct);
                        inventoryTemp.Items.Add(inventoryItemTemp);
                    }

                    await _unitOfWork.CommitAsync(ct);
                }
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw new Exception("Подтверждение продажи не прошло");
            }
        }

        public async Task<Receipt> GetByIdAsync(Guid id)
        {
            var receipt = await _unitOfWork.ReceiptRepository.GetByIdAsync(id);
            return receipt;
        }
    }
}
