using Domain.Entities;

namespace Services.DTOs
{
    public class ProductCatalogDto
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public List<ProductDto>? Products { get; set; } //!!!!! Product или ProductDto
    }
}
