# 🚀 ASP.NET Core Web API — Full Backend Cheat Sheet

> Panduan lengkap **seluruh komponen** backend app dari nol sampai production-ready.
> Berdasarkan project nyata: **ProductCatalogAPI** (.NET 8 + PostgreSQL + JWT + EF Core).

---

## 📂 Struktur Project

```
ProductCatalogAPI/
├── Program.cs                  ← Entry point + konfigurasi semua service & middleware
├── appsettings.json            ← Konfigurasi: DB connection, JWT secret, logging
├── appsettings.Development.json ← Override config untuk development
├── ProductCatalogAPI.csproj    ← NuGet packages & target framework
├── Controllers/
│   ├── ProductsController.cs   ← CRUD endpoints produk
│   └── AuthController.cs       ← Register & Login endpoints
├── Models/
│   ├── Product.cs              ← Entity: tabel Products
│   └── User.cs                 ← Entity: tabel Users + DTO login
├── Data/
│   └── AppDbContext.cs         ← EF Core DbContext + seed data
├── Migrations/                 ← Auto-generated migration files
└── Properties/
    └── launchSettings.json     ← URL & port saat development
```

### Kenapa Struktur Ini?

| Folder | Tanggung Jawab | Prinsip |
|--------|---------------|---------|
| `Controllers/` | Handle HTTP request & response | **Thin controller** — hanya routing & validasi |
| `Models/` | Definisi struktur data | **Single Responsibility** — 1 class = 1 tabel |
| `Data/` | Akses database | **Repository pattern** sederhana via DbContext |
| `Program.cs` | Wiring semua komponen | **Composition Root** — semua DI daftar di sini |

---

## 📦 1. Project File (`.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- ORM untuk akses database -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />

    <!-- Provider PostgreSQL untuk EF Core -->
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />

    <!-- Tool untuk generate migration (dotnet ef migrations add) -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />

    <!-- JWT authentication middleware -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />

    <!-- Password hashing -->
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />

    <!-- Swagger/OpenAPI documentation -->
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>

</Project>
```

### Properti Penting

| Properti | Nilai | Arti |
|----------|-------|------|
| `TargetFramework` | `net8.0` | Pakai .NET 8 (LTS, support sampai 2026) |
| `Nullable` | `enable` | Peringatan compiler jika nullable reference tidak di-handle |
| `ImplicitUsings` | `enable` | Auto-import namespace umum (System, System.Linq, dll.) |

### NuGet Package Cheat Sheet

| Package | Fungsi | Kapan Pakai |
|---------|--------|-------------|
| `Microsoft.EntityFrameworkCore` | ORM — mapping C# class → tabel DB | **Selalu** untuk akses DB |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Provider PostgreSQL | Jika pakai PostgreSQL |
| `Microsoft.EntityFrameworkCore.SqlServer` | Provider SQL Server | Jika pakai SQL Server |
| `Microsoft.EntityFrameworkCore.Design` | CLI tools (`dotnet ef`) | **Selalu** untuk migration |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT middleware | Jika pakai token auth |
| `BCrypt.Net-Next` | Hash password | Jika simpan password user |
| `Swashbuckle.AspNetCore` | Swagger UI | **Selalu** untuk dokumentasi API |
| `Serilog.AspNetCore` | Structured logging | Opsional — logging lanjut |
| `AutoMapper` | Mapping entity ↔ DTO otomatis | Opsional — banyak DTO |

### Command NuGet

```bash
# Install package
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0

# Lihat semua package yang terinstall
dotnet list package

# Hapus package
dotnet remove package NamaPackage

# Restore semua package (setelah clone repo)
dotnet restore
```

---

## ⚙️ 2. Konfigurasi (`appsettings.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Key": "ProductCatalogSuperSecretKey2024_NQA_DotNet_Training!",
    "Issuer": "ProductCatalogAPI"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ProductCatalogDB;Username=sulaimansaleh"
  }
}
```

### Penjelasan Setiap Section

| Section | Key | Fungsi |
|---------|-----|--------|
| `Logging.LogLevel.Default` | `"Information"` | Level minimum log yang ditampilkan |
| `Logging.LogLevel.Microsoft.AspNetCore` | `"Warning"` | Kurangi noise log dari framework |
| `AllowedHosts` | `"*"` | Host mana yang boleh akses (wildcard = semua) |
| `Jwt.Key` | String panjang | Secret key untuk sign/verify JWT — **JANGAN commit ke git** |
| `Jwt.Issuer` | `"ProductCatalogAPI"` | Identitas penerbit token |
| `ConnectionStrings.DefaultConnection` | Connection string | Alamat + kredensial database |

### Cara Baca Config di Code

```csharp
// Di Program.cs
var jwtKey = builder.Configuration["Jwt:Key"];
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

