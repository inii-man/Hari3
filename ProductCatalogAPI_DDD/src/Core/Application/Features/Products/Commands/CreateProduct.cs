using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ProductCatalogAPI.Application.DTOs;
using ProductCatalogAPI.Domain.Entities;
using ProductCatalogAPI.Domain.Interfaces;

namespace ProductCatalogAPI.Application.Features.Products.Commands;

/// <summary>
/// Command untuk membuat Produk baru. 
/// Kita tidak lagi mengirim Entity langsung ke Controller, tapi melalui Command ini.
/// </summary>
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category
) : IRequest<ProductDto>;

/// <summary>
/// Handler yang berisi logika aplikasi untuk memproses CreateProductCommand.
/// </summary>
public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;

    public CreateProductHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Instansiasi Rich Domain Model (Validasi sudah ada di dalam konstruktor Product).
        var product = new Product(
            request.Name,
            request.Description,
            request.Price,
            request.Stock,
            request.Category
        );

        // 2. Simpan ke Repository.
        await _productRepository.AddAsync(product);

        // 3. Kembalikan dalam bentuk DTO.
        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.Category,
            product.CreatedAt
        );
    }
}
