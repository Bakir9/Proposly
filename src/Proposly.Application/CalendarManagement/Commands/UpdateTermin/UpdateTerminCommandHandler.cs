using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Repositories;

namespace Proposly.Application.CalendarManagement.Commands.UpdateTermin;

public sealed class UpdateTerminCommandHandler : ICommandHandler<UpdateTerminCommand>
{
    private readonly ITerminRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public UpdateTerminCommandHandler(ITerminRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(UpdateTerminCommand command, CancellationToken cancellationToken = default)
    {
        var termin = await _repository.GetByIdAsync(command.TerminId, cancellationToken)
            ?? throw new InvalidOperationException("Termin not found.");
        if (termin.OrganizerId != _currentUser.UserId && _currentUser.Role == "Member")
            throw new UnauthorizedAccessException("Only the organizer can update this meeting.");

        termin.Update(command.Title, command.Description, command.Location);
        await _repository.UpdateAsync(termin, cancellationToken);
    }
}