// Di Controller (via DI)
public class MyController : ControllerBase
{
    private readonly IConfiguration _config;
    public MyController(IConfiguration config) { _config = config; }

    public IActionResult Example()
    {
        var issuer = _config["Jwt:Issuer"]; // "ProductCatalogAPI"
    }
}
```

### Environment-Specific Config

```
appsettings.json                  ← Base (selalu di-load)
appsettings.Development.json      ← Override saat ASPNETCORE_ENVIRONMENT=Development
appsettings.Production.json       ← Override saat ASPNETCORE_ENVIRONMENT=Production
```

**Prioritas:** Environment-specific > Base > Defaults

### ⚠️ Security: Jangan Hardcode Secrets!

```bash
# Gunakan User Secrets untuk development
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "SuperSecretKeyHere"

# Atau environment variable untuk production
export Jwt__Key="SuperSecretKeyHere"    # __ = : di env var
```

---

## 🗄️ 3. Models (Entity Classes)

### Apa itu Model/Entity?

Model = class C# yang merepresentasikan **1 tabel** di database.  
Setiap property = 1 kolom. EF Core otomatis mapping berdasarkan konvensi.

### Product Entity

```csharp
namespace ProductCatalogAPI.Models;

public class Product
{
    // PK — nama "Id" atau "<ClassName>Id" otomatis jadi primary key
    public int Id { get; set; }

    // String default = nvarchar(max) di SQL Server, text di PostgreSQL
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // decimal (bukan float/double) untuk uang — hindari floating point error
    public decimal Price { get; set; }

    public int Stock { get; set; }

    public string Category { get; set; } = string.Empty;

    // DateTime — di-set server-side, bukan dari client
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### UserEntity + User DTO

```csharp
// DTO untuk Login request — TIDAK masuk database
public class User
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;  // plaintext, sementara
}

// Entity untuk tabel Users — yang disimpan di DB
public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;  // BCrypt hash
    public string Role { get; set; } = "User";               // "Admin" atau "User"
}
```

### Entity vs DTO — Kapan Pakai Apa?

| | Entity | DTO (Data Transfer Object) |
|--|--------|---------------------------|
| **Tujuan** | Mapping ke tabel database | Carrier data request/response |
| **Punya Id?** | Ya | Tidak selalu |
| **Di-track EF Core?** | Ya | Tidak |
| **Contoh** | `UserEntity`, `Product` | `User` (login), `RegisterRequest` |
| **Simpan ke DB?** | Ya | Tidak pernah |

### Tipe Data C# → Tipe Kolom Database

| C# Type | PostgreSQL | SQL Server | Kapan Pakai |
|---------|------------|------------|-------------|
| `int` | `integer` | `int` | ID, jumlah, counter |
| `long` | `bigint` | `bigint` | ID besar, timestamp unix |
| `string` | `text` | `nvarchar(max)` | Teks bebas |
| `decimal` | `numeric` | `decimal(18,2)` | **Uang / harga** — wajib ini! |
| `float` / `double` | `double precision` | `float` | Koordinat, scientific — **bukan uang!** |
| `bool` | `boolean` | `bit` | Flag on/off |
| `DateTime` | `timestamp` | `datetime2` | Tanggal & waktu |
| `Guid` | `uuid` | `uniqueidentifier` | ID unik global |

### Data Annotations untuk Model

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Product
{
    [Key]                                    // Eksplisit primary key (opsional jika nama = "Id")
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-increment
    public int Id { get; set; }

    [Required(ErrorMessage = "Nama wajib diisi")]
    [MaxLength(200)]
    [Column("product_name")]                 // Override nama kolom di DB
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 99999999.99)]
    [Column(TypeName = "decimal(18,2)")]     // Presisi untuk uang
    public decimal Price { get; set; }

    [NotMapped]                              // TIDAK dipetakan ke kolom database
    public string DisplayName => $"{Name} - Rp{Price:N0}";
}
```

| Annotation | Fungsi |
|-----------|--------|
| `[Key]` | Tandai sebagai primary key |
| `[Required]` | NOT NULL di database + validasi input |
| `[MaxLength(n)]` | VARCHAR(n) di database + validasi |
| `[Column("name")]` | Override nama kolom |
| `[Column(TypeName = "...")]` | Override tipe kolom |
| `[Table("TableName")]` | Override nama tabel (taruh di class) |
| `[NotMapped]` | Skip — tidak jadi kolom di DB |
| `[DatabaseGenerated(...)]` | Auto-increment, computed, dll. |
| `[ForeignKey("NavProp")]` | Definisikan foreign key |
| `[Index]` | Buat index di kolom ini |

---

## 🔌 4. DbContext — Jembatan ke Database

### Apa itu DbContext?

`DbContext` = representasi session ke database. Semua interaksi DB lewat sini.

```csharp
using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Data;

