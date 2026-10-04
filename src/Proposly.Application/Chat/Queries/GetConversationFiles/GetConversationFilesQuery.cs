using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;

namespace Proposly.Application.Chat.Queries.GetConversationFiles;

/// <summary>All sent attachments of a conversation, newest first (project "Shared files" panel).</summary>
public sealed record GetConversationFilesQuery(Guid ConversationId) : IQuery<IReadOnlyList<ConversationFileResponse>>;
