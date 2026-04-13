using System.Collections.Generic;
using System.Threading.Tasks;
using ProductCatalogAPI.Domain.Entities;

namespace ProductCatalogAPI.Domain.Interfaces;

/// <summary>
/// Abstraksi Repository untuk Produk. 
/// Sesuai prinsip DDD, interface ini didefinisikan di Layer Domain.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
}
