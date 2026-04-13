using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ProductCatalogAPI.Application.Interfaces;
using ProductCatalogAPI.Domain.Interfaces;

namespace ProductCatalogAPI.Application.Features.Auth.Commands;

public record LoginUserCommand(string Username, string Password) : IRequest<string?>;

public class LoginUserHandler : IRequestHandler<LoginUserCommand, string?>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginUserHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<string?> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Cari user.
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null) return null;

        // 2. Verifikasi passsword.
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        // 3. Generate token.
        return _tokenService.CreateToken(user);
    }
}
