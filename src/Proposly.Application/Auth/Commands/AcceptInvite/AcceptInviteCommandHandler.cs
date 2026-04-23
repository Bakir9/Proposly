using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;

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
            ?? throw new InvalidOperationException("Invalid or expired invite link.");

        if (user.InviteTokenExpiry is null || user.InviteTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired invite link.");

        user.AcceptInvite(_passwordHasher.Hash(command.NewPassword));
        await _userRepository.UpdateAsync(user, ct);
    }
}
