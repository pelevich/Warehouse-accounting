namespace Domain.Entities
{
    public class Receipt
    {
        public Guid Id { get; set; }
        public List<ReceiptItem>? Items { get; set; } = new ();
        public DateTime SoldAt { get; set; }
        public Guid? WorkShiftId { get; set; }
        public WorkShift? WorkShift { get; set; }
    }
}
