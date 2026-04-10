# 🎮 ASP.NET Core Controller — Cheat Sheet Lengkap

> Referensi cepat untuk membangun controller yang bersih, aman, dan siap produksi.
> Semua contoh menggunakan konteks **Product Catalog API**.

---

## 📦 1. Struktur Dasar Controller

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Data;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Controllers;

[ApiController]                      // ⭐ WAJIB untuk Web API
[Route("api/[controller]")]          // ⭐ Route otomatis dari nama class
[Authorize]                          // Semua endpoint butuh JWT (bisa di-override per-method)
public class ProductsController : ControllerBase  // ⭐ ControllerBase, bukan Controller!
{
    private readonly AppDbContext _db;             // Dependency Injection field

    public ProductsController(AppDbContext db)     // Constructor Injection
    {
        _db = db;
    }
}
```

### Kenapa `ControllerBase` bukan `Controller`?

| | `ControllerBase` | `Controller` |
|---|---|---|
| **Untuk** | Web API (JSON response) | MVC (HTML Views) |
| **View support** | ❌ Tidak ada | ✅ Ada (`View()`, `Partial()`, dll.) |
| **Ukuran / Berat** | Lebih ringan | Lebih berat |
| **Rekomendasi untuk API** | ✅ Gunakan ini | ❌ Jangan |

---

## 🏷️ 2. Class-Level Attributes

### `[ApiController]`
```csharp
[ApiController]
```
**Apa:** Mengaktifkan fitur bawaan khusus Web API.

**Kapan:** SELALU pasang di **setiap** API controller.

**Manfaat otomatis yang aktif:**
- ✅ Jika `ModelState` tidak valid → langsung return `400 Bad Request` (tanpa tulis manual)
- ✅ `[FromBody]` di-infer otomatis untuk parameter objek kompleks
- ✅ Format error response menggunakan standar `ProblemDetails` (RFC 7807)
- ✅ Binding source inference — tidak perlu tulis `[FromRoute]`, `[FromQuery]` di setiap parameter

---

### `[Route("api/[controller]")]`
```csharp
[Route("api/[controller]")]          // → /api/products
[Route("api/v1/[controller]")]       // → /api/v1/products  (versioning)
[Route("api/products")]              // Hard-coded (hindari — tidak fleksibel)
```
**Apa:** Base URL untuk semua endpoint di class ini.

**`[controller]`** adalah placeholder yang otomatis diganti dengan nama class **tanpa** kata "Controller":
- `ProductsController` → `products`
- `AuthController`     → `auth`

---

### `[Authorize]`
```csharp
[Authorize]                          // Wajib login (JWT valid, semua role)
[Authorize(Roles = "Admin")]         // Hanya role Admin
[Authorize(Roles = "Admin,User")]    // Admin ATAU User
[Authorize(Policy = "MinAge18")]     // Custom policy (didefinisikan di Program.cs)
```
**Kapan di class level:** Kalau mayoritas endpoint perlu auth, lalu override dengan `[AllowAnonymous]` untuk yang publik.

**Kapan di method level:** Kalau hanya beberapa endpoint yang perlu auth.

---

## 🔗 3. HTTP Method Attributes

```csharp
[HttpGet]                  // GET /api/products          → Ambil semua
[HttpGet("{id}")]          // GET /api/products/5        → Ambil by ID
[HttpGet("search")]        // GET /api/products/search   → Endpoint khusus search
[HttpGet("{id}/reviews")]  // GET /api/products/5/reviews → Sub-resource

[HttpPost]                 // POST /api/products         → Tambah baru
[HttpPost("bulk")]         // POST /api/products/bulk    → Tambah banyak sekaligus

[HttpPut("{id}")]          // PUT /api/products/5        → Update SEMUA field
[HttpPatch("{id}")]        // PATCH /api/products/5      → Update SEBAGIAN field

