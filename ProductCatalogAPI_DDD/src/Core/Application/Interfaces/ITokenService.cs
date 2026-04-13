using ProductCatalogAPI.Domain.Entities;

namespace ProductCatalogAPI.Application.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}
