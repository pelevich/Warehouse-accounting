namespace Domain.Entities
{
    public class InventoryItem
    {
        public Guid Id { get; set; }
        public Guid InventoryId { get; set; }
        public Inventory Inventory { get; set; }

        public Guid ProductId { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }
        public double TotalAmount => Product?.Price * Quantity ?? 0;
    }
}
