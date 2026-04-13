using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ProductCatalogAPI.Application.DTOs;
using ProductCatalogAPI.Domain.Interfaces;

namespace ProductCatalogAPI.Application.Features.Products.Commands;

public record UpdateProductCommand(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category
) : IRequest<ProductDto?>;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ProductDto?>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto?> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id);
        if (product == null) return null;

        // LOGIKA DOMAIN : Panggil method di entity, bukan langsung set properti.
        product.UpdateDetails(request.Name, request.Description, request.Category);
        product.UpdatePrice(request.Price);
        product.UpdateStock(request.Stock);

        await _productRepository.UpdateAsync(product);

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
