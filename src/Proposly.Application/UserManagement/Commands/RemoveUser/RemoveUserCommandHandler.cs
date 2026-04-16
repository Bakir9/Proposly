using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.UserManagement.Commands.RemoveUser;

public sealed class RemoveUserCommandHandler : ICommandHandler<RemoveUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public RemoveUserCommandHandler(IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(RemoveUserCommand command, CancellationToken ct = default)
    {
        if (command.UserId == _currentUser.UserId)
            throw new InvalidOperationException("You cannot remove yourself.");

        var user = await _userRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"User {command.UserId} not found.");

        if (user.Role == UserRole.Owner)
            throw new InvalidOperationException("Cannot remove the company Owner.");

        await _userRepository.RemoveAsync(user, ct);
    }
}
