using Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Runtime.InteropServices;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            bool isDesignTime = AppDomain.CurrentDomain.FriendlyName.Contains("ef", StringComparison.OrdinalIgnoreCase) ||
                                AppDomain.CurrentDomain.FriendlyName.Contains("dotnet", StringComparison.OrdinalIgnoreCase);

            if (isDesignTime)
                return;

            var connection = (SqliteConnection)this.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            var extensionFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "unicode-win.dll"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? "unicode-mac.dylib"
                        : null;

            if (extensionFile is null) return;

            var extensionPath = Path.Combine(AppContext.BaseDirectory, "ext", extensionFile);

            // Добавим проверку на существование файла (чтобы приложение не падало)
            if (File.Exists(extensionPath))
            {
                connection.LoadExtension(extensionPath, "sqlite3_unicode_init");
            }
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<ProductCatalog> Catalogs { get; set; }
        public DbSet<InventoryItem> Items { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<ReceiptItem> ReceiptItems{ get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<WorkShift> WorkShifts => Set<WorkShift>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.Property(f => f.Name).IsRequired().HasMaxLength(255);
                entity.HasIndex(f => f.Barcode).IsUnique();

                entity.HasOne(f => f.Catalog)
                      .WithMany(s => s.Products)
                      .HasForeignKey(f => f.CatalogId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasMany(e => e.Items)
                      .WithOne(i => i.Inventory)
                      .HasForeignKey(i => i.InventoryId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Ignore(e => e.TotalAmount);
            });

            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId);

                entity.HasIndex(e => new { e.InventoryId, e.ProductId }).IsUnique();
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.MiddleName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.IsActive).IsRequired();
            });

            modelBuilder.Entity<WorkShift>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OpenedAt)
                      .IsRequired();

                entity.Property(e => e.ClosedAt)
                      .IsRequired(false);

                entity.Ignore(e => e.IsOpen);

                entity.HasOne(e => e.Employee)
                      .WithMany(e => e.WorkShifts)
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.EmployeeId);
                entity.HasIndex(e => e.OpenedAt);
            });

            modelBuilder.Entity<Receipt>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.WorkShift)
                      .WithMany(s => s.Receipts)
                      .HasForeignKey(e => e.WorkShiftId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.WorkShiftId);

                entity.HasMany(e => e.Items)
                      .WithOne(i => i.Receipt)
                      .HasForeignKey(i => i.ReceiptId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReceiptItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId);

                entity.HasIndex(e => new { e.ReceiptId, e.ProductId }).IsUnique();
            });

            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.WorkShift)
                      .WithMany(s => s.Sales)
                      .HasForeignKey(e => e.WorkShiftId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.WorkShiftId);

                entity.Ignore(e => e.TotalPrice);

                entity.HasMany(e => e.Items)
                      .WithOne(i => i.Sale)
                      .HasForeignKey(i => i.SaleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SaleItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId);

                entity.HasIndex(e => new { e.SaleId, e.ProductId }).IsUnique();
            });
        }
    }
}
