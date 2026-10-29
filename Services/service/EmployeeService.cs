
using Domain.Entities;
using Services.Interfaces.repository;
using Services.Interfaces.service;

namespace Services.service;

public sealed class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _uow;
    private readonly ISessionService _session;
    private readonly IPasswordHasher _hasher;

    public EmployeeService(
        IUnitOfWork uow,
        ISessionService session,
        IPasswordHasher hasher)
    {
        _uow = uow;
        _session = session;
        _hasher = hasher;
    }

    public Task<IReadOnlyList<Employee>> GetAllAsync(
        CancellationToken ct = default)
    {
        RequireAdmin();
        return _uow.EmployeeRepository.GetAllAsync(ct);
    }

    // Для страницы открытия смены разрешён список
    // активных сотрудников без входа.
    public async Task<IReadOnlyList<Employee>> GetActiveAsync(
        CancellationToken ct = default)
    {
        var active = await _uow.EmployeeRepository.GetActiveAsync(ct);

        // Экран выбора кассира не должен получать
        // даже хеши администраторов.
        return active.Select(e => new Employee
        {
            Id = e.Id,
            FirstName = e.FirstName,
            MiddleName = e.MiddleName,
            LastName = e.LastName,
            IsActive = e.IsActive
        }).ToArray();
    }

    public Task<Employee?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        RequireAdmin();
        return _uow.EmployeeRepository.GetByIdAsync(id, ct);
    }

    public async Task<Employee> SaveAsync(
        Guid? id,
        string firstName,
        string middleName,
        string lastName,
        bool isActive,
        bool isAdmin,
        string? login,
        string? newPassword,
        CancellationToken ct = default)
    {
        RequireAdmin();

        var first = ValidateName(firstName, "Имя");
        var middle = ValidateName(middleName, "Отчество");
        var last = ValidateName(lastName, "Фамилия");

        var normalizedLogin = string.IsNullOrWhiteSpace(login)
            ? null
            : login.Trim().ToLowerInvariant();

        if (isAdmin && string.IsNullOrWhiteSpace(normalizedLogin))
            throw new ArgumentException(
                "Для администратора нужен логин.");

        if (normalizedLogin is { Length: > 80 })
            throw new ArgumentException(
                "Логин слишком длинный.");

        if (!string.IsNullOrEmpty(newPassword) &&
            newPassword.Length < 8)
            throw new ArgumentException(
                "Новый пароль должен содержать минимум 8 символов.");

        Employee employee;

        if (id is Guid employeeId)
        {
            employee = await _uow.EmployeeRepository
                .GetByIdAsync(employeeId, ct)
                ?? throw new InvalidOperationException(
                    "Сотрудник не найден.");

            if (employee.Id == _session.CurrentEmployeeId &&
                (!isAdmin || !isActive))
                throw new InvalidOperationException(
                    "Нельзя снять права или отключить свою учётную запись.");

            if (employee.IsActive && !isActive)
            {
                var open = await _uow.WorkShiftRepository
                    .GetOpenAsync(ct);

                if (open?.EmployeeId == employee.Id)
                    throw new InvalidOperationException(
                        "Сначала закройте смену сотрудника.");
            }
        }
        else
        {
            employee = new Employee
            {
                Id = Guid.NewGuid()
            };
        }

        if (isAdmin)
        {
            var other = await _uow.EmployeeRepository
                .GetByLoginAsync(normalizedLogin!, ct);

            if (other is not null && other.Id != employee.Id)
                throw new InvalidOperationException(
                    "Этот логин уже используется.");

            if (string.IsNullOrWhiteSpace(employee.PasswordHash) &&
                string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException(
                    "Для нового администратора укажите пароль.");
        }

        // Нельзя случайно удалить последнего
        // активного администратора.
        if (employee.IsAdmin &&
            employee.IsActive &&
            (!isAdmin || !isActive))
        {
            var all = await _uow.EmployeeRepository
                .GetAllAsync(ct);

            if (!all.Any(e =>
                e.Id != employee.Id &&
                e.IsAdmin &&
                e.IsActive))
            {
                throw new InvalidOperationException(
                    "Нельзя отключить последнего администратора.");
            }
        }

        employee.FirstName = first;
        employee.MiddleName = middle;
        employee.LastName = last;
        employee.IsActive = isActive;
        employee.IsAdmin = isAdmin;

        employee.Login = isAdmin
            ? normalizedLogin
            : null;

        employee.PasswordHash = isAdmin
            ? (string.IsNullOrWhiteSpace(newPassword)
                ? employee.PasswordHash
                : _hasher.Hash(newPassword))
            : null;

        if (id is null)
            await _uow.EmployeeRepository.AddAsync(employee, ct);

        await _uow.CommitAsync(ct);

        return employee;
    }

    private void RequireAdmin()
    {
        if (!_session.IsAdmin)
            throw new UnauthorizedAccessException(
                "Требуются права администратора.");
    }

    private static string ValidateName(
        string? value,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                $"Заполните поле «{field}».");

        var result = value.Trim();

        if (result.Length > 200)
            throw new ArgumentException(
                $"Поле «{field}» слишком длинное.");

        return result;
    }
}
