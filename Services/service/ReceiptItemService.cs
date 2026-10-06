using Domain.Entities;
using Services.Interfaces.repository;
using Services.Interfaces.service;

namespace Services.service
{
    public class ReceiptItemService : IReceiptItemService
    {
        private IUnitOfWork _unitOfWork;

        public ReceiptItemService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task AddItemAsync(string barcode, int count, Guid receiptId, CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                if (count < 1)
                    throw new ArgumentException("Количество добавляемого товара не может быть меньше 1", nameof(count));

                var product = await _unitOfWork.ProductRepository.GetByBarcodeAsync(barcode, ct)
                    ?? throw new ArgumentNullException(nameof(barcode), "Продукта с таким штрих-кодом нет");

                var receipt = await _unitOfWork.ReceiptRepository.GetByIdAsync(receiptId, ct)
                    ?? throw new ArgumentNullException(nameof(receiptId), "Прихода (Receipt) с таким id нет");

                var existingItem = receipt.Items
                    .FirstOrDefault(i => i.Product.Barcode == barcode);

                if (existingItem == null)
                {
                    var tempItem = new ReceiptItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        ReceiptId = receipt.Id,
                        Quantity = count
                    };

                    receipt.Items.Add(tempItem);
                    await _unitOfWork.ReceiptItemRepository.AddAsync(tempItem, ct);
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

        public async Task DelReceiptItemAsync(Guid itemId, CancellationToken ct)
        {
            await _unitOfWork.ReceiptItemRepository.DeleteAsync(itemId, ct);
            await _unitOfWork.CommitAsync();
        }

        public async Task UpdateReceiptItemAsync(Guid itemId, int count, CancellationToken ct)
        {
            if (count < 1)
                throw new ArgumentException("Количество добавляемого товара не может быть меньше 1", nameof(count));

            await _unitOfWork.BeginTransactionAsync();

            var receiptItem = await _unitOfWork.ReceiptItemRepository.GetByIdAsync(itemId)
                ?? throw new ArgumentNullException(nameof(itemId), "Такого товара в приходе нет");

            receiptItem.Quantity = count;

            await _unitOfWork.ReceiptItemRepository.Update(receiptItem);
            await _unitOfWork.CommitAsync();
        }
    }
}
