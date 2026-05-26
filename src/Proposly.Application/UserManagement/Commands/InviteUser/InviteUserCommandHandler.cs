using System.Security.Cryptography;
using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.UserManagement.Commands.InviteUser;

public sealed class InviteUserCommandHandler : ICommandHandler<InviteUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IEmailService _emailService;
    private readonly IAppSettings _appSettings;
    private readonly ICurrentUserService _currentUser;

    public InviteUserCommandHandler(
        IUserRepository userRepository,
        ICompanyRepository companyRepository,
        IEmailService emailService,
        IAppSettings appSettings,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _companyRepository = companyRepository;
        _emailService = emailService;
        _appSettings = appSettings;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(InviteUserCommand command, CancellationToken ct = default)
    {
        var company = await _companyRepository.GetByIdAsync(_currentUser.CompanyId, ct)
            ?? throw new InvalidOperationException("Company not found.");

        var invitedCount = await _userRepository.CountInvitedByCompanyIdAsync(_currentUser.CompanyId, ct);
        if (company.IsUserLimitReached(invitedCount))
            throw new InvalidOperationException($"User limit reached for your current plan. Upgrade to invite more users.");

        if (await _userRepository.ExistsByEmailAsync(command.Email, ct))
            throw new InvalidOperationException($"Email '{command.Email}' is already registered.");

        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
            throw new ArgumentException($"Invalid role '{command.Role}'. Valid values: Admin, Member.");

        if (role == UserRole.Owner)
            throw new InvalidOperationException("Cannot assign Owner role via invite.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var user = User.CreateInvited(
            _currentUser.CompanyId,
            command.Email.ToLowerInvariant(),
            command.FirstName,
            command.LastName,
            role,
            token,
            DateTime.UtcNow.AddDays(7));

        await _userRepository.AddAsync(user, ct);

        var acceptLink = $"{_appSettings.AppUrl}/accept-invite?token={Uri.EscapeDataString(token)}";
        var html = $"""
            <p>Hi {command.FirstName},</p>
            <p>You've been invited to join a workspace on Proposly.</p>
            <p><a href="{acceptLink}">Click here to set your password and activate your account</a></p>
            <p>This link expires in 7 days.</p>
            """;

        await _emailService.SendAsync(command.Email, "You've been invited to Proposly", html, ct: ct);
    }
}
