# 🔬 Anatomy of a Controller — Bedah Tuntas ProductsController

> Dokumen ini menjelaskan **arti dan alasan** dari setiap baris kode di dalam satu controller lengkap.
> Dibaca dari atas ke bawah, sama seperti cara compiler membacanya.

---

## 📄 File Lengkap: `ProductsController.cs`

```csharp
// ═══════════════════════════════════════════════════════
// BAGIAN 1 — USING DIRECTIVES
// ═══════════════════════════════════════════════════════
using Microsoft.AspNetCore.Authorization;   // [Authorize], [AllowAnonymous]
using Microsoft.AspNetCore.Mvc;             // ControllerBase, [ApiController], [HttpGet], dll.
using Microsoft.AspNetCore.RateLimiting;   // [EnableRateLimiting]
using Microsoft.EntityFrameworkCore;        // ToListAsync(), FindAsync(), AnyAsync(), dll.
using ProductCatalogAPI.Data;              // AppDbContext (akses database)
using ProductCatalogAPI.Models;            // Class Product, UserEntity, dll.

// ═══════════════════════════════════════════════════════
// BAGIAN 2 — NAMESPACE
// ═══════════════════════════════════════════════════════
namespace ProductCatalogAPI.Controllers;

// ═══════════════════════════════════════════════════════
// BAGIAN 3 — CLASS DECLARATION & CLASS-LEVEL ATTRIBUTES
// ═══════════════════════════════════════════════════════
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // ═══════════════════════════════════════════════════
    // BAGIAN 4 — FIELDS & CONSTRUCTOR (Dependency Injection)
    // ═══════════════════════════════════════════════════
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    // ═══════════════════════════════════════════════════
    // BAGIAN 5 — ENDPOINT METHODS (Actions)
    // ═══════════════════════════════════════════════════

    [HttpGet]
    [Authorize(Roles = "Admin,User")]
    [EnableRateLimiting("general")]
    public async Task<IActionResult> GetAll() { ... }

    [AllowAnonymous]
    [HttpGet("public")]
    [EnableRateLimiting("general")]
    public async Task<IActionResult> GetPublic() { ... }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetById(int id) { ... }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Product product) { ... }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Product updated) { ... }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id) { ... }
}
```

---

## 🔍 Bedah Per-Bagian

---

