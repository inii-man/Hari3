using System;

namespace ProductCatalogAPI.Application.DTOs;

/// <summary>
/// Data Transfer Object untuk mengirim data Produk ke Client.
/// Tidak mengandung logika bisnis, hanya properti untuk serialisasi.
/// </summary>
public record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    DateTime CreatedAt
);