public class AppDbContext : DbContext
{
    // Constructor — menerima options (connection string, provider) dari DI
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSet<T> = representasi tabel di database
    // Nama property = nama tabel (bisa di-override)
    public DbSet<Product> Products { get; set; }       // → tabel "Products"
    public DbSet<UserEntity> Users { get; set; }       // → tabel "Users"

    // Konfigurasi model & seed data
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed data — data awal saat migration pertama
        modelBuilder.Entity<UserEntity>().HasData(new UserEntity
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "Admin"
        });

        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop Pro 15", Price = 15000000, ... },
            new Product { Id = 2, Name = "Mechanical Keyboard", Price = 1200000, ... }
        );
    }
}
```

### DbSet = Tabel

```csharp
public DbSet<Product> Products { get; set; }
//     ^^^^^^^^^^^^^^  ^^^^^^^^
//     Tipe generic    Nama property = nama tabel di DB
```

Ini memberikan akses ke semua operasi CRUD:
```csharp
_db.Products.ToListAsync();      // SELECT * FROM Products
_db.Products.FindAsync(id);      // SELECT * WHERE Id = ?
_db.Products.Add(entity);        // INSERT INTO Products
_db.Products.Remove(entity);     // DELETE FROM Products
_db.SaveChangesAsync();           // COMMIT semua perubahan
```

### OnModelCreating — Kapan Dipakai?

| Kasus | Contoh |
|-------|--------|
| **Seed data** | Insert data default (admin, sample products) |
| **Konfigurasi relasi** | One-to-many, many-to-many |
| **Override konvensi** | Nama tabel, tipe kolom, index |
| **Unique constraint** | Username harus unik |

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Unique constraint
    modelBuilder.Entity<UserEntity>()
        .HasIndex(u => u.Username)
        .IsUnique();

    // Relasi one-to-many
    modelBuilder.Entity<Product>()
        .HasOne(p => p.Category)
        .WithMany(c => c.Products)
        .HasForeignKey(p => p.CategoryId);

    // Override nama tabel
    modelBuilder.Entity<Product>().ToTable("product_catalog");

    // Override tipe kolom
    modelBuilder.Entity<Product>()
        .Property(p => p.Price)
        .HasColumnType("decimal(18,2)");
}
```

### Registrasi DbContext di Program.cs

```csharp
// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// SQLite (untuk development/testing cepat)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

// In-Memory (untuk unit testing)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));
```

---

## 🔄 5. EF Core Migrations

### Apa itu Migration?

Migration = version control untuk skema database. Setiap perubahan model → buat migration → apply ke DB.

### Command Penting

```bash
# Buat migration baru (setelah ubah model/entity)
dotnet ef migrations add NamaMigration

# Apply semua migration ke database
dotnet ef database update

# Rollback ke migration tertentu
dotnet ef database update NamaMigrationSebelumnya

# Hapus migration terakhir (jika belum di-apply)
dotnet ef migrations remove

# Lihat semua migration dan statusnya
dotnet ef migrations list

# Generate SQL script (untuk production deployment)
dotnet ef migrations script

# Reset database (hapus & buat ulang)
dotnet ef database drop --force
dotnet ef database update
```

### Alur Kerja Migration

```
1. Ubah Model (Product.cs)
   ↓  tambah property: public string Slug { get; set; }
2. Buat migration
   ↓  dotnet ef migrations add AddSlugToProduct
3. Review file migration yang ter-generate
   ↓  Migrations/20240410_AddSlugToProduct.cs
4. Apply ke database
   ↓  dotnet ef database update
5. Kolom baru "Slug" sudah ada di tabel Products ✅
```

### Auto-Migration di Program.cs

```csharp
// Jalankan migration otomatis saat startup (cocok untuk development)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();  // Apply semua pending migrations
}
```

> ⚠️ **Production:** Jangan pakai auto-migrate! Gunakan `dotnet ef migrations script` → review → jalankan manual.

---

## 🏗️ 6. Program.cs — Pusat Konfigurasi

### Struktur Program.cs (Minimal Hosting Model)

