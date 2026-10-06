using Domain.Entities;
using Services.DTOs;
using Services.Interfaces.repository;
using Services.Interfaces.service;
using Microsoft.Extensions.Logging;

namespace Services.service
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IProductService> _logger;

        public ProductService(IUnitOfWork unitOfWork, ILogger<IProductService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task AddProductAsync(ProductDto dto, Guid CatalogId)
        {
            if (CatalogId == Guid.Empty)
            {
                var ex = new ArgumentNullException(nameof(CatalogId));
                _logger.LogError(ex, "Id расписания равно null");
                throw ex;
            }

            var file = new Product
            {
                Id = dto.Id.Value,
                Name = dto.Name,
                Barcode = dto.Barcode,
                Price = dto.Price.Value,
                Description = dto.Description,
                CatalogId = CatalogId
            };

            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.ProductRepository.AddAsync(file);
            await _unitOfWork.CommitAsync();
        }

        public async Task DeleteProductAsync(Guid id)
        {
            if (id == null || id == Guid.Empty)
            {
                var ex = new ArgumentNullException("id равен null", nameof(id));
                _logger.LogError(ex, "Аргумент не может быть равен null, так как не существует такой таблицы");
                throw ex;
            }

            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.ProductRepository.DeleteAsync(id);
            await _unitOfWork.CommitAsync();
        }

        public async Task UpdateProductAsync(ProductDto dto)
        {
            if (dto.Id == null)
            {
                var ex = new ArgumentNullException("id равен null", nameof(dto.Id));
                _logger.LogError(ex, "Таблицы newProduct с id равно null не существует");
                throw ex;
            }

            await _unitOfWork.BeginTransactionAsync();

            var file = new Product
            {
                Id = dto.Id.Value,
                Name = dto.Name,
                Barcode = dto.Barcode,
                Price = dto.Price.Value,
                Description = dto.Description,
            };

            var newProduct= await _unitOfWork.ProductRepository.GetByIdAsync(dto.Id.Value);
            newProduct.Name = dto.Name ?? newProduct.Name;
            newProduct.Barcode = dto.Barcode ?? newProduct.Barcode;
            newProduct.Price = dto.Price ?? newProduct.Price;
            newProduct.Description = dto.Description ?? newProduct.Description;

            await _unitOfWork.ProductRepository.Update(newProduct);
            await _unitOfWork.CommitAsync();
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await _unitOfWork.ProductRepository.GetAllAsync();
            List<ProductDto>? productsDto = new List<ProductDto>() { };

            foreach (var product in products)
            {
                var productDto = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Barcode = product.Barcode,
                    Price = product.Price,
                    Description = product.Description,
                };

                productsDto.Add(productDto);
            }

            return productsDto;
        }

        public async Task<ProductDto> GetByIdAsync(Guid Id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(Id);

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Barcode = product.Barcode,
                Price = product.Price,
                Description = product.Description,
            };

            return productDto;
        }

        public async Task<IReadOnlyList<Product>> SearchAsync(string query, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Array.Empty<Product>();

            var q = query.Trim();

            // Ищем по имени ИЛИ штрихкоду
            var results = await _unitOfWork.ProductRepository.SearchAsync(q, ct);
            return results;
        }
    }
}
