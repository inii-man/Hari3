using System.Threading.Tasks;
using ProductCatalogAPI.Domain.Entities;

namespace ProductCatalogAPI.Domain.Interfaces;

/// <summary>
/// Abstraksi Repository untuk User. 
/// Memungkinkan Application Layer untuk memanggil data tanpa tahu detail implementasi database.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