```csharp
// ════════════════════════════════════════════
// FASE 1: REGISTRASI SERVICES (DI Container)
// ════════════════════════════════════════════
var builder = WebApplication.CreateBuilder(args);

// 1. Database
builder.Services.AddDbContext<AppDbContext>(...);

// 2. CORS
builder.Services.AddCors(...);

// 3. Rate Limiting
builder.Services.AddRateLimiter(...);

// 4. Authentication (JWT)
builder.Services.AddAuthentication(...).AddJwtBearer(...);
builder.Services.AddAuthorization();

// 5. Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(...);

// ════════════════════════════════════════════
// FASE 2: BUILD APP
// ════════════════════════════════════════════
var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope()) { ... }

// ════════════════════════════════════════════
// FASE 3: MIDDLEWARE PIPELINE (URUTAN PENTING!)
// ════════════════════════════════════════════
app.UseSwagger();           // Swagger JSON endpoint
app.UseSwaggerUI();         // Swagger HTML UI
app.UseCors("DevPolicy");   // CORS headers
app.UseRateLimiter();       // Rate limit check
app.UseAuthentication();    // Parse JWT token → User.Identity
app.UseAuthorization();     // Check [Authorize] attributes
app.MapControllers();       // Route request → controller action

app.Run();                  // Start listening
```

### Dua Fase Penting

| Fase | Apa yang terjadi | Kapan |
|------|-----------------|-------|
| **builder.Services.Add...** | Daftarkan service ke DI container | Sebelum `builder.Build()` |
| **app.Use...** | Susun middleware pipeline | Setelah `builder.Build()` |

> ⚠️ Setelah `builder.Build()`, tidak bisa `AddService` lagi. Sebelumnya, tidak bisa `Use...` middleware.

---

## 🔐 7. JWT Authentication (Lengkap)

### Alur JWT

```
┌─────────┐    POST /api/auth/login      ┌──────────┐
│  Client  │ ──────────────────────────→  │   Server  │
│          │    { username, password }     │           │
│          │                              │  1. Cari user di DB
│          │                              │  2. Verify password (BCrypt)
│          │    { token: "eyJhb..." }      │  3. Generate JWT token
│          │ ←──────────────────────────  │           │
│          │                              │           │
│          │    GET /api/products          │           │
│          │    Authorization: Bearer ...  │           │
│          │ ──────────────────────────→  │  4. Validate JWT
│          │                              │  5. Extract claims (role, user)
│          │    200 OK + data              │  6. Check [Authorize]
│          │ ←──────────────────────────  │  7. Execute action
└─────────┘                              └──────────┘
```

### Setup di Program.cs

```csharp
// Baca config
var jwtKey    = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;

// Daftarkan authentication service
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,   // Cek penerbit token
            ValidateAudience         = false,  // Skip cek audience
            ValidateLifetime         = true,   // Cek expired
            ValidateIssuerSigningKey = true,   // Verify tanda tangan
            ValidIssuer              = jwtIssuer,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                          Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
```

### Parameter Validasi JWT

| Parameter | Nilai | Arti |
|-----------|-------|------|
| `ValidateIssuer` | `true` | Tolak token dari issuer lain |
| `ValidateAudience` | `false` | Tidak cek siapa penerima token |
| `ValidateLifetime` | `true` | Tolak token yang sudah expired |
| `ValidateIssuerSigningKey` | `true` | Verifikasi signature dengan secret key |
| `ClockSkew` | Default 5 min | Toleransi waktu untuk expired check |

### Generate Token di AuthController

```csharp
private string GenerateToken(UserEntity user)
{
    // Claims = informasi tentang user yang ditanam di token
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role)  // Dibaca oleh [Authorize(Roles)]
    };

    // Buat signing key dari secret
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    // Bangun token
    var token = new JwtSecurityToken(
        issuer: _config["Jwt:Issuer"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),  // Expired 1 jam
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### Baca User Info dari Token di Controller

```csharp
[Authorize]
[HttpGet("me")]
public IActionResult GetProfile()
{
    var userId   = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var username = User.Identity?.Name;
    var role     = User.FindFirst(ClaimTypes.Role)?.Value;
    var isAdmin  = User.IsInRole("Admin");

    return Ok(new { userId, username, role, isAdmin });
}
```

---

## 🔒 8. Password Hashing (BCrypt)

### Kenapa BCrypt?

| | MD5/SHA | BCrypt |
|--|---------|--------|
| Kecepatan | Sangat cepat | Sengaja lambat (anti brute force) |
| Salt | Harus manual | Otomatis built-in |
| Cost factor | Tidak ada | Ada (bisa ditingkatkan) |
| Keamanan | ❌ Mudah di-crack | ✅ Industry standard |

### Cara Pakai

```csharp
// Hash password saat register
var hash = BCrypt.Net.BCrypt.HashPassword("password123");
// → "$2a$11$KjY7.5P3B..."  (setiap kali hash beda karena random salt)

