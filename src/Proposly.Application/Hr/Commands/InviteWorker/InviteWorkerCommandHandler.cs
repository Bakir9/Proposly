using System.Security.Cryptography;
using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Hr.Commands.InviteWorker;

public sealed class InviteWorkerCommandHandler(
    IUserRepository users,
    ICompanyRepository companies,
    IEmailService emailService,
    IAppSettings appSettings,
    ICurrentUserService currentUser) : ICommandHandler<InviteWorkerCommand>
{
    public async Task HandleAsync(InviteWorkerCommand command, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"Worker {command.UserId} not found.");

        if (!user.IsProfileOnly)
            throw new InvalidOperationException("This worker already has login access.");

        var company = await companies.GetByIdAsync(currentUser.CompanyId, ct)
            ?? throw new InvalidOperationException("Company not found.");

        // Login access is what counts toward the plan — profile-only workers are free.
        var invitedCount = await users.CountInvitedByCompanyIdAsync(currentUser.CompanyId, ct);
        if (company.IsUserLimitReached(invitedCount))
            throw new InvalidOperationException("User limit reached for your current plan. Upgrade to invite more users.");

        var email = command.Email.ToLowerInvariant();
        if (await users.ExistsByEmailAsync(email, ct))
            throw new InvalidOperationException($"Email '{command.Email}' is already registered.");

        var role = Enum.Parse<UserRole>(command.Role, ignoreCase: true);
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        // Email first: if SMTP fails nothing is persisted and the action can simply be retried.
        var acceptLink = $"{appSettings.AppUrl}/accept-invite?token={Uri.EscapeDataString(token)}";
        var html = $"""
            <p>Hi {user.FirstName},</p>
            <p>You've been invited to join a workspace on Proposly.</p>
            <p><a href="{acceptLink}">Click here to set your password and activate your account</a></p>
            <p>This link expires in 7 days.</p>
            """;
        await emailService.SendAsync(email, "You've been invited to Proposly", html, ct: ct);

        user.GrantLoginAccess(email, role, token, DateTime.UtcNow.AddDays(7));
        await users.UpdateAsync(user, ct);
    }
}
