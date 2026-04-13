# ProductCatalogAPI - DDD & CQRS Version

Proyek ini adalah implementasi ulang dari **ProductCatalogAPI** menggunakan arsitektur **Domain-Driven Design (DDD)** yang ketat dan pola **CQRS** dengan **MediatR**.

---

## 🏗️ Struktur Arsitektur (Multi-Project)

Solusi ini menerapkan *Clean Architecture* sederhana yang dibagi menjadi 4 layer utama:

### 1. [Domain Layer](file:///Users/sulaimansaleh/Downloads/NQA%20DOTNET%20DJP/Hari3/ProductCatalogAPI_DDD/src/Core/Domain)
- **Rich Domain Models**: [Product.cs](file:///Users/sulaimansaleh/Downloads/NQA%20DOTNET%20DJP/Hari3/ProductCatalogAPI_DDD/src/Core/Domain/Entities/Product.cs), [User.cs](file:///Users/sulaimansaleh/Downloads/NQA%20DOTNET%20DJP/Hari3/ProductCatalogAPI_DDD/src/Core/Domain/Entities/User.cs).
- **Abstractions**: [IProductRepository.cs](file:///Users/sulaimansaleh/Downloads/NQA%20DOTNET%20DJP/Hari3/ProductCatalogAPI_DDD/src/Core/Domain/Interfaces/IProductRepository.cs).
- **Penting**: Layer ini murni POCO (Plain Old CLR Objects) dan tidak memiliki dependensi ke infrastruktur maupun library eksternal.

### 2. [Application Layer](file:///Users/sulaimansaleh/Downloads/NQA%20DOTNET%20DJP/Hari3/ProductCatalogAPI_DDD/src/Core/Application)
- **Features (CQRS)**: Memisahkan [Commands](file:///Users/sulaimansaleh/Downloads/NQA%20DOTNET%20DJP/Hari3/ProductCatalogAPI_DDD/src/Core/Application/Features/Products/Commands) (Tulis) dan [Queries](file:///Users/sulaimansaleh/Downloads/NQA%20DOTNET%20DJP/Hari3/ProductCatalogAPI_DDD/src/Core/Application/Features/Products/Queries) (Baca) menggunakan MediatR.
- **Validation**: Menggunakan FluentValidation untuk memvalidasi request sebelum diproses.
- **DTOs**: Objek untuk transfer data keluar API.

### 3. [Infrastructure Layer](file:///Users/sulaimansaleh/Downloads/NQA%20DOTNET%20DJP/Hari3/ProductCatalogAPI_DDD/src/Infrastructure)
- **Persistence**: [AppDbContext.cs](file:///Users/sulaimansaleh/Downloads/NQA%20DOTNET%20DJP/Hari3/ProductCatalogAPI_DDD/src/Infrastructure/Persistence/AppDbContext.cs).
- **Security**: Implementasi JWT Token Service dan BCrypt Password Hasher.
- **Repositories**: Implementasi konkret akses data ke PostgreSQL.

### 4. [API Layer (Presentation)](file:///Users/sulaimansaleh/Downloads/NQA%20DOTNET%20DJP/Hari3/ProductCatalogAPI_DDD/src/Presentation/Api)
- **Thin Controllers**: Controller hanya bertugas mem-forward request ke MediatR.
- **Startup Config**: [Program.cs](file:///Users/sulaimansaleh/Downloads/NQA%20DOTNET%20DJP/Hari3/ProductCatalogAPI_DDD/src/Presentation/Api/Program.cs) yang menangani DI registration dan Auto-Migration.

---

## 💎 Perbandingan: Anemic vs Rich Domain Model

| Fitur | Anemic Model (Hari 3) | Rich Domain Model (Proyek Ini) |
| :--- | :--- | :--- |
| **Logic Location** | Tersebar di Controller / Service | Terpusat di dalam Entity (Domain) |
| **Encapsulation** | Publik (`get; set;`), bebas diubah siapa saja | Private setter, hanya bisa diubah via Method |
| **Object State** | Bisa berada dalam state tidak valid | Selalu valid (Invariants checked in Const/Method) |
| **Maintainability** | Sulit seiring bertambahnya aturan bisnis | Sangat mudah karena aturan bisnis terisolasi |

---

## 🚀 Fitur & Tech Stack

- **Framework**: .NET 8.0 / 9.0
- **Database**: PostgreSQL (EF Core)
- **Messaging**: MediatR (In-process Mediator)
- **Validation**: FluentValidation
- **Auth**: JWT Bearer + Role-Based Authorization
- **Security**: BCrypt.Net-Next
- **DX**: Swagger UI dengan JWT Authorize Button & Auto-Migration.

---

### ⚙️ Cara Menjalankan

1.  Pastikan **PostgreSQL** aktif di port `5432`.
2.  Database akan otomatis dibuat dengan nama `ProductCatalogDB_DDD` (cek ConnectionString di `appsettings.json`).
3.  Jalankan perintah berikut di folder root (`/ProductCatalogAPI_DDD/`):
    ```bash
    dotnet build
    dotnet run --project src/Presentation/Api
    ```
    > [!NOTE]
    > **Kenapa harus menggunakan `--project`?**
    > Karena solusi ini bersifat **Multi-Project**, terdapat lebih dari satu file `.csproj`. Dari folder root, .NET tidak tahu proyek mana yang merupakan "Entry Point" (aplikasi yang bisa dijalankan). 
    >- `Domain`, `Application`, dan `Infrastructure` adalah **Class Library** (tidak bisa dijalankan sendiri).
    >- `Api` adalah satu-satunya proyek **Web API** yang memiliki `Program.cs`. 
    > Dengan menambahkan `--project src/Presentation/Api`, kita memberitahu .NET untuk menjalankan proyek spesifik tersebut sebagai aplikasi utama.

4.  Buka browser ke halaman **Swagger**: `https://localhost:xxxx/swagger` (port menyesuaikan output terminal).

---

> [!TIP]
> **Pro-Tip**: Perhatikan bagaimana `Product.cs` menangani perubahan harga melalui method `UpdatePrice()`. Ini menjamin tidak ada harga negatif yang masuk ke database di level manapun!

---

*Dibuat oleh Senior .NET Architect Assistant untuk simulasi pengembangan aplikasi Enterprise.*
