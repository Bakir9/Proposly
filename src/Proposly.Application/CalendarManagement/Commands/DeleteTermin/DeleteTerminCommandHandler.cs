using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.CalendarManagement.Commands.DeleteTermin;

public sealed class DeleteTerminCommandHandler : ICommandHandler<DeleteTerminCommand>
{
    private readonly ITerminRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public DeleteTerminCommandHandler(ITerminRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(DeleteTerminCommand command, CancellationToken cancellationToken = default)
    {
        var termin = await _repository.GetByIdAsync(command.TerminId, cancellationToken)
            ?? throw AppException.NotFound("termin.not_found", "Termin not found.");
        if (termin.OrganizerId != _currentUser.UserId && _currentUser.Role == "Member")
            throw new UnauthorizedAccessException("Only the organizer can delete this meeting.");

        await _repository.DeleteAsync(termin, cancellationToken);
    }
}
