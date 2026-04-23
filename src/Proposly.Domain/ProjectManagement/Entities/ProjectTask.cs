using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Entities;

public sealed class ProjectTask : Entity<Guid>
{
    private readonly List<TaskComment> _comments = [];

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
    public IReadOnlyCollection<TaskComment> Comments => _comments.AsReadOnly();

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

    public TaskComment AddComment(Guid authorId, string authorName, string body)
    {
        var comment = TaskComment.Create(Id, authorId, authorName, body);
        _comments.Add(comment);
        return comment;
    }

    public void EditComment(Guid commentId, Guid editorId, string newBody)
    {
        var comment = _comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw new InvalidOperationException($"Comment {commentId} not found.");
        comment.Edit(editorId, newBody);
    }

    public void DeleteComment(Guid commentId, Guid userId, bool isAdmin)
    {
        var comment = _comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw new InvalidOperationException($"Comment {commentId} not found.");
        if (!isAdmin && comment.AuthorId != userId)
            throw new UnauthorizedAccessException("Only the author or an admin can delete this comment.");
        _comments.Remove(comment);
    }
}
