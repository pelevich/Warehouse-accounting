using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace App;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        var window = new Window(new ContentPage
        {
            Content = new ActivityIndicator
            {
                IsRunning = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        });

        _ = InitializeDatabaseAsync(window);

        return window;
    }

    private async Task InitializeDatabaseAsync(Window window)
    {
        try
        {
            using var scope = _services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            // Применяем миграции
            await db.Database.MigrateAsync();

            // Создаём первоначального администратора
            var seeder = scope.ServiceProvider
                .GetRequiredService<AdminSeeder>();

            await seeder.SeedAsync();

            // Открываем приложение после инициализации
            //window.Page = _services
            //    .GetRequiredService<AppShell>();
            using var shellScope = _services.CreateScope();
            window.Page = shellScope.ServiceProvider
                .GetRequiredService<AppShell>();
        }
        catch (Exception ex)
        {
            window.Page = new ContentPage
            {
                Content = new Label
                {
                    Text = $"Ошибка запуска: {ex.Message}",
                    TextColor = Colors.Red,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
        }
    }
}