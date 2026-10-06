namespace Domain.Entities
{
    public class ProductCatalog
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<Product>? Products { get; set; } = new ();
    }
}
