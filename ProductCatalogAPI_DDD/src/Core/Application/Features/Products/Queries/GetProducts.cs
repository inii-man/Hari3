using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ProductCatalogAPI.Application.DTOs;
using ProductCatalogAPI.Domain.Interfaces;

namespace ProductCatalogAPI.Application.Features.Products.Queries;

/// <summary>
/// Query untuk mengambil semua Produk. 
/// Dalam CQRS, operasi baca (Queries) dipisahkan dari operasi tulis (Commands).
/// </summary>
public record GetProductsQuery() : IRequest<IEnumerable<ProductDto>>;

/// <summary>
/// Handler untuk memproses GetProductsQuery.
/// </summary>
public class GetProductsHandler : IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync();

        return products.Select(p => new ProductDto(
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.Stock,
            p.Category,
            p.CreatedAt
        ));
    }
}
