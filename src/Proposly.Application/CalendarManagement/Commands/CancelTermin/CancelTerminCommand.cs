using Proposly.Application.Abstractions;

namespace Proposly.Application.CalendarManagement.Commands.CancelTermin;

public record CancelTerminCommand(Guid TerminId) : ICommand;
