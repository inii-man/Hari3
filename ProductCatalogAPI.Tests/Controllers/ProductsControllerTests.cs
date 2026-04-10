using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Controllers;
using ProductCatalogAPI.Data;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Tests.Controllers;

public class ProductsControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        // Setup EF Core In-Memory Database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;

        _db = new AppDbContext(options);

        // Seed initial data
        _db.Products.AddRange(
            new Product { Id = 1, Name = "Laptop", Price = 15000000, Category = "Electronics", Stock = 10, Description = "Dell Laptop", CreatedAt = System.DateTime.UtcNow },
            new Product { Id = 2, Name = "Mouse", Price = 150000, Category = "Accessories", Stock = 50, Description = "Wireless Mouse", CreatedAt = System.DateTime.UtcNow }
        );
        _db.SaveChanges();

        // Initialize Controller
        _controller = new ProductsController(_db);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithAllProducts()
    {
        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<IEnumerable<Product>>(okResult.Value);
        Assert.Equal(2, products.Count());
    }

    [Fact]
    public async Task GetById_ValidId_ReturnsOkResult_WithProduct()
    {
        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var product = Assert.IsType<Product>(okResult.Value);
        Assert.Equal(1, product.Id);
        Assert.Equal("Laptop", product.Name);
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFoundResult()
    {
        // Act
        var result = await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_ValidProduct_ReturnsCreatedAtAction()
    {
        // Arrange
        var newProduct = new Product { Name = "Keyboard", Price = 500000, Category = "Accessories", Stock = 20 };

        // Act
        var result = await _controller.Create(newProduct);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var product = Assert.IsType<Product>(createdResult.Value);
        Assert.Equal("Keyboard", product.Name);
        Assert.NotEqual(0, product.Id); // ID should be generated
    }

    [Fact]
    public async Task Create_InvalidProductPrice_ReturnsBadRequest()
    {
        // Arrange
        var newProduct = new Product { Name = "Free Item", Price = 0, Category = "Promo" };

        // Act
        var result = await _controller.Create(newProduct);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Harga produk harus lebih dari 0", badRequestResult.Value?.ToString() ?? "");
    }
}
