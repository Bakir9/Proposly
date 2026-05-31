using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Proposly.Application.Abstractions;
using Proposly.Application.Auth.Commands.AcceptInvite;
using Proposly.Application.Auth.Commands.ForgotPassword;
using Proposly.Application.Auth.Commands.Login;
using Proposly.Application.Auth.Commands.Register;
using Proposly.Application.Auth.Commands.ResetPassword;
using Proposly.Application.Auth.Commands.VerifyEmail;
using Proposly.Application.Auth.Responses;

namespace Proposly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Create a new company and owner account. Sends a verification email.</summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand command,
        [FromServices] ICommandHandler<RegisterCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(command, ct);
        return Ok(new { message = "Registration successful. Please check your email to verify your account." });
    }

    /// <summary>Verify email address using the token from the verification email.</summary>
    [HttpGet("verify-email")]
    public async Task<ActionResult<AuthResponse>> VerifyEmail(
        [FromQuery] string token,
        [FromServices] ICommandHandler<VerifyEmailCommand, AuthResponse> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new VerifyEmailCommand(token), ct);
        return Ok(result);
    }

    /// <summary>Login and receive a JWT token.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
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
    [EnableRateLimiting("auth")]
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
