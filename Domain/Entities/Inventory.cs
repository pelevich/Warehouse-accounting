namespace Domain.Entities
{
    public class Inventory
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<InventoryItem>? Items { get; set; } = new();
        public double TotalAmount => Items?.Sum(i => i.Product.Price * i.Quantity) ?? 0;
    }
}
