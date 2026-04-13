using System;

namespace ProductCatalogAPI.Domain.Entities;

/// <summary>
/// Representasi "Rich Domain Model" untuk User.
/// State PasswordHash dan Role dilindungi agar tidak sembarang diubah (Encapsulasi).
/// </summary>
public class User
{
    public int Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "User"; // Default Role

    private User() { }

    public User(string username, string passwordHash, string role)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username tidak boleh kosong.");
        
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash tidak boleh kosong.");

        if (role != "Admin" && role != "User")
            throw new ArgumentException("Role tidak valid. Gunakan 'Admin' atau 'User'.");

        Username = username;
        PasswordHash = passwordHash;
        Role = role;
    }

    /// <summary>
    /// Logika untuk mengganti role hanya untuk admin yang berwenang (di sisi domain).
    /// </summary>
    public void ChangeRole(string newRole)
    {
        if (newRole != "Admin" && newRole != "User")
            throw new ArgumentException("Role tidak valid.");

        Role = newRole;
    }

    /// <summary>
    /// Update password hash setelah divalidasi dan di-hash di layer application/infrastructure.
    /// </summary>
    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash baru tidak boleh kosong.");

        PasswordHash = newPasswordHash;
    }
}
