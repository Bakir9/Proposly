using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Entities;

public sealed class ProjectTask : Entity<Guid>
{
    private ProjectTask() { } // For EF Core

    private ProjectTask(
        Guid id,
        Guid projectId,
        string title,
        string? description,
        decimal? estimatedHours,
        DateOnly? dueDate,
        Guid? milestoneId)
        : base(id)
    {
        ProjectId = projectId;
        Title = title;
        Description = description;
        EstimatedHours = estimatedHours;
        DueDate = dueDate;
        MilestoneId = milestoneId;
        Status = ProjectTaskStatus.Todo;
    }

    public static ProjectTask Create(
        Guid projectId,
        string title,
        string? description,
        decimal? estimatedHours,
        DateOnly? dueDate,
        Guid? milestoneId = null)
        => new(Guid.NewGuid(), projectId, title, description, estimatedHours, dueDate, milestoneId);

    public Guid ProjectId { get; private set; }
    public Guid? MilestoneId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal? EstimatedHours { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public DateOnly? DueDate { get; private set; }

    public void Start()
    {
        if (Status != ProjectTaskStatus.Todo)
            throw new InvalidOperationException("Only todo tasks can be started.");
        Status = ProjectTaskStatus.InProgress;
    }

    public void Complete()
    {
        if (Status == ProjectTaskStatus.Done)
            throw new InvalidOperationException("Task is already completed.");
        Status = ProjectTaskStatus.Done;
    }
}
