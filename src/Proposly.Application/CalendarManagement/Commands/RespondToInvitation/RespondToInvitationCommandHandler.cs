using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Repositories;

namespace Proposly.Application.CalendarManagement.Commands.RespondToInvitation;

public sealed class RespondToInvitationCommandHandler : ICommandHandler<RespondToInvitationCommand>
{
    private readonly ITerminRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public RespondToInvitationCommandHandler(ITerminRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(RespondToInvitationCommand command, CancellationToken cancellationToken = default)
    {
        var termin = await _repository.GetByIdAsync(command.TerminId, cancellationToken)
            ?? throw new InvalidOperationException("Termin not found.");

        if (command.Action == "Accept")
            termin.AcceptInvitation(_currentUser.UserId);
        else
            termin.DeclineInvitation(_currentUser.UserId);

        await _repository.UpdateAsync(termin, cancellationToken);
    }
}
