using Proposly.Shared.Primitives;

namespace Proposly.Domain.OfferManagement.Entities;

public sealed class ClientNote : Entity<Guid>
{
    private ClientNote() { } // For EF Core

    private ClientNote(Guid id, Guid clientId, string content, Guid authorId, string authorName)
        : base(id)
    {
        ClientId = clientId;
        Content = content;
        AuthorId = authorId;
        AuthorName = authorName;
        CreatedAt = DateTime.UtcNow;
    }

    public static ClientNote Create(Guid clientId, string content, Guid authorId, string authorName)
        => new(Guid.NewGuid(), clientId, content, authorId, authorName);

    public Guid ClientId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public Guid AuthorId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
}
