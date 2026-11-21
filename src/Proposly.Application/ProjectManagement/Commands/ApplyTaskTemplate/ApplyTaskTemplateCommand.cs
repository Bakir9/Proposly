using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.ApplyTaskTemplate;

/// <summary>
/// Creates the template's parent task plus one sub-task per step in the project.
/// Returns the parent task id.
/// </summary>
public sealed record ApplyTaskTemplateCommand(
    Guid ProjectId,
    Guid TemplateId,
    Guid? AssignedMemberId,
    DateOnly? StartDate,
    DateOnly? DueDate,
    Guid? MilestoneId) : ICommand<Guid>;