[HttpDelete("{id}")]       // DELETE /api/products/5     → Hapus
```

> **PUT vs PATCH:**
> - `PUT` → Kirim **semua** field. Field yang tidak dikirim akan jadi null/default.
> - `PATCH` → Kirim **hanya field yang berubah**. Lebih efisien untuk update sebagian.

---

## 📥 4. Parameter Binding — Sumber Data Request

```csharp
// Dari URL path segment
public IActionResult GetById([FromRoute] int id)
// URL: GET /api/products/5  →  id = 5
// Catatan: [FromRoute] bisa dihilangkan jika nama param = nama di route template

// Dari query string
public IActionResult Search([FromQuery] string? name, [FromQuery] int page = 1)
// URL: GET /api/products?name=iphone&page=2

// Dari request body (JSON)
public IActionResult Create([FromBody] Product product)
// Body: {"name":"iPhone","price":15000000}

// Dari HTTP header
public IActionResult Action([FromHeader(Name = "X-Api-Key")] string apiKey)

// Dari form data (file upload)
public IActionResult Upload([FromForm] IFormFile file)
```

> 💡 Dengan `[ApiController]`, parameter tipe kompleks otomatis dari `[FromBody]`, parameter tipe primitif dari route/query. Jadi sering tidak perlu tulis atributnya secara eksplisit.

---

## 📤 5. Return Types — Response Helper Methods

### `IActionResult` — Paling Fleksibel (Direkomendasikan)

```csharp
return Ok(product);                // 200 OK + data JSON
return Ok(new { message = "Berhasil", data = product }); // 200 + wrapper

return Created("url", product);    // 201 Created + Location header
return CreatedAtAction(nameof(GetById), new { id = product.Id }, product); // 201 (best practice)

return BadRequest("Input salah");  // 400 Bad Request
return BadRequest(new { message = "Harga harus > 0" }); // 400 + pesan JSON

return Unauthorized();             // 401 Unauthorized
return Unauthorized(new { message = "Token tidak valid" });

return Forbid();                   // 403 Forbidden

return NotFound();                 // 404 Not Found
return NotFound(new { message = $"Produk ID {id} tidak ditemukan" }); // 404 + pesan

return Conflict(new { message = "Data sudah ada" });      // 409 Conflict

return NoContent();                // 204 No Content (ideal untuk DELETE berhasil)

return StatusCode(503, new { message = "Service unavailable" }); // Custom status code
```

### `ActionResult<T>` — Type-Safe + Swagger Friendly

```csharp
public async Task<ActionResult<Product>> GetById(int id)
{
    var product = await _db.Products.FindAsync(id);
    if (product == null) return NotFound();
    return product; // Implicit → 200 OK + JSON
}

public async Task<ActionResult<List<Product>>> GetAll()
{
    return await _db.Products.ToListAsync(); // Implicit → 200 OK + JSON array
}
```

> **Kapan pakai apa?**
> - `IActionResult` → Saat return type bisa bermacam-macam (misal `Product` atau anonymous object)
> - `ActionResult<T>` → Saat return type jelas dan ingin Swagger auto-generate response schema

---

## ✅ 6. Validasi Input

### Cara 1: Manual Check (Explicit)

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] Product product)
{
    if (string.IsNullOrWhiteSpace(product.Name))
        return BadRequest(new { message = "Nama produk tidak boleh kosong." });

    if (product.Price <= 0)
        return BadRequest(new { message = "Harga produk harus lebih dari 0." });

    // lanjut proses...
}
```
**Kapan:** Validasi bisnis yang tidak bisa direpresentasikan oleh Data Annotations.

---

### Cara 2: Data Annotations di Model (Otomatis)

```csharp
// Model/Product.cs
public class Product
{
    [Required(ErrorMessage = "Nama wajib diisi")]
    [MaxLength(100, ErrorMessage = "Nama maksimal 100 karakter")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Harga harus > 0")]
    public decimal Price { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

// Controller — TIDAK perlu cek manual lagi!
// [ApiController] otomatis return 400 jika ada annotation yang gagal
[HttpPost]
public async Task<IActionResult> Create([FromBody] Product product)
{
    // Jika masuk ke sini, validasi sudah pasti lolos
    _db.Products.Add(product);
    await _db.SaveChangesAsync();
    return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
}
```

