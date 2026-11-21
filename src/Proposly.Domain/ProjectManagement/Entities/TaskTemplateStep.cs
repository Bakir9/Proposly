using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Entities;

/// <summary>One ordered step of a <see cref="TaskTemplate"/>; becomes a sub-task when applied.</summary>
public sealed class TaskTemplateStep : Entity<Guid>
{
    private TaskTemplateStep() { } // For EF Core

    private TaskTemplateStep(Guid id, Guid templateId, string title, decimal? estimatedHours, int sortOrder)
        : base(id)
    {
        TemplateId = templateId;
        Title = title;
        EstimatedHours = estimatedHours;
        SortOrder = sortOrder;
    }

    public static TaskTemplateStep Create(Guid templateId, string title, decimal? estimatedHours, int sortOrder)
        => new(Guid.NewGuid(), templateId, title, estimatedHours, sortOrder);

    public Guid TemplateId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public decimal? EstimatedHours { get; private set; }
    public int SortOrder { get; private set; }
}
