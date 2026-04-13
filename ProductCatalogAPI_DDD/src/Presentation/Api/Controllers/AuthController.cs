using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductCatalogAPI.Application.Features.Auth.Commands;

namespace ProductCatalogAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result) return BadRequest("Username already exists or invalid data.");
        
        return Ok("Registration successful.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserCommand command)
    {
        var token = await _mediator.Send(command);
        if (token == null) return Unauthorized("Invalid username or password.");

        return Ok(new { Token = token });
    }
}
