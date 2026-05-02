using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Entities;

public sealed class ProjectNote : Entity<Guid>
{
    private ProjectNote() { } // For EF Core

    private ProjectNote(Guid id, Guid projectId, string title, string content, Guid authorId, string authorName)
        : base(id)
    {
        ProjectId = projectId;
        Title = title;
        Content = content;
        AuthorId = authorId;
        AuthorName = authorName;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static ProjectNote Create(Guid projectId, string title, string content, Guid authorId, string authorName)
        => new(Guid.NewGuid(), projectId, title, content, authorId, authorName);

    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public Guid AuthorId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void Update(string title, string content)
    {
        Title = title;
        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }
}
