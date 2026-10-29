using Domain.Entities;
using Services.Interfaces.repository;
using Services.Interfaces.service;

namespace Services.service;

public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ISessionService _session;

    public AuthService(IUnitOfWork uow, IPasswordHasher hasher, ISessionService session)
    {
        _uow = uow;
        _hasher = hasher;
        _session = session;
    }

    public async Task<Employee> LoginAdminAsync(
        string login, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
            throw new UnauthorizedAccessException("Введите логин и пароль.");

        var employee = await _uow.EmployeeRepository.GetByLoginAsync(
            login.Trim().ToLowerInvariant(), ct);
        if (employee is null || !employee.IsActive || !employee.IsAdmin ||
            string.IsNullOrWhiteSpace(employee.PasswordHash) ||
            !_hasher.Verify(password, employee.PasswordHash))
            throw new UnauthorizedAccessException("Неверный логин или пароль.");

        _session.StartAdmin(employee.Id);

        return employee;
    }
}
