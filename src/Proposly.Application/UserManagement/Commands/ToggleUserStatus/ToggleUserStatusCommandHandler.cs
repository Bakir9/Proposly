using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.UserManagement.Commands.ToggleUserStatus;

public sealed class ToggleUserStatusCommandHandler : ICommandHandler<ToggleUserStatusCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public ToggleUserStatusCommandHandler(IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(ToggleUserStatusCommand command, CancellationToken ct = default)
    {
        if (command.UserId == _currentUser.UserId)
            throw new InvalidOperationException("You cannot change your own status.");

        var user = await _userRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"User {command.UserId} not found.");

        if (command.Disable)
            user.Disable();
        else
            user.Enable();

        await _userRepository.UpdateAsync(user, ct);
    }
}
