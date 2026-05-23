using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Repositories;

namespace Proposly.Application.CalendarManagement.Commands.RespondToProposal;

public sealed class RespondToProposalCommandHandler : ICommandHandler<RespondToProposalCommand>
{
    private readonly ITerminRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public RespondToProposalCommandHandler(ITerminRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(RespondToProposalCommand command, CancellationToken cancellationToken = default)
    {
        var termin = await _repository.GetByIdAsync(command.TerminId, cancellationToken)
            ?? throw new InvalidOperationException("Termin not found.");
        if (termin.OrganizerId != _currentUser.UserId)
            throw new UnauthorizedAccessException("Only the organizer can respond to reschedule proposals.");

        if (command.Accept)
            termin.AcceptRescheduleProposal(command.InviteeId);
        else
            termin.DeclineRescheduleProposal(command.InviteeId);

        await _repository.UpdateAsync(termin, cancellationToken);
    }
}
