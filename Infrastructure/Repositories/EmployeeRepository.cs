using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.repository;

namespace Infrastructure.Repositories;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;
    public EmployeeRepository(AppDbContext context) => _context = context;

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Set<Employee>()
            .SingleOrDefaultAsync(e => e.Id == id, ct);

    public Task<Employee?> GetByLoginAsync(string login, CancellationToken ct = default) =>
        _context.Employees.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Login == login, ct);

    public async Task<IReadOnlyList<Employee>> GetAllAsync(
        CancellationToken ct = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Employee>> GetActiveAsync(CancellationToken ct = default) =>
        await _context.Set<Employee>().AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync(ct);

    public async Task AddAsync(Employee employee, CancellationToken ct = default) =>
        await _context.Set<Employee>().AddAsync(employee, ct);

    public Task UpdateAsync(
        Employee employee,
        CancellationToken ct = default)
    {
        // Для сущности, полученной через GetByIdAsync,
        // EF Core уже отслеживает изменения.
        // Не вызываем Update(), чтобы не помечать
        // все свойства как изменённые.
        if (_context.Entry(employee).State ==
            EntityState.Detached)
        {
            throw new InvalidOperationException(
                "Сотрудник не отслеживается текущим DbContext.");
        }

        return Task.CompletedTask;
    }
}