### 1️⃣ BAGIAN 1 — Using Directives

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Data;
using ProductCatalogAPI.Models;
```

| Baris | Isi | Kenapa Dibutuhkan |
|---|---|---|
| `using Microsoft.AspNetCore.Authorization` | Namespace untuk attribute `[Authorize]` dan `[AllowAnonymous]` | Tanpa ini, `[Authorize]` tidak dikenali compiler |
| `using Microsoft.AspNetCore.Mvc` | Namespace untuk semua hal inti Web API: `ControllerBase`, `[ApiController]`, `[HttpGet]`, `IActionResult`, `Ok()`, `NotFound()`, dll. | **Paling penting** — tanpa ini controller tidak bisa dibuat |
| `using Microsoft.AspNetCore.RateLimiting` | Namespace untuk `[EnableRateLimiting]` | Untuk membatasi jumlah request per waktu |
| `using Microsoft.EntityFrameworkCore` | Extension methods EF Core: `ToListAsync()`, `FindAsync()`, `AnyAsync()`, dll. | Tanpa ini, method async query database tidak tersedia |
| `using ProductCatalogAPI.Data` | Namespace project sendiri yang berisi `AppDbContext` | Supaya controller bisa menggunakan DbContext untuk akses DB |
| `using ProductCatalogAPI.Models` | Namespace berisi class `Product`, `UserEntity`, dll. | Supaya controller tahu struktur data yang akan dipakai |

---

### 2️⃣ BAGIAN 2 — Namespace

```csharp
namespace ProductCatalogAPI.Controllers;
```

**Apa:** Membungkus class ini dalam namespace `ProductCatalogAPI.Controllers`.

**Kenapa:** Konvensi standar .NET — namespace mencerminkan struktur folder. File ini ada di folder `Controllers/`, jadi namespacenya `ProjectName.Controllers`.

**Format modern:** Tanpa kurung kurawal `{}` (file-scoped namespace — C# 10+). Lebih ringkas dari yang lama:
```csharp
// Cara lama (C# 9 ke bawah):
namespace ProductCatalogAPI.Controllers
{
    public class ProductsController : ControllerBase { ... }
}
```

---

### 3️⃣ BAGIAN 3 — Class Declaration & Attributes

```csharp
[Authorize]                          // ← Attribute 1
[ApiController]                      // ← Attribute 2
[Route("api/[controller]")]          // ← Attribute 3
public class ProductsController : ControllerBase   // ← Class Declaration
```

#### `[Authorize]` — Proteksi Default Semua Endpoint

```
[Authorize]
```

- **Apa:** Mewajibkan JWT token valid untuk **semua** method di class ini.
- **Cara kerja:** Middleware ASP.NET Core memeriksa header `Authorization: Bearer <token>` sebelum method dieksekusi. Jika tidak ada atau tidak valid → `401 Unauthorized` otomatis.
- **Kenapa di class level:** Lebih aman daripada harus pasang `[Authorize]` di setiap method satu per satu (mudah kelewatan). Override di method yang butuh pengecualian.
- **Override tersedia:**
  - `[AllowAnonymous]` — Endpoint bebas akses
  - `[Authorize(Roles = "Admin")]` — Ganti role requirement

#### `[ApiController]` — Aktifkan Fitur Web API

```
[ApiController]
```

- **Apa:** Metadata attribute yang mengaktifkan beberapa behavior khusus untuk API.
- **Behavior yang aktif secara otomatis:**
  1. **Automatic Model Validation** — Jika `ModelState.IsValid == false` (misalnya ada `[Required]` yang kosong), langsung return `400 Bad Request` tanpa perlu tulis kode validasi manual
  2. **Binding Source Inference** — Parameter bertipe primitif (int, string) otomatis dari route/query, bertipe kompleks otomatis dari body
  3. **Problem Details** — Format error response mengikuti standar RFC 7807
- **Wajib dipakai:** Untuk semua API controller. Jangan pernah buat API controller tanpa ini.

#### `[Route("api/[controller]")]` — Tentukan Base URL

```
[Route("api/[controller]")]
```

- **Apa:** Menentukan URL dasar untuk semua endpoint di class ini.
- **`[controller]`** adalah **route token** — diganti otomatis dengan nama class tanpa suffix "Controller":
  - `ProductsController` → `products` → `/api/products`
  - `AuthController` → `auth` → `/api/auth`
- **Kombinasi dengan method route:**
  ```
  Class:  [Route("api/[controller]")]   →  /api/products
  Method: [HttpGet("{id}")]             →  /api/products/{id}
  Final:  GET /api/products/5
  ```
- **Kenapa pakai `[controller]` bukan hard-code:** Kalau nama class berubah, route ikut berubah otomatis. Tidak perlu update dua tempat.

#### `public class ProductsController : ControllerBase`

```csharp
public class ProductsController : ControllerBase
```

| Bagian | Penjelasan |
|---|---|
| `public` | Class bisa diakses dari luar assembly (wajib agar framework bisa instantiate) |
| `class ProductsController` | Nama class. Konvensi: selalu akhiri dengan "Controller" |
| `: ControllerBase` | Mewarisi `ControllerBase` — base class yang menyediakan helper methods: `Ok()`, `NotFound()`, `BadRequest()`, property `User`, `Request`, `Response`, dll. |

---

### 4️⃣ BAGIAN 4 — Fields & Constructor (Dependency Injection)

```csharp
private readonly AppDbContext _db;

