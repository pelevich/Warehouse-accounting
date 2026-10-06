using BarcodeStandard;
using Domain.Entities;
using Moq;
using Services.Interfaces.repository;
using Services.Interfaces.service;
using Services.service;

namespace Pos_system.Test.Services
{
    public class SaleServiceTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IProductRepository> _productRepoMock;
        private readonly Mock<IReceiptRepository> _receiptRepoMock;
        private readonly Mock<IReceiptItemRepository> _receiptItemRepoMock;
        private readonly Mock<IInventoryRepository> _inventoryRepoMock;
        private readonly Mock<IInventoryItemRepository> _inventoryItemRepoMock;
        private readonly Mock<ISaleRepository> _saleRepoMock;
        private readonly Mock<ISaleItemRepository> _saleItemRepoMock;
        private readonly ISaleService _saleService;

        public SaleServiceTest()
        {
            _productRepoMock = new Mock<IProductRepository>();
            _receiptRepoMock = new Mock<IReceiptRepository>();
            _receiptItemRepoMock = new Mock<IReceiptItemRepository>();
            _inventoryRepoMock = new Mock<IInventoryRepository>();
            _inventoryItemRepoMock = new Mock<IInventoryItemRepository>();
            _saleRepoMock = new Mock<ISaleRepository>();
            _saleItemRepoMock = new Mock<ISaleItemRepository>();

            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _unitOfWorkMock.SetupGet(x => x.ProductRepository).Returns(_productRepoMock.Object);
            _unitOfWorkMock.SetupGet(x => x.ReceiptRepository).Returns(_receiptRepoMock.Object);
            _unitOfWorkMock.SetupGet(x => x.ReceiptItemRepository).Returns(_receiptItemRepoMock.Object);
            _unitOfWorkMock.SetupGet(x => x.InventoryRepository).Returns(_inventoryRepoMock.Object);
            _unitOfWorkMock.SetupGet(x => x.InventoryItemRepository).Returns(_inventoryItemRepoMock.Object);
            _unitOfWorkMock.SetupGet(x => x.SaleRepository).Returns(_saleRepoMock.Object);
            _unitOfWorkMock.SetupGet(x => x.SaleItemRepository).Returns(_saleItemRepoMock.Object);

            _saleService = new SaleService(_unitOfWorkMock.Object);
        }

        private void SetupTransaction()
        {
            _unitOfWorkMock.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(x => x.RollbackAsync()).Returns(Task.CompletedTask);
        }


        [Fact]
        public async Task AddItemAsync_NewItem_AddsToRepository()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            { 
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            inventory.Items.Add(inventoryItem);

            _productRepoMock
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(product);
            _inventoryItemRepoMock
                .Setup(x => x.GetByProductIdAsync(product.Id))
                .ReturnsAsync(inventoryItem);
            _saleRepoMock
                .Setup(x => x.GetByIdAsync(sale.Id))
                .ReturnsAsync(sale);
            _saleItemRepoMock
                .Setup(x => x.AddAsync(It.IsAny<SaleItem>(), It.IsAny<CancellationToken>()))
                .Callback<SaleItem, CancellationToken>((item, ct) => captured = item)
                .ReturnsAsync((SaleItem item, CancellationToken ct) => item);

            await _saleService.AddItemAsync(barcode, 3, sale.Id, default);

            Assert.NotNull(captured);
            Assert.Equal(3, captured.Quantity);
        }

        [Fact]
        public async Task AddItemAsync_CountLessOne_ThrowsArgumentException()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            inventory.Items.Add(inventoryItem);

            _productRepoMock
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(product);
            _inventoryItemRepoMock
                .Setup(x => x.GetByProductIdAsync(product.Id))
                .ReturnsAsync(inventoryItem);
            _saleRepoMock
                .Setup(x => x.GetByIdAsync(sale.Id))
                .ReturnsAsync(sale);
            _saleItemRepoMock
                .Setup(x => x.AddAsync(It.IsAny<SaleItem>(), It.IsAny<CancellationToken>()))
                .Callback<SaleItem, CancellationToken>((item, ct) => captured = item)
                .ReturnsAsync((SaleItem item, CancellationToken ct) => item);

