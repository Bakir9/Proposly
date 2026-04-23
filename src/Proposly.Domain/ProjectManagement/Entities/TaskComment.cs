using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Entities;

public sealed class TaskComment : Entity<Guid>
{
    private TaskComment() { }

    private TaskComment(Guid id, Guid taskId, Guid authorId, string authorName, string body) : base(id)
    {
        TaskId = taskId;
        AuthorId = authorId;
        AuthorName = authorName;
        Body = body;
        CreatedAt = DateTime.UtcNow;
    }

    public static TaskComment Create(Guid taskId, Guid authorId, string authorName, string body)
        => new(Guid.NewGuid(), taskId, authorId, authorName, body);

    public Guid TaskId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public void Edit(Guid editorId, string newBody)
    {
        if (AuthorId != editorId)
            throw new UnauthorizedAccessException("Only the author can edit this comment.");
        Body = newBody;
        UpdatedAt = DateTime.UtcNow;
    }
}
