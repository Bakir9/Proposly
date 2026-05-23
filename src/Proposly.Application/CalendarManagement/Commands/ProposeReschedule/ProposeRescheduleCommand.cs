using Proposly.Application.Abstractions;

namespace Proposly.Application.CalendarManagement.Commands.ProposeReschedule;

public record ProposeRescheduleCommand(Guid TerminId, DateTime ProposedStart, DateTime ProposedEnd, string? Message) : ICommand;