            await Assert.ThrowsAnyAsync<ArgumentException> (
                () => _saleService.AddItemAsync(barcode, -5, sale.Id, default)
            );
        }

        [Fact]
        public async Task AddItemAsync_NotFoundProduct_ThrowsArgumentNullException()
        {
            SetupTransaction();

            var barcode = "4607176833381";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            inventory.Items.Add(inventoryItem);

            _productRepoMock
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(product);
            _inventoryItemRepoMock
                .Setup(x => x.GetByProductIdAsync(product.Id))
                .ReturnsAsync(inventoryItem);
            _saleRepoMock
                .Setup(x => x.GetByIdAsync(sale.Id))
                .ReturnsAsync(sale);
            _saleItemRepoMock
                .Setup(x => x.AddAsync(It.IsAny<SaleItem>(), It.IsAny<CancellationToken>()))
                .Callback<SaleItem, CancellationToken>((item, ct) => captured = item)
                .ReturnsAsync((SaleItem item, CancellationToken ct) => item);

            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => _saleService.AddItemAsync("4607176832181", 5, sale.Id, default)
            );
        }

        [Fact]
        public async Task AddItemAsync_NotFoundSale_ThrowsArgumentException()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            inventory.Items.Add(inventoryItem);

            _productRepoMock
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(product);
            _inventoryItemRepoMock
                .Setup(x => x.GetByProductIdAsync(product.Id))
                .ReturnsAsync(inventoryItem);
            _saleRepoMock
                .Setup(x => x.GetByIdAsync(sale.Id))
                .ReturnsAsync(sale);
            _saleItemRepoMock
                .Setup(x => x.AddAsync(It.IsAny<SaleItem>(), It.IsAny<CancellationToken>()))
                .Callback<SaleItem, CancellationToken>((item, ct) => captured = item)
                .ReturnsAsync((SaleItem item, CancellationToken ct) => item);

            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => _saleService.AddItemAsync(barcode, 5, Guid.NewGuid(), default)
            );
        }

        // Товара нет на складе, но пытаються добавить в чек
        [Fact]
        public async Task AddItemAsync_ProductNotInStock_ThrowsArgumentException()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            

            _productRepoMock
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(product);
            _saleRepoMock
                .Setup(x => x.GetByIdAsync(sale.Id))
                .ReturnsAsync(sale);
            _saleItemRepoMock
                .Setup(x => x.AddAsync(It.IsAny<SaleItem>(), It.IsAny<CancellationToken>()))
                .Callback<SaleItem, CancellationToken>((item, ct) => captured = item)
                .ReturnsAsync((SaleItem item, CancellationToken ct) => item);

            await Assert.ThrowsAnyAsync<ArgumentNullException>(
                () => _saleService.AddItemAsync(barcode, 5, sale.Id, default)
            );
        }

        [Fact]
        public async Task AddItemAsync_ExistingItemNull_CountMoreInStock_ThrowsInvalidOperationException()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            inventory.Items.Add(inventoryItem);

            _productRepoMock
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(product);
            _inventoryItemRepoMock
                .Setup(x => x.GetByProductIdAsync(product.Id))
                .ReturnsAsync(inventoryItem);
            _saleRepoMock
                .Setup(x => x.GetByIdAsync(sale.Id))
                .ReturnsAsync(sale);
            _saleItemRepoMock
                .Setup(x => x.AddAsync(It.IsAny<SaleItem>(), It.IsAny<CancellationToken>()))
                .Callback<SaleItem, CancellationToken>((item, ct) => captured = item)
                .ReturnsAsync((SaleItem item, CancellationToken ct) => item);

            await Assert.ThrowsAnyAsync<InvalidOperationException>(
                () => _saleService.AddItemAsync(barcode, 30, sale.Id, default)
            );
        }

        [Fact]
        public async Task AddItemAsync_ExistingItem_CountMoreInStock_ThrowsInvalidOperationException()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            sale.Items.Add(saleItem);

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            inventory.Items.Add(inventoryItem);

            _productRepoMock
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(product);
            _inventoryItemRepoMock
                .Setup(x => x.GetByProductIdAsync(product.Id))
                .ReturnsAsync(inventoryItem);
            _saleRepoMock
                .Setup(x => x.GetByIdAsync(sale.Id))
                .ReturnsAsync(sale);
            _saleItemRepoMock
                .Setup(x => x.AddAsync(It.IsAny<SaleItem>(), It.IsAny<CancellationToken>()))
                .Callback<SaleItem, CancellationToken>((item, ct) => captured = item)
                .ReturnsAsync((SaleItem item, CancellationToken ct) => item);

            await Assert.ThrowsAnyAsync<InvalidOperationException>(
                () => _saleService.AddItemAsync(barcode, 30, sale.Id, default)
            );

            Assert.Null( captured );
        }

        [Fact]
        public async Task UpdateQuantity_ValidCode_QuantityFive()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            sale.Items.Add(saleItem);

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            inventory.Items.Add(inventoryItem);

            _saleItemRepoMock
                .Setup(x => x.GetByIdAsync(saleItem.Id))
                .ReturnsAsync(saleItem);

            await _saleService.UpdateQuantityAsync(saleItem.Id, 5, default);

            Assert.Equal(saleItem.Quantity, 5);
        }

        [Fact]
        public async Task UpdateQuantity_CountLessOne_ThrowsArgumentException()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            sale.Items.Add(saleItem);

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            inventory.Items.Add(inventoryItem);

            _saleItemRepoMock
                .Setup(x => x.GetByIdAsync(saleItem.Id))
                .ReturnsAsync(saleItem);

            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => _saleService.UpdateQuantityAsync(saleItem.Id, -25, default)
            );
        }

        [Fact]
        public async Task UpdateQuantity_NotFoundSaleItem_ThrowsArgumentException()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            sale.Items.Add(saleItem);

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            inventory.Items.Add(inventoryItem);

            _saleItemRepoMock
                .Setup(x => x.GetByIdAsync(saleItem.Id))
                .ReturnsAsync(saleItem);

            await Assert.ThrowsAnyAsync<ArgumentNullException>(
                () => _saleService.UpdateQuantityAsync(Guid.NewGuid(), 3, default)
            );
        }

        [Fact]
        public async Task UpdateQuantity_NotFoundInventoryItem_ThrowsArgumentException()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = Guid.NewGuid(),
                Quantity = 3,
            };

            sale.Items.Add(saleItem);

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            inventory.Items.Add(inventoryItem);

            _saleItemRepoMock
                .Setup(x => x.GetByIdAsync(saleItem.Id))
                .ReturnsAsync(saleItem);

            await Assert.ThrowsAnyAsync<ArgumentNullException>(
                () => _saleService.UpdateQuantityAsync(saleItem.Id, 3, default)
            );
        }

        [Fact]
        public async Task UpdateQuantity_CountMoreInStock_ThrowsInvalidOperationException()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            sale.Items.Add(saleItem);

            SaleItem? captured = null;

            var inventory = new Inventory
            {
                Id = Guid.NewGuid()
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Inventory = inventory,
                InventoryId = inventory.Id,
                Quantity = 10
            };

            inventory.Items.Add(inventoryItem);

            _saleItemRepoMock
                .Setup(x => x.GetByIdAsync(saleItem.Id))
                .ReturnsAsync(saleItem);

            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => _saleService.UpdateQuantityAsync(saleItem.Id, 25, default)
            );
        }

        [Fact]
        public async Task ConfirmAsync_WriteOff_NoThrows()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            sale.Items.Add(saleItem);

            var inventory = new Inventory
            {
                Id = Guid.NewGuid(),
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                InventoryId = inventory.Id,
                Inventory = inventory,
                ProductId = product.Id,
                Product = product,
                Quantity = 5
            };

            inventory.Items.Add(inventoryItem);

            _inventoryItemRepoMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(inventory.Items);
            _saleRepoMock
                .Setup(x => x.GetByIdAsync(sale.Id))
                .ReturnsAsync(sale);
            _inventoryRepoMock
                .Setup(x => x.GetByIdAsync(inventory.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(inventory);

            await _saleService.ConfirmAsync(sale.Id, inventory.Id, default);

            Assert.Equal(2, inventoryItem.Quantity);
        }

        [Fact]
        public async Task ConfirmAsync_WriteOff_NoThrows2()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var product2 = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча маленькая",
                Description = "Свеча из воска",
                Barcode = "4607176833381",
                Price = 50
            };

            var sale = new Sale
            {
                Id = Guid.NewGuid()
            };

            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                ProductId = product.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 3,
            };

            var saleItem2 = new SaleItem
            {
                Id = Guid.NewGuid(),
                Product = product2,
                ProductId = product2.Id,
                Sale = sale,
                SaleId = sale.Id,
                Quantity = 1,
            };

            sale.Items.Add(saleItem);
            sale.Items.Add(saleItem2);

            var inventory = new Inventory
            {
                Id = Guid.NewGuid(),
            };

            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                InventoryId = inventory.Id,
                Inventory = inventory,
                ProductId = product.Id,
                Product = product,
                Quantity = 5
            };

            inventory.Items.Add(inventoryItem);

            _inventoryItemRepoMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(inventory.Items);
            _saleRepoMock
                .Setup(x => x.GetByIdAsync(sale.Id))
                .ReturnsAsync(sale);
            _inventoryRepoMock
                .Setup(x => x.GetByIdAsync(inventory.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(inventory);

            await Assert.ThrowsAnyAsync<Exception>(
                () => _saleService.ConfirmAsync(sale.Id, inventory.Id, default)
            );

            _unitOfWorkMock.Verify(
                x => x.RollbackAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
