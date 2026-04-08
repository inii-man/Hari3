namespace ProductCatalogAPI.Models;

/// <summary>
/// Entity yang merepresentasikan satu baris di tabel "Products" di database.
/// EF Core akan memetakan setiap properti menjadi kolom tabel secara otomatis.
/// </summary>
public class Product
{
    /// <summary>Primary key — nilai di-generate otomatis oleh PostgreSQL (SERIAL/IDENTITY).</summary>
    public int Id { get; set; }

    /// <summary>Nama produk. Tidak boleh kosong (divalidasi di controller).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Deskripsi lengkap produk.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Harga produk. Menggunakan <c>decimal</c> (bukan float/double)
    /// untuk menghindari kesalahan pembulatan pada nilai keuangan.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>Jumlah stok yang tersedia.</summary>
    public int Stock { get; set; }

    /// <summary>Kategori produk, misal: "Electronics", "Accessories".</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Waktu produk dibuat, disimpan dalam UTC agar konsisten di berbagai zona waktu.
    /// Diset otomatis saat controller memanggil Create().
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
