namespace Domain.Entities
{
    public class Sale
    {
        public Guid Id { get; set; }
        public List<SaleItem>? Items { get; set; } = new ();
        public DateTime SoldAt { get; set; } = DateTime.UtcNow;
        public string PaymentType { get; set; }
        public double TotalPrice => Items?.Sum(x => x.FullPrice) ?? 0;
    }
}
