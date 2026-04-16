using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.UserManagement.Commands.UpdateUserRole;

public sealed class UpdateUserRoleCommandHandler : ICommandHandler<UpdateUserRoleCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public UpdateUserRoleCommandHandler(IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(UpdateUserRoleCommand command, CancellationToken ct = default)
    {
        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
            throw new ArgumentException($"Invalid role '{command.Role}'. Valid values: Owner, Admin, Member.");

        if (role == UserRole.Owner)
            throw new InvalidOperationException("Cannot transfer Owner role. The Owner is set at registration.");

        var user = await _userRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"User {command.UserId} not found.");

        if (user.Role == UserRole.Owner)
            throw new InvalidOperationException("Cannot change the role of the company Owner.");

        user.UpdateRole(role);
        await _userRepository.UpdateAsync(user, ct);
    }
}
