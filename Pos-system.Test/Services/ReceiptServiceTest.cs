using Domain.Entities;
using Moq;
using Services.Interfaces.repository;
using Services.Interfaces.service;
using Services.service;

namespace Pos_system.Test.Services
{
    public class ReceiptServiceTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IProductRepository> _productRepoMock;
        private readonly Mock<IReceiptRepository> _receiptRepoMock;
        private readonly Mock<IReceiptItemRepository> _receiptItemRepoMock;
        private readonly Mock<IInventoryRepository> _inventoryRepoMock;
        private readonly Mock<IInventoryItemRepository> _inventoryItemRepoMock;
        private readonly IReceiptService _receiptService;

        public ReceiptServiceTest()
        {
            _productRepoMock = new Mock<IProductRepository>();
            _receiptRepoMock = new Mock<IReceiptRepository>();
            _receiptItemRepoMock = new Mock<IReceiptItemRepository>();
            _inventoryRepoMock = new Mock<IInventoryRepository>();
            _inventoryItemRepoMock = new Mock<IInventoryItemRepository>();

            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _unitOfWorkMock.SetupGet(x => x.ProductRepository).Returns(_productRepoMock.Object);
            _unitOfWorkMock.SetupGet(x => x.ReceiptRepository).Returns(_receiptRepoMock.Object);
            _unitOfWorkMock.SetupGet(x => x.ReceiptItemRepository).Returns(_receiptItemRepoMock.Object);
            _unitOfWorkMock.SetupGet(x => x.InventoryRepository).Returns(_inventoryRepoMock.Object);
            _unitOfWorkMock.SetupGet(x => x.InventoryItemRepository).Returns(_inventoryItemRepoMock.Object);

            _receiptService = new ReceiptService(_unitOfWorkMock.Object);
        }

        private void SetupTransaction()
        {
            _unitOfWorkMock.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task AddItemAsync_NewItem_AddsToRepository()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var receipt = new Receipt 
            { 
                Id = Guid.NewGuid()
            };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            ReceiptItem? captured = null;

            _unitOfWorkMock
                .Setup(x => x.BeginTransactionAsync())
                .Returns(Task.FromResult(0));
            _unitOfWorkMock
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            _productRepoMock
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(product);
            _receiptRepoMock
                .Setup(x => x.GetByIdAsync(receipt.Id))
                .ReturnsAsync(receipt);
            _receiptItemRepoMock
                .Setup(x => x.AddAsync(It.IsAny<ReceiptItem>(), It.IsAny<CancellationToken>()))
                .Callback<ReceiptItem, CancellationToken>((item, ct) => captured = item)
                .ReturnsAsync((ReceiptItem item, CancellationToken ct) => item);

            await _receiptService.AddItemAsync(barcode, 10, receipt.Id, default);


            var existingItem = receipt.Items
                .FirstOrDefault(i => i.Product.Barcode == barcode);

            Assert.NotNull(existingItem);
            Assert.NotNull(captured);
            Assert.NotNull(receipt.Items);
            Assert.Equal(captured.Quantity, 10);
            Assert.Equal(captured.ReceiptId, receipt.Id);
            Assert.Equal(captured.ProductId, product.Id);
            Assert.Equal(captured.Product.Barcode, barcode);
        }

        [Fact]
        public async Task AddItemAsync_ExistingItem_IncrQuantity()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var receipt = new Receipt
            {
                Id = Guid.NewGuid()
            };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var exisItem = new ReceiptItem
            {
                Id = Guid.NewGuid(),
                ReceiptId = receipt.Id,
                Receipt = receipt,
                ProductId = product.Id,
                Product = product,
                Quantity = 10
            };

            receipt.Items.Add(exisItem);

            _unitOfWorkMock
                .Setup(x => x.BeginTransactionAsync())
                .Returns(Task.FromResult(0));
            _unitOfWorkMock
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            _productRepoMock
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(product);
            _receiptRepoMock
                .Setup(x => x.GetByIdAsync(receipt.Id))
                .ReturnsAsync(receipt);

            await _receiptService.AddItemAsync(barcode, 5, receipt.Id, default);

