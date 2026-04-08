# 🔍 Penjelasan Kode — Product Catalog API

Dokumen ini menjelaskan **setiap file kode** secara detail: fungsinya, bagian-bagiannya, dan keputusan desain di baliknya.

---

## 📁 Struktur File

```
ProductCatalogAPI/
├── Program.cs                    ← Entry point aplikasi
├── appsettings.json              ← Konfigurasi (JWT, DB, Logging)
├── ProductCatalogAPI.csproj      ← Daftar dependencies
├── Controllers/
│   ├── AuthController.cs         ← Endpoint register & login
│   └── ProductsController.cs     ← Endpoint CRUD produk
├── Data/
│   └── AppDbContext.cs           ← EF Core DbContext + seed data
└── Models/
    ├── Product.cs                ← Model/entity tabel Products
    └── User.cs                   ← Model User (DTO + Entity)
```

---

## 📄 `Program.cs` — Entry Point & Konfigurasi

File ini adalah **titik masuk aplikasi** dan tempat semua service (dependency injection) serta middleware dikonfigurasi.

### Bagian 1 — Database (EF Core + PostgreSQL)

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

| Baris | Penjelasan |
|-------|-----------|
| `AddDbContext<AppDbContext>` | Mendaftarkan `AppDbContext` ke DI container dengan lifetime **Scoped** (satu instance per request HTTP) |
| `UseNpgsql(...)` | Memberitahu EF Core untuk menggunakan database **PostgreSQL** (via Npgsql driver) |
| `GetConnectionString("DefaultConnection")` | Membaca connection string dari `appsettings.json` |

---

### Bagian 2 — CORS

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});
```

**CORS** (Cross-Origin Resource Sharing) adalah mekanisme keamanan browser yang membatasi request dari domain lain.

- `AllowAnyOrigin()` — mengizinkan semua domain (bebas untuk development)
- `AllowAnyMethod()` — mengizinkan semua HTTP method (GET, POST, PUT, DELETE, dll.)
- `AllowAnyHeader()` — mengizinkan semua header termasuk `Authorization`

> ⚠️ Di production, ganti dengan `policy.WithOrigins("https://domain-frontend-anda.com")`

---

### Bagian 3 — Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth_policy", opt =>
    {
        opt.Window       = TimeSpan.FromMinutes(15);
        opt.PermitLimit  = 50;
        opt.QueueLimit   = 0;
    });

    options.AddFixedWindowLimiter("general", opt =>
    {
        opt.Window       = TimeSpan.FromMinutes(1);
        opt.PermitLimit  = 100;
        opt.QueueLimit   = 10;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            status  = 429,
            message = "Terlalu banyak percobaan. Coba lagi nanti."
        }, token);
    };
});
```

**Fixed Window Limiter** = membatasi jumlah request dalam jendela waktu tetap.

| Policy | Window | Limit | Queue | Digunakan Di |
|--------|--------|-------|-------|--------------|
| `auth_policy` | 15 menit | 50 req | 0 | `/api/auth/*` — mencegah brute force |
| `general` | 1 menit | 100 req | 10 | `/api/products/*` |

- `QueueLimit = 0` → request yang melebihi batas langsung ditolak (tidak diqueue)
- `QueueLimit = 10` → 10 request bisa masuk antrian menunggu slot tersedia
- `OnRejected` → callback yang dijalankan saat request ditolak; mengembalikan JSON 429

---

### Bagian 4 — JWT Authentication

```csharp
var jwtKey    = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = false,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtKey))
        };
    });
```

**Alur JWT**:
1. Client login → server buat token JWT
2. Client kirim token di header `Authorization: Bearer <token>` pada setiap request
3. Middleware `UseAuthentication()` membaca & memvalidasi token otomatis

| Parameter | Nilai | Penjelasan |
|-----------|-------|-----------|
| `ValidateIssuer = true` | `true` | Token harus dikeluarkan oleh issuer yang valid |
| `ValidateAudience = false` | `false` | Tidak cek audience (untuk simplisitas) |
| `ValidateLifetime = true` | `true` | Token yang sudah expired akan ditolak |
| `ValidateIssuerSigningKey = true` | `true` | Verifikasi tanda tangan token dengan secret key |
| `IssuerSigningKey` | SymmetricSecurityKey | Key yang sama digunakan untuk sign dan verify token |

---

### Bagian 5 — Swagger dengan JWT Support

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { ... });

    // Definisi skema keamanan JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });

    // Terapkan ke semua endpoint
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { ... });
});
```

- `SwaggerDoc` → metadata API (judul, versi, deskripsi) yang muncul di UI
- `AddSecurityDefinition("Bearer", ...)` → menambahkan tombol "Authorize" di Swagger UI
- `AddSecurityRequirement` → mengaktifkan lock icon 🔒 di setiap endpoint

---

### Bagian 6 — Auto Migrate saat Startup

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```

