using Microsoft.EntityFrameworkCore;
using Proposly.Application.Chat.Responses;
using Proposly.Application.Chat.Services;
using Proposly.Domain.Chat.Enums;

namespace Proposly.Infrastructure.Persistence.Repositories;

/// <summary>
/// Read-only chat projections (see IChatReadService). Participants are always reached through
/// the tenant-filtered Conversations set, never queried directly.
/// </summary>
public sealed class ChatReadService(AppDbContext context) : IChatReadService
{
    public async Task<IReadOnlyList<ConversationResponse>> GetConversationsForUserAsync(
        Guid userId, string? filter, CancellationToken ct = default)
    {
        var query = context.Conversations
            .Where(c => c.Participants.Any(p => p.UserId == userId));

        query = filter?.ToLowerInvariant() switch
        {
            "direct" => query.Where(c => c.Kind == ConversationKind.Direct),
            "group" => query.Where(c => c.Kind == ConversationKind.Group),
            "project" => query.Where(c => c.Kind == ConversationKind.Project),
            _ => query,
        };

        var rows = await ProjectRows(query.OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt), userId)
            .ToListAsync(ct);

        return await ComposeAsync(rows, userId, ct);
    }

    public async Task<ConversationResponse?> GetConversationForUserAsync(
        Guid conversationId, Guid userId, CancellationToken ct = default)
    {
        var query = context.Conversations
            .Where(c => c.Id == conversationId && c.Participants.Any(p => p.UserId == userId));

        var rows = await ProjectRows(query, userId).ToListAsync(ct);
        var composed = await ComposeAsync(rows, userId, ct);
        return composed.Count == 0 ? null : composed[0];
    }

    public async Task<ChatMessagesPageResponse> GetMessagesPageAsync(
        Guid conversationId, Guid? beforeMessageId, int limit, CancellationToken ct = default)
    {
        var messages = context.ChatMessages.Where(m => m.ConversationId == conversationId);

        if (beforeMessageId is not null)
        {
            var cursor = await context.ChatMessages
                .Where(m => m.Id == beforeMessageId)
                .Select(m => (DateTime?)m.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (cursor is not null)
                messages = messages.Where(m => m.CreatedAt < cursor);
        }

        var window = await messages
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = window.Count > limit;
        var page = window.Take(limit).OrderBy(m => m.CreatedAt).ToList();

        var messageIds = page.Select(m => m.Id).ToList();
        var attachments = await context.ChatAttachments
            .Where(a => a.MessageId != null && messageIds.Contains(a.MessageId.Value))
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);
        var attachmentsByMessage = attachments.ToLookup(a => a.MessageId!.Value);

        var items = page.Select(m => new ChatMessageResponse(
            m.Id,
            m.ConversationId,
            m.AuthorUserId,
            m.AuthorName,
            m.Body,
            m.CreatedAt,
            attachmentsByMessage[m.Id]
                .Select(a => new ChatAttachmentResponse(a.Id, a.FileName, a.ContentType, a.SizeBytes))
                .ToList())).ToList();

        return new ChatMessagesPageResponse(items, hasMore);
    }

    public async Task<int> GetUnreadTotalAsync(Guid userId, CancellationToken ct = default)
        => await context.Conversations
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .Select(c => context.ChatMessages.Count(m =>
                m.ConversationId == c.Id
                && m.AuthorUserId != userId
                && (c.Participants.First(p => p.UserId == userId).LastReadAt == null
                    || m.CreatedAt > c.Participants.First(p => p.UserId == userId).LastReadAt)))
            .SumAsync(ct);

    public async Task<IReadOnlyList<ConversationFileResponse>> GetConversationFilesAsync(
        Guid conversationId, CancellationToken ct = default)
    {
        var files = await context.ChatAttachments
            .Where(a => a.ConversationId == conversationId && a.MessageId != null)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.FileName,
                a.ContentType,
                a.SizeBytes,
                a.UploaderUserId,
                a.CreatedAt,
            })
            .ToListAsync(ct);

        var uploaderIds = files.Select(f => f.UploaderUserId).Distinct().ToList();
        var uploaders = await context.Users
            .Where(u => uploaderIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, ct);

        return files.Select(f => new ConversationFileResponse(
            f.Id,
            f.FileName,
            f.ContentType,
            f.SizeBytes,
            uploaders.TryGetValue(f.UploaderUserId, out var u) ? $"{u.FirstName} {u.LastName}" : "Unknown",
            f.CreatedAt)).ToList();
    }

    private sealed record LastMessageRow(
        Guid Id, string? Body, Guid AuthorUserId, string AuthorName, DateTime CreatedAt, int AttachmentCount, string? FirstAttachmentName);

    private sealed record ConversationRow(
        Guid Id, ConversationKind Kind, string? Title, Guid? ProjectId, DateTime CreatedAt, DateTime? LastMessageAt,
        List<Guid> ParticipantUserIds, int UnreadCount, LastMessageRow? Last);

    private IQueryable<ConversationRow> ProjectRows(IQueryable<Domain.Chat.Entities.Conversation> query, Guid userId)
        => query.Select(c => new ConversationRow(
            c.Id,
            c.Kind,
            c.Title,
            c.ProjectId,
            c.CreatedAt,
            c.LastMessageAt,
            c.Participants.Select(p => p.UserId).ToList(),
            context.ChatMessages.Count(m =>
                m.ConversationId == c.Id
                && m.AuthorUserId != userId
                && (c.Participants.First(p => p.UserId == userId).LastReadAt == null
                    || m.CreatedAt > c.Participants.First(p => p.UserId == userId).LastReadAt)),
            context.ChatMessages
                .Where(m => m.ConversationId == c.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new LastMessageRow(
                    m.Id,
                    m.Body,
                    m.AuthorUserId,
                    m.AuthorName,
                    m.CreatedAt,
                    context.ChatAttachments.Count(a => a.MessageId == m.Id),
                    context.ChatAttachments
                        .Where(a => a.MessageId == m.Id)
                        .OrderBy(a => a.CreatedAt)
                        .Select(a => a.FileName)
                        .FirstOrDefault()))
                .FirstOrDefault()));

    private async Task<IReadOnlyList<ConversationResponse>> ComposeAsync(
        List<ConversationRow> rows, Guid userId, CancellationToken ct)
    {
        if (rows.Count == 0)
            return [];

        var allUserIds = rows.SelectMany(r => r.ParticipantUserIds).Distinct().ToList();
        var users = await context.Users
            .Where(u => allUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Role })
            .ToDictionaryAsync(u => u.Id, ct);

        return rows.Select(r =>
        {
            var participants = r.ParticipantUserIds
                .Select(id => users.TryGetValue(id, out var u)
                    ? new ChatParticipantResponse(id, $"{u.FirstName} {u.LastName}", u.Role.ToString())
                    : new ChatParticipantResponse(id, "Unknown", string.Empty))
                .OrderBy(p => p.Name)
                .ToList();

            var others = participants.Where(p => p.UserId != userId).ToList();
            var (title, subtitle) = r.Kind switch
            {
                ConversationKind.Direct => (
                    others.FirstOrDefault()?.Name ?? "Conversation",
                    others.FirstOrDefault()?.Role ?? string.Empty),
                ConversationKind.Project => (
                    r.Title ?? "Project",
                    $"Project channel · {participants.Count} members"),
                _ => (
                    r.Title ?? "Group",
                    string.Join(", ", participants.Select(p => p.Name.Split(' ')[0]))),
            };

            string? preview = null;
            var attachmentsOnly = false;
            if (r.Last is not null)
            {
                attachmentsOnly = r.Last.Body is null && r.Last.AttachmentCount > 0;
                preview = r.Last.Body
                    ?? (r.Last.AttachmentCount == 1 ? r.Last.FirstAttachmentName : $"{r.Last.AttachmentCount} files");
            }

            return new ConversationResponse(
                r.Id,
                r.Kind.ToString(),
                title,
                subtitle,
                r.ProjectId,
                r.Last?.CreatedAt ?? r.LastMessageAt,
                preview,
                attachmentsOnly,
                r.Last?.AuthorName.Split(' ')[0],
                r.Last is not null && r.Last.AuthorUserId == userId,
                r.UnreadCount,
                participants);
        }).ToList();
    }
}
