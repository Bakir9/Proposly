using Microsoft.AspNetCore.Mvc;
using Proposly.Application.Abstractions;
using Proposly.Application.Auth.Commands.Login;
using Proposly.Application.Auth.Commands.Register;
using Proposly.Application.Auth.Responses;

namespace Proposly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Create a new company and owner account.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterCommand command,
        [FromServices] ICommandHandler<RegisterCommand, AuthResponse> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }

    /// <summary>Login and receive a JWT token.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginCommand command,
        [FromServices] ICommandHandler<LoginCommand, AuthResponse> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }
}
