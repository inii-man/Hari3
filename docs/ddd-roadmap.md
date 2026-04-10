# Roadmap Pengembangan Menuju Domain-Driven Design (DDD) Penuh

Dokumen ini berisi panduan langkah demi langkah (roadmap) untuk mentransformasi proyek **ProductCatalogAPI** dari arsitektur CRUD tradisional menjadi implementasi DDD yang matang.

---

## Tahap 1: Restrukturisasi Solusi (Architecture Layers)

Pindahkan sistem dari satu project tunggal menjadi struktur multi-layer (biasanya menggunakan *Clean Architecture*):

1.  **Domain Layer (Pusat)**:
    - Tidak boleh ada dependensi ke layer lain.
    - Berisi: `Entities`, `Value Objects`, `Domain Services`, dan `Repository Interfaces`.
2.  **Application Layer**:
    - Dependensi hanya ke Domain Layer.
    - Berisi: `Application Services` (Use Cases), `DTOs`, dan `Mappings` (AutoMapper).
3.  **Infrastructure Layer**:
    - Implementasi detail teknis.
    - Berisi: `Data/AppDbContext`, `Repositories Implementation`, `Services` (Email, Cloud Storage).
4.  **Web API Layer**:
    - Hanya sebagai entry point (HTTP).
    - Berisi: `Controllers`, `Middleware`, `Program.cs`.

---

## Tahap 2: Transformasi ke Rich Domain Model

Hapus *Anemic Domain Model* dan pindahkan logika ke dalam Entity.

### Dari (Anemic):
```csharp
public class Product {
    public int Id { get; set; }
    public decimal Price { get; set; } // Bisa diubah siapa saja
}
```

### Ke (Rich Domain):
```csharp
public class Product {
    public int Id { get; private set; }
    public decimal Price { get; private set; }

    // Constructor untuk validasi awal
    public Product(string name, decimal price) {
        if (price <= 0) throw new BusinessRuleException("Harga tidak valid");
        // ...
    }

    // Perilaku (Behavior) menggantikan setter publik
    public void UpdatePrice(decimal newPrice) {
        if (newPrice <= 0) throw new BusinessRuleException("Harga harus positif");
        Price = newPrice;
    }
}
```

---

## Tahap 3: Implementasi Repository Pattern

Putuskan ketergantungan langsung Controller ke `AppDbContext`.

1.  **Definisikan Interface di Domain**:
    ```csharp
    public interface IProductRepository {
        Task<Product?> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
    }
    ```
2.  **Implementasikan di Infrastructure**:
    Menggunakan `AppDbContext` di dalam kelas `ProductRepository`.

---

## Tahap 4: Penggunaan Value Objects

Identifikasi atribut yang tidak punya identitas tapi punya aturan bisnis.
Misalnya, daripada hanya `decimal Price`, buatlah Value Object `Money`:

```csharp
public record Money(decimal Amount, string Currency) {
    public static Money FromIDR(decimal amount) => new(amount, "IDR");
}
```

---

## Tahap 5: Application Services & MediatR (Opsional)

Pindahkan logika navigasi/orkestrasi dari Controller ke *Application Service* atau menggunakan pola *Command/Query Responsibility Segregation* (CQRS) dengan **MediatR**.

**Controller** nantinya hanya akan berisi satu baris kode:
```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateProductCommand command) {
    var result = await _mediator.Send(command);
    return Ok(result);
}
```

---

## Tahap 6: Domain Events

Gunakan *Domain Events* untuk memicu aksi di bagian lain sistem (atau Bounded Context lain) ketika terjadi perubahan penting di Domain.
Contoh: Saat stok habis (`StockDepletedEvent`), kirim notifikasi ke tim pengadaan.

---

## Ringkasan Perubahan Konsep

| Fitur | Sekarang (CRUD) | Masa Depan (DDD) |
| :--- | :--- | :--- |
| **Pusat Aplikasi** | Database Schema | Domain Business Logic |
| **Validasi** | Di Controller | Di dalam Entity (Invariants) |
| **Akses Data** | DbContext langsung | Repository Abstraction |
| **Komunikasi** | Method Calls | Ubiquitous Language & Events |

> [!IMPORTANT]
> **Kapan Harus Pindah ke DDD?**
> Gunakan DDD jika aplikasi Anda mulai memiliki aturan bisnis yang sangat kompleks. Jika aplikasi hanya sekadar "input data dan tampilkan data" (Simple CRUD), arsitektur sekarang sudah cukup efisien.
