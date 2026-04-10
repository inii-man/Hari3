using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProductCatalogAPI.Controllers;
using ProductCatalogAPI.Data;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Tests.Controllers;

/// <summary>
/// Unit test untuk AuthController (Register dan Login).
/// Menggunakan EF Core InMemory Database + IConfiguration in-memory
/// sehingga tidak butuh database atau config file nyata.
/// Konvensi penamaan: MethodName_Scenario_ExpectedResult
/// Pola: AAA (Arrange - Act - Assert)
/// </summary>
public class AuthControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        // ── InMemory Database (unique per test) ───────────────────────────────
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);

        // ── IConfiguration dengan JWT settings minimal ────────────────────────
        // ConfigurationBuilder dengan AddInMemoryCollection = simulasi appsettings.json
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]    = "test-secret-key-that-is-at-least-32-characters-long!",
                ["Jwt:Issuer"] = "TestIssuer"
            })
            .Build();

        _controller = new AuthController(_db, config);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    // =========================================================================
    //  REGISTER
    // =========================================================================

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Password = "password123",
            Role     = "User"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Registrasi berhasil", okResult.Value?.ToString() ?? "");
    }

    [Fact]
    public async Task Register_EmptyUsername_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest { Username = "", Password = "password123" };

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        // Arrange — password < 6 karakter
        var request = new RegisterRequest { Username = "user1", Password = "abc" };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("minimal 6 karakter", badRequest.Value?.ToString() ?? "");
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsBadRequest()
    {
        // Arrange — daftarkan user pertama dulu
        var first = new RegisterRequest { Username = "dupuser", Password = "password123" };
        await _controller.Register(first);

        // Coba daftar lagi dengan username yang sama
        var duplicate = new RegisterRequest { Username = "dupuser", Password = "anotherpass" };

        // Act
        var result = await _controller.Register(duplicate);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("sudah digunakan", badRequest.Value?.ToString() ?? "");
    }

    [Fact]
    public async Task Register_InvalidRole_DefaultsToUserRole()
    {
        // Arrange — role tidak valid
        var request = new RegisterRequest
        {
            Username = "roleless",
            Password = "password123",
            Role     = "SuperAdmin" // tidak valid
        };

        // Act
        var result = await _controller.Register(request);

        // Assert — role harus di-default ke "User"
        Assert.IsType<OkObjectResult>(result);
        var saved = await _db.Users.FirstOrDefaultAsync(u => u.Username == "roleless");
        Assert.NotNull(saved);
        Assert.Equal("User", saved!.Role);
    }

    // =========================================================================
    //  LOGIN
    // =========================================================================

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkResult_WithToken()
    {
        // Arrange — daftarkan user dulu, baru login
        await _controller.Register(new RegisterRequest
        {
            Username = "adminuser",
            Password = "securepass",
            Role     = "Admin"
        });

        var loginRequest = new User { Username = "adminuser", Password = "securepass" };

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var body     = okResult.Value?.ToString() ?? "";
        Assert.Contains("Login berhasil", body);
        Assert.Contains("token", body.ToLower());
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        await _controller.Register(new RegisterRequest
        {
            Username = "testuser",
            Password = "correctpass",
        });

        var loginRequest = new User { Username = "testuser", Password = "wrongpass" };

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsUnauthorized()
    {
        // Arrange — user tidak pernah didaftarkan
        var loginRequest = new User { Username = "ghost", Password = "nopass" };

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_EmptyCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new User { Username = "", Password = "" };

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
