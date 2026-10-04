using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Infrastructure.Services.Storage;

/// <summary>
/// Backing row for <see cref="DatabaseFileStorage"/>. Persistence detail of the IFileStorage seam,
/// not a domain concept — nothing outside this folder and its EF configuration references it.
/// ITenantEntity keeps blobs inside the uploading company via the global query filter.
/// </summary>
public sealed class StoredFile : Entity<Guid>, ITenantEntity
{
    private StoredFile() { } // For EF Core

    private StoredFile(Guid id, Guid companyId, string contentType, byte[] content) : base(id)
    {
        CompanyId = companyId;
        ContentType = contentType;
        Content = content;
        SizeBytes = content.LongLength;
        CreatedAt = DateTime.UtcNow;
    }

    public static StoredFile Create(Guid companyId, string contentType, byte[] content)
        => new(Guid.NewGuid(), companyId, contentType, content);

    public Guid CompanyId { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public byte[] Content { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }
}