// Verify password saat login
bool valid = BCrypt.Net.BCrypt.Verify("password123", storedHash);
// → true/false
```

### Aturan Penting Password

```csharp
// ✅ BENAR — simpan hash, verify dengan BCrypt
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

// ❌ SALAH — simpan plaintext
user.Password = request.Password;  // JANGAN PERNAH!

// ❌ SALAH — gunakan MD5/SHA
user.PasswordHash = SHA256.Hash(request.Password);  // terlalu cepat, tidak aman
```

---

## 🌐 9. CORS (Cross-Origin Resource Sharing)

### Apa itu CORS?

Browser memblokir request dari domain yang berbeda secara default:
```
Frontend: http://localhost:3000  →  API: http://localhost:5000
❌ CORS Error! (berbeda origin/port)
```

### Setup CORS

```csharp
// Di Program.cs — daftarkan policy
builder.Services.AddCors(options =>
{
    // Development: izinkan semua
    options.AddPolicy("DevPolicy", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());

    // Production: hanya domain tertentu
    options.AddPolicy("ProdPolicy", policy =>
        policy.WithOrigins("https://myapp.com", "https://admin.myapp.com")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Authorization", "Content-Type")
              .AllowCredentials());  // Izinkan cookie/credentials
});

// Terapkan (sebelum UseAuthentication!)
app.UseCors("DevPolicy");
```

### Kapan Butuh CORS?

| Skenario | Butuh CORS? |
|----------|------------|
| Frontend & API di domain/port berbeda | ✅ Ya |
| Frontend & API di domain/port sama | ❌ Tidak |
| API dipanggil dari Postman/curl | ❌ Tidak (bukan browser) |
| Mobile app panggil API | ❌ Tidak (bukan browser) |

---

## 🛡️ 10. Rate Limiting

### Setup Rate Limiter

```csharp
builder.Services.AddRateLimiter(options =>
{
    // Fixed Window: counter reset setiap window habis
    options.AddFixedWindowLimiter("general", opt =>
    {
        opt.Window      = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;      // Max 100 request per menit
        opt.QueueLimit  = 10;       // 10 boleh antri
    });

    // Policy ketat untuk auth endpoint
    options.AddFixedWindowLimiter("auth_policy", opt =>
    {
        opt.Window      = TimeSpan.FromMinutes(15);
        opt.PermitLimit = 50;       // Max 50 per 15 menit
        opt.QueueLimit  = 0;        // Tidak ada antrian
    });

    // Custom response saat limit terlampaui
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            status = 429,
            message = "Terlalu banyak percobaan. Coba lagi nanti."
        }, token);
    };
});

// Terapkan middleware
app.UseRateLimiter();
```

### Jenis Rate Limiter

| Jenis | Cara Kerja | Cocok Untuk |
|-------|-----------|-------------|
| **Fixed Window** | Counter reset tiap interval tetap | API umum |
| **Sliding Window** | Window bergeser lebih halus | Lebih smooth |
| **Token Bucket** | Replenish token secara bertahap | Burst-friendly |
| **Concurrency** | Batasi request paralel | Download/upload |

### Pakai di Controller

```csharp
[EnableRateLimiting("general")]     // Terapkan policy
[HttpGet]
public async Task<IActionResult> GetAll() { ... }

[EnableRateLimiting("auth_policy")] // Policy ketat
[HttpPost("login")]
public async Task<IActionResult> Login() { ... }

