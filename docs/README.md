# 📦 Product Catalog API

> **REST API** berbasis ASP.NET Core 8 dengan autentikasi JWT, role-based access control, dan koneksi ke PostgreSQL.

Proyek ini dibuat sebagai bagian dari **NQA DotNet Training — Hari 3** yang berfokus pada implementasi keamanan API menggunakan JWT Authentication.

---

## 🗂️ Daftar Dokumen

| File | Deskripsi |
|------|-----------|
| [README.md](./README.md) | Gambaran umum proyek ini (file ini) |
| [INSTALLATION.md](./INSTALLATION.md) | Panduan instalasi & konfigurasi lengkap |
| [USAGE.md](./USAGE.md) | Cara pemakaian API — endpoint, contoh request/response |
| [CODE_EXPLANATION.md](./CODE_EXPLANATION.md) | Penjelasan teknis per file kode |

---

## 🌟 Fitur Utama

| Fitur | Deskripsi |
|-------|-----------|
| 🔐 **JWT Authentication** | Token berbasis HS256, berlaku 1 jam |
| 👥 **Role-Based Access Control** | Role `Admin` dan `User` dengan hak akses berbeda |
| 🛡️ **BCrypt Password Hashing** | Password di-hash sebelum disimpan ke database |
| 🚦 **Rate Limiting** | Endpoint auth: 50 req/15 menit, endpoint umum: 100 req/menit |
| 🌐 **CORS** | Konfigurasi DevPolicy untuk development |
| 📄 **Swagger / OpenAPI** | UI dokumentasi API otomatis dengan dukungan JWT |
| 🗄️ **PostgreSQL + EF Core** | ORM dengan auto-migration dan data seed |

---

## 🏗️ Struktur Proyek

```
ProductCatalogAPI/
├── Controllers/
│   ├── AuthController.cs       # Register & Login (JWT generation)
│   └── ProductsController.cs   # CRUD produk dengan otorisasi
├── Data/
│   └── AppDbContext.cs         # EF Core DbContext + seed data
├── Models/
│   ├── Product.cs              # Entity/model tabel Products
│   └── User.cs                 # DTO User + Entity UserEntity
├── Migrations/                 # File migrasi EF Core
├── Program.cs                  # Entry point + konfigurasi semua service
├── appsettings.json            # Konfigurasi JWT, DB, Logging
└── ProductCatalogAPI.csproj    # Daftar dependencies/paket NuGet
```

---

## ⚡ Quick Start

```bash
# 1. Clone / buka project
cd ProductCatalogAPI

# 2. Sesuaikan connection string di appsettings.json

# 3. Jalankan migrasi & seed database
dotnet ef database update

# 4. Jalankan aplikasi
dotnet run
```

Akses Swagger UI di: **http://localhost:5xxx/**

Login default Admin: `admin` / `admin123`

---

## 🔑 Ringkasan Endpoint

| Method | Endpoint | Auth | Role |
|--------|----------|------|------|
| `POST` | `/api/auth/register` | ❌ Tidak perlu | - |
| `POST` | `/api/auth/login` | ❌ Tidak perlu | - |
| `GET` | `/api/products/public` | ❌ Tidak perlu | - |
| `GET` | `/api/products` | ✅ JWT | Admin, User |
| `GET` | `/api/products/{id}` | ✅ JWT | Admin, User |
| `POST` | `/api/products` | ✅ JWT | Admin only |
| `PUT` | `/api/products/{id}` | ✅ JWT | Admin only |
| `DELETE` | `/api/products/{id}` | ✅ JWT | Admin only |

---

## 🛠️ Tech Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL (via Npgsql)
- **ORM**: Entity Framework Core 8.0
- **Auth**: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **Security**: BCrypt.Net-Next
- **Docs**: Swashbuckle (Swagger/OpenAPI)
