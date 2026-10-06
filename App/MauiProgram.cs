using App.ViewModels;
using App.ViewModels.Popups;
using App.ViewModels.Tabs;
using App.Views;
using App.Views.Popups;
using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.Messaging;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.Interfaces.repository;
using Services.Interfaces.service;
using Services.service;
using System.Diagnostics;

namespace App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Page
            builder.Services.AddTransient<WarehousePage>();
            builder.Services.AddSingleton<MainPage>();

            //ViewModel
            builder.Services.AddTransient<WarehouseViewModel>();
            builder.Services.AddTransient<CatalogViewModel>();
            builder.Services.AddSingleton<StockViewModel>();
            builder.Services.AddSingleton<ReceiptViewModel>();
            builder.Services.AddTransient<AddProductViewModel>();
            builder.Services.AddTransient<AddReceiptItemViewModel>();
            builder.Services.AddTransient<EditProductViewModel>();

            // Views
            builder.Services.AddTransient<CatalogView>();
            builder.Services.AddTransient<StockView>();
            builder.Services.AddTransient<ReceiptView>();
            builder.Services.AddTransient<AddProductPopup>();
            builder.Services.AddTransient<AddReceiptItemPopup>();
            builder.Services.AddTransient<EditProductPopup>();

            //Transient
            builder.Services.AddTransient<IBarcodeService, BarcodeService>();
            builder.Services.AddTransient<IProductService, ProductService>();
            builder.Services.AddTransient<IReceiptService, ReceiptService>();
            builder.Services.AddTransient<IReceiptItemService, ReceiptItemService>();
            builder.Services.AddTransient<IInventoryItemService, InventoryItemService>();
            builder.Services.AddTransient<IInventoryService, InventoryService>();
            builder.Services.AddTransient<IProductCatalogService, ProductCatalogService>();

            //Scoped
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

            //БД (пока sqlite потом может перейдем на postgress)
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "pos.db");
            Debug.WriteLine(dbPath);
            Debug.WriteLine("------------------");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Filename={dbPath}")
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine, LogLevel.Information));

            //Startup task
            builder.Services.AddSingleton<IStartupTask, DatabaseStartupTask>();
            builder.Services.AddSingleton<IMauiInitializeService, StartupService>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
