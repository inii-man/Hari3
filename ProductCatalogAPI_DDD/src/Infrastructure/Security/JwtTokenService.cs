using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProductCatalogAPI.Application.Interfaces;
using ProductCatalogAPI.Domain.Entities;

namespace ProductCatalogAPI.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(User user)
    {
        // 1. Ambil secret key dari config.
        var keyStr = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found in config.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));

        // 2. Tentukan Claims (Payload).
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role)
        };

        // 3. Signing Credentials.
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        // 4. Token Descriptor.
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddHours(2), // Token berlaku 2 jam
            SigningCredentials = creds,
            Issuer = _config["Jwt:Issuer"]
        };

        // 5. Generate Token.
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
