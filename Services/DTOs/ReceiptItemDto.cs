namespace Services.DTOs
{
    public class ReceiptItemDto
    {
        public Guid? Id { get; set; }
        public Guid? ReceiptId { get; set; } // не используется
        public ProductDto? ProductDto { get; set; }
        public int? Quantity { get; set; }
    }
}
