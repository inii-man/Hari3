// ============================================================
//  Program.cs — Entry Point & Konfigurasi Aplikasi
//  ProductCatalogAPI — NQA DotNet Training, Hari 3
// ============================================================
//
//  File ini adalah titik masuk aplikasi ASP.NET Core.
//  Semua service (DI), middleware, dan pipeline dikonfigurasi di sini
//  menggunakan model "Minimal Hosting" (WebApplication.CreateBuilder).
//
//  Urutan konfigurasi:
//    1. Database (EF Core + PostgreSQL)
//    2. CORS
//    3. Rate Limiting
//    4. JWT Authentication
//    5. Controllers & Swagger
//    6. Build App + Middleware Pipeline
// ============================================================

using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProductCatalogAPI.Data;

// WebApplicationBuilder: builder pola untuk mendaftarkan semua service sebelum app dibangun
var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════════════════════════════════════
//  1. DATABASE — Entity Framework Core + PostgreSQL
// ══════════════════════════════════════════════════════════════════════════════
// Mendaftarkan AppDbContext ke DI container dengan lifetime Scoped (1 instance/request).
// UseNpgsql() menentukan bahwa provider database yang digunakan adalah PostgreSQL via Npgsql.
// Connection string dibaca dari appsettings.json → ConnectionStrings:DefaultConnection.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ══════════════════════════════════════════════════════════════════════════════
//  2. CORS — Cross-Origin Resource Sharing
// ══════════════════════════════════════════════════════════════════════════════
// CORS adalah mekanisme keamanan browser yang membatasi request dari origin berbeda.
// Contoh: frontend di localhost:3000 tidak bisa akses API di localhost:5000 tanpa CORS.
builder.Services.AddCors(options =>
{
    // Policy "DevPolicy" — sangat longgar, hanya untuk development!
    // Di production: ganti AllowAnyOrigin() dengan .WithOrigins("https://domain-anda.com")
    options.AddPolicy("DevPolicy", policy =>
        policy.AllowAnyOrigin()   // Izinkan semua domain
              .AllowAnyMethod()   // Izinkan semua HTTP method
              .AllowAnyHeader()); // Izinkan semua header (termasuk Authorization)
});

// ══════════════════════════════════════════════════════════════════════════════
//  3. RATE LIMITING — Batasi request per waktu
// ══════════════════════════════════════════════════════════════════════════════
// Rate limiting mencegah penyalahgunaan API (brute force, DDoS, spam).
// Menggunakan Fixed Window: jendela waktu tetap, counter reset saat window habis.
builder.Services.AddRateLimiter(options =>
{
    // ── Policy ketat untuk /auth — cegah brute force login ─────────────
    // Max 50 request dalam 15 menit per client (berdasarkan IP secara default)
    options.AddFixedWindowLimiter("auth_policy", opt =>
    {
        opt.Window       = TimeSpan.FromMinutes(15); // durasi jendela waktu
        opt.PermitLimit  = 50;   // maks request yang diizinkan dalam window
        opt.QueueLimit   = 0;    // tidak ada antrian — request berlebih langsung ditolak
    });

    // ── Policy normal untuk endpoint umum ──────────────────────────────
    // Max 100 request per menit; 10 request bisa mengantri jika limit tercapai
    options.AddFixedWindowLimiter("general", opt =>
    {
        opt.Window       = TimeSpan.FromMinutes(1);
        opt.PermitLimit  = 100; // maks 100 request/menit
        opt.QueueLimit   = 10;  // 10 request bisa masuk antrian
    });

    // ── Response kustom saat limit terlampaui ────────────────────────
    // Alih-alih error default, kita kirim JSON yang lebih informatif
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429; // 429 = Too Many Requests
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            status  = 429,
            message = "Terlalu banyak percobaan. Coba lagi nanti."
        }, token);
    };
});