### Data Annotations Referensi Cepat

| Annotation | Kegunaan |
|---|---|
| `[Required]` | Field wajib diisi, tidak boleh null |
| `[MaxLength(n)]` | Panjang string maksimal n karakter |
| `[MinLength(n)]` | Panjang string minimal n karakter |
| `[StringLength(max, MinimumLength=min)]` | Panjang antara min dan max |
| `[Range(min, max)]` | Nilai numerik dalam rentang tertentu |
| `[EmailAddress]` | Format email valid |
| `[Phone]` | Format nomor telepon |
| `[Url]` | Format URL valid |
| `[RegularExpression("pattern")]` | Validasi dengan regex custom |
| `[Compare("OtherField")]` | Nilai harus sama dengan field lain |

---

## 🔐 7. Authorization — Kontrol Akses

```csharp
// Semua endpoint di class → Wajib JWT
[Authorize]
public class ProductsController : ControllerBase
{
    // ① Siapapun yang login bisa akses (inherit dari class)
    [HttpGet]
    public async Task<IActionResult> GetAll() { ... }

    // ② Override: hanya Admin
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create() { ... }

    // ③ Override: Admin ATAU User
    [Authorize(Roles = "Admin,User")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id) { ... }

    // ④ Override: Bebas akses (tidak perlu login)
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> GetPublic() { ... }
}
```

### Baca Identitas User dari JWT dalam Method

```csharp
[Authorize]
[HttpGet("me")]
public IActionResult GetMyInfo()
{
    var userId   = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // ID user
    var username = User.Identity?.Name;                               // Username
    var role     = User.FindFirst(ClaimTypes.Role)?.Value;           // Role
    var isAdmin  = User.IsInRole("Admin");                           // Boolean cek role

    return Ok(new { userId, username, role, isAdmin });
}
```

---

## ⚡ 8. Async / Await — WAJIB untuk Operasi I/O

```csharp
// ❌ JANGAN — synchronous memblokir thread server
[HttpGet]
public IActionResult GetAll()
{
    var products = _db.Products.ToList(); // Thread tertahan nunggu DB!
    return Ok(products);
}

// ✅ SELALU — async membebaskan thread saat menunggu
[HttpGet]
public async Task<IActionResult> GetAll()
{
    var products = await _db.Products.ToListAsync(); // Thread bebas saat DB query
    return Ok(products);
}
```

> **Kenapa async penting?**
> Saat query ke database berjalan (bisa 10-500ms), thread yang sync ditahan — tidak bisa melayani request lain.
> Dengan async, thread dibebaskan dan bisa melayani request lain selama menunggu — throughput jauh lebih tinggi.

---

## 📊 9. Query EF Core yang Sering Dipakai

```csharp
// Ambil semua
await _db.Products.ToListAsync();

// Cari by primary key (PALING CEPAT — cek memory cache dulu)
await _db.Products.FindAsync(id);

// Cari berdasarkan kondisi (return null jika tidak ada)
await _db.Products.FirstOrDefaultAsync(p => p.Name == "iPhone");

// Filtering
await _db.Products
    .Where(p => p.Category == "Electronics" && p.Price < 5_000_000)
    .ToListAsync();

// Sorting
await _db.Products.OrderBy(p => p.Price).ToListAsync();           // ASC
await _db.Products.OrderByDescending(p => p.CreatedAt).ToListAsync(); // DESC

// Projection — pilih field tertentu (lebih efisien dari SELECT *)
await _db.Products
    .Select(p => new { p.Id, p.Name, p.Price })
    .ToListAsync();

// Pagination
await _db.Products
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// Cek keberadaan (lebih efisien dari FirstOrDefault untuk pure boolean check)
await _db.Products.AnyAsync(p => p.Name == "iPhone");

// Hitung total
await _db.Products.CountAsync();
await _db.Products.CountAsync(p => p.Category == "Electronics"); // Dengan filter

// Kombinasi lengkap: filter + sort + pagination + projection
await _db.Products
    .Where(p => p.Category == category)
    .OrderByDescending(p => p.CreatedAt)
    .Skip((page - 1) * size)
    .Take(size)
    .Select(p => new { p.Id, p.Name, p.Price, p.Category })
    .ToListAsync();
```

