using Domain.Entities;

namespace Services.Interfaces.service
{
    public interface IAuthService
    {
        Task<Employee> LoginAdminAsync(string login, string password, CancellationToken ct = default);
    }
}
