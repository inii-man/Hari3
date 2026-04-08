namespace ProductCatalogAPI.Models;

/// <summary>
/// DTO (Data Transfer Object) — digunakan hanya untuk menerima data dari request body Login.
/// Class ini TIDAK disimpan ke database; hanya sebagai carrier data input dari client.
/// </summary>
public class User
{
    /// <summary>Username yang diketik user di form login.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password plaintext dari user — HANYA digunakan sementara untuk diverifikasi
    /// dengan BCrypt, tidak pernah disimpan ke database.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Entity yang merepresentasikan baris di tabel "Users" di database.
/// Berbeda dari <see cref="User"/> (DTO), class ini menyimpan hash password, bukan plaintext.
/// </summary>
public class UserEntity
{
    /// <summary>Primary key — di-generate otomatis oleh database.</summary>
    public int Id { get; set; }

    /// <summary>Username unik user. Divalidasi tidak boleh duplikat di AuthController.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hash BCrypt dari password user. BCrypt menghasilkan hash satu arah (one-way),
    /// sehingga password asli tidak bisa didapat kembali dari hash ini.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Role user untuk role-based access control. Nilai valid: "Admin" atau "User".
    /// Default value "User" otomatis diset jika tidak dispesifikasikan saat register.
    /// </summary>
    public string Role { get; set; } = "User";
}
