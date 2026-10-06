namespace Services.DTOs
{
    public class ProductDto
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public string? Barcode { get; set; }
        public double? Price { get; set; }
        public string? Description { get; set; }

        //public InventoryDto? Inventory { get; set; }
        //public ProductCatalogDto? Catalog { get; set; }
    }
}
