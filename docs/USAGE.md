# 📖 Panduan Pemakaian API — Product Catalog API

Dokumen ini menjelaskan cara menggunakan setiap endpoint yang tersedia di **Product Catalog API**, lengkap dengan contoh request dan response.

---

## 🌐 Base URL

```
http://localhost:XXXX
```

> Port dapat berbeda, lihat output `dotnet run` untuk port yang aktif.

---

## 🔐 Cara Autentikasi

API ini menggunakan **JWT Bearer Token**. Alur penggunaannya:

```
1. Register akun  →  POST /api/auth/register
2. Login          →  POST /api/auth/login  →  dapat token
3. Gunakan token  →  Tambahkan header: Authorization: Bearer <token>
```

### Di Swagger UI:
1. Klik tombol **Authorize 🔒** di kanan atas
2. Di field `Value`, ketik: `Bearer eyJhbGci...` (token dari login)
3. Klik **Authorize** → **Close**
4. Sekarang semua request akan otomatis menyertakan token

---

## 📌 Ringkasan Endpoint

| Method | Endpoint | Auth | Role | Deskripsi |
|--------|----------|------|------|-----------|
| `POST` | `/api/auth/register` | ❌ | - | Daftarkan akun baru |
| `POST` | `/api/auth/login` | ❌ | - | Login, dapatkan JWT token |
| `GET` | `/api/products/public` | ❌ | - | Lihat daftar produk (data terbatas) |
| `GET` | `/api/products` | ✅ JWT | Admin, User | Lihat semua produk (data lengkap) |
| `GET` | `/api/products/{id}` | ✅ JWT | Admin, User | Lihat produk berdasarkan ID |
| `POST` | `/api/products` | ✅ JWT | Admin only | Tambah produk baru |
| `PUT` | `/api/products/{id}` | ✅ JWT | Admin only | Update produk |
| `DELETE` | `/api/products/{id}` | ✅ JWT | Admin only | Hapus produk |

---

## 👤 Auth Endpoints

### POST `/api/auth/register` — Daftar Akun

**Rate Limit**: 50 request per 15 menit

**Request Body**:
```json
{
  "username": "budi",
  "password": "password123",
  "role": "User"
}
```

> Field `role` opsional. Nilai valid: `"Admin"` atau `"User"`. Default: `"User"`.

**Response 200 OK**:
```json
{
  "message": "Registrasi berhasil!",
  "username": "budi",
  "role": "User"
}
```

**Response 400 Bad Request** (username sudah ada):
```json
{
  "message": "Username sudah digunakan."
}
```

**Response 400 Bad Request** (password terlalu pendek):
```json
{
  "message": "Password minimal 6 karakter."
}
```

---

### POST `/api/auth/login` — Login

**Rate Limit**: 50 request per 15 menit

**Request Body**:
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Response 200 OK**:
```json
{
  "message": "Login berhasil!",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "admin",
  "role": "Admin",
  "expiresIn": "1 jam"
}
```

> ⚠️ **Simpan nilai `token`** — Anda butuhkan ini untuk mengakses endpoint yang terproteksi.

**Response 401 Unauthorized**:
```json
{
  "message": "Username atau password salah."
}
```

---

## 🛒 Products Endpoints

### GET `/api/products/public` — Daftar Produk Publik

Tidak perlu login. Mengembalikan daftar produk dengan field terbatas (tanpa deskripsi lengkap).

**Rate Limit**: 100 request per menit

**Response 200 OK**:
```json
{
  "message": "Data publik — login untuk melihat detail lengkap.",
  "total": 3,
  "data": [
    { "id": 1, "name": "Laptop Pro 15", "category": "Electronics", "price": 15000000 },
    { "id": 2, "name": "Mechanical Keyboard", "category": "Accessories", "price": 1200000 },
    { "id": 3, "name": "Wireless Mouse", "category": "Accessories", "price": 450000 }
  ]
}
```

---

### GET `/api/products` — Semua Produk (Perlu Login)

**Header wajib**:
```
Authorization: Bearer eyJhbGci...
```

**Role**: Admin atau User

**Response 200 OK**:
```json
[
  {
    "id": 1,
    "name": "Laptop Pro 15",
    "description": "Laptop high-performance untuk profesional",
    "price": 15000000,
    "stock": 10,
    "category": "Electronics",
    "createdAt": "2024-01-01T00:00:00Z"
  },
  ...
]
```

