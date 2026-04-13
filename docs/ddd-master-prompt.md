# Master Prompt: Membangun Backend DDD + MediatR

Copy dan paste prompt di bawah ini ke AI (seperti Antigravity, ChatGPT, atau Claude) untuk menghasilkan boilerplate project C# ASP.NET Core yang mengimplementasikan **Domain-Driven Design (DDD)** penuh dengan **MediatR**.

---

## 🚀 Copy Prompt Ini:

```text
Bertindaklah sebagai Senior .NET Architect. Saya ingin membangun ulang project "ProductCatalogAPI" menggunakan arsitektur Domain-Driven Design (DDD) yang ketat dan pola CQRS menggunakan MediatR.

Gunakan spesifikasi berikut:
1. Framework: ASP.NET Core 8.0/9.0 Web API.
2. Database: PostgreSQL (menggunakan Entity Framework Core).
3. Skema Data (sama dengan Hari 3):
   - Product: Id, Name, Description, Price, Stock, Category, CreatedAt.
   - User: Id, Username, PasswordHash, Role (Admin/User).

4. Struktur Arsitektur (Multi-Project/Layer):
   - Domain Layer: Berisi Rich Domain Model (Private setter, Validation di constructor/method), IProductRepository, dan IUserRepository. Tidak boleh punya dependensi ke luar.
   - Application Layer: Menggunakan MediatR. Pindahkan semua logika dari Controller ke Commands (Create, Update, Delete) dan Queries (GetById, GetAll). Sertakan Validation menggunakan FluentValidation (opsional tapi disarankan).
   - Infrastructure Layer: Berisi AppDbContext, Implementasi Repository, dan konfigurasi EF Core (Fluent API).
   - API Layer: Controllers yang sangat tipis (hanya menginjeksi IMediator dan mem-forward request).

5. Fitur Keamanan:
   - Tetap implementasikan JWT Authentication & Role-Based Authorization (Admin/User).
   - Gunakan BCrypt untuk hashing password.

6. Deployment & Dev Experience:
   - Sediakan Program.cs yang melakukan auto-migration saat startup.
   - Konfigurasi Swagger agar mendukung Authorize Lock (JWT).

Tolong buatkan kode lengkap untuk setiap filenya (struktur folder disebutkan dengan jelas) dengan komentar penjelasan yang sangat detail dalam Bahasa Indonesia untuk setiap baris kodenya. Saya ingin melihat perbedaan nyata antara "Anemic Model" (Hari 3) dengan "Rich Domain Model" di Domain Layer ini.
```

---

## 💡 Apa yang akan dihasilkan oleh Prompt ini?

Jika Anda menjalankan prompt di atas, AI akan menghasilkan struktur kode seperti ini:

1.  **Rich Entity**: `Product` tidak lagi memiliki `set;`, tapi memiliki method seperti `UpdateDetails(...)` atau `AdjustStock(...)`.
2.  **Clean Controllers**: Controller Anda tidak akan menyentuh `DbContext` lagi. Isinya hanya `_mediator.Send(command)`.
3.  **Separation of Concerns**: Logika database ada di *Infrastructure*, aturan bisnis ada di *Domain*, dan alur kerja ada di *Application*.
4.  **Auto-Migration**: Command yang memudahkan Anda running database tanpa harus pusing manual migration di terminal.

### Tip Tambahan:
Jika Anda ingin AI langsung membuatkan script bash untuk membuat foldernya, tambahkan baris ini di akhir prompt:
> *"Sertakan juga script `dotnet new` dan `mkdir` untuk membuat struktur project tersebut secara otomatis melalui terminal Mac/Linux."*
