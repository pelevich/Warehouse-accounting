namespace Domain.Entities
{
    public class Sale
    {
        public Guid Id { get; set; }
        public List<SaleItem>? Items { get; set; } = new ();
        public DateTime SoldAt { get; set; } = DateTime.UtcNow;
        public string PaymentType { get; set; }
        public decimal TotalPrice => Items?.Sum(x => x.FullPrice) ?? 0;

        public Guid? WorkShiftId { get; set; }
        public WorkShift? WorkShift { get; set; }
    }
}
