namespace Domain.Entities
{
    public class ReceiptItem
    {
        public Guid Id { get; set; }
        public Guid ReceiptId { get; set; }
        public Receipt Receipt { get; set; }

        public Guid ProductId { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }
    }
}