public ProductsController(AppDbContext db)
{
    _db = db;
}
```

#### `private readonly AppDbContext _db;`

| Kata Kunci | Arti |
|---|---|
| `private` | Hanya bisa diakses dalam class ini (enkapsulasi) |
| `readonly` | Nilai hanya bisa di-assign sekali, di constructor. Setelah itu tidak bisa diubah. Ini mencegah bug di mana `_db` di-reassign di tengah request. |
| `AppDbContext` | Tipe field — class yang mengrepresentasikan koneksi ke database |
| `_db` | Nama field dengan prefix `_` (konvensi C# untuk private field) |

#### `public ProductsController(AppDbContext db)`

**Ini adalah Constructor Injection (pola Dependency Injection).**

- **Apa yang terjadi:**
  1. App startup → controller di-register ke DI container
  2. Request masuk → ASP.NET Core butuh instance `ProductsController`
  3. DI container melihat: "Constructor butuh `AppDbContext`"
  4. DI container sudah punya `AppDbContext` (di-register di `Program.cs`)
  5. DI container otomatis buat instance `AppDbContext` dan inject ke constructor
  6. `_db = db` → simpan referensi ke field

- **Kenapa DI (bukan `new AppDbContext()`):**
  - ✅ Lifetime dikelola framework (Scoped per-request untuk DbContext)
  - ✅ Mudah di-test (bisa inject mock DbContext)
  - ✅ Tidak perlu tahu cara buat `AppDbContext` (configuration connection string, dll.)

```
Program.cs                          ProductsController
─────────────────                   ──────────────────────────────
builder.Services                    public ProductsController(AppDbContext db)
  .AddDbContext<AppDbContext>(...)  ←  DI otomatis inject ini
