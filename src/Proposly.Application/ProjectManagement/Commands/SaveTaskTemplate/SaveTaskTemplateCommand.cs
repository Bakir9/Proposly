using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.SaveTaskTemplate;

/// <summary>Creates a company-wide task template: a parent task name plus its ordered steps.</summary>
public sealed record SaveTaskTemplateCommand(
    string Name,
    string? Description,
    IReadOnlyList<SaveTaskTemplateStep> Steps) : ICommand<Guid>;

public sealed record SaveTaskTemplateStep(string Title, decimal? EstimatedHours);