[DisableRateLimiting]               // Matikan rate limit
[HttpGet("health")]
public IActionResult HealthCheck() { ... }
```

---

## 📝 11. Swagger / OpenAPI

### Setup Swagger dengan JWT Support

```csharp
builder.Services.AddSwaggerGen(c =>
{
    // Info API
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Product Catalog API",
        Version     = "v1",
        Description = "REST API dengan JWT Authentication"
    });

    // Tombol Authorize di Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Masukkan JWT token"
    });

    // Apply ke semua endpoint
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Aktifkan di pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Catalog API v1");
    c.RoutePrefix = string.Empty;  // Swagger di root "/"
});
```

### Dokumentasi Endpoint di Controller

```csharp
/// <summary>
/// Tambah produk baru. Hanya Admin.
/// </summary>
/// <param name="product">Data produk dari body JSON.</param>
/// <returns>Produk yang baru dibuat + ID.</returns>
/// <response code="201">Produk berhasil dibuat.</response>
/// <response code="400">Input tidak valid.</response>
/// <response code="401">Token tidak ada/invalid.</response>
[HttpPost]
[ProducesResponseType(typeof(Product), 201)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
public async Task<IActionResult> Create([FromBody] Product product) { ... }
```

---

## 🔀 12. Middleware Pipeline — Urutan Penting!

### Urutan yang Benar

```csharp
var app = builder.Build();

// ↓ Request masuk dari atas
app.UseSwagger();           // 1. Serve Swagger JSON
app.UseSwaggerUI();         // 2. Serve Swagger HTML
app.UseHttpsRedirection();  // 3. Redirect HTTP → HTTPS
app.UseCors("DevPolicy");   // 4. Tambah CORS headers
app.UseRateLimiter();       // 5. Cek rate limit
app.UseAuthentication();    // 6. Parse JWT → set User.Identity
app.UseAuthorization();     // 7. Cek [Authorize] → izin akses
app.MapControllers();       // 8. Route ke controller action
// ↑ Response kembali ke atas

app.Run();
```

### Kenapa Urutan Penting?

```
❌ SALAH:
app.UseAuthorization();    // Cek auth...
app.UseAuthentication();   // ...tapi belum parse token! User selalu null → 401

✅ BENAR:
app.UseAuthentication();   // Parse token dulu → User.Identity terisi
app.UseAuthorization();    // Baru cek auth → bisa baca role dari token
```

### Aturan Urutan Middleware

```
UseExceptionHandler     ← Pertama (tangkap error dari semua middleware di bawah)
UseHsts                 ← Security headers
UseHttpsRedirection     ← Redirect HTTP → HTTPS
UseStaticFiles          ← Serve file statis (wwwroot)
UseCors                 ← CORS headers (sebelum auth!)
UseRateLimiter          ← Rate limiting
UseAuthentication       ← Parse token → identity
UseAuthorization        ← Check permissions
MapControllers          ← Terakhir: route ke controller
```

---

## 🏗️ 13. Dependency Injection (DI)

### Konsep

DI = framework yang otomatis "inject" dependency ke class yang membutuhkan.

```
Program.cs                          Controller
────────────                        ──────────
Register:                           Terima:
builder.Services                    public ProductsController(AppDbContext db)
  .AddDbContext<AppDbContext>(...)      _db = db;  ← DI inject otomatis!
```

### 3 Lifetime

```csharp
// Singleton — 1 instance selama app hidup
builder.Services.AddSingleton<IMyService, MyService>();
// Contoh: cache global, konfigurasi

// Scoped — 1 instance per HTTP request (setiap request baru = instance baru)
builder.Services.AddScoped<IMyService, MyService>();
// Contoh: DbContext (HARUS scoped!), service yang akses data per-user

// Transient — instance baru setiap kali diminta
builder.Services.AddTransient<IMyService, MyService>();
// Contoh: lightweight utility, mapper, formatter
```

### Mana yang Pakai Apa?

| Service | Lifetime | Kenapa |
|---------|----------|--------|
| `DbContext` | **Scoped** | 1 context per request, dispose di akhir request |
| `IConfiguration` | **Singleton** | Config tidak berubah selama app jalan |
| `ILogger<T>` | **Singleton** | Logger bisa shared karena thread-safe |
| Custom business service | **Scoped** | Biasanya akses DbContext (juga scoped) |
| Utility/helper | **Transient** | Stateless, ringan |

### Registrasi vs Injection

```csharp
// ═══ REGISTRASI (Program.cs) ═══

// DbContext — pakai method khusus
builder.Services.AddDbContext<AppDbContext>(options => ...);

// Controller — pakai method khusus
builder.Services.AddControllers();

// Custom service — pilih lifetime
builder.Services.AddScoped<IProductService, ProductService>();

// ═══ INJECTION (Constructor) ═══

public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    // Semua dependency di-inject di constructor
    public ProductsController(
        AppDbContext db,
        IProductService productService,
        ILogger<ProductsController> logger)
    {
        _db = db;
        _productService = productService;
        _logger = logger;
    }
}
```

---

## 📊 14. EF Core — Query Cheat Sheet

### CRUD Operations

```csharp
// ── CREATE ──
_db.Products.Add(product);           // Insert satu
_db.Products.AddRange(products);     // Insert banyak
await _db.SaveChangesAsync();        // Commit ke DB

// ── READ ──
await _db.Products.ToListAsync();                        // SELECT *
await _db.Products.FindAsync(id);                        // By PK (cepat, cek cache)
await _db.Products.FirstOrDefaultAsync(p => p.Name == x); // By kondisi
await _db.Products.SingleOrDefaultAsync(p => p.Slug == x); // By kondisi (error kalau > 1)

// ── UPDATE ──
var p = await _db.Products.FindAsync(id);  // Fetch
p.Name = "New Name";                        // Ubah property
await _db.SaveChangesAsync();               // EF auto-generate UPDATE

