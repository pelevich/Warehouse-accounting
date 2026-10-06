namespace Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }

        public Guid? CatalogId { get; set; }
        public ProductCatalog? Catalog{ get; set; }
    }
}
