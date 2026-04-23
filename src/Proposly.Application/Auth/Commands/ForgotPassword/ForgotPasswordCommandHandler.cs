using System.Security.Cryptography;
using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IAppSettings _appSettings;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IAppSettings appSettings)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _appSettings = appSettings;
    }

    public async Task HandleAsync(ForgotPasswordCommand command, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email, ct);
        if (user is null) return; // Never reveal whether the email exists

        var rawToken = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(rawToken)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        user.SetPasswordResetToken(token, DateTime.UtcNow.AddHours(1));
        await _userRepository.UpdateAsync(user, ct);

        var resetLink = $"{_appSettings.AppUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        var html = $"""
            <p>Hi {user.FirstName},</p>
            <p>You requested a password reset for your Proposly account.</p>
            <p><a href="{resetLink}">Click here to reset your password</a></p>
            <p>This link expires in 1 hour. If you didn't request this, you can safely ignore this email.</p>
            """;

        await _emailService.SendAsync(user.Email, "Reset your Proposly password", html, ct: ct);
    }
}
