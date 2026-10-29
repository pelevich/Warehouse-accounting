using App.ViewModels;
using App.ViewModels.Popups;
using App.ViewModels.Tabs;
using App.ViewModels.Trade.Tabs;
using App.ViewModels.Warehouse.Popups;
using App.Views;
using App.Views.Popups;
using App.Views.Trade;
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
            builder.Services.AddTransient<SalePage>();
            builder.Services.AddSingleton<WelcomePage>();
            builder.Services.AddSingleton<HistoryPage>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<AppShell>();

            //ViewModel
            builder.Services.AddTransient<WarehouseViewModel>();  // Склад
            builder.Services.AddTransient<TradeViewModel>();      // Продажа
            builder.Services.AddTransient<WelcomeViewModel>();
            builder.Services.AddTransient<EmployeesViewModel>();
            builder.Services.AddTransient<CatalogViewModel>();      //Tab склада
            builder.Services.AddTransient<StockViewModel>();        //Tab склада
            builder.Services.AddTransient<ReceiptViewModel>();      //Tab склада
            builder.Services.AddTransient<SaleViewModel>();         //Tab торговли
            builder.Services.AddTransient<HistoryViewModel>();                 //Tab истории
            builder.Services.AddTransient<ReceiptsHistoryViewModel>();         //Tab истории
            builder.Services.AddTransient<ShiftHistoryViewModel>();            //Tab истории
            builder.Services.AddTransient<SalesHistoryViewModel>();            //Tab истории
            builder.Services.AddTransient<AddProductViewModel>();       //popup склада
            builder.Services.AddTransient<AddReceiptItemViewModel>();   //popup склада
            builder.Services.AddTransient<EditProductViewModel>();      //popup склада
            builder.Services.AddTransient<ShowBarcodeImgViewModel>();   //popup склада
            builder.Services.AddTransient<ManageShiftViewModel>();

            // Views
            builder.Services.AddTransient<CatalogView>();
            builder.Services.AddTransient<StockView>();
            builder.Services.AddTransient<ReceiptView>();
            builder.Services.AddTransient<AddProductPopup>();
            builder.Services.AddTransient<AddReceiptItemPopup>();
            builder.Services.AddTransient<EditProductPopup>();
            builder.Services.AddTransient<ShowBarcodeImgPopup>();
            builder.Services.AddTransient<ManageShiftPopup>();
            builder.Services.AddTransient<SaleView>();
            builder.Services.AddTransient<EmployeesView>();
            builder.Services.AddTransient<ReceiptsHistoryView>();
            builder.Services.AddTransient<SalesHistoryView>();
            builder.Services.AddTransient<ShiftHistoryView>();

            //Singleton
            builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
            builder.Services.AddSingleton<ISessionService, SessionService>();

            //Transient
            builder.Services.AddTransient<IBarcodeService, BarcodeService>();
            builder.Services.AddTransient<IProductService, ProductService>();
            builder.Services.AddTransient<IReceiptService, ReceiptService>();
            builder.Services.AddTransient<IAuthService, AuthService>();
            builder.Services.AddTransient<ISaleService, SaleService>();
            builder.Services.AddTransient<IReceiptItemService, ReceiptItemService>();
            builder.Services.AddTransient<IInventoryItemService, InventoryItemService>();
            builder.Services.AddTransient<IInventoryService, InventoryService>();
            builder.Services.AddTransient<IProductCatalogService, ProductCatalogService>();
            builder.Services.AddTransient<IWorkShiftService, WorkShiftService>();
            builder.Services.AddTransient<IHistoryService, HistoryService>();

            //Scoped
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<AdminSeeder>();
            builder.Services.AddScoped<IEmployeeService, EmployeeService>();

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
