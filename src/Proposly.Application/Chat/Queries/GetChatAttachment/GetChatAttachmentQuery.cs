using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;

namespace Proposly.Application.Chat.Queries.GetChatAttachment;

/// <summary>Attachment bytes for the authorized download endpoint; null when unknown.</summary>
public sealed record GetChatAttachmentQuery(Guid AttachmentId) : IQuery<ChatFileContentResponse?>;
