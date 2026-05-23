using Proposly.Application.Abstractions;

namespace Proposly.Application.CalendarManagement.Commands.RespondToInvitation;

// Action: "Accept" or "Decline"
public record RespondToInvitationCommand(Guid TerminId, string Action) : ICommand;
