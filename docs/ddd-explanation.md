# Analisis Implementasi Domain-Driven Design (DDD)

Dokumen ini menjelaskan status implementasi DDD pada proyek **ProductCatalogAPI** serta memberikan panduan lengkap mengenai konsep DDD.

## 1. Apakah Project Ini Sudah Menerapkan DDD?

Berdasarkan analisis struktur kode, project ini **belum sepenuhnya menerapkan DDD**. Project ini saat ini menggunakan pola **Layered Architecture (N-Layer)** yang sederhana dengan model **Anemic Domain Model**.

### Mengapa belum dikategorikan DDD murni?
1.  **Anemic Domain Model**: Kelas `Product` dan `UserEntity` hanya berisi properti (Data Transfer Objects semu) tanpa perilaku atau logika bisnis di dalamnya.
2.  **Logic in Controllers**: Logika bisnis (seperti validasi harga atau stok) berada langsung di dalam Controller. Dalam DDD, logika ini seharusnya ada di dalam *Domain Entity* atau *Domain Service*.
3.  **Ketergantungan Infrastruktur**: Controller bergantung langsung pada `AppDbContext` (EF Core). Dalam DDD, terdapat abstraksi *Repository* untuk memisahkan domain dari detail database.

---

## 2. Bagian yang "Mendekati" Prinsip DDD

Meskipun belum sepenuhnya DDD, beberapa bagian sudah mencerminkan dasar-dasar pemisahan tanggung jawab:

### A. Model sebagai Entity (Data-Centric)
File: `ProductCatalogAPI/Models/Product.cs`
- Secara struktur, ini adalah calon **Domain Entity**. Namun saat ini masih bersifat *anemic* (hanya getter/setter).

### B. Dependency Injection
File: `ProductCatalogAPI/Program.cs` & `ProductsController.cs`
- Penggunaan DI untuk menginjeksi Konteks Database adalah pondasi awal menuju *Inversion of Control* yang diperlukan DDD.

---

## 3. Penjelasan Syntax (Aspirasi DDD)

Jika kita ingin mengubah kode saat ini menjadi DDD, berikut adalah perbandingan syntax-nya:

### Pola Sekarang (Transaction Script / Anemic)
```csharp
// Di Controller
if (product.Price <= 0) return BadRequest("Harga tidak valid");
_db.Products.Add(product);
await _db.SaveChangesAsync();
```

### Pola DDD (Rich Domain Model)
```csharp
// Di Domain Entity (Product.cs)
public void UpdatePrice(decimal newPrice) {
    if (newPrice <= 0) throw new DomainException("Harga harus positif");
    Price = newPrice;
}

// Di Application Service / Controller
var product = await _repository.GetById(id);
product.UpdatePrice(150000); // Logika ada di Entity
await _repository.Update(product);
```

---

## 4. Penjelasan Lengkap Konsep DDD

Domain-Driven Design (DDD) adalah pendekatan pengembangan perangkat lunak yang berfokus pada pemahaman mendalam tentang domain bisnis dan menerjemahkannya ke dalam kode.

### Pilar Utama DDD:

#### 1. Ubiquitous Language (Bahasa Universal)
Bahasa yang sama digunakan oleh pengembang dan pakar bisnis (*domain experts*). Istilah dalam kode (nama class, method) harus sama dengan istilah yang digunakan bisnis.

#### 2. Bounded Context
Membagi sistem besar menjadi sub-sistem yang lebih kecil dengan batasan yang jelas. Misalnya, konteks "Pemesanan" mungkin memiliki model `Product` yang berbeda dengan konteks "Inventaris".

#### 3. Building Blocks (Blok Pembangun)
- **Entity**: Objek yang memiliki identitas unik (misal: `User` dengan ID). Identitasnya tidak berubah meski atributnya berubah.
- **Value Object**: Objek yang didefinisikan oleh atributnya, bukan identitas (misal: `Address`, `Money`). Dua objek dianggap sama jika nilainya sama.
- **Aggregate**: Kumpulan objek (Entity & Value Object) yang diperlakukan sebagai satu unit data. Memiliki satu *Aggregate Root*.
- **Repository**: Abstraksi untuk menyimpan dan mengambil Aggregate (seolah-olah seperti koleksi di memory).
- **Domain Service**: Digunakan ketika suatu logika bisnis melibatkan banyak Entity atau tidak cocok diletakkan di satu Entity spesifik.

### Keuntungan DDD:
- Kode sangat mencerminkan kebutuhan bisnis.
- Lebih mudah diuji (logika bisnis tidak bergantung pada database/framework).
- Skalabilitas tim yang lebih baik karena adanya *Bounded Context*.

---

> [!TIP]
> **Rekomendasi Langkah Selanjutnya:**
> Jika Anda ingin menerapkan DDD di project ini, mulailah dengan memindahkan logika validasi dari `ProductsController` ke dalam method di dalam `Product.cs`, dan buatlah folder `Repositories` untuk mengabstraksi `AppDbContext`.
