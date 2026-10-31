using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.Auth.Commands.AcceptInvite;

public sealed class AcceptInviteCommandHandler : ICommandHandler<AcceptInviteCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AcceptInviteCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task HandleAsync(AcceptInviteCommand command, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByInviteTokenAsync(command.Token, ct)
            ?? throw new AppException("auth.invalid_invite_link", "Invalid or expired invite link.");

        if (user.InviteTokenExpiry is null || user.InviteTokenExpiry < DateTime.UtcNow)
            throw new AppException("auth.invalid_invite_link", "Invalid or expired invite link.");

        user.AcceptInvite(_passwordHasher.Hash(command.NewPassword));
        await _userRepository.UpdateAsync(user, ct);
    }
}