// ══════════════════════════════════════════════════════════════════════════════
//  4. JWT AUTHENTICATION
// ══════════════════════════════════════════════════════════════════════════════
// JWT (JSON Web Token) adalah standar untuk membuat token autentikasi stateless.
// Token ditandatangani dengan secret key → tidak bisa dipalsukan tanpa key yang sama.
var jwtKey    = builder.Configuration["Jwt:Key"]!;    // secret signing key dari appsettings
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!; // identitas penerbit token

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // TokenValidationParameters: aturan validasi token yang masuk
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,   // Pastikan token dikeluarkan oleh issuer yang benar
            ValidateAudience         = false,   // Tidak cek audience (disederhanakan)
            ValidateLifetime         = true,    // Tolak token yang sudah expired
            ValidateIssuerSigningKey = true,    // Verifikasi tanda tangan dengan secret key
            ValidIssuer              = jwtIssuer,
            // Kunci simetris: key yang sama digunakan untuk sign (di AuthController)
            // dan verify (di middleware ini)
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Mendaftarkan service authorization (cek [Authorize] attribute di controller)
builder.Services.AddAuthorization();

// ══════════════════════════════════════════════════════════════════════════════
//  5. CONTROLLERS & SWAGGER dengan JWT Support
// ══════════════════════════════════════════════════════════════════════════════
// Mendaftarkan semua controller (ProductsController, AuthController) ke DI
builder.Services.AddControllers();

// Konfigurasi Swagger/OpenAPI — UI untuk mendokumentasikan dan mencoba API
builder.Services.AddSwaggerGen(c =>
{
    // Metadata yang muncul di bagian atas Swagger UI
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Product Catalog API",
        Version     = "v1",
        Description = "REST API dengan JWT Authentication — NQA DotNet Training Day 3"
    });

    // Tambahkan definisi skema keamanan JWT di Swagger UI
    // Ini yang membuat tombol "Authorize 🔒" muncul di swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",      // nama header HTTP yang digunakan
        Type        = SecuritySchemeType.Http,
        Scheme      = "Bearer",             // skema auth: Bearer token
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Masukkan JWT token. Contoh: Bearer eyJhbGci..."
    });

    // Terapkan security requirement ke SEMUA endpoint (semua tampil dengan ikon lock)
    // Endpoint dengan [AllowAnonymous] tetap bisa diakses tanpa token meski ada lock icon
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer" // merujuk ke definisi di atas
                }
            },
            Array.Empty<string>() // tidak ada scope khusus yang diperlukan
        }
    });
});

// ══════════════════════════════════════════════════════════════════════════════
//  BUILD APP
// ══════════════════════════════════════════════════════════════════════════════
// Setelah semua service didaftarkan, build WebApplication yang siap menerima request
var app = builder.Build();

// ── Auto Migrate & Seed database saat startup ─────────────────────────────
// Membuat scope baru karena DbContext bersifat Scoped (tidak bisa diakses dari Singleton context)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Migrate() menjalankan semua migrasi yang belum diapply secara otomatis.
    // Jika database belum ada, akan dibuat terlebih dahulu.
    db.Database.Migrate();
}

// ══════════════════════════════════════════════════════════════════════════════
//  6. MIDDLEWARE PIPELINE
//  ⚠️ URUTAN SANGAT PENTING! Setiap middleware memproses request secara berurutan.
// ══════════════════════════════════════════════════════════════════════════════
if (app.Environment.IsDevelopment())
{
    // Di Development: aktifkan Swagger UI untuk eksplorasi API
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Catalog API v1");
        c.RoutePrefix = string.Empty; // Swagger tersedia di root URL "/" bukan "/swagger"
    });
}
else
{
    // Di Production: aktifkan HSTS (HTTP Strict Transport Security)
    // Memberi tahu browser untuk selalu gunakan HTTPS untuk domain ini
    app.UseHsts();
}

app.UseHttpsRedirection();   // 1. Redirect semua request HTTP → HTTPS otomatis
app.UseCors("DevPolicy");    // 2. Terapkan CORS policy yang sudah dikonfigurasi
app.UseRateLimiter();        // 3. Periksa batas request sebelum request diproses lebih lanjut
app.UseAuthentication();     // 4. Baca header Authorization, validasi JWT, set User.Identity
app.UseAuthorization();      // 5. Periksa [Authorize] attribute — pastikan user punya hak akses

app.MapControllers();        // 6. Routing: teruskan request ke controller & action yang sesuai

// Mulai mendengarkan request HTTP
app.Run();
