using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Entities;

public sealed class ProjectTask : Entity<Guid>
{
    private readonly List<TaskComment> _comments = [];
    private readonly List<ProjectTask> _blockedBy = [];

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
    public decimal? ActualHours { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateOnly? CompletedAt { get; private set; }
    public IReadOnlyCollection<TaskComment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<ProjectTask> BlockedBy => _blockedBy.AsReadOnly();

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

    public void AddBlocker(ProjectTask blocker)
    {
        if (blocker.Id == Id)
            throw new InvalidOperationException("A task cannot block itself.");
        if (_blockedBy.Any(b => b.Id == blocker.Id))
            return;
        if (IsReachableFrom(blocker, Id))
            throw new InvalidOperationException("Adding this dependency would create a circular dependency.");
        _blockedBy.Add(blocker);
    }

    public void RemoveBlocker(Guid blockingTaskId)
    {
        var blocker = _blockedBy.FirstOrDefault(b => b.Id == blockingTaskId);
        if (blocker is not null)
            _blockedBy.Remove(blocker);
    }

    private static bool IsReachableFrom(ProjectTask start, Guid targetId)
    {
        var visited = new HashSet<Guid>();
        var queue = new Queue<ProjectTask>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Id == targetId) return true;
            if (!visited.Add(current.Id)) continue;
            foreach (var b in current._blockedBy)
                queue.Enqueue(b);
        }
        return false;
    }

    public void Start()
    {
        if (Status == ProjectTaskStatus.InProgress)
            throw new InvalidOperationException("Task is already in progress.");
        if (_blockedBy.Any(b => b.Status != ProjectTaskStatus.Done))
            throw new InvalidOperationException("Cannot start a task that has incomplete dependencies.");
        Status = ProjectTaskStatus.InProgress;
    }

    public void MoveToReview(decimal actualHours)
    {
        if (Status == ProjectTaskStatus.InReview)
            throw new InvalidOperationException("Task is already in review.");
        ActualHours = actualHours;
        Status = ProjectTaskStatus.InReview;
    }

    public void Complete(decimal? actualHours = null)
    {
        if (Status == ProjectTaskStatus.Done)
            throw new InvalidOperationException("Task is already completed.");
        if (actualHours.HasValue)
            ActualHours = actualHours;
        if (!ActualHours.HasValue)
            throw new InvalidOperationException("Actual hours must be recorded before completing a task.");
        Status = ProjectTaskStatus.Done;
        CompletedAt = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public void Reopen()
    {
        if (Status == ProjectTaskStatus.Todo)
            throw new InvalidOperationException("Task is already in todo.");
        Status = ProjectTaskStatus.Todo;
        CompletedAt = null;
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
