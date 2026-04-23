using Microsoft.AspNetCore.Mvc;
using Proposly.Application.Abstractions;
using Proposly.Application.Auth.Commands.AcceptInvite;
using Proposly.Application.Auth.Commands.ForgotPassword;
using Proposly.Application.Auth.Commands.Login;
using Proposly.Application.Auth.Commands.Register;
using Proposly.Application.Auth.Commands.ResetPassword;
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

    /// <summary>Request a password reset email.</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        [FromServices] ICommandHandler<ForgotPasswordCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(command, ct);
        return Ok(); // Always 200 — never reveal whether the email exists
    }

    /// <summary>Reset password using a token from email.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        [FromServices] ICommandHandler<ResetPasswordCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(command, ct);
        return Ok();
    }

    /// <summary>Accept an invite and set a password to activate the account.</summary>
    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptInviteCommand command,
        [FromServices] ICommandHandler<AcceptInviteCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(command, ct);
        return Ok();
    }
}
