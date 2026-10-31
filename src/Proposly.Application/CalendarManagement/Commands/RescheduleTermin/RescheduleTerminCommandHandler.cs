using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.CalendarManagement.Commands.RescheduleTermin;

public sealed class RescheduleTerminCommandHandler : ICommandHandler<RescheduleTerminCommand>
{
    private readonly ITerminRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public RescheduleTerminCommandHandler(ITerminRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(RescheduleTerminCommand command, CancellationToken cancellationToken = default)
    {
        var termin = await _repository.GetByIdAsync(command.TerminId, cancellationToken)
            ?? throw AppException.NotFound("termin.not_found", "Termin not found.");
        if (termin.OrganizerId != _currentUser.UserId)
            throw new UnauthorizedAccessException("Only the organizer can reschedule this meeting.");

        termin.Reschedule(command.NewStart, command.NewEnd);
        await _repository.UpdateAsync(termin, cancellationToken);
    }
}
