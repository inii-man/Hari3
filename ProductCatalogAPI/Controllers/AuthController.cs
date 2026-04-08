using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductCatalogAPI.Data;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Controllers;

/// <summary>
/// Controller untuk autentikasi user: registrasi akun baru dan login.
/// Semua endpoint di sini bersifat publik (tidak memerlukan JWT token).
/// Route dasar: /api/auth
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Injeksi dependensi: AppDbContext untuk akses database, IConfiguration untuk appsettings
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    /// <summary>
    /// Constructor menerima AppDbContext dan IConfiguration melalui Dependency Injection.
    /// </summary>
    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Daftarkan akun baru. Default role: "User".
    /// Untuk mendaftar sebagai Admin, tambahkan field "role": "Admin" di body.
    /// </summary>
    /// <remarks>
    /// Rate limit: 50 request per 15 menit (mencegah pembuatan akun massal / spam).
    /// Password di-hash menggunakan BCrypt sebelum disimpan ke database.
    /// </remarks>
    [HttpPost("register")]           // Route: POST /api/auth/register
    [EnableRateLimiting("auth_policy")] // Terapkan rate limit ketat untuk endpoint auth
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // ── Validasi input dasar ─────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username dan password tidak boleh kosong." });

        if (request.Password.Length < 6)
            return BadRequest(new { message = "Password minimal 6 karakter." });

        // ── Cek duplikasi username ───────────────────────────────────────────
        // AnyAsync lebih efisien dari FirstOrDefaultAsync karena hanya cek keberadaan, tidak fetch data
        var userExists = await _db.Users.AnyAsync(u => u.Username == request.Username);
        if (userExists)
            return BadRequest(new { message = "Username sudah digunakan." });

        // ── Hash password dengan BCrypt ──────────────────────────────────────
        // BCrypt menghasilkan hash satu arah + salt otomatis; tidak bisa di-reverse
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // ── Tentukan role ────────────────────────────────────────────────────
        // Hanya "Admin" dan "User" yang valid; nilai lain (termasuk null) di-default ke "User"
        var role = request.Role?.Trim();
        if (role != "Admin" && role != "User")
            role = "User";

        // ── Buat dan simpan user baru ────────────────────────────────────────
        var newUser = new UserEntity
        {
            Username = request.Username.Trim(),
            PasswordHash = passwordHash,
            Role = role
        };

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync(); // eksekusi INSERT ke database

        return Ok(new
        {
            message = "Registrasi berhasil!",
            username = newUser.Username,
            role = newUser.Role
        });
    }

    /// <summary>
    /// Login dengan username dan password. Mengembalikan JWT token jika berhasil.
    /// Token berlaku selama 1 jam dan harus disertakan di header Authorization
    /// pada setiap request ke endpoint yang terproteksi.
    /// </summary>
    [HttpPost("login")]              // Route: POST /api/auth/login
    [EnableRateLimiting("auth_policy")] // Terapkan rate limit ketat — mencegah brute force
    public async Task<IActionResult> Login([FromBody] User request)
    {
        // ── Validasi input ───────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username dan password tidak boleh kosong." });

        // ── Cari user di database ────────────────────────────────────────────
        // Menggunakan FirstOrDefaultAsync untuk mendapat entity lengkap (butuh PasswordHash)
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null)
            // Pesan error dibuat generik — tidak memberitahu apakah username atau password yang salah
            return Unauthorized(new { message = "Username atau password salah." });

        // ── Verifikasi password ──────────────────────────────────────────────
        // BCrypt.Verify membandingkan plaintext dengan hash yang tersimpan di DB
        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
            return Unauthorized(new { message = "Username atau password salah." });

        // ── Generate JWT token ───────────────────────────────────────────────
        var token = GenerateToken(user);

        return Ok(new
        {
            message = "Login berhasil!",
            token,                   // JWT string yang perlu disimpan client
            username = user.Username,
            role = user.Role,
            expiresIn = "1 jam"
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private Helper: Generate JWT Token
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Membuat JWT token yang berisi informasi (claims) tentang user.
    /// Token ditandatangani dengan HMAC-SHA256 menggunakan secret key dari appsettings.
    /// </summary>
    /// <param name="user">UserEntity yang sudah terautentikasi.</param>
    /// <returns>JWT token string siap pakai (format: xxxxx.yyyyy.zzzzz).</returns>
    private string GenerateToken(UserEntity user)
    {
        // Claims adalah "pernyataan" tentang identitas user yang disematkan ke dalam token
        // Middleware Authorization membaca claims ini untuk cek role
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ID unik user
            new Claim(ClaimTypes.Name, user.Username),                // Username
            new Claim(ClaimTypes.Role, user.Role)                     // Role — dibaca oleh [Authorize(Roles = "...")]
        };

        // Buat signing key dari secret string di appsettings.json → Jwt:Key
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        // Kombinasi key + algoritma signing (HMAC SHA-256)
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Bangun struktur JWT token
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],  // Identitas penerbit token (divalidasi saat request)
            audience: null,                  // Audience tidak digunakan (ValidateAudience = false)
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1), // Token expired setelah 1 jam
            signingCredentials: creds);

        // Serialize token object menjadi string JWT (format: header.payload.signature)
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ─── DTO untuk endpoint Register ──────────────────────────────────────────────

/// <summary>
/// Data Transfer Object untuk request body endpoint POST /api/auth/register.
/// Dipisah dari <see cref="User"/> (DTO login) karena register butuh field Role opsional.
/// </summary>
public class RegisterRequest
{
    /// <summary>Username yang akan didaftarkan. Wajib diisi, harus unik.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Password plaintext. Wajib diisi, minimum 6 karakter.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Role opsional. Nilai valid: "Admin" atau "User".
    /// Jika tidak diisi atau nilai tidak valid, otomatis di-default ke "User".
    /// </summary>
    public string? Role { get; set; } // nullable — boleh tidak dikirim oleh client
}