- `CreateScope()` → membuat scope DI baru (penting karena DbContext bersifat Scoped)
- `db.Database.Migrate()` → menjalankan migrasi yang belum diapply secara otomatis saat app pertama kali jalan

---

### Bagian 7 — Middleware Pipeline

```csharp
app.UseHttpsRedirection();   // 1. Redirect HTTP → HTTPS
app.UseCors("DevPolicy");    // 2. Terapkan CORS
app.UseRateLimiter();        // 3. Batasi jumlah request
app.UseAuthentication();     // 4. Baca & validasi JWT token
app.UseAuthorization();      // 5. Cek hak akses [Authorize]
app.MapControllers();        // 6. Route ke controller yang sesuai
```

> **Urutan middleware sangat penting!** `UseAuthentication` harus sebelum `UseAuthorization`, dan keduanya harus setelah routing.

---

## 📄 `appsettings.json` — Konfigurasi Aplikasi

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
    "Key": "ProductCatalogSuperSecretKey...",
    "Issuer": "ProductCatalogAPI"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;..."
  }
}
```

| Key | Deskripsi |
|-----|-----------|
| `Logging.LogLevel.Default` | Level log default untuk semua komponen |
| `Logging.LogLevel.Microsoft.AspNetCore` | Log framework ASP.NET Core dikurangi ke Warning saja |
| `AllowedHosts` | Host yang diizinkan; `*` berarti semua |
| `Jwt.Key` | Secret key untuk sign JWT token — **jaga kerahasiaannya!** |
| `Jwt.Issuer` | Identitas penerbit token, dicek saat validasi |
| `ConnectionStrings.DefaultConnection` | String koneksi ke PostgreSQL |

---

## 📄 `ProductCatalogAPI.csproj` — Dependencies

```xml
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
```

| Package | Fungsi |
|---------|--------|
| `BCrypt.Net-Next` | Hashing & verifikasi password dengan algoritma BCrypt |
| `JwtBearer` | Middleware autentikasi JWT untuk ASP.NET Core |
| `EntityFrameworkCore` | ORM — pemetaan class C# ke tabel database |
| `EntityFrameworkCore.Design` | Tools CLI untuk membuat & menjalankan migrasi (`dotnet ef`) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Provider EF Core untuk database PostgreSQL |
| `Swashbuckle.AspNetCore` | Generates Swagger/OpenAPI spec dan UI Swagger |

---

## 📄 `Models/Product.cs` — Entity Produk

```csharp
namespace ProductCatalogAPI.Models;

public class Product
{
    public int Id { get; set; }                              // Primary key (auto-increment)
    public string Name { get; set; } = string.Empty;        // Nama produk
    public string Description { get; set; } = string.Empty; // Deskripsi produk
    public decimal Price { get; set; }                       // Harga (presisi tinggi)
    public int Stock { get; set; }                           // Jumlah stok
    public string Category { get; set; } = string.Empty;    // Kategori
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Waktu dibuat (UTC)
}
```

- Class ini adalah **Entity** — digunakan oleh EF Core untuk membuat tabel `Products`
- `= string.Empty` → menghindari null reference; properti string diinisialisasi kosong
- `decimal` untuk `Price` → lebih akurat dari `float`/`double` untuk nilai keuangan
- `DateTime.UtcNow` → menyimpan waktu dalam UTC; lebih konsisten di environment multi-zona waktu

---

## 📄 `Models/User.cs` — DTO & Entity User

```csharp
// DTO: request body untuk Register & Login
public class User
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// Entity: representasi data user di database
public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; // Default role: "User"
}
```

File ini memiliki **dua class** dengan tujuan berbeda:

| Class | Tipe | Digunakan Untuk | Disimpan ke DB? |
|-------|------|-----------------|-----------------|
| `User` | **DTO** (Data Transfer Object) | Menerima request body login | ❌ Tidak |
| `UserEntity` | **Entity** | Menyimpan data user di tabel `Users` | ✅ Ya |

> Alasan dipisah: `User` (DTO) hanya berisi `Password` plaintext sementara yang tidak boleh disimpan ke DB. `UserEntity` menyimpan `PasswordHash` (hasil BCrypt).

---

## 📄 `Data/AppDbContext.cs` — Database Context

```csharp
public class AppDbContext : DbContext
{
    // Constructor: menerima options dari DI (connection string, dll.)
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Deklarasi tabel
    public DbSet<Product> Products { get; set; }
    public DbSet<UserEntity> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed data: user admin default
        modelBuilder.Entity<UserEntity>().HasData(new UserEntity
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "Admin"
        });

        // Seed data: produk awal
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop Pro 15", ... },
            new Product { Id = 2, Name = "Mechanical Keyboard", ... },
            new Product { Id = 3, Name = "Wireless Mouse", ... }
        );
    }
}
```

| Bagian | Penjelasan |
|--------|-----------|
| `DbContext` | Base class dari EF Core yang menyediakan fungsi query, save, tracking, dll. |
| `DbContextOptions` | Berisi konfigurasi (connection string, provider) yang di-inject dari `Program.cs` |
| `DbSet<Product>` | Merepresentasikan tabel `Products`, digunakan untuk query: `_db.Products.ToListAsync()` |
| `DbSet<UserEntity>` | Merepresentasikan tabel `Users` |
| `OnModelCreating` | Override untuk konfigurasi model & **seed data** |
| `HasData(...)` | Seed data yang akan dimasukkan saat migrasi pertama dijalankan |

> **Penting**: `HasData` memiliki **Id tetap** (`Id = 1`) karena EF Core perlu identifier stabil untuk tracking seed data antar migrasi.

---

## 📄 `Controllers/AuthController.cs` — Autentikasi

Controller ini menangani **register dan login** user.

### Dependency Injection

```csharp
private readonly AppDbContext _db;
private readonly IConfiguration _config;

