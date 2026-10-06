using Services.DTOs;

namespace Services.Interfaces.service
{
    public interface IProductCatalogService
    {
        public Task<ProductCatalogDto> FindByNameAsync(string name);
    }
}