---

## 🛡️ 10. Rate Limiting

```csharp
using Microsoft.AspNetCore.RateLimiting;

[EnableRateLimiting("general")]      // Gunakan policy "general" (100 req/menit)
[HttpGet]
public async Task<IActionResult> GetAll() { ... }

[EnableRateLimiting("auth_policy")]  // Policy lebih ketat untuk auth (50 req/15 menit)
[HttpPost("login")]
public async Task<IActionResult> Login() { ... }

[DisableRateLimiting]                // Matikan rate limit untuk endpoint ini
[HttpGet("health")]
public IActionResult Health() { ... }
```

> **Policy didefinisikan di `Program.cs`:**
> ```csharp
> builder.Services.AddRateLimiter(options => {
>     options.AddFixedWindowLimiter("general", opt => {
>         opt.Window = TimeSpan.FromMinutes(1);
>         opt.PermitLimit = 100;
>     });
>     options.AddFixedWindowLimiter("auth_policy", opt => {
>         opt.Window = TimeSpan.FromMinutes(15);
>         opt.PermitLimit = 50;
>     });
> });
> ```

---

## 📝 11. DTO (Data Transfer Object)

```csharp
// ⚠️ MASALAH: Expose entity langsung → rawan over-posting / mass-assignment attack
[HttpPost]
public async Task<IActionResult> Create([FromBody] Product product) // Bahaya!
// User bisa kirim field seperti "Id", "CreatedAt" atau field sensitif lain!

// ✅ SOLUSI: Pakai DTO terpisah untuk input dan output
public class CreateProductDto           // Input DTO
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    public string? Category { get; set; }
    public string? Description { get; set; }
}

public class ProductResponseDto         // Output DTO (kontrol apa yang tampil)
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    // TIDAK include field sensitif apapun
}
```

---

## 🔄 12. Pola CRUD Lengkap — Best Practice

```csharp
// ── CREATE ─────────────────────────────────────────────────────────────────
[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
{
    var product = new Product {
        Name        = dto.Name,
        Description = dto.Description ?? string.Empty,
        Price       = dto.Price,
        Category    = dto.Category ?? string.Empty,
        CreatedAt   = DateTime.UtcNow  // Server-side timestamp, bukan dari client
    };

    _db.Products.Add(product);
    await _db.SaveChangesAsync();

    return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    // → 201 Created + header: Location: /api/products/{id}
}

// ── READ ALL (dengan filter & pagination) ──────────────────────────────────
[HttpGet]
[Authorize(Roles = "Admin,User")]
public async Task<IActionResult> GetAll(
    [FromQuery] string? category,
    [FromQuery] string? search,
    [FromQuery] int page = 1,
    [FromQuery] int size = 10)
{
    var query = _db.Products.AsQueryable();

    if (!string.IsNullOrEmpty(category))
        query = query.Where(p => p.Category == category);

    if (!string.IsNullOrEmpty(search))
        query = query.Where(p => p.Name.Contains(search));

    var total = await query.CountAsync();
    var data  = await query
        .OrderByDescending(p => p.CreatedAt)
        .Skip((page - 1) * size)
        .Take(size)
        .ToListAsync();

    return Ok(new { total, page, size, data });
}

// ── READ ONE ───────────────────────────────────────────────────────────────
[HttpGet("{id}")]
[Authorize(Roles = "Admin,User")]
public async Task<IActionResult> GetById(int id)
{
    var product = await _db.Products.FindAsync(id);
    if (product == null)
        return NotFound(new { message = $"Produk ID {id} tidak ditemukan." });

    return Ok(product);
}

// ── UPDATE ─────────────────────────────────────────────────────────────────
[HttpPut("{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
{
    var product = await _db.Products.FindAsync(id);
    if (product == null)
        return NotFound(new { message = $"Produk ID {id} tidak ditemukan." });

    product.Name        = dto.Name ?? product.Name;        // Null-safe update
    product.Price       = dto.Price ?? product.Price;
    product.Category    = dto.Category ?? product.Category;
    product.Description = dto.Description ?? product.Description;
    // JANGAN update product.Id dan product.CreatedAt !

    await _db.SaveChangesAsync();
    return Ok(product);
}

// ── DELETE ─────────────────────────────────────────────────────────────────
[HttpDelete("{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Delete(int id)
{
    var product = await _db.Products.FindAsync(id);
    if (product == null)
        return NotFound(new { message = $"Produk ID {id} tidak ditemukan." });

    _db.Products.Remove(product);
    await _db.SaveChangesAsync();

    return NoContent(); // 204 — Standar REST untuk delete sukses (tidak perlu body)
}
```

