using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.AddTask;

public record AddTaskCommand(Guid ProjectId, string Title, string? Description, decimal? EstimatedHours, DateOnly? StartDate, DateOnly? DueDate, Guid? MilestoneId, Guid? AssignedMemberId) : ICommand<Guid>;
