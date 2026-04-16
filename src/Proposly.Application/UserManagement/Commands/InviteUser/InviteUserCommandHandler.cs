using Proposly.Application.Abstractions;
using Proposly.Application.UserManagement.Responses;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.UserManagement.Commands.InviteUser;

public sealed class InviteUserCommandHandler : ICommandHandler<InviteUserCommand, UserDetailResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;

    public InviteUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<UserDetailResponse> HandleAsync(InviteUserCommand command, CancellationToken ct = default)
    {
        if (await _userRepository.ExistsByEmailAsync(command.Email, ct))
            throw new InvalidOperationException($"Email '{command.Email}' is already registered.");

        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
            throw new ArgumentException($"Invalid role '{command.Role}'. Valid values: Owner, Admin, Member.");

        // Owner role can only be assigned by the system (registration), not by invite
        if (role == UserRole.Owner)
            throw new InvalidOperationException("Cannot assign Owner role via invite. A company can only have one Owner.");

        var passwordHash = _passwordHasher.Hash(command.Password);
        var user = User.Create(_currentUser.CompanyId, command.Email, passwordHash, command.FirstName, command.LastName, role);

        await _userRepository.AddAsync(user, ct);

        return new UserDetailResponse(user.Id, user.FirstName, user.LastName, user.FullName, user.Email, user.Role.ToString(), user.CompanyId, user.CreatedAt);
    }
}
