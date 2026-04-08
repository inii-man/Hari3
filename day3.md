Day 3:
Authentication
& Security
Securing Web API with JWT in ASP.NET Core
Learning Objectives
Setelah sesi ini, peserta mampu:
Authentication & Authorization
Memahami konsep Authentication &
Authorization secara mendalam.
JWT untuk Secure API
Menggunakan JSON Web Token untuk
mengamankan Web API.
Login & Register
Mengimplementasikan endpoint login
dan register pada API.
Protect Endpoint
Melindungi endpoint dengan token agar tidak diakses
sembarangan.
Role-Based Access
Mengatur hak akses berdasarkan role yang dimiliki user.
2
Recap Day 2
Kemarin kita sudah membangun fondasi backend yang solid. Hari ini kita mengamankannya.
1. Membuat REST API
Routing, controller, dan HTTP method yang benar
2. CRUD dengan PostgreSQL
Operasi Create, Read, Update, Delete ke database
3. Testing via Swagger
Dokumentasi dan pengujian endpoint secara interaktif
< Ada masalah besar: API masih terbuka, tidak ada login, tidak ada proteksi data
4 siapa saja bisa akses semua endpoint!
3
Masalah API Tanpa Security
Tanpa mekanisme keamanan, API Anda rentan terhadap berbagai ancaman yang
serius.
Akses Bebas
Siapa saja bisa mengakses seluruh
endpoint tanpa batasan apapun.
Tidak Ada Identitas
Server tidak mengenali siapa yang
melakukan request ke API.
Data Rentan
Data bisa dimanipulasi, dihapus, atau dicuri oleh pihak tidak bertanggung
jawab.
4
SECTION 0 4 API SECURITY
CORS: Cross-Origin Resource Sharing
CORS mengontrol domain mana saja yang diizinkan mengakses API kamu. Tanpa konfigurasi CORS yang benar, browser akan memblokir request dari domain yang berbeda.
o Tanpa CORS
// Request dari http://frontend.com
// ke http://api.myapp.com
// ³ Diblokir browser!
Access to fetch has been blocked
by CORS policy
' Dengan CORS
// Program.cs
builder.Services.AddCors(options =>
{
 options.AddPolicy("AllowFrontend",
 policy => policy
 .WithOrigins("http://frontend.com")
 .AllowAnyMethod()
 .AllowAnyHeader());
});
app.UseCors("AllowFrontend");
WithOrigins
Tentukan domain yang diizinkan mengakses API.
AllowAnyMethod
Izinkan semua HTTP method (GET, POST, PUT, DELETE).
AllowAnyHeader
Izinkan semua request header dari client.
' Best Practice CORS
Hindari AllowAnyOrigin() di production 4 selalu tentukan domain spesifik dengan WithOrigins()
Gunakan AllowAnyOrigin() hanya di environment development/testing
Pisahkan policy CORS untuk endpoint publik dan endpoint sensitif (login, register)
' Production 4 spesifik domain
options.AddPolicy("ProductionPolicy",
 policy => policy
 .WithOrigins("https://myapp.com", "https://www.myapp.com")
 .WithMethods("GET", "POST") // hanya method yang dibutuhkan
 .AllowAnyHeader()
 .AllowCredentials());
