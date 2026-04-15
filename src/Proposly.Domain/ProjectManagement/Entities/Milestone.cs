using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Entities;

public sealed class Milestone : Entity<Guid>
{
    private Milestone() { } // For EF Core

    private Milestone(Guid id, Guid projectId, string title, DateOnly dueDate)
        : base(id)
    {
        ProjectId = projectId;
        Title = title;
        DueDate = dueDate;
    }

    public static Milestone Create(Guid projectId, string title, DateOnly dueDate)
        => new(Guid.NewGuid(), projectId, title, dueDate);

    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateOnly DueDate { get; private set; }
    public bool IsCompleted { get; private set; }

    public void Complete() => IsCompleted = true;
}
