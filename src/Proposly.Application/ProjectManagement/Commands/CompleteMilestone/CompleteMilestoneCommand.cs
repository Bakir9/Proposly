using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.CompleteMilestone;

public record CompleteMilestoneCommand(Guid ProjectId, Guid MilestoneId) : ICommand;
