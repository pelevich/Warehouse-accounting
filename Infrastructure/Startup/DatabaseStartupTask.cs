using Domain.Entities;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Startup
{
    public class DatabaseStartupTask : IStartupTask
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseStartupTask> _logger;
        private const string DefaultCatalogName = "Main catalog";
            private const string DefaultInventoryName = "Main inventory";

        public DatabaseStartupTask(
            AppDbContext context,
            ILogger<DatabaseStartupTask> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Инициализация БД...");

                // 1. Миграции
                await _context.Database.MigrateAsync(ct);
                _logger.LogInformation("Миграции примене" +
                    "ны");

                // 2. Каталог
                await SeedCatalogAsync(ct);

                // 3. Склад
                await SeedInventoryAsync(ct);

                _logger.LogInformation("БД инициализирована");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка инициализации БД");
                throw;
            }
        }

        private async Task SeedCatalogAsync(CancellationToken ct)
        {
            var exists = await _context.Catalogs
                .AnyAsync(c => c.Name == DefaultCatalogName, ct);   

            if (exists) return;

            _logger.LogInformation("Создаём основной каталог...");

            await _context.Catalogs.AddAsync(new ProductCatalog
            {
                Id = Guid.NewGuid(),
                Name = "Main catalog"
            }, ct);

            await _context.SaveChangesAsync(ct);
        }

        private async Task SeedInventoryAsync(CancellationToken ct)
        {
            var exists = await _context.Inventories.AnyAsync(c => c.Name == DefaultInventoryName, ct);
            if (exists) return;

            _logger.LogInformation("Создаём основной склад...");

            await _context.Inventories.AddAsync(new Inventory
            {
                Id = Guid.NewGuid(),
                Name = "Main inventory",
            }, ct);

            await _context.SaveChangesAsync(ct);
        }
    }
}
