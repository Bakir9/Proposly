using Proposly.Application.Abstractions;

namespace Proposly.Application.CalendarManagement.Commands.RespondToProposal;

// Accept the proposed new time (reschedules the whole meeting) or decline it
public record RespondToProposalCommand(Guid TerminId, Guid InviteeId, bool Accept) : ICommand;
