using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Data;

/// <summary>
/// Database context utama aplikasi menggunakan Entity Framework Core.
/// Kelas ini menjadi jembatan antara C# (model/entity) dan tabel PostgreSQL.
/// Di-inject ke controller via Dependency Injection (DI) dengan lifetime Scoped
/// (satu instance per HTTP request).
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Constructor menerima options (connection string, provider, dll.) dari DI container
    /// yang dikonfigurasi di Program.cs menggunakan AddDbContext().
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Representasi tabel "Products" di database.
    /// Digunakan untuk operasi CRUD: _db.Products.ToListAsync(), _db.Products.Add(), dll.
    /// </summary>
    public DbSet<Product> Products { get; set; }

    /// <summary>
    /// Representasi tabel "Users" di database.
    /// Digunakan untuk cari user saat login dan daftarkan user baru saat register.
    /// </summary>
    public DbSet<UserEntity> Users { get; set; }

    /// <summary>
    /// Override method ini untuk konfigurasi tambahan model dan seed data awal.
    /// Dipanggil otomatis oleh EF Core saat membuild model (saat startup / migrasi).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Seed Data: Admin Default ─────────────────────────────────────────
        // HasData() menyisipkan data awal ke tabel saat migrasi pertama dijalankan.
        // Id harus tetap (fixed) agar EF Core bisa track data antar migrasi.
        // Password "admin123" di-hash dengan BCrypt sebelum disimpan.
        modelBuilder.Entity<UserEntity>().HasData(new UserEntity
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // hash statis saat migration
            Role = "Admin"
        });

        // ── Seed Data: Produk Awal ───────────────────────────────────────────
        // Tiga produk contoh yang langsung tersedia begitu database dibuat.
        // CreatedAt di-hardcode agar nilai seed data konsisten di semua environment.
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Laptop Pro 15",
                Description = "Laptop high-performance untuk profesional",
                Price = 15000000,
                Stock = 10,
                Category = "Electronics",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 2,
                Name = "Mechanical Keyboard",
                Description = "Keyboard gaming dengan switch Cherry MX",
                Price = 1200000,
                Stock = 25,
                Category = "Accessories",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 3,
                Name = "Wireless Mouse",
                Description = "Mouse wireless ergonomis dengan baterai tahan lama",
                Price = 450000,
                Stock = 50,
                Category = "Accessories",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
