using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.UpdateTask;

public record UpdateTaskCommand(
    Guid ProjectId,
    Guid TaskId,
    string Title,
    string? Description,
    decimal? EstimatedHours,
    decimal? ActualHours,
    DateOnly? StartDate,
    DateOnly? DueDate,
    Guid? MilestoneId,
    Guid? AssignedMemberId) : ICommand;
