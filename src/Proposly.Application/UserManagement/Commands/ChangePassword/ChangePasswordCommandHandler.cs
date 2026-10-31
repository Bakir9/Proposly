using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.UserManagement.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(IUserRepository repository, ICurrentUserService currentUser, IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(_currentUser.UserId, cancellationToken)
            ?? throw AppException.NotFound("current_user.not_found", "Current user not found.");

        if (!_passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        user.ChangePassword(_passwordHasher.Hash(command.NewPassword));
        await _repository.UpdateAsync(user, cancellationToken);
    }
}
