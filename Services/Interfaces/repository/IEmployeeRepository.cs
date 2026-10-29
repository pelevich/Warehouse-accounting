using Domain.Entities;

namespace Services.Interfaces.repository;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Employee?> GetByLoginAsync(string login, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(Employee employee, CancellationToken ct = default);
    Task UpdateAsync(Employee employee, CancellationToken ct = default);
}
