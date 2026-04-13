using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ProductCatalogAPI.Domain.Interfaces;

namespace ProductCatalogAPI.Application.Features.Products.Commands;

public record DeleteProductCommand(int Id) : IRequest<bool>;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id);
        if (product == null) return false;

        await _productRepository.DeleteAsync(product.Id);
        return true;
    }
}
