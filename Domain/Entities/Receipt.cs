namespace Domain.Entities
{
    public class Receipt
    {
        public Guid Id { get; set; }
        public List<ReceiptItem>? Items { get; set; } = new ();
    }
}