            Assert.Equal(exisItem.Quantity, 15);
        }

        // !!!!!
        [Fact]
        public async Task AddItemAsync_ThrowEx_BarcodeNoValid()
        {
            SetupTransaction();

            var barcode = "4607176877781";

            var receipt = new Receipt
            {
                Id = Guid.NewGuid()
            };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            _unitOfWorkMock
                .Setup(x => x.BeginTransactionAsync())
                .Returns(Task.FromResult(0));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _receiptService.AddItemAsync(barcode, 5, receipt.Id, default)
            );
        }

        // !!!!!
        [Fact]
        public async Task AddItemAsync_ThrowEx_ReceiptNoValid()
        { 
            SetupTransaction();

            var barcode = "4607176877781";

            var receipt = new Receipt
            {
                Id = Guid.NewGuid()
            };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            _unitOfWorkMock
                .Setup(x => x.BeginTransactionAsync())
                .Returns(Task.FromResult(0));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _receiptService.AddItemAsync(barcode, 5, receipt.Id, default)
            );
        }

        [Fact]
        public async Task AddItemAsync_QuantityLessOne_ArgumentException()
        {
            SetupTransaction();

            var barcode = "4607176877781";

            var receipt = new Receipt
            {
                Id = Guid.NewGuid()
            };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            _unitOfWorkMock
                .Setup(x => x.BeginTransactionAsync())
                .Returns(Task.FromResult(0));

            await Assert.ThrowsAsync<ArgumentException>(
                () => _receiptService.AddItemAsync(barcode, -5, receipt.Id, default)
            );
        }

        [Fact]
        public async Task UpdateQuantity_ValidCode_Quantity5()
        {
            SetupTransaction();

            var barcode = "4607176877781";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var item = new ReceiptItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Product = product,
                Quantity = 10
            };

            _receiptItemRepoMock
                .Setup(x => x.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            await _receiptService.UpdateQuantityAsync(item.Id, 5, default);

            Assert.Equal(item.Quantity, 5);
        }

        [Fact]
        public async Task UpdateQuantity_ThrowEx_ArgumentNullException()
        {
            SetupTransaction();

            var barcode = "4607176877781";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var item = new ReceiptItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Product = product,
                Quantity = 10
            };



            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _receiptService.UpdateQuantityAsync(item.Id, 5, default)
            );
        }

        [Fact]
        public async Task UpdateQuantityAsync_QuantityLessOne_ArgumentException()
        {
            SetupTransaction();

            var barcode = "4607176877781";

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var item = new ReceiptItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Product = product,
                Quantity = 10
            };

            _receiptItemRepoMock
                .Setup(x => x.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _receiptService.UpdateQuantityAsync(item.Id, -5, default)
            );
        }

        [Fact]
        public async Task ConfirmAsync_ThrowEx_ArgumentNullException()
        {
            SetupTransaction();

            var barcode = "4607176800081";

            var receipt = new Receipt
            {
                Id = Guid.NewGuid()
            };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Свеча большая",
                Description = "Свеча из воска",
                Barcode = barcode,
                Price = 100
            };

            var exisItem = new ReceiptItem
            {
                Id = Guid.NewGuid(),
                ReceiptId = receipt.Id,
                Receipt = receipt,
                ProductId = product.Id,
                Product = product,
                Quantity = 10
            };

            receipt.Items.Add(exisItem);

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

            _unitOfWorkMock
                .Setup(x => x.BeginTransactionAsync())
                .Returns(Task.FromResult(0));
            _unitOfWorkMock
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);
            _inventoryRepoMock
                .Setup(x => x.GetByIdAsync(inventory.Id))
                .ReturnsAsync(inventory);
            _inventoryItemRepoMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<InventoryItem> { inventoryItem });
            _productRepoMock
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(product);
            _receiptRepoMock
                .Setup(x => x.GetByIdAsync(receipt.Id))
                .ReturnsAsync(receipt);

            await _receiptService.ConfirmAsync(receipt.Id, inventory.Id, default);

            Assert.Equal(inventoryItem.Quantity, 15);
        }
    }
}
