using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Data;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Controllers;

/// <summary>
/// Controller untuk operasi CRUD produk.
/// Semua endpoint memerlukan autentikasi JWT (kecuali /public yang [AllowAnonymous]).
/// Role "Admin" diperlukan untuk operasi create, update, dan delete.
/// Route dasar: /api/products
/// </summary>
[Authorize]                          // Default: semua method di controller ini wajib JWT token
[ApiController]                      // Aktifkan fitur: model binding otomatis, validasi, dll.
[Route("api/[controller]")]          // Route dinamis: [controller] → "Products" → /api/products
public class ProductsController : ControllerBase
{
    // AppDbContext di-inject melalui constructor (Dependency Injection)
    private readonly AppDbContext _db;

    /// <summary>
    /// Constructor menerima AppDbContext dari DI container.
    /// </summary>
    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    // ─── GET /api/products ─────────────────────────────────────────────────────

    /// <summary>
    /// Ambil semua produk beserta data lengkap. Dapat diakses oleh Admin maupun User.
    /// </summary>
    [HttpGet]                         // Mapping ke HTTP GET /api/products
    [Authorize(Roles = "Admin,User")] // Override: hanya role Admin atau User yang bisa akses
    [EnableRateLimiting("general")]   // Terapkan rate limit: maks 100 request/menit
    public async Task<IActionResult> GetAll()
    {
        // ToListAsync(): query SELECT * FROM Products, hasilnya di-load ke memory
        var products = await _db.Products.ToListAsync();
        return Ok(products); // HTTP 200 + JSON array semua produk
    }

    // ─── GET /api/products/public ──────────────────────────────────────────────

    /// <summary>
    /// Endpoint publik — tidak perlu login. Mengembalikan daftar produk dengan
    /// field terbatas (Id, Name, Category, Price) sebagai preview untuk pengunjung.
    /// </summary>
    [AllowAnonymous]                  // Override [Authorize] di class level — endpoint ini bebas akses
    [HttpGet("public")]               // Route: GET /api/products/public
    [EnableRateLimiting("general")]
    public async Task<IActionResult> GetPublic()
    {
        // Projection query: pilih hanya kolom tertentu → lebih efisien dari SELECT *
        // Hasil adalah anonymous type, tidak perlu class tambahan
        var products = await _db.Products
            .Select(p => new { p.Id, p.Name, p.Category, p.Price })
            .ToListAsync();

        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(products));

        return Ok(new
        {
            message = "Data publik — login untuk melihat detail lengkap.",
            total = products.Count,  // total jumlah produk
            data = products
        });
    }

    // ─── GET /api/products/{id} ────────────────────────────────────────────────

    /// <summary>
    /// Ambil detail satu produk berdasarkan ID. Dapat diakses oleh Admin maupun User.
    /// </summary>
    /// <param name="id">ID produk yang ingin diambil (dari URL path).</param>
    [HttpGet("{id}")]                 // Route: GET /api/products/1 → id = 1
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetById(int id)
    {
        // FindAsync() mencari berdasarkan primary key — dioptimasi oleh EF Core
        // Lebih cepat dari FirstOrDefaultAsync(p => p.Id == id) karena cek first-level cache
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = $"Produk dengan ID {id} tidak ditemukan." }); // HTTP 404

        return Ok(product); // HTTP 200 + JSON object produk
    }

    // ─── POST /api/products ────────────────────────────────────────────────────

    /// <summary>
    /// Tambah produk baru ke database. HANYA dapat diakses oleh Admin.
    /// </summary>
    /// <param name="product">Data produk baru dari request body (JSON).</param>
    [HttpPost]                        // Route: POST /api/products
    [Authorize(Roles = "Admin")]      // Hanya Admin yang bisa tambah produk
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        // ── Validasi input ─────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(product.Name))
            return BadRequest(new { message = "Nama produk tidak boleh kosong." });

        if (product.Price <= 0)
            return BadRequest(new { message = "Harga produk harus lebih dari 0." });

        // Reset Id ke 0 agar database yang generate (hindari konflik/override ID eksisting)
        product.Id = 0;
        // Set timestamp pembuatan ke waktu sekarang (UTC)
        product.CreatedAt = DateTime.UtcNow;

        _db.Products.Add(product);    // Stage: tandai entity untuk di-INSERT
        await _db.SaveChangesAsync(); // Commit: eksekusi INSERT INTO Products ke database

        // CreatedAtAction: HTTP 201 Created + header Location: /api/products/{id-baru}
        // Best practice REST: response 201 untuk resource yang baru dibuat
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // ─── PUT /api/products/{id} ────────────────────────────────────────────────

    /// <summary>
    /// Update data produk yang sudah ada. HANYA dapat diakses oleh Admin.
    /// Semua field (Name, Description, Price, Stock, Category) akan di-replace dengan nilai baru.
    /// </summary>
    /// <param name="id">ID produk yang akan di-update (dari URL path).</param>
    /// <param name="updated">Data baru dari request body (JSON).</param>
    [HttpPut("{id}")]                 // Route: PUT /api/products/1
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Product updated)
    {
        // ── Fetch entity yang existing ─────────────────────────────────────
        // Pola "fetch then update" → EF Core Change Tracking secara otomatis
        // akan mendeteksi perubahan dan generate UPDATE SQL yang tepat
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = $"Produk dengan ID {id} tidak ditemukan." });

        // ── Update hanya field yang relevan ────────────────────────────────
        // CreatedAt TIDAK diupdate — timestamp awal tetap dipertahankan
        product.Name        = updated.Name;
        product.Description = updated.Description;
        product.Price       = updated.Price;
        product.Stock       = updated.Stock;
        product.Category    = updated.Category;

        await _db.SaveChangesAsync(); // Commit: eksekusi UPDATE Products SET ... WHERE Id = ?
        return Ok(product);           // HTTP 200 + data produk yang sudah diupdate
    }

    // ─── DELETE /api/products/{id} ─────────────────────────────────────────────

    /// <summary>
    /// Hapus produk dari database secara permanen. HANYA dapat diakses oleh Admin.
    /// </summary>
    /// <param name="id">ID produk yang akan dihapus (dari URL path).</param>
    [HttpDelete("{id}")]              // Route: DELETE /api/products/1
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = $"Produk dengan ID {id} tidak ditemukan." });

        _db.Products.Remove(product); // Stage: tandai entity untuk di-DELETE
        await _db.SaveChangesAsync(); // Commit: eksekusi DELETE FROM Products WHERE Id = ?

        // HTTP 200 dengan pesan konfirmasi (nama produk disertakan untuk clarity)
        return Ok(new { message = $"Produk '{product.Name}' berhasil dihapus." });
    }
}
