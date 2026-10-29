namespace Domain.Entities
{
    public class Employee
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }

        public string? Login { get; set; }
        public string? PasswordHash { get; set; }

        public ICollection<WorkShift> WorkShifts { get; set; }
            = new List<WorkShift>();
    }
}
