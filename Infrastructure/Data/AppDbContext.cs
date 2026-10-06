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
            connection.LoadExtension(extensionPath, "sqlite3_unicode_init");
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<ProductCatalog> Catalogs { get; set; }
        public DbSet<InventoryItem> Items { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<ReceiptItem> ReceiptItems{ get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }

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

            modelBuilder.Entity<Receipt>(entity =>
            {
                entity.HasKey(e => e.Id);

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
