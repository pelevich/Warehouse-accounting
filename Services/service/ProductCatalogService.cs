using Services.DTOs;
using Services.Interfaces.repository;
using Services.Interfaces.service;

namespace Services.service
{
    public class ProductCatalogService : IProductCatalogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductCatalogService(IUnitOfWork unitOfWork) 
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ProductCatalogDto> FindByNameAsync(string name)
        {
            var result = await _unitOfWork.ProductCatalogRepository.GetByNameAsync(name);
            var obj = new ProductCatalogDto
            {
                Id = result.Id,
                Name = result.Name,
            };
            return obj;
        }
    }
}
