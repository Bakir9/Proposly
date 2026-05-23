using Proposly.Application.Abstractions;

namespace Proposly.Application.CalendarManagement.Commands.DeleteTermin;

public record DeleteTerminCommand(Guid TerminId) : ICommand;
