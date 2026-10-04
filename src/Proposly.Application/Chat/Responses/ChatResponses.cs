namespace Proposly.Application.Chat.Responses;

public sealed record ChatParticipantResponse(
    Guid UserId,
    string Name,
    string Role);

public sealed record ConversationResponse(
    Guid Id,
    string Kind,
    string Title,
    string Subtitle,
    Guid? ProjectId,
    DateTime? LastMessageAt,
    string? LastMessagePreview,
    bool LastMessageHasAttachmentsOnly,
    string? LastMessageAuthorFirstName,
    bool LastMessageIsOwn,
    int UnreadCount,
    IReadOnlyList<ChatParticipantResponse> Participants);

public sealed record ChatAttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid AuthorUserId,
    string AuthorName,
    string? Body,
    DateTime CreatedAt,
    IReadOnlyList<ChatAttachmentResponse> Attachments);

public sealed record ChatMessagesPageResponse(
    IReadOnlyList<ChatMessageResponse> Messages,
    bool HasMore);

public sealed record ChatUnreadResponse(int TotalUnread);

/// <summary>A shared file in a conversation, newest first, for the project discussion side panel.</summary>
public sealed record ConversationFileResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string UploaderName,
    DateTime CreatedAt);

/// <summary>Attachment bytes for the authorized download endpoint.</summary>
public sealed record ChatFileContentResponse(
    string FileName,
    string ContentType,
    byte[] Content);
