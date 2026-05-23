using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Repositories;

namespace Proposly.Application.CalendarManagement.Commands.CancelTermin;

public sealed class CancelTerminCommandHandler : ICommandHandler<CancelTerminCommand>
{
    private readonly ITerminRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public CancelTerminCommandHandler(ITerminRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(CancelTerminCommand command, CancellationToken cancellationToken = default)
    {
        var termin = await _repository.GetByIdAsync(command.TerminId, cancellationToken)
            ?? throw new InvalidOperationException("Termin not found.");
        if (termin.OrganizerId != _currentUser.UserId && _currentUser.Role == "Member")
            throw new UnauthorizedAccessException("Only the organizer can cancel this meeting.");

        termin.Cancel();
        await _repository.UpdateAsync(termin, cancellationToken);
    }
}