// ── DELETE ──
var p = await _db.Products.FindAsync(id);
_db.Products.Remove(p);                    // Stage delete
await _db.SaveChangesAsync();               // Commit DELETE
```

### Query Lanjutan

```csharp
// Filter
.Where(p => p.Category == "Electronics")
.Where(p => p.Price > 1000000 && p.Stock > 0)

// Sort
.OrderBy(p => p.Name)                    // ASC
.OrderByDescending(p => p.Price)          // DESC
.OrderBy(p => p.Category).ThenBy(p => p.Name) // Multi-sort

// Projection (pilih kolom)
.Select(p => new { p.Id, p.Name, p.Price })
.Select(p => new ProductDto { Id = p.Id, Name = p.Name })

// Pagination
.Skip((page - 1) * pageSize).Take(pageSize)

// Aggregation
await _db.Products.CountAsync();
await _db.Products.SumAsync(p => p.Price);
await _db.Products.AverageAsync(p => p.Price);
await _db.Products.MaxAsync(p => p.Price);
await _db.Products.MinAsync(p => p.Price);

// Check existence
await _db.Products.AnyAsync(p => p.Name == "iPhone");

// Include (JOIN/eager loading)
await _db.Products.Include(p => p.Category).ToListAsync();
await _db.Products.Include(p => p.Reviews).ThenInclude(r => r.Author).ToListAsync();

// Raw SQL (escape hatch)
await _db.Products.FromSqlRaw("SELECT * FROM \"Products\" WHERE \"Price\" > {0}", 1000000).ToListAsync();

// No Tracking (read-only, lebih cepat)
await _db.Products.AsNoTracking().ToListAsync();
```

### Tips Performa

| Situasi | Pakai | Kenapa |
|---------|-------|--------|
| Cari by ID | `FindAsync(id)` | Cek cache dulu |
| Read-only list | `AsNoTracking()` | Skip change tracker → 30% lebih cepat |
| Hanya perlu cek ada/tidak | `AnyAsync()` | Tidak load seluruh entity |
| Hanya perlu hitung | `CountAsync()` | SQL COUNT, tidak load data |
| Tampilkan ke UI | `.Select(p => new { ... })` | Hanya ambil kolom yang dibutuhkan |
| Update 1 entity | Fetch → ubah → Save | Change tracker auto-detect changes |

---

## 📝 15. Logging

### Built-in Logging (ILogger)

```csharp
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ILogger<ProductsController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Create([FromBody] Product product)
    {
        _logger.LogInformation("Creating product: {Name}", product.Name);

        // ... create logic ...

        _logger.LogInformation("Product created: ID={Id}", product.Id);
        return CreatedAtAction(...);
    }
}
```

### Log Levels

```
Trace       → Detail internal (sangat verbose)
Debug       → Debugging info
Information → Alur normal (request masuk, data dibuat)
Warning     → Sesuatu yang tidak biasa tapi app masih jalan
Error       → Error yang ditangani (catch)
Critical    → App crash / tidak bisa jalan
```

### Konfigurasi di appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "ProductCatalogAPI": "Debug"
    }
  }
}
```

| Key | Efek |
|-----|------|
| `Default: Information` | Semua log Information ke atas ditampilkan |
| `Microsoft.AspNetCore: Warning` | Framework hanya tampilkan Warning+ (kurangi noise) |
| `Microsoft.EntityFrameworkCore: Warning` | EF Core hanya Warning+ (hide SQL queries) |
| `ProductCatalogAPI: Debug` | Code kita sendiri tampilkan Debug+ (lebih detail) |

---

## ⚡ 16. CLI Commands — Cheat Sheet

### Project

```bash
# Buat project baru
dotnet new webapi -n ProductCatalogAPI -o ./ProductCatalogAPI

# Run project
dotnet run

# Run dengan hot reload (auto-restart saat code berubah)
dotnet watch run

# Build tanpa run
dotnet build

# Publish untuk production
dotnet publish -c Release -o ./publish
```

### NuGet Packages

```bash
dotnet add package <PackageName> --version <Version>
dotnet remove package <PackageName>
dotnet list package
dotnet restore
```

### EF Core Migrations

```bash
dotnet ef migrations add <NamaMigration>
dotnet ef database update
dotnet ef database drop --force
dotnet ef migrations remove
dotnet ef migrations list
dotnet ef migrations script    # Generate SQL file
```

### Testing

```bash
dotnet test                     # Run semua test
dotnet test --filter "ClassName" # Run test tertentu
dotnet test --verbosity normal  # Output lebih detail
```

### Lainnya

```bash
dotnet --version                # Cek versi .NET
dotnet --list-sdks              # Semua SDK terinstall
dotnet clean                    # Hapus output build
dotnet user-secrets init        # Init User Secrets
dotnet user-secrets set "Key" "Value"
```

