using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;

namespace Proposly.Application.Chat.Commands.UploadChatAttachment;

public sealed record UploadChatAttachmentCommand(
    Guid ConversationId,
    string FileName,
    string ContentType,
    byte[] Content) : ICommand<ChatAttachmentResponse>;
