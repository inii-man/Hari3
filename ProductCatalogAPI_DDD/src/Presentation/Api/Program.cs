using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProductCatalogAPI.Application.Features.Products.Commands;
using ProductCatalogAPI.Application.Interfaces;
using ProductCatalogAPI.Domain.Interfaces;
using ProductCatalogAPI.Infrastructure.Persistence;
using ProductCatalogAPI.Infrastructure.Repositories;
using ProductCatalogAPI.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. KONFIGURASI SERVICES (DI CONTAINER)
// ==========================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger dengan dukungan JWT Authorize
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProductCatalogAPI DDD", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Masukkan token JWT saja (tanpa kata 'Bearer ')"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Database (EF Core PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MediatR (Register semua handler di assembly Application)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly));

// Dependency Injection untuk Repositories & Services
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "fallback_key_for_dev_only_must_be_at_least_64_characters_long_for_sha512_security_standard";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = false
        };
    });

var app = builder.Build();

// ==========================================
// 2. AUTO-MIGRATION SAAT STARTUP
// ==========================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Melakukan migrasi otomatis setiap kali aplikasi jalan
        context.Database.Migrate();
        Console.WriteLine("Database migration check completed successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Terjadi kesalahan saat migrasi database: {ex.Message}");
    }
}

// ==========================================
// 3. MIDDLEWARE PIPELINE
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