---

## 🚨 13. Error Handling

### Cara 1: Try-Catch di Controller (per-endpoint)
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
        // Contoh: constraint violation (unique key, foreign key, dll.)
        return StatusCode(500, new { message = "Gagal menyimpan ke database.", detail = ex.Message });
    }
}
```

### Cara 2: Global Exception Handler di `Program.cs` (Lebih Clean)
```csharp
app.UseExceptionHandler(appError => {
    appError.Run(async context => {
        context.Response.StatusCode  = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new {
            message = "Terjadi kesalahan server. Coba lagi nanti."
        });
    });
});
```
> Gunakan cara 2 untuk tangkap semua exception yang tidak tertangani, kombinasikan dengan cara 1 untuk error spesifik yang butuh handling khusus.

---

## 📚 14. XML Doc Comments (untuk Swagger)

```csharp
/// <summary>
/// Tambah produk baru. Hanya bisa diakses oleh Admin.
/// </summary>
/// <param name="product">Data produk dari request body (JSON).</param>
/// <returns>Produk yang baru dibuat, lengkap dengan ID dari database.</returns>
/// <response code="201">Produk berhasil dibuat.</response>
/// <response code="400">Data input tidak valid.</response>
/// <response code="401">Tidak ada atau token JWT tidak valid.</response>
/// <response code="403">Bukan role Admin.</response>
[HttpPost]
[ProducesResponseType(typeof(Product), 201)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(403)]
public async Task<IActionResult> Create([FromBody] Product product) { ... }
```

---

## ⚡ 15. Quick Reference — HTTP Status Codes

| Method | Success | Common Errors |
|---|---|---|
| `GET` | `200 OK` | `404 Not Found` |
| `POST` | `201 Created` | `400 Bad Request`, `409 Conflict` |
| `PUT / PATCH` | `200 OK` | `400 Bad Request`, `404 Not Found` |
| `DELETE` | `204 No Content` | `404 Not Found` |
| Auth gagal | — | `401 Unauthorized` |
| Role salah | — | `403 Forbidden` |
| Server error | — | `500 Internal Server Error` |

---

## 🏆 16. Checklist Controller yang Bagus

- [ ] Class punya `[ApiController]` dan `[Route("api/[controller]")]`
- [ ] Warisi dari `ControllerBase` (bukan `Controller`)
- [ ] Semua method pakai `async Task<IActionResult>`
- [ ] Validasi input di awal, lalu return 400 dengan pesan jelas
- [ ] `[Authorize]` dengan role yang tepat di setiap endpoint
- [ ] Gunakan `FindAsync` untuk cari by primary key
- [ ] `CreatedAtAction` untuk response POST (HTTP 201)
- [ ] `NoContent` (204) untuk response DELETE
- [ ] Pakai DTO untuk input — jangan expose entity langsung
- [ ] Semua field sensitif tidak ikut di response
- [ ] XML doc comments agar Swagger terdokumentasi
- [ ] Pisahkan logika bisnis kompleks ke Service class