```

---

### 5️⃣ BAGIAN 5 — Endpoint Methods (Actions)

#### `GetAll` — GET /api/products

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

| Baris | Arti |
|---|---|
| `[HttpGet]` | Method ini menangani HTTP GET ke `/api/products` (tanpa sub-path) |
| `[Authorize(Roles = "Admin,User")]` | Override class-level `[Authorize]` — hanya role Admin atau User yang boleh. Koma artinya OR, bukan AND |
| `[EnableRateLimiting("general")]` | Aktifkan rate limiter bernama "general" untuk endpoint ini (didefinisikan di Program.cs) |
| `public async Task<IActionResult>` | Method publik, async, return `IActionResult` |
| `GetAll()` | Nama method. Tidak ada parameter karena tidak butuh input dari URL |
| `await _db.Products.ToListAsync()` | Query `SELECT * FROM "Products"` ke database, async. `_db.Products` adalah `DbSet<Product>`. `ToListAsync()` eksekusi query dan load semua hasil ke List |
| `return Ok(products)` | Bungkus `products` dalam HTTP `200 OK` response. Framework serialize `products` ke JSON otomatis |

---

#### `GetPublic` — GET /api/products/public

```csharp
[AllowAnonymous]
[HttpGet("public")]
[EnableRateLimiting("general")]
public async Task<IActionResult> GetPublic()
{
    var products = await _db.Products
        .Select(p => new { p.Id, p.Name, p.Category, p.Price })
        .ToListAsync();

    return Ok(new
    {
        message = "Data publik — login untuk melihat detail lengkap.",
        total   = products.Count,
        data    = products
    });
}
```

| Baris | Arti |
|---|---|
| `[AllowAnonymous]` | Override `[Authorize]` di class level — endpoint ini **bebas diakses** tanpa token. Penting untuk data preview publik |
| `[HttpGet("public")]` | Method ini menangani GET `/api/products/public`. String `"public"` ditambahkan ke base route |
| `.Select(p => new { p.Id, p.Name, p.Category, p.Price })` | **Projection** — pilih hanya 4 field, bukan semua kolom (`SELECT Id, Name, Category, Price` bukan `SELECT *`). Lebih efisien di jaringan dan menyembunyikan field sensitif |
| `new { ... }` | **Anonymous type** — objek tanpa nama class. Cukup untuk JSON response, tidak perlu bikin class DTO baru |
| `return Ok(new { message, total, data })` | Bungkus response dalam wrapper object. Best practice: sertakan metadata (message, total) selain data mentah |

---

#### `GetById` — GET /api/products/{id}

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

| Baris | Arti |
|---|---|
| `[HttpGet("{id}")]` | Method ini menangani GET `/api/products/5`. `{id}` adalah **route parameter** — nilai dari URL segment ini akan di-bind ke parameter `id` |
| `int id` | Parameter `id` di-binding otomatis dari `{id}` di URL. ASP.NET Core parse string "5" → int 5 |
| `FindAsync(id)` | Cari entity berdasarkan **primary key**. Lebih cepat dari `FirstOrDefaultAsync` karena: (1) cek memory cache EF Core dulu, (2) query dioptimasi by PK |
| `if (product == null)` | Cek apakah data ditemukan. Pola standar "check then act" |
| `return NotFound(new { message = $"..." })` | HTTP `404 Not Found` dengan pesan informatif. Gunakan string interpolation (`$"..."`) untuk menyertakan ID yang dicari |
| `return Ok(product)` | HTTP `200 OK` dengan data produk (JSON) |

---

#### `Create` — POST /api/products

```csharp
[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Create([FromBody] Product product)
{
    if (string.IsNullOrWhiteSpace(product.Name))
        return BadRequest(new { message = "Nama produk tidak boleh kosong." });

    if (product.Price <= 0)
        return BadRequest(new { message = "Harga produk harus lebih dari 0." });

    product.Id        = 0;
    product.CreatedAt = DateTime.UtcNow;

    _db.Products.Add(product);
    await _db.SaveChangesAsync();

    return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
}
```

| Baris | Arti |
|---|---|
| `[HttpPost]` | Method ini menangani HTTP POST ke `/api/products` (tanpa ID, karena data baru) |
| `[Authorize(Roles = "Admin")]` | Hanya Admin yang bisa tambah produk |
| `[FromBody] Product product` | Data produk diambil dari **request body** (JSON). ASP.NET Core deserialize JSON → object `Product` |
| `string.IsNullOrWhiteSpace(product.Name)` | Validasi: cek null, string kosong `""`, atau hanya spasi `"   "`. Lebih robust dari hanya `== null` atau `== ""` |
| `return BadRequest(new { message = "..." })` | HTTP `400 Bad Request` — client mengirim data tidak valid |
| `product.Id = 0` | Reset ID ke 0 agar tidak conflict dengan ID yang ada. Database yang generate ID baru sendiri (SERIAL/IDENTITY). Ini mencegah bug jika client sengaja/tidak sengaja kirim ID |
| `product.CreatedAt = DateTime.UtcNow` | Set timestamp server-side, bukan percaya timestamp dari client (client bisa manipulasi waktu) |
| `_db.Products.Add(product)` | **Stage / Track** entity untuk di-INSERT. Ini belum eksekusi SQL — hanya menandai ke EF Core Change Tracker |
| `await _db.SaveChangesAsync()` | **Commit** — baru di sini SQL `INSERT INTO "Products" VALUES (...)` dieksekusi ke database. Setelah ini, `product.Id` terisi dengan ID yang di-generate database |
| `CreatedAtAction(nameof(GetById), new { id = product.Id }, product)` | HTTP `201 Created`. **3 argumen:** (1) nama action untuk generate Location URL, (2) route values, (3) body response. Response juga menyertakan header `Location: /api/products/99` |
| `nameof(GetById)` | Gunakan `nameof` daripada string literal `"GetById"` — aman dari typo dan refactor-proof |

---

#### `Update` — PUT /api/products/{id}

```csharp
[HttpPut("{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Update(int id, [FromBody] Product updated)
{
    var product = await _db.Products.FindAsync(id);
    if (product == null)
        return NotFound(new { message = $"Produk dengan ID {id} tidak ditemukan." });

    product.Name        = updated.Name;
    product.Description = updated.Description;
    product.Price       = updated.Price;
    product.Stock       = updated.Stock;
    product.Category    = updated.Category;

    await _db.SaveChangesAsync();
    return Ok(product);
}
```

| Baris | Arti |
|---|---|
| `[HttpPut("{id}")]` | Menangani PUT `/api/products/5`. ID produk yang diupdate ada di URL |
| `int id, [FromBody] Product updated` | **Dua parameter:** `id` dari URL path, `updated` dari request body. Inilah kenapa kita perlu ID di URL — kalau hanya dari body, bisa ambiguous |
| `FindAsync(id)` | Ambil entitas yang **sedang di-track** EF Core dari database |
| **Pola "Fetch then Update"** | Fetch entitas dulu → update field → SaveChanges. EF Core Change Tracker otomatis deteksi perubahan dan generate `UPDATE ... SET ... WHERE Id = ?` yang tepat (hanya field yang berubah) |
| `product.Name = updated.Name;` | Assign satu per satu (tidak gunakan object assignment). Ini penting untuk kontrol — tidak semua field boleh diupdate |
| **Field yang TIDAK diupdate** | `product.Id` dan `product.CreatedAt` — ID tidak boleh berubah, CreatedAt adalah timestamp awal yang harus dipertahankan |
| `await _db.SaveChangesAsync()` | Commit — eksekusi `UPDATE "Products" SET Name=?, Description=?, Price=?, Stock=?, Category=? WHERE "Id"=?` |
| `return Ok(product)` | Return data produk yang **sudah terupdate** (bukan data lama dari `updated`) |

---

#### `Delete` — DELETE /api/products/{id}

```csharp
[HttpDelete("{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Delete(int id)
{
    var product = await _db.Products.FindAsync(id);
    if (product == null)
        return NotFound(new { message = $"Produk dengan ID {id} tidak ditemukan." });

    _db.Products.Remove(product);
    await _db.SaveChangesAsync();

    return Ok(new { message = $"Produk '{product.Name}' berhasil dihapus." });
}
```

| Baris | Arti |
|---|---|
| `[HttpDelete("{id}")]` | Menangani DELETE `/api/products/5` |
| `FindAsync(id)` | Fetch dulu — double check data ada sebelum dihapus, dan dapatkan entity yang perlu di-remove |
| `_db.Products.Remove(product)` | **Stage** entity untuk di-DELETE. Belum eksekusi SQL |
| `await _db.SaveChangesAsync()` | Commit — eksekusi `DELETE FROM "Products" WHERE "Id"=?` |
| `return Ok(new { message = $"Produk '{product.Name}' berhasil dihapus." })` | HTTP `200 OK` dengan pesan konfirmasi. Nama produk disertakan agar user tahu produk mana yang terhapus |

> **Alternatif:** `return NoContent()` (HTTP 204) juga valid untuk DELETE, bahkan lebih "RESTful". Pilih sesuai kebutuhan — 204 tidak ada body, 200 bisa pakai untuk kirim pesan konfirmasi.

---

## 🗺️ Alur Flow Lengkap: POST /api/products

```
Client
  │
  │  POST /api/products
  │  Authorization: Bearer eyJhb...
  │  Content-Type: application/json
  │  Body: {"name":"MacBook","price":25000000,"category":"Electronics"}
  │
  ▼
ASP.NET Core Pipeline
  │
  ├─ 1. Routing Middleware       → Cocokkan URL ke ProductsController.Create
  ├─ 2. Auth Middleware          → Validasi JWT token di header
  │     ├─ Token valid?         → Lanjut
  │     └─ Token tidak valid?   → 401 Unauthorized (STOP)
  ├─ 3. Authorization Middleware → Cek role "Admin"
  │     ├─ Role Admin?          → Lanjut
  │     └─ Role bukan Admin?    → 403 Forbidden (STOP)
  ├─ 4. Rate Limit Check        → Apakah quota habis?
  │     ├─ Masih ada quota      → Lanjut
  │     └─ Quota habis          → 429 Too Many Requests (STOP)
  ├─ 5. Model Binding           → Parse JSON body → object Product
  ├─ 6. Model Validation        → Cek [Required], [Range], dll.
  │     ├─ Valid                → Lanjut
  │     └─ Tidak valid          → 400 Bad Request (STOP) [jika ada annotations]
  │
  ▼
ProductsController.Create(product) — Kode kita dieksekusi
  │
  ├─ Validasi manual            → Cek IsNullOrWhiteSpace, Price > 0
  ├─ Set Id = 0, CreatedAt = Now
  ├─ _db.Products.Add(product)  → Stage untuk INSERT
  ├─ await SaveChangesAsync()   → Eksekusi INSERT ke PostgreSQL
  │
  ▼
return CreatedAtAction(...)
  │
  ▼
ASP.NET Core serialize → JSON

Client menerima:
  HTTP 201 Created
  Location: /api/products/42
  Content-Type: application/json
  {
    "id": 42,
    "name": "MacBook",
    "price": 25000000,
    "category": "Electronics",
    "createdAt": "2026-04-10T01:30:00Z"
  }
```

---

## 📋 Ringkasan — Setiap Kata Kunci

| Keyword/Attribute | Kategori | Fungsi Singkat |
|---|---|---|
| `[ApiController]` | Class attribute | Aktifkan fitur Web API otomatis |
| `[Route("api/[controller]")]` | Class attribute | Tentukan base URL controller |
| `[Authorize]` | Class/Method attribute | Wajibkan JWT token |
| `[Authorize(Roles="Admin")]` | Method attribute | Batasi ke role tertentu |
| `[AllowAnonymous]` | Method attribute | Override Authorize, bebas akses |
| `[EnableRateLimiting("x")]` | Method attribute | Batasi jumlah request |
| `[HttpGet]` / `[HttpPost]` / dll. | Method attribute | Mapping ke HTTP method + sub-route |
| `[FromBody]` | Parameter attribute | Ambil data dari request body |
| `[FromRoute]` | Parameter attribute | Ambil data dari URL path |
| `[FromQuery]` | Parameter attribute | Ambil data dari query string |
| `ControllerBase` | Base class | Sediakan helper methods (`Ok()`, dll.) |
| `IActionResult` | Return type | Fleksibel: bisa return berbagai HTTP response |
| `Task<IActionResult>` | Return type | Versi async dari IActionResult |
| `Ok(data)` | Helper method | Return HTTP 200 + data JSON |
| `NotFound(data)` | Helper method | Return HTTP 404 |
| `BadRequest(data)` | Helper method | Return HTTP 400 |
| `CreatedAtAction(...)` | Helper method | Return HTTP 201 + Location header |
| `NoContent()` | Helper method | Return HTTP 204, tanpa body |
| `Unauthorized(data)` | Helper method | Return HTTP 401 |
| `FindAsync(id)` | EF Core method | Query by primary key (dengan cache) |
| `ToListAsync()` | EF Core method | Eksekusi query, return List |
| `FirstOrDefaultAsync(...)` | EF Core method | Ambil satu data, null jika tidak ada |
| `AnyAsync(...)` | EF Core method | Cek keberadaan, return bool |
| `Add(entity)` | EF Core method | Stage entity untuk INSERT |
| `Remove(entity)` | EF Core method | Stage entity untuk DELETE |
| `SaveChangesAsync()` | EF Core method | Commit semua perubahan ke DB |
| `private readonly` | C# keyword combo | Field yang hanya bisa di-set di constructor |
| `async Task<T>` | C# async pattern | Method yang bisa di-await |
| `await` | C# keyword | Tunggu operasi async selesai tanpa blokir thread |
| `nameof(GetById)` | C# keyword | Ambil nama method sebagai string, refactor-safe |
| `DateTime.UtcNow` | .NET property | Waktu sekarang dalam UTC |
| `string.IsNullOrWhiteSpace()` | .NET method | Cek null, empty, dan whitespace |
