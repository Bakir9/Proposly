using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.Notifications;

public sealed class Notification : Entity<Guid>, ITenantEntity, IAuditableEntity
{
    private Notification() { } // For EF Core

    private Notification(Guid id, Guid companyId, Guid userId, string title, string? link)
        : base(id)
    {
        CompanyId = companyId;
        UserId = userId;
        Title = title;
        Link = link;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Notification Create(Guid companyId, Guid userId, string title, string? link = null)
        => new(Guid.NewGuid(), companyId, userId, title, link);

    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public void MarkAsRead()
    {
        IsRead = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
