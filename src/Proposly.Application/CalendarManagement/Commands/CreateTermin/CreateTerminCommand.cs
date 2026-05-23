using Proposly.Application.Abstractions;

namespace Proposly.Application.CalendarManagement.Commands.CreateTermin;

public record CreateTerminCommand(
    string Title,
    string? Description,
    DateTime Start,
    DateTime End,
    string? Location,
    IReadOnlyList<Guid> InviteeUserIds) : ICommand<Guid>;
