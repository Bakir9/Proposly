using Proposly.Application.Abstractions;

namespace Proposly.Application.CalendarManagement.Commands.RescheduleTermin;

public record RescheduleTerminCommand(Guid TerminId, DateTime NewStart, DateTime NewEnd) : ICommand;
