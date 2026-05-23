using Proposly.Application.Abstractions;

namespace Proposly.Application.CalendarManagement.Commands.UpdateTermin;

public record UpdateTerminCommand(Guid TerminId, string Title, string? Description, string? Location) : ICommand;
