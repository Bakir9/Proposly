using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.AddTask;

public record AddTaskCommand(Guid ProjectId, string Title, string? Description, decimal? EstimatedHours, DateOnly? DueDate, Guid? MilestoneId, Guid? AssignedMemberId) : ICommand<Guid>;
