using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.UserManagement.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand>
{
    private readonly IUserRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public UpdateProfileCommandHandler(IUserRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(UpdateProfileCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(_currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Current user not found.");

        user.UpdateProfile(command.FirstName, command.LastName, command.Email);
        await _repository.UpdateAsync(user, cancellationToken);
    }
}
