namespace Proposly.Application.ProjectManagement.Responses;

/// <summary>A reusable task recipe: parent task name plus ordered steps.</summary>
public sealed record TaskTemplateResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal? TotalEstimatedHours,
    IReadOnlyList<TaskTemplateStepResponse> Steps);

public sealed record TaskTemplateStepResponse(
    Guid Id,
    string Title,
    decimal? EstimatedHours,
    int SortOrder);
