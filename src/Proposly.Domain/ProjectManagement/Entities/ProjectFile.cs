using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.ProjectManagement.Entities;

/// <summary>
/// A file uploaded directly to a project (Collaborate › Files). Bytes live behind IFileStorage;
/// this row is the metadata. Chat attachments and receipts stay in their own tables — the Files
/// page aggregates them in the read model.
/// </summary>
public sealed class ProjectFile : Entity<Guid>, ITenantEntity, IAuditableEntity
{
    private ProjectFile() { } // For EF Core

    private ProjectFile(
        Guid id,
        Guid companyId,
        Guid projectId,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        Guid uploadedByUserId,
        string uploadedByName)
        : base(id)
    {
        CompanyId = companyId;
        ProjectId = projectId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        UploadedByUserId = uploadedByUserId;
        UploadedByName = uploadedByName;
    }

    public static ProjectFile Create(
        Guid companyId,
        Guid projectId,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        Guid uploadedByUserId,
        string uploadedByName)
        => new(Guid.NewGuid(), companyId, projectId, fileName, contentType, sizeBytes, storageKey, uploadedByUserId, uploadedByName);

    public Guid CompanyId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public Guid UploadedByUserId { get; private set; }

    /// <summary>Name snapshot at upload time — same pattern as ChatMessage.AuthorName.</summary>
    public string UploadedByName { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
}
