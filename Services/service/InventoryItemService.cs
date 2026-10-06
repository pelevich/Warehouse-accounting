using Domain.Entities;
using Microsoft.Extensions.Logging;
using Services.DTOs;
using Services.Interfaces.repository;
using Services.Interfaces.service;
using SkiaSharp;

namespace Services.service
{
    public class InventoryItemService : IInventoryItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IInventoryItemService> _logger;

        public InventoryItemService(IUnitOfWork unitOfWork, ILogger<IInventoryItemService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<InventoryItemDto>> GetAllAsync() 
        { 
            var items = await _unitOfWork.InventoryItemRepository.GetAllAsync();
            var itemDTOs = new List<InventoryItemDto>();

            foreach (var item in items)
            {
                var itemsDto = new InventoryItemDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    InventoryId = item.InventoryId,
                    Quantity = item.Quantity,
                };
                
                itemDTOs.Add(itemsDto);
            }

            return itemDTOs;
        }

        public async Task AddItemAsync(InventoryItemDto item, CancellationToken ct) 
        {
            try
            {
                if (item is null)
                {
                    _logger.LogError("Передаваемый аргумент (item) равен null", nameof(item));
                    throw new ArgumentNullException(nameof(item));
                }

                if (item.Id.Value == null)
                {
                    _logger.LogError("InventoryItem с данным Id не существует", nameof(item.Id));
                    throw new ArgumentNullException(nameof(item.Id));
                }

                if (item.ProductId.Value == null)
                {
                    _logger.LogError("ProductId равен null", nameof(item.ProductId));
                    throw new ArgumentNullException(nameof(item.ProductId));
                }

                if (item.InventoryId.Value == null)
                {
                    _logger.LogError("InventoryId равен null", nameof(item.InventoryId));
                    throw new ArgumentNullException(nameof(item.InventoryId));
                }

                await _unitOfWork.BeginTransactionAsync(ct);

                var obj = new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId.Value,
                    InventoryId = item.InventoryId.Value,
                    Quantity = item.Quantity.Value,
                };

                await _unitOfWork.InventoryItemRepository.AddAsync(obj, ct);
                await _unitOfWork.CommitAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw new Exception("Добавить товар на склад не удалось");
            }
        }

        public async Task UpdateItemAsync(InventoryItemDto item, CancellationToken ct)
        {
            try
            {    
                if (item is null)
                {
                    _logger.LogError("Передаваемый аргумент (item) равен null", nameof(item));
                    throw new ArgumentNullException(nameof(item));
                }

                if (item.Id == null)
                {
                    _logger.LogError("InventoryItem с данным Id не существует", nameof(item.Id));
                    throw new ArgumentNullException(nameof(item.Id));
                }

                if (item.ProductId == null)
                {
                    _logger.LogError("ProductId равен null", nameof(item.ProductId));
                    throw new ArgumentNullException(nameof(item.ProductId));
                }

                if (item.InventoryId == null)
                {
                    _logger.LogError("InventoryId равен null", nameof(item.InventoryId));
                    throw new ArgumentNullException(nameof(item.InventoryId));
                }

                await _unitOfWork.BeginTransactionAsync(ct);

                var obj = new InventoryItem
                {
                    Id = item.Id.Value,
                    ProductId = item.ProductId.Value,
                    InventoryId = item.InventoryId.Value,
                    Quantity = item.Quantity.Value,
                };

                await _unitOfWork.InventoryItemRepository.Update(obj, ct);
                await _unitOfWork.CommitAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw new Exception("Обновить товар на складе не удалось");
            }
        }
    }
}
