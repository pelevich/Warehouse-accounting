using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Startup
{
    public class StartupService : IMauiInitializeService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StartupService> _logger;

        public StartupService(
            IServiceProvider serviceProvider,
            ILogger<StartupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void Initialize(IServiceProvider services)
        {
            _logger.LogInformation("Запуск инициализации...");

            // ✅ НЕ блокирует UI — запускает в фоне
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = services.CreateScope();

                    var tasks = scope.ServiceProvider
                        .GetServices<IStartupTask>()
                        .ToList();

                    if (tasks.Count == 0)
                    {
                        _logger.LogWarning("Нет задач инициализации");
                        return;
                    }

                    _logger.LogInformation("Задач инициализации: {Count}", tasks.Count);

                    foreach (var task in tasks)
                    {
                        try
                        {
                            // ✅ await, а не GetResult()
                            await task.ExecuteAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Ошибка в задаче {Task}",
                                task.GetType().Name);
                            throw;
                        }
                    }

                    _logger.LogInformation("Инициализация завершена");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Критическая ошибка инициализации");
                }
            });
        }
    }
}
