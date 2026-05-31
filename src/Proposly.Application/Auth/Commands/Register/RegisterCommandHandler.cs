using System.Security.Cryptography;
using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IAppSettings _appSettings;

    public RegisterCommandHandler(
        ICompanyRepository companyRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IAppSettings appSettings)
    {
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _appSettings = appSettings;
    }

    public async Task HandleAsync(RegisterCommand command, CancellationToken ct = default)
    {
        if (await _userRepository.ExistsByEmailAsync(command.Email, ct))
            throw new InvalidOperationException($"Email '{command.Email}' is already registered.");

        var company = Company.Create(command.CompanyName);
        var passwordHash = _passwordHasher.Hash(command.Password);
        var user = User.Create(company.Id, command.Email, passwordHash, command.FirstName, command.LastName, UserRole.Owner);

        var rawToken = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(rawToken)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        user.SetEmailVerificationToken(token, DateTime.UtcNow.AddHours(24));

        await _companyRepository.AddAsync(company, ct);
        await _userRepository.AddAsync(user, ct);

        var verifyLink = $"{_appSettings.AppUrl}/verify-email?token={Uri.EscapeDataString(token)}";
        var html = $"""
            <p>Hi {user.FirstName},</p>
            <p>Welcome to Proposly! Please verify your email address to activate your account.</p>
            <p><a href="{verifyLink}">Verify my email address</a></p>
            <p>This link expires in 24 hours.</p>
            """;

        await _emailService.SendAsync(user.Email, "Verify your Proposly email", html, ct: ct);
    }
}