---

## 🚨 17. Error Handling Patterns

### Per-Endpoint (Try-Catch)

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] Product product)
{
    try
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "DB error saat membuat produk.");
        return StatusCode(500, new { message = "Gagal menyimpan ke database." });
    }
}
```

### Global Exception Handler

```csharp
// Di Program.cs — tangkap semua exception yang lolos
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = 500,
            message = "Terjadi kesalahan server. Coba lagi nanti."
        });
    });
});
```

### Jangan Pernah

```csharp
// ❌ Expose stack trace ke client
catch (Exception ex) { return StatusCode(500, ex.ToString()); }

// ❌ Catch kosong (silent fail)
catch (Exception) { }

// ✅ Log internal, return pesan generic
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error in Create");
    return StatusCode(500, new { message = "Internal server error." });
}
```

---

## 📋 18. Checklist: Backend App yang Lengkap

### Foundation
- [ ] `.csproj` — target framework & semua NuGet packages
- [ ] `appsettings.json` — config DB, JWT, logging
- [ ] `Program.cs` — semua services ter-register & middleware urut benar

### Database
- [ ] Models/Entity dengan tipe data yang benar
- [ ] `DbContext` dengan `DbSet` untuk setiap tabel
- [ ] Migration sudah di-apply
- [ ] Seed data untuk development

### Security
- [ ] JWT Authentication ter-setup
- [ ] Password di-hash dengan BCrypt (bukan plaintext!)
- [ ] `[Authorize]` di setiap endpoint yang perlu auth
- [ ] Role-based access control (Admin/User)
- [ ] CORS ter-konfigurasi
- [ ] Rate limiting aktif di endpoint publik & auth
- [ ] Secret key tidak di-hardcode (User Secrets / env var)

### API Design
- [ ] RESTful routing (`GET /api/products`, `POST /api/products`, dll.)
- [ ] Status code yang tepat (200, 201, 400, 401, 404, 500)
- [ ] Response body yang informatif (pesan error jelas)
- [ ] Validasi input di controller / via Data Annotations
- [ ] Async/await untuk semua operasi I/O

### DevEx
- [ ] Swagger aktif di development
- [ ] Hot reload (`dotnet watch run`)
- [ ] Logging yang memadai
- [ ] Error handling (try-catch + global handler)

---

## 🗺️ Peta Alur Request (Gambaran Besar)

```
Client (Postman / Browser / Frontend)
  │
  │  HTTP Request
  │  GET /api/products
  │  Authorization: Bearer eyJhb...
  │
  ▼
┌─────────────────────────────────────────────────┐
│              ASP.NET Core Pipeline               │
│                                                  │
│  1. CORS Middleware         → Tambah CORS headers│
│  2. Rate Limiter            → Cek quota          │
│  3. Authentication          → Parse JWT token    │
│  4. Authorization           → Cek [Authorize]    │
│  5. Routing                 → Match URL → Action │
│                                                  │
│  ┌─────────────────────────────────────────┐     │
│  │        ProductsController.GetAll()       │     │
│  │                                          │     │
│  │  var products = await _db.Products       │     │
│  │      .ToListAsync();                     │     │
│  │                                          │     │
│  │  return Ok(products);                    │     │
│  └───────────────┬──────────────────────────┘     │
│                  │                                │
│                  ▼                                │
│  ┌─────────────────────────────────────────┐     │
│  │           AppDbContext (_db)              │     │
│  │                                          │     │
│  │  DbSet<Product> Products                 │     │
│  │      ↕ EF Core                           │     │
│  │  SQL: SELECT * FROM "Products"           │     │
│  └───────────────┬──────────────────────────┘     │
│                  │                                │
│                  ▼                                │
│  ┌─────────────────────────────────────────┐     │
│  │          PostgreSQL Database              │     │
│  │                                          │     │
│  │  Table: Products                         │     │
│  │  ┌────┬──────────┬──────────┬─────┐     │     │
│  │  │ Id │ Name     │ Price    │ ... │     │     │
│  │  ├────┼──────────┼──────────┼─────┤     │     │
│  │  │  1 │ Laptop   │ 15000000 │ ... │     │     │
│  │  │  2 │ Keyboard │  1200000 │ ... │     │     │
│  │  └────┴──────────┴──────────┴─────┘     │     │
│  └──────────────────────────────────────────┘     │
└─────────────────────────────────────────────────┘
  │
  ▼
Client menerima:
  HTTP 200 OK
  Content-Type: application/json
  [
    { "id": 1, "name": "Laptop", "price": 15000000, ... },
    { "id": 2, "name": "Keyboard", "price": 1200000, ... }
  ]
```
