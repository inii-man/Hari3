# 🛠️ Panduan Instalasi — Product Catalog API

Dokumen ini menjelaskan langkah-langkah lengkap untuk menginstal, mengkonfigurasi, dan menjalankan **Product Catalog API** dari awal.

---

## 📋 Prasyarat

Pastikan semua perangkat berikut sudah terinstal di sistem Anda:

| Perangkat | Versi Minimum | Cara Cek |
|-----------|---------------|----------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ | `dotnet --version` |
| [PostgreSQL](https://www.postgresql.org/download/) | 14+ | `psql --version` |
| [Git](https://git-scm.com/) | Terbaru | `git --version` |

> **Opsional**: [pgAdmin](https://www.pgadmin.org/) untuk manajemen database secara visual.

---

## 📥 Langkah 1 — Clone / Buka Project

Jika project sudah ada di lokal:
```bash
cd "/path/ke/Hari3/ProductCatalogAPI"
```

Jika clone dari repository:
```bash
git clone <url-repository>
cd Hari3/ProductCatalogAPI
```

---

## 🗄️ Langkah 2 — Siapkan Database PostgreSQL

### 2a. Buat database baru

Buka terminal dan masuk ke PostgreSQL:
```bash
psql -U postgres
```

Buat database:
```sql
CREATE DATABASE "ProductCatalogDB";
-- Jika ingin membuat user khusus:
CREATE USER namauser WITH PASSWORD 'passwordkamu';
GRANT ALL PRIVILEGES ON DATABASE "ProductCatalogDB" TO namauser;
\q
```

### 2b. Konfigurasi Connection String

Buka file `appsettings.json` dan sesuaikan bagian `ConnectionStrings`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ProductCatalogDB;Username=NAMA_USER_POSTGRES;Password=PASSWORD_ANDA;Trust Server Certificate=true"
  }
}
```

> **Catatan**: Jika PostgreSQL berjalan di port default (5432) dan menggunakan Windows Authentication / peer auth, Anda mungkin tidak perlu `Password`.

---

## 🔑 Langkah 3 — Konfigurasi JWT

Buka `appsettings.json`, pastikan bagian `Jwt` berisi:

```json
{
  "Jwt": {
    "Key": "GantiDenganSecretKeyYangSangatPanjangDanAman_MinimalRahasianya!",
    "Issuer": "ProductCatalogAPI"
  }
}
```

> ⚠️ **PENTING UNTUK PRODUCTION**: Jangan simpan `Jwt:Key` di `appsettings.json` langsung. Gunakan **environment variables** atau **User Secrets**:
>
> ```bash
> dotnet user-secrets set "Jwt:Key" "SecretKeyAnda"
> dotnet user-secrets set "Jwt:Issuer" "ProductCatalogAPI"
> ```

---

## 📦 Langkah 4 — Restore Dependencies

```bash
dotnet restore
```

Paket yang akan diunduh:

| Paket | Fungsi |
|-------|--------|
| `BCrypt.Net-Next 4.0.3` | Hashing password |
| `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0` | Middleware JWT |
| `Microsoft.EntityFrameworkCore 8.0.0` | ORM |
| `Microsoft.EntityFrameworkCore.Design 8.0.0` | Tools migrasi (dev only) |
| `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.0` | Driver PostgreSQL |
| `Swashbuckle.AspNetCore 6.6.2` | Swagger/OpenAPI UI |

---

## 🗃️ Langkah 5 — Jalankan Migrasi Database

```bash
dotnet ef database update
```

Perintah ini akan:
1. Membuat tabel `Products` dan `Users` di database
2. **Menjalankan seed data** — membuat user admin default dan 3 produk awal

> Jika perintah `dotnet ef` tidak ditemukan, install dulu:
> ```bash
> dotnet tool install --global dotnet-ef
> ```

### Verifikasi Seed Data

Setelah migrasi, cek isi database:
```bash
psql -U namauser -d ProductCatalogDB -c "SELECT * FROM \"Users\";"
psql -U namauser -d ProductCatalogDB -c "SELECT * FROM \"Products\";"
```

**User Admin Default:**
- Username: `admin`
- Password: `admin123`
- Role: `Admin`

**Produk Default:**
- Laptop Pro 15 (Rp 15.000.000)
- Mechanical Keyboard (Rp 1.200.000)
- Wireless Mouse (Rp 450.000)

---

## ▶️ Langkah 6 — Jalankan Aplikasi

```bash
dotnet run
```

Output yang diharapkan:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5xxx
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

---

## 🌐 Langkah 7 — Akses Swagger UI

Buka browser dan pergi ke:
```
http://localhost:XXXX/
```
(port sesuai output terminal)

Swagger UI akan tampil di root URL karena konfigurasi `RoutePrefix = string.Empty` di `Program.cs`.

---

## 🔁 Reset Database (Jika Diperlukan)

Jika ingin menghapus semua data dan mulai ulang:

```bash
# Hapus database lama
psql -U postgres -c "DROP DATABASE \"ProductCatalogDB\";"

# Buat ulang
psql -U postgres -c "CREATE DATABASE \"ProductCatalogDB\";"

# Jalankan migrasi ulang
dotnet ef database update
```

---

## 🧱 Membuat Migrasi Baru (Developer)

Jika Anda mengubah Model dan perlu membuat migrasi baru:

```bash
dotnet ef migrations add NamaMigrasiAnda
dotnet ef database update
```

---

## ❗ Troubleshooting Umum

| Error | Kemungkinan Penyebab | Solusi |
|-------|----------------------|--------|
| `Connection refused` | PostgreSQL tidak berjalan | `brew services start postgresql` (Mac) atau `net start postgresql-x64-14` (Windows) |
| `password authentication failed` | Username/password salah di connection string | Cek `appsettings.json` |
| `database does not exist` | Database belum dibuat | Jalankan `CREATE DATABASE` di psql |
| `dotnet ef not found` | dotnet-ef tools belum diinstal | `dotnet tool install --global dotnet-ef` |
| `Jwt:Key is null` | Key tidak ada di appsettings | Tambahkan section `Jwt` di `appsettings.json` |
