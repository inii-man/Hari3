using System;

namespace ProductCatalogAPI.Domain.Entities;

/// <summary>
/// Representasi "Rich Domain Model" untuk Produk.
/// Perhatikan bahwa properti memiliki 'private set' untuk memastikan state hanya bisa diubah melalui method (Encapsulation).
/// </summary>
public class Product
{
    // Id tetap publik untuk kebutuhan EF Core, tapi biasanya di-set oleh DB atau Konstruktor.
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Konstruktor kosong untuk EF Core (wajib ada jika menggunakan konstruktor berparameter).
    private Product() { }

    /// <summary>
    /// Konstruktor utama untuk membuat Produk baru. 
    /// Validasi dilakukan langsung di sini (Invariant check).
    /// </summary>
    public Product(string name, string description, decimal price, int stock, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nama produk tidak boleh kosong.", nameof(name));
        
        if (price < 0)
            throw new ArgumentException("Harga tidak boleh negatif.", nameof(price));

        if (stock < 0)
            throw new ArgumentException("Stok tidak boleh negatif.", nameof(stock));

        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        Category = category;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Method untuk memperbarui detail produk. 
    /// Bisnis logic diletakkan di sini, bukan di Service atau Controller.
    /// </summary>
    public void UpdateDetails(string name, string description, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nama produk tidak boleh kosong.");

        Name = name;
        Description = description;
        Category = category;
    }

    /// <summary>
    /// Method khusus untuk mengubah harga dengan validasi domain.
    /// </summary>
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Harga baru tidak boleh negatif.");

        Price = newPrice;
    }

    /// <summary>
    /// Method untuk mengatur stok. Bisa ditambahkan logic jika stok habis, dll.
    /// </summary>
    public void UpdateStock(int newStock)
    {
        if (newStock < 0)
            throw new ArgumentException("Jumlah stok tidak boleh negatif.");

        Stock = newStock;
    }
}
