using Domain.Entities;

namespace Services.Interfaces.service
{
    public interface IEmployeeService
    {
        Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken ct = default);

        Task<IReadOnlyList<Employee>> GetActiveAsync(CancellationToken ct = default);

        Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Employee> SaveAsync(
            Guid? id,
            string firstName,
            string middleName,
            string lastName,
            bool isActive,
            bool isAdmin,
            string? login,
            string? newPassword,
            CancellationToken ct = default);
    }
}
