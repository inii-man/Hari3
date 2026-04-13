using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ProductCatalogAPI.Application.Interfaces;
using ProductCatalogAPI.Domain.Entities;
using ProductCatalogAPI.Domain.Interfaces;

namespace ProductCatalogAPI.Application.Features.Auth.Commands;

public record RegisterUserCommand(string Username, string Password, string Role) : IRequest<bool>;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Cek apakah user sudah ada.
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUser != null) return false;

        // 2. Hash Password.
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // 3. Buat User baru (Rich Domain Model).
        var user = new User(request.Username, passwordHash, request.Role);

        // 4. Simpan.
        await _userRepository.AddAsync(user);
        return true;
    }
}
