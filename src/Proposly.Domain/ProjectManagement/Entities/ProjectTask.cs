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
        DateOnly? startDate,
        DateOnly? dueDate,
        Guid? milestoneId,
        Guid? assignedMemberId)
        : base(id)
    {
        ProjectId = projectId;
        Title = title;
        Description = description;
        EstimatedHours = estimatedHours;
        StartDate = startDate;
        DueDate = dueDate;
        MilestoneId = milestoneId;
        AssignedMemberId = assignedMemberId;
        Status = ProjectTaskStatus.Todo;
    }

    public static ProjectTask Create(
        Guid projectId,
        string title,
        string? description,
        decimal? estimatedHours,
        DateOnly? startDate,
        DateOnly? dueDate,
        Guid? milestoneId = null,
        Guid? assignedMemberId = null)
        => new(Guid.NewGuid(), projectId, title, description, estimatedHours, startDate, dueDate, milestoneId, assignedMemberId);

    public Guid ProjectId { get; private set; }
    public Guid? MilestoneId { get; private set; }
    public Guid? AssignedMemberId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal? EstimatedHours { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? DueDate { get; private set; }

    public void UpdateDetails(string title, string? description, decimal? estimatedHours, DateOnly? startDate, DateOnly? dueDate, Guid? milestoneId, Guid? assignedMemberId)
    {
        Title = title;
        Description = description;
        EstimatedHours = estimatedHours;
        StartDate = startDate;
        DueDate = dueDate;
        MilestoneId = milestoneId;
        AssignedMemberId = assignedMemberId;
    }

    public void Start()
    {
        if (Status == ProjectTaskStatus.InProgress)
            throw new InvalidOperationException("Task is already in progress.");
        Status = ProjectTaskStatus.InProgress;
    }

    public void MoveToReview()
    {
        if (Status == ProjectTaskStatus.InReview)
            throw new InvalidOperationException("Task is already in review.");
        Status = ProjectTaskStatus.InReview;
    }

    public void Complete()
    {
        if (Status == ProjectTaskStatus.Done)
            throw new InvalidOperationException("Task is already completed.");
        Status = ProjectTaskStatus.Done;
    }

    public void Reopen()
    {
        if (Status == ProjectTaskStatus.Todo)
            throw new InvalidOperationException("Task is already in todo.");
        Status = ProjectTaskStatus.Todo;
    }
}
