using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.LogTime;

public record LogTimeCommand(Guid ProjectId, Guid MemberId, decimal HoursWorked, string? Description, DateOnly Date) : ICommand<Guid>;