**Response 401 Unauthorized** (token tidak ada/salah):
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

---

### GET `/api/products/{id}` — Produk By ID

**Header wajib**: `Authorization: Bearer <token>`

**Role**: Admin atau User

**Contoh**: `GET /api/products/1`

**Response 200 OK**:
```json
{
  "id": 1,
  "name": "Laptop Pro 15",
  "description": "Laptop high-performance untuk profesional",
  "price": 15000000,
  "stock": 10,
  "category": "Electronics",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**Response 404 Not Found**:
```json
{
  "message": "Produk dengan ID 99 tidak ditemukan."
}
```

---

### POST `/api/products` — Tambah Produk (Admin Only)

**Header wajib**: `Authorization: Bearer <token-admin>`

**Request Body**:
```json
{
  "name": "Monitor 4K",
  "description": "Monitor gaming 27 inci resolusi 4K",
  "price": 5500000,
  "stock": 15,
  "category": "Electronics"
}
```

**Response 201 Created**:
```json
{
  "id": 4,
  "name": "Monitor 4K",
  "description": "Monitor gaming 27 inci resolusi 4K",
  "price": 5500000,
  "stock": 15,
  "category": "Electronics",
  "createdAt": "2026-04-08T07:00:00Z"
}
```

**Response 400 Bad Request** (nama kosong):
```json
{
  "message": "Nama produk tidak boleh kosong."
}
```

**Response 403 Forbidden** (user biasa mencoba akses):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

---

### PUT `/api/products/{id}` — Update Produk (Admin Only)

**Header wajib**: `Authorization: Bearer <token-admin>`

**Contoh**: `PUT /api/products/1`

**Request Body** (semua field wajib diisi):
```json
{
  "name": "Laptop Pro 15 Updated",
  "description": "Laptop terbaru edisi 2026",
  "price": 16500000,
  "stock": 8,
  "category": "Electronics"
}
```

**Response 200 OK**:
```json
{
  "id": 1,
  "name": "Laptop Pro 15 Updated",
  "description": "Laptop terbaru edisi 2026",
  "price": 16500000,
  "stock": 8,
  "category": "Electronics",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

---

### DELETE `/api/products/{id}` — Hapus Produk (Admin Only)

**Header wajib**: `Authorization: Bearer <token-admin>`

**Contoh**: `DELETE /api/products/2`

**Response 200 OK**:
```json
{
  "message": "Produk 'Mechanical Keyboard' berhasil dihapus."
}
```

**Response 404 Not Found**:
```json
{
  "message": "Produk dengan ID 2 tidak ditemukan."
}
```

---

## 🧪 Contoh Alur Kerja Lengkap

### Skenario: Admin menambah produk baru

```bash
# Step 1: Login sebagai admin
curl -X POST http://localhost:5xxx/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin123"}'

# Simpan token dari response di atas, misalnya:
TOKEN="eyJhbGciOiJIUzI1NiIs..."

# Step 2: Tambah produk
curl -X POST http://localhost:5xxx/api/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "name": "Headset Gaming",
    "description": "Headset dengan surround sound 7.1",
    "price": 850000,
    "stock": 30,
    "category": "Accessories"
  }'

# Step 3: Lihat semua produk
curl -X GET http://localhost:5xxx/api/products \
  -H "Authorization: Bearer $TOKEN"
```

---

### Skenario: User biasa melihat produk

```bash
# Step 1: Daftar akun user baru
curl -X POST http://localhost:5xxx/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username": "budi", "password": "budi1234", "role": "User"}'

# Step 2: Login
curl -X POST http://localhost:5xxx/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "budi", "password": "budi1234"}'

# Step 3: Lihat produk (berhasil)
curl -X GET http://localhost:5xxx/api/products \
  -H "Authorization: Bearer $TOKEN"

# Step 4: Coba hapus produk (akan gagal 403)
curl -X DELETE http://localhost:5xxx/api/products/1 \
  -H "Authorization: Bearer $TOKEN"
```

---

## ⚡ Rate Limiting

Jika request melebihi batas, akan muncul error **429 Too Many Requests**:

```json
{
  "status": 429,
  "message": "Terlalu banyak percobaan. Coba lagi nanti."
}
```

| Policy | Endpoint | Batas |
|--------|----------|-------|
| `auth_policy` | `/api/auth/register`, `/api/auth/login` | 50 req / 15 menit |
| `general` | `/api/products/*` | 100 req / menit |
