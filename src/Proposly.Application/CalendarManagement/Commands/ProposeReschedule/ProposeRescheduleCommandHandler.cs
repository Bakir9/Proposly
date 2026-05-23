using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Repositories;

namespace Proposly.Application.CalendarManagement.Commands.ProposeReschedule;

public sealed class ProposeRescheduleCommandHandler : ICommandHandler<ProposeRescheduleCommand>
{
    private readonly ITerminRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ProposeRescheduleCommandHandler(ITerminRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(ProposeRescheduleCommand command, CancellationToken cancellationToken = default)
    {
        var termin = await _repository.GetByIdAsync(command.TerminId, cancellationToken)
            ?? throw new InvalidOperationException("Termin not found.");

        termin.ProposeReschedule(_currentUser.UserId, command.ProposedStart, command.ProposedEnd, command.Message);
        await _repository.UpdateAsync(termin, cancellationToken);
    }
}