public AuthController(AppDbContext db, IConfiguration config)
{
    _db = db;       // akses database
    _config = config; // akses appsettings.json
}
```

### Endpoint Register

```csharp
[HttpPost("register")]
[EnableRateLimiting("auth_policy")]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
```

| Attribute | Fungsi |
|-----------|--------|
| `[HttpPost("register")]` | Mapping ke `POST /api/auth/register` |
| `[EnableRateLimiting("auth_policy")]` | Terapkan rate limit: max 50 req/15 menit |
| `[FromBody]` | Baca data dari JSON request body |

**Alur logika Register:**
1. Validasi input (username & password tidak kosong, password min. 6 karakter)
2. Cek duplikasi username di database
3. Hash password dengan BCrypt
4. Validasi dan set role (default `"User"` jika tidak valid)
5. Simpan `UserEntity` baru ke database
6. Return 200 OK dengan informasi user

### Endpoint Login

```csharp
[HttpPost("login")]
[EnableRateLimiting("auth_policy")]
public async Task<IActionResult> Login([FromBody] User request)
```

**Alur logika Login:**
1. Validasi input tidak kosong
2. Cari user berdasarkan username di database
3. Verifikasi password dengan `BCrypt.Verify(plaintext, hash)`
4. Generate JWT token dengan `GenerateToken(user)`
5. Return token beserta info user

### Helper: GenerateToken

```csharp
private string GenerateToken(UserEntity user)
{
    // Claims = "klaim" tentang identitas user yang disematkan ke dalam token
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ID user
        new Claim(ClaimTypes.Name, user.Username),                // Username
        new Claim(ClaimTypes.Role, user.Role)                     // Role (Admin/User)
    };

    // Buat signing key dari secret di appsettings
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    // Buat JWT token
    var token = new JwtSecurityToken(
        issuer: _config["Jwt:Issuer"],
        audience: null,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1), // expired dalam 1 jam
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token); // serialize ke string
}
```

**Struktur JWT token** (bisa di-decode di [jwt.io](https://jwt.io)):
```json
// Header
{ "alg": "HS256", "typ": "JWT" }