// Î Development 4 lebih longgar
options.AddPolicy("DevPolicy",
 policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
5
SECTION 0 4 API SECURITY
HTTPS: Enkripsi Komunikasi API
HTTPS memastikan semua data yang dikirim antara client dan server dienkripsi. Tanpa HTTPS, data sensitif seperti token dan password bisa disadap (man-in-the-middle attack).
HTTP (Tidak Aman)
Data dikirim dalam bentuk plain text. Token, password, dan data sensitif bisa dibaca siapa saja di jaringan.
HTTPS (Aman)
Data dienkripsi dengan TLS/SSL. Hanya client dan server yang bisa membaca isi komunikasi.
Enforce HTTPS di ASP.NET Core
// Program.cs
// Redirect HTTP ³ HTTPS otomatis
app.UseHttpsRedirection();
// appsettings.json 4 konfigurasi port HTTPS
"Kestrel": {
 "Endpoints": {
 "Https": {
 "Url": "https://localhost:7001"
 }
 }
}
D Tip: Di production, selalu gunakan sertifikat SSL valid (Let's Encrypt gratis!) dan aktifkan HSTS untuk memaksa browser selalu pakai HTTPS.
' Best Practice HTTPS
HSTS Header
Aktifkan HTTP Strict Transport Security agar browser selalu pakai HTTPS. Tambahkan
app.UseHsts() di production.
Let's Encrypt
Gunakan sertifikat SSL gratis dari Let's Encrypt untuk production. Hindari self-signed
certificate di environment publik.
Endpoint Sensitif
Pastikan endpoint /login, /register, dan /forgot-password HANYA bisa diakses via
HTTPS 4 tolak semua request HTTP.
// Program.cs 4 urutan middleware yang benar
if (!app.Environment.IsDevelopment())
{
 app.UseHsts(); // aktifkan HSTS di production
}
app.UseHttpsRedirection(); // redirect HTTP ³ HTTPS
app.UseAuthentication();
app.UseAuthorization();
6
SECTION 0 4 API SECURITY
Rate Limiting: Batasi Request ke API
Rate Limiting membatasi jumlah request yang bisa dilakukan client dalam periode waktu tertentu. Ini melindungi API dari abuse, brute force attack, dan serangan DDoS.
Tanpa Rate Limiting
Attacker bisa mengirim ribuan request per detik 4 brute force password, spam endpoint, atau crash server.
Fixed Window
Batasi jumlah request per jendela waktu tetap. Contoh: maks 100 request per menit.
Sliding Window
Lebih halus dari fixed window 4 menghitung request dalam jendela waktu yang bergerak.
Setup Rate Limiting di ASP.NET Core (.NET 7+)
// Program.cs
builder.Services.AddRateLimiter(options =>
{
 options.AddFixedWindowLimiter("fixed", opt =>
 {
 opt.Window = TimeSpan.FromMinutes(1);
 opt.PermitLimit = 100; // maks 100 request/menit
 opt.QueueLimit = 0;
 });
});
app.UseRateLimiter();
// Terapkan ke controller/endpoint
[EnableRateLimiting("fixed")]
[HttpPost("login")]
public IActionResult Login(User user) { ... }
Penting: Terapkan rate limiting terutama pada endpoint sensitif seperti /login, /register, dan /forgot-password untuk mencegah brute force attack.
' Best Practice Rate Limiting pada Endpoint Sensitif
// Buat policy berbeda untuk endpoint sensitif
builder.Services.AddRateLimiter(options =>
{
 // Policy ketat untuk login 4 cegah brute force
 options.AddFixedWindowLimiter("login_policy", opt =>
 {
 opt.Window = TimeSpan.FromMinutes(15);
 opt.PermitLimit = 5; // maks 5 percobaan per 15 menit
 opt.QueueLimit = 0;
 });
 // Policy normal untuk endpoint umum
 options.AddFixedWindowLimiter("general", opt =>
 {
 opt.Window = TimeSpan.FromMinutes(1);
 opt.PermitLimit = 100;
 opt.QueueLimit = 10;
 });
 // Response saat limit terlampaui
 options.OnRejected = async (context, token) =>
 {
 context.HttpContext.Response.StatusCode = 429; // Too Many Requests
 await context.HttpContext.Response.WriteAsync(
 "Terlalu banyak percobaan. Coba lagi nanti.", token);
 };
});
/login & /register
Gunakan policy ketat: maks 5 request per 15 menit untuk mencegah brute force attack.
/forgot-password
Batasi lebih ketat: maks 3 request per jam untuk mencegah email spam dan enumeration attack.
Endpoint Umum
Policy lebih longgar: maks 100 request per menit cukup untuk penggunaan normal.
7
SECTION 1 4 AUTH CONCEPT
Authentication vs Authorization
Dua konsep fundamental keamanan yang sering disamakan, namun memiliki peran
yang berbeda.
Authentication
"Siapa kamu?" 4 Proses
memverifikasi identitas
pengguna. Memastikan user
adalah siapa yang mereka klaim.
Authorization
"Apa yang boleh kamu
lakukan?" 4 Proses menentukan
hak akses user setelah identitas
terverifikasi.
8
SECTION 1 4 AUTH CONCEPT
Flow Authentication
Bagaimana proses login dan penggunaan token berlangsung dari awal hingga akhir:
Request Simpan
Token Login Validasi Kirim Token
Token JWT dikirimkan di setiap request berikutnya melalui HTTP Header Authorization: Bearer <token> sehingga server dapat mengenali identitas pengguna tanpa menyimpan session.
9
SECTION 1 4 AUTH CONCEPT
Apa Itu JWT?
JWT (JSON Web Token) adalah standar terbuka untuk transmisi informasi secara aman antar pihak sebagai objek JSON.
Token Berbentuk String
JWT adalah sebuah string terenkripsi
yang ringkas dan dapat dikirim melalui
URL, Header HTTP, maupun body.
Berisi Informasi User
Token menyimpan data user (claims)
seperti username, role, dan ID yang bisa
dibaca oleh server.
Untuk Authentication
Digunakan sebagai bukti identitas digital
yang dikirim di setiap request ke
protected endpoint.
10
SECTION 1 4 AUTH CONCEPT
Struktur JWT
JWT terdiri dari tiga bagian yang dipisahkan oleh titik (.):
xxxxx.yyyyy.zzzzz
^ ^ ^
Header Payload Signature
Header
Menyimpan tipe token dan algoritma
hashing yang digunakan, biasanya HS256
atau RS256.
Payload
Berisi claims 4 data user seperti
username, role, userId, dan waktu
kedaluwarsa token.
Signature
Hash kriptografi dari Header + Payload
menggunakan secret key. Memastikan
token tidak dimanipulasi.
11
SECTION 1 4 AUTH CONCEPT
Kenapa JWT?
JWT menjadi pilihan utama untuk authentication di API modern karena beberapa keunggulan teknis yang signifikan.
Stateless
Tidak perlu menyimpan session di server 4 server hanya memvalidasi token di setiap request.
Cepat & Scalable
Cocok untuk arsitektur microservices dan aplikasi dengan jutaan pengguna.
Cocok untuk API Modern
Standar industri yang didukung oleh hampir semua platform dan
bahasa pemrograman.
12
SECTION 2 4 SETUP JWT
Install Package
Tambahkan package NuGet yang diperlukan untuk JWT authentication di ASP.NET
Core:
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
JwtBearer
Package resmi Microsoft untuk
validasi JWT token di ASP.NET Core
middleware.
Versi
Pastikan versi package sesuai
dengan versi .NET SDK yang
digunakan (contoh: .NET 8).
13
SECTION 2 4 SETUP JWT
Config JWT di appsettings.json
Simpan konfigurasi JWT di file appsettings.json agar mudah dikelola dan aman:
{
 "Jwt": {
 "Key": "supersecretkey",
 "Issuer": "myapi"
 }
}
Key
Secret key yang digunakan untuk menandatangani token. Gunakan
string yang panjang dan acak di production.
Issuer
Identitas penerbit token 4 biasanya nama atau domain API Anda.
· Jangan hardcode secret key di production! Gunakan environment variable atau secrets manager untuk menyimpan konfigurasi sensitif.
14
SECTION 2 4 SETUP JWT
Register Authentication
Daftarkan JWT authentication service di Program.cs:
builder.Services.AddAuthentication("Bearer")
 .AddJwtBearer(options =>
 {
 options.TokenValidationParameters = new TokenValidationParameters
 {
ValidateIssuer = true,
ValidateAudience = false,
ValidateLifetime = true,
ValidateIssuerSigningKey = true,
ValidIssuer = builder.Configuration["Jwt:Issuer"],
IssuerSigningKey = new SymmetricSecurityKey(
Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
 };
 });
15
SECTION 2 4 SETUP JWT
Enable Middleware
Aktifkan middleware authentication dan authorization di pipeline request ASP.NET Core:
app.UseAuthentication();
app.UseAuthorization();
UseAuthentication()
Membaca dan memvalidasi token JWT dari setiap incoming
request.
UseAuthorization()
Memeriksa apakah user yang terautentikasi memiliki izin untuk
mengakses endpoint tertentu.
< Urutan middleware sangat penting!
16
SECTION 3 4 USER & LOGIN
User Model
Definisikan model User sebagai representasi data pengguna dalam sistem:
public class User
{
 public string Username { get; set; }
 public string Password { get; set; }
}
// Untuk database, tambahkan Id
public class UserEntity
{
 public int Id { get; set; }
 public string Username { get; set; }
 public string PasswordHash { get; set; }
 public string Role { get; set; }
}
· Konvensi penamaan: Simpan password sebagai hash, bukan plain text. Field
17
SECTION 3 4 USER & LOGIN
Register Endpoint
Endpoint untuk mendaftarkan pengguna baru ke dalam sistem:
[HttpPost("register")]
public IActionResult Register(User user)
{
 // Cek apakah username sudah ada
 if (_users.Any(u => u.Username == user.Username))
 return BadRequest("Username sudah digunakan.");
 // Simpan user (gunakan hashing di production)
 _users.Add(user);
 return Ok("Registrasi berhasil!");
}
Validasi Duplikasi
Pastikan username belum terdaftar sebelum menyimpan user baru.
Response
Kembalikan status yang jelas 4 200 OK jika berhasil, 400 Bad Request
jika gagal.
18
SECTION 3 4 USER & LOGIN
Login Endpoint
Endpoint untuk memvalidasi kredensial user dan mengembalikan token JWT:
[HttpPost("login")]
public IActionResult Login(User user)
{
// Cari user berdasarkan username dan password
var existingUser = _users
.FirstOrDefault(u => u.Username == user.Username
 && u.Password == user.Password);
 if (existingUser == null)
 return Unauthorized("Username atau password salah.");
// Generate token
var token = GenerateToken(existingUser);
 return Ok(new { token });
}
19
SECTION 3 4 USER & LOGIN
Generate Token
Fungsi untuk membuat JWT token berdasarkan data user yang login:
private string GenerateToken(User user)
{
var claims = new[]
 {
new Claim(ClaimTypes.Name, user.Username),
new Claim(ClaimTypes.Role, user.Role ?? "User")
 };
var key = new SymmetricSecurityKey(
 Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
var token = new JwtSecurityToken(
 issuer: _config["Jwt:Issuer"],
 claims: claims,
 expires: DateTime.Now.AddHours(1),
 signingCredentials: creds);
return new JwtSecurityTokenHandler().WriteToken(token);
}
20
SECTION 3 4 USER & LOGIN
Return Token
Kembalikan token ke client setelah login berhasil:
return Ok(new { token = tokenString });
Format Response
Token dikembalikan dalam objek JSON
sehingga mudah diakses oleh client.
Penyimpanan di Client
Client menyimpan token di localStorage
atau memory untuk digunakan pada
request berikutnya.
Penggunaan Token
Token dikirim di setiap request sebagai
Authorization: Bearer <token>.
21
SECTION 4 4 PROTECT API
Authorize Attribute
Lindungi endpoint dengan menambahkan attribute [Authorize] pada controller atau action
method:
// Melindungi seluruh controller
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
 [HttpGet]
 public IActionResult GetAll() { ... }
 // Endpoint spesifik yang tidak memerlukan login
 [AllowAnonymous]
 [HttpGet("public")]
 public IActionResult GetPublic() { ... }
}
< Endpoint hanya bisa diakses jika user sudah login dan menyertakan token yang
valid.
22
SECTION 4 4 PROTECT API
Test Tanpa Token & Dengan Token
Perbedaan response ketika mengakses endpoint protected dengan dan tanpa token:
o Tanpa Token
GET /api/products
// Tidak menyertakan header Authorization
// Response:
HTTP 401 Unauthorized
{
 "status": 401,
 "title": "Unauthorized"
}
Request langsung ditolak oleh middleware sebelum masuk ke controller.
' Dengan Token
GET /api/products
Authorization: Bearer eyJhbGc...
// Response:
HTTP 200 OK
[
 { "id": 1, "name": "..." },
 ...
]
Request diterima dan data dikembalikan setelah token berhasil divalidasi.
23
SECTION 4 4 PROTECT API
Swagger Auth
Konfigurasi Swagger agar mendukung input JWT token untuk testing endpoint protected:
builder.Services.AddSwaggerGen(c =>
{
 c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
 {
 Name = "Authorization",
 Type = SecuritySchemeType.Http,
 Scheme = "Bearer",
 BearerFormat = "JWT",
 In = ParameterLocation.Header,
 Description = "Masukkan token JWT"
 });
 c.AddSecurityRequirement(new OpenApiSecurityRequirement { ... });
});
Cara Testing JWT di Swagger UI
Login via /api/auth/login
Kirim POST request dengan body { "username": "admin", "password": "password123" }
4 copy token dari response.
Klik tombol 'Authorize' ¹
Klik tombol Authorize di pojok kanan atas Swagger UI. Masukkan token dengan format:
Bearer <token_kamu>
Test Endpoint Protected
Setelah authorize, semua request ke endpoint [Authorize] akan otomatis menyertakan
header Authorization: Bearer <token>.
Logout / Ganti Token
Klik Authorize lagi ³ klik Logout ³ masukkan token baru. Token lama tidak akan digunakan lagi.
' Request Header yang Dikirim
GET /api/products HTTP/1.1
Host: localhost:7001
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json
o Jika Token Salah/Expired
HTTP/1.1 401 Unauthorized
{
 "status": 401,
 "error": "Unauthorized",
 "message": "Token tidak valid atau sudah expired"
}
D Tip: Gunakan jwt.io untuk decode dan inspect isi JWT token kamu 4 bisa melihat claims, expiration time, dan memverifikasi signature secara visual.
24
SECTION 5 4 ROLE & PERMISSION
Role-Based Access
Batasi akses endpoint berdasarkan role user menggunakan attribute [Authorize(Roles)]:
// Hanya Admin yang bisa akses
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public IActionResult Delete(int id) { ... }
// Semua user yang login bisa akses
[Authorize(Roles = "Admin,User")]
[HttpGet]
public IActionResult GetAll() { ... }
Admin
Akses penuh 4 dapat membaca, menulis, mengubah, dan menghapus
semua data.
User
Akses terbatas 4 biasanya hanya dapat membaca data atau
mengelola data miliknya sendiri.
25
SECTION 5 4 ROLE & PERMISSION
Claims dalam JWT
Claims adalah informasi yang disimpan di dalam payload JWT token dan dapat dibaca oleh server:
// Membaca claims dari token
var username = User.FindFirst(ClaimTypes.Name)?.Value;
var role = User.FindFirst(ClaimTypes.Role)?.Value;
var userId = User.FindFirst("UserId")?.Value;
Username
Identitas unik pengguna yang disimpan
dalam claim ClaimTypes.Name.
Role
Peran user (Admin/User) yang
menentukan hak akses, disimpan dalam
ClaimTypes.Role.
UserId
ID unik user dari database, berguna
untuk query data spesifik milik user
tersebut.
26
SECTION 6 4 BEST PRACTICE
Password Security
Jangan pernah menyimpan password dalam bentuk plain text 4 selalu gunakan hashing.
o Jangan Lakukan Ini
// BERBAHAYA 4 plain text!
user.Password = "password123";
_db.Users.Add(user);
Jika database bocor, semua password langsung terbaca.
' Lakukan Ini
// Install: BCrypt.Net-Next
var hash = BCrypt.Net.BCrypt
 .HashPassword(user.Password);
user.PasswordHash = hash;
// Verifikasi
BCrypt.Net.BCrypt
 .Verify(inputPassword, user.PasswordHash);
· Gunakan BCrypt! Package
27
SECTION 6 4 BEST PRACTICE
Token Expiration
Setiap JWT token harus memiliki waktu kedaluwarsa untuk membatasi jendela serangan jika token dicuri.
var token = new JwtSecurityToken(
 issuer: _config["Jwt:Issuer"],
 claims: claims,
 // Token berlaku selama 1 jam
 expires: DateTime.UtcNow.AddHours(1),
 signingCredentials: creds
);
Short-lived Token
Token akses berumur pendek (1324 jam)
mengurangi risiko jika token bocor atau
dicuri.
UTC Time
Selalu gunakan DateTime.UtcNow agar
konsisten di semua timezone server.
ValidateLifetime
Aktifkan ValidateLifetime = true di
konfigurasi agar token expired otomatis
ditolak.
28
SECTION 6 4 BEST PRACTICE
Refresh Token (Intro)
Refresh token memungkinkan pengguna tetap login tanpa harus memasukkan password lagi setelah token akses kedaluwarsa.
1. Login
Server mengembalikan dua token: Access Token (pendek) dan Refresh Token (panjang).
2. Access Token Expired
Client menerima respons 401 Unauthorized karena token sudah kedaluwarsa.
3. Kirim Refresh Token
Client mengirim refresh token ke endpoint khusus untuk mendapatkan access token baru.
4. Token Diperbarui
Server memvalidasi refresh token dan mengembalikan access token baru tanpa perlu login ulang.
29
SECTION 7 4 PRAKTIK
Mini Project: Upgrade Day 2
Tambahkan sistem autentikasi ke project REST API yang sudah dibuat kemarin. Project ini mengintegrasikan semua materi hari ini.
Register
Endpoint untuk daftar akun baru
Login
Endpoint untuk masuk dan dapat token
JWT
Generate dan validasi token
Protect
Lindungi semua endpoint API
< Sesuai silabus 4 Project ini dirancang untuk melatih implementasi authentication end-to-end di atas project REST API Day 2.
30
SECTION 7 4 PRAKTIK
Step-by-Step
Langkah-langkah implementasi JWT authentication dari awal hingga selesai:
01
Install JWT Package
Jalankan dotnet add package
Microsoft.AspNetCore.Authentication.JwtBear
er
02
Config appsettings
Tambahkan section "Jwt" dengan Key dan
Issuer di appsettings.json
03
Setup Authentication
Daftarkan
AddAuthentication().AddJwtBearer() di
Program.cs
04
Buat Login Endpoint
Tambahkan AuthController dengan endpoint
register dan login
05
Generate Token
Implementasikan fungsi GenerateToken()
menggunakan JwtSecurityToken
06
Protect Endpoint
Tambahkan [Authorize] pada controller atau
action yang ingin dilindungi
31
SECTION 7 4 PRAKTIK
Test & Verifikasi
Pastikan implementasi berjalan dengan benar melalui serangkaian pengujian berikut:
Langkah 1: Login
POST ke /auth/login dengan username &
password 4 pastikan mendapatkan
token di response.
Langkah 2: Copy Token
Salin nilai token dari response JSON dan
tempelkan di Swagger Authorize.
Langkah 3: Test API
Akses endpoint protected 4 harus
mengembalikan 200 OK dengan data
yang benar.
32
SECTION 8 4 PENUTUP
Summary
Hari ini kita telah berhasil mengamankan Web API menggunakan JWT Authentication di ASP.NET Core:
Authentication & Authorization
Memahami perbedaan konsep "Siapa
kamu?" vs "Apa yang boleh kamu
lakukan?"
JWT
Struktur token (Header, Payload,
Signature), cara kerja, dan keunggulan
stateless authentication.
Login System
Implementasi endpoint register, login,
generate token, dan return token ke
client.
Secure API
Protect endpoint dengan [Authorize], role-based access, dan best practice keamanan.
33
Next Session: Day 4
FRONTEND INTEGRATION
Besok kita akan:
Membangun UI dengan
Bootstrap
Menggunakan ASP.NET Core
MVC & Razor
Menghubungkan frontend
dengan API
Menampilkan data dari backend ke UI
< Dari backend ³ jadi fullstack application