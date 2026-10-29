namespace Domain.Entities
{
    public class WorkShift
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public bool IsOpen => ClosedAt == null;

        public ICollection<Sale> Sales { get; set; }
        = new List<Sale>();

        public ICollection<Receipt> Receipts { get; set; }
            = new List<Receipt>();
    }
}
