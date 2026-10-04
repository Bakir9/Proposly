using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.Chat.Entities;

/// <summary>
/// A file uploaded into a conversation. Created with no message (pending) while the composer
/// uploads, then bound to a <see cref="ChatMessage"/> when the message is sent.
/// </summary>
public sealed class ChatAttachment : Entity<Guid>, ITenantEntity
{
    private ChatAttachment() { } // For EF Core

    private ChatAttachment(Guid id, Guid companyId, Guid conversationId, Guid uploaderUserId, string fileName, string contentType, long sizeBytes, string storageKey)
        : base(id)
    {
        CompanyId = companyId;
        ConversationId = conversationId;
        UploaderUserId = uploaderUserId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        CreatedAt = DateTime.UtcNow;
    }

    public static ChatAttachment Create(Guid companyId, Guid conversationId, Guid uploaderUserId, string fileName, string contentType, long sizeBytes, string storageKey)
        => new(Guid.NewGuid(), companyId, conversationId, uploaderUserId, fileName, contentType, sizeBytes, storageKey);

    public Guid CompanyId { get; private set; }
    public Guid ConversationId { get; private set; }

    /// <summary>Null while pending in the composer; set exactly once when the message is sent.</summary>
    public Guid? MessageId { get; private set; }

    public Guid UploaderUserId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }

    /// <summary>Opaque key into IFileStorage — never exposed to clients.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public void AttachTo(Guid messageId, Guid senderUserId)
    {
        if (MessageId is not null)
            throw new InvalidOperationException("This attachment already belongs to a message.");
        if (UploaderUserId != senderUserId)
            throw new InvalidOperationException("Only the uploader can send this attachment.");
        MessageId = messageId;
    }
}
