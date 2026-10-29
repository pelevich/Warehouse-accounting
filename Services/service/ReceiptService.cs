using Domain.Entities;
using Services.Interfaces.repository;
using Services.Interfaces.service;

namespace Services.service
{
    public class ReceiptService : IReceiptService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISessionService _session;

        public ReceiptService(IUnitOfWork unitOfWork, ISessionService session)
        {
            _unitOfWork = unitOfWork;
            _session = session;
        }

        public async Task AddReceiptAsync(Guid receiptId, CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var receipt_t = new Receipt
                {
                    Id = receiptId,
                    WorkShiftId = _session.CurrentShiftId
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

        public async Task ConfirmAsync(
            Guid receiptId,
            Guid inventoryId,
            CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var receipt = await _unitOfWork
                    .ReceiptRepository
                    .GetByIdAsync(receiptId, ct)
                    ?? throw new InvalidOperationException(
                        "Документ прихода не найден.");

                if (receipt.SoldAt != default)
                    throw new InvalidOperationException(
                        "Этот приход уже проведён.");

                if (receipt.Items == null || receipt.Items.Count == 0)
                    throw new InvalidOperationException(
                        "Нельзя провести пустой приход.");

                var inventory = await _unitOfWork
                    .InventoryRepository
                    .GetByIdAsync(inventoryId, ct)
                    ?? throw new InvalidOperationException(
                        "Склад не найден.");

                foreach (var receiptItem in receipt.Items)
                {
                    if (receiptItem.Quantity <= 0)
                        throw new InvalidOperationException(
                            "Количество товара должно быть больше нуля.");

                    // Получаем существующий остаток по ProductId.
                    var inventoryItem = await _unitOfWork
                        .InventoryItemRepository
                        .GetByProductIdAsync(
                            receiptItem.ProductId, ct);

                    if (inventoryItem != null)
                    {
                        // EF Core сохранит изменение автоматически,
                        // если репозиторий возвращает tracked-сущность.
                        inventoryItem.Quantity = checked(
                            inventoryItem.Quantity + receiptItem.Quantity);
                    }
                    else
                    {
                        var newItem = new InventoryItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = receiptItem.ProductId,
                            InventoryId = inventoryId,
                            Quantity = receiptItem.Quantity
                        };

                        await _unitOfWork
                            .InventoryItemRepository
                            .AddAsync(newItem, ct);
                    }
                }

                // Дата фиксируется только при успешном проведении.
                receipt.SoldAt = DateTime.UtcNow;

                // Сохраняем остатки и дату одной транзакцией.
                await _unitOfWork.CommitAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw;
            }
        }



        public async Task<Receipt> GetByIdAsync(Guid id)
        {
            var receipt = await _unitOfWork.ReceiptRepository.GetByIdAsync(id);
            return receipt;
        }
    }
}
