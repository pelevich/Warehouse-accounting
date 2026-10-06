using Domain.Entities;
using Moq;
using Services.Interfaces.repository;
using Services.service;

namespace Pos_system.Test.Services
{
    public class BarcodeServiceTest
    {
        // IUnitOfWork !!!!!!!!!!! изменить тесты так как изменил IUnitOfWork
        private readonly Mock<IUnitOfWork> _productRepositoryMock;

        public BarcodeServiceTest()
        {
            _productRepositoryMock = new Mock<IProductRepository>();
        }

        [Fact]
        public async Task GenerateBarcodeNumber_ValidCode_ReturnsString()
        {
            var serviceBarcode = new BarcodeService(_productRepositoryMock.Object);

            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Свеча большая", Barcode = "5901234123457", Description = "Свеча большая восковая" },
                new Product { Id = Guid.NewGuid(), Name = "Ладен", Barcode = "4601234567890", Description = "Ладен жидкий" },
                new Product { Id = Guid.NewGuid(), Name = "Свеча маленькая", Barcode = "4607176800081", Description = "Свеча маленькая восковая" }
            };

            _productRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            var result = await serviceBarcode.GenerateBarcodeNumber();

            var code = result.Substring(0, 12);

            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = code[i] - '0';

                if (i % 2 == 0)
                {
                    sum += digit;
                }
                else
                {
                    sum += digit*3;
                }
            }

            int checkDigit = (10 - (sum % 10)) % 10;

            Assert.IsType<string>(result);
            Assert.Equal(result.Length, 13);
            Assert.True(result.All(char.IsDigit));
            Assert.Equal(result[^1].ToString(), checkDigit.ToString());
        }

        [Fact]
        public async Task ValideCode_Valide_ReturnTrue()
        {
            var serviceBarcode = new BarcodeService(_productRepositoryMock.Object);
            string code = "4607176800081";

            var result = await serviceBarcode.ValideCode(code);

            Assert.True(result);
        }

        [Fact]
        public async Task ValideCode_Valide_ReturnFalse()
        {
            var serviceBarcode = new BarcodeService(_productRepositoryMock.Object);
            string code = "4607176800080";

            var result = await serviceBarcode.ValideCode(code);

            Assert.False(result);
        }

        [Fact]
        public async Task GenerateBarcodeCodeImg_Valide_ReturnFalse()
        {
            var serviceBarcode = new BarcodeService(_productRepositoryMock.Object);
            string code = "4607176800080";

            var result = await serviceBarcode.GenerateBarcodeCodeImg(code);

            
        }
    }
}