// Payload (claims)
{
  "nameid": "1",
  "unique_name": "admin",
  "role": "Admin",
  "iss": "ProductCatalogAPI",
  "exp": 1234567890
}
```

### DTO: RegisterRequest

```csharp
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; } // nullable — opsional di request body
}
```

Dipisah dari `User` (yang digunakan untuk login) karena register membutuhkan field `Role` opsional.

---

## 📄 `Controllers/ProductsController.cs` — CRUD Produk

### Class-level Attributes

```csharp
[Authorize]                    // Semua endpoint di controller ini wajib login
[ApiController]                // Aktifkan fitur API: model binding otomatis, dll.
[Route("api/[controller]")]    // Route: api/products ([controller] = "Products")
public class ProductsController : ControllerBase
```

`[Authorize]` di level class berlaku sebagai **default** untuk semua method. Method individual bisa meng-override dengan `[AllowAnonymous]` atau `[Authorize(Roles = "...")]`.

### GET All — `GET /api/products`

```csharp
[HttpGet]
[Authorize(Roles = "Admin,User")]
[EnableRateLimiting("general")]
public async Task<IActionResult> GetAll()
{
    var products = await _db.Products.ToListAsync();
    return Ok(products);
}
```

- `ToListAsync()` → mengambil semua record dari tabel Products secara asynchronous
- `Ok(products)` → return HTTP 200 dengan body JSON berisi list produk

### GET Public — `GET /api/products/public`

```csharp
[AllowAnonymous]
[HttpGet("public")]
[EnableRateLimiting("general")]
public async Task<IActionResult> GetPublic()
{
    var products = await _db.Products
        .Select(p => new { p.Id, p.Name, p.Category, p.Price }) // hanya field tertentu
        .ToListAsync();

    return Ok(new {
        message = "Data publik — login untuk melihat detail lengkap.",
        total = products.Count,
        data = products
    });
}
```

- `[AllowAnonymous]` → override `[Authorize]` di class level; endpoint ini tidak butuh login
- `.Select(p => new { ... })` → **projection query** — mengambil hanya kolom tertentu (lebih efisien dari SELECT *)

### GET By ID — `GET /api/products/{id}`

```csharp
[HttpGet("{id}")]
[Authorize(Roles = "Admin,User")]
public async Task<IActionResult> GetById(int id)
{
    var product = await _db.Products.FindAsync(id);
    if (product == null)
        return NotFound(new { message = $"Produk dengan ID {id} tidak ditemukan." });

    return Ok(product);
}
```

- `{id}` di route di-binding ke parameter `int id` secara otomatis
- `FindAsync(id)` → menggunakan primary key untuk pencarian (lebih cepat dari `FirstOrDefaultAsync`)
- Return `404 NotFound` jika tidak ada

### POST Create — `POST /api/products`

```csharp
[HttpPost]
[Authorize(Roles = "Admin")]   // HANYA Admin yang bisa tambah produk
public async Task<IActionResult> Create([FromBody] Product product)
{
    if (string.IsNullOrWhiteSpace(product.Name))
        return BadRequest(new { message = "Nama produk tidak boleh kosong." });

    if (product.Price <= 0)
        return BadRequest(new { message = "Harga produk harus lebih dari 0." });

    product.Id = 0;             // Reset ID — biarkan database yang generate
    product.CreatedAt = DateTime.UtcNow;

    _db.Products.Add(product);
    await _db.SaveChangesAsync();

    return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
}
```

- `product.Id = 0` → penting untuk memastikan database yang autogenerate ID (hindari konflik)
- `CreatedAtAction(nameof(GetById), ...)` → return **HTTP 201 Created** dengan header `Location: /api/products/{id}` — best practice untuk REST API

### PUT Update — `PUT /api/products/{id}`

```csharp
[HttpPut("{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Update(int id, [FromBody] Product updated)
{
    var product = await _db.Products.FindAsync(id);
    if (product == null)
        return NotFound(...);

    product.Name        = updated.Name;
    product.Description = updated.Description;
    product.Price       = updated.Price;
    product.Stock       = updated.Stock;
    product.Category    = updated.Category;

    await _db.SaveChangesAsync();
    return Ok(product);
}
```

- Pola **"fetch then update"**: ambil entity yang sudah di-track EF Core, update propertinya, lalu save
- EF Core secara otomatis mendeteksi perubahan (**Change Tracking**) dan generate `UPDATE` SQL yang benar
- `CreatedAt` tidak diupdate — timestamp pembuatan awal tetap dipertahankan

### DELETE — `DELETE /api/products/{id}`

```csharp
[HttpDelete("{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Delete(int id)
{
    var product = await _db.Products.FindAsync(id);
    if (product == null)
        return NotFound(...);

    _db.Products.Remove(product);
    await _db.SaveChangesAsync();

    return Ok(new { message = $"Produk '{product.Name}' berhasil dihapus." });
}
```

- `Remove(product)` → menandai entity untuk dihapus (belum eksekusi ke DB)
- `SaveChangesAsync()` → baru eksekusi perintah `DELETE FROM Products WHERE Id = ?` ke database

---

## 🔄 Diagram Alur Request

```
Client Request
     │
     ▼
[HTTPS Redirect]         — HTTP → HTTPS
     │
     ▼
[CORS Middleware]         — Cek origin yang diizinkan
     │
     ▼
[Rate Limiter]            — Cek batas request/waktu
     │
     ▼
[Authentication]          — Validasi JWT token dari header
     │
     ▼
[Authorization]           — Cek role (Admin/User) sesuai [Authorize]
     │
     ▼
[Controller Action]       — Logika bisnis (CRUD, dll.)
     │
     ▼
[Database (PostgreSQL)]   — Query melalui EF Core
     │
     ▼
[Response JSON]           — Kembali ke Client
```
