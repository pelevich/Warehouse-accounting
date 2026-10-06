namespace Services.DTOs
{
    public class InventoryItemDto
    {
        public Guid? Id {  get; set; }
        public Guid? InventoryId { get; set; }
        public Guid? ProductId { get; set; }
        public int? Quantity { get; set; }
    }
}
