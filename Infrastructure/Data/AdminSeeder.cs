using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.service;

namespace Infrastructure.Data
{
    public class AdminSeeder
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public AdminSeeder(
            AppDbContext context,
            IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedAsync(
            CancellationToken ct = default)
        {
            // Проверяем наличие администратора
            bool adminExists = await _context.Employees
                .AnyAsync(e => e.IsAdmin, ct);

            if (adminExists)
                return;

            var admin = new Employee
            {
                Id = Guid.NewGuid(),

                FirstName = "Администратор",
                MiddleName = "Системный",
                LastName = "Главный",

                Login = "admin",
                PasswordHash = _passwordHasher.Hash("admin"),

                IsAdmin = true,
                IsActive = true
            };

            await _context.Employees.AddAsync(admin, ct);
            await _context.SaveChangesAsync(ct);
        }
    }
}
