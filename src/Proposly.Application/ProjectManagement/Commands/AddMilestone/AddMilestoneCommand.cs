using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.AddMilestone;

public record AddMilestoneCommand(Guid ProjectId, string Title, DateOnly DueDate) : ICommand<Guid>;
