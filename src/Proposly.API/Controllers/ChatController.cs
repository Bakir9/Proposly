using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Commands.CreateConversation;
using Proposly.Application.Chat.Commands.MarkConversationRead;
using Proposly.Application.Chat.Commands.OpenProjectConversation;
using Proposly.Application.Chat.Commands.SendMessage;
using Proposly.Application.Chat.Commands.UploadChatAttachment;
using Proposly.Application.Chat.Queries.GetChatAttachment;
using Proposly.Application.Chat.Queries.GetConversation;
using Proposly.Application.Chat.Queries.GetConversationFiles;
using Proposly.Application.Chat.Queries.GetConversations;
using Proposly.Application.Chat.Queries.GetMessages;
using Proposly.Application.Chat.Queries.GetUnreadTotal;
using Proposly.Application.Chat.Responses;

namespace Proposly.API.Controllers;

// Plain [Authorize] like NotificationsController: chat is for every signed-in employee,
// and per-conversation access is enforced by participant checks in the handlers.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ChatController : ControllerBase
{
    [HttpGet("conversations")]
    public async Task<IReadOnlyList<ConversationResponse>> GetConversations(
        [FromServices] IQueryHandler<GetConversationsQuery, IReadOnlyList<ConversationResponse>> handler,
        CancellationToken ct,
        [FromQuery] string? filter = null)
        => await handler.HandleAsync(new GetConversationsQuery(filter), ct);

    [HttpGet("conversations/{id:guid}")]
    public async Task<ActionResult<ConversationResponse>> GetConversation(
        Guid id,
        [FromServices] IQueryHandler<GetConversationQuery, ConversationResponse?> handler,
        CancellationToken ct)
    {
        var conversation = await handler.HandleAsync(new GetConversationQuery(id), ct);
        return conversation is null ? NotFound() : conversation;
    }

    [HttpPost("conversations")]
    public async Task<ActionResult<Guid>> CreateConversation(
        [FromBody] CreateConversationCommand command,
        [FromServices] ICommandHandler<CreateConversationCommand, Guid> handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetConversation), new { id }, id);
    }

    [HttpGet("conversations/{id:guid}/messages")]
    public async Task<ChatMessagesPageResponse> GetMessages(
        Guid id,
        [FromServices] IQueryHandler<GetMessagesQuery, ChatMessagesPageResponse> handler,
        CancellationToken ct,
        [FromQuery] Guid? before = null,
        [FromQuery] int limit = 50)
        => await handler.HandleAsync(new GetMessagesQuery(id, before, limit), ct);

    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<ActionResult<ChatMessageResponse>> SendMessage(
        Guid id,
        [FromBody] SendMessageRequest request,
        [FromServices] ICommandHandler<SendMessageCommand, ChatMessageResponse> handler,
        CancellationToken ct)
    {
        var message = await handler.HandleAsync(new SendMessageCommand(id, request.Body, request.AttachmentIds ?? []), ct);
        return CreatedAtAction(nameof(GetMessages), new { id }, message);
    }

    [HttpPut("conversations/{id:guid}/read")]
    public async Task<IActionResult> MarkRead(
        Guid id,
        [FromBody] MarkReadRequest request,
        [FromServices] ICommandHandler<MarkConversationReadCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new MarkConversationReadCommand(id, request.LastReadMessageId), ct);
        return NoContent();
    }

    [HttpGet("unread")]
    public async Task<ChatUnreadResponse> GetUnreadTotal(
        [FromServices] IQueryHandler<GetUnreadTotalQuery, ChatUnreadResponse> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetUnreadTotalQuery(), ct);

    [HttpPost("conversations/{id:guid}/attachments")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<ActionResult<ChatAttachmentResponse>> UploadAttachment(
        Guid id,
        IFormFile file,
        [FromServices] ICommandHandler<UploadChatAttachmentCommand, ChatAttachmentResponse> handler,
        CancellationToken ct)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);

        var command = new UploadChatAttachmentCommand(
            id,
            Path.GetFileName(file.FileName),
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            stream.ToArray());

        return await handler.HandleAsync(command, ct);
    }

    [HttpGet("attachments/{id:guid}")]
    public async Task<IActionResult> DownloadAttachment(
        Guid id,
        [FromServices] IQueryHandler<GetChatAttachmentQuery, ChatFileContentResponse?> handler,
        CancellationToken ct)
    {
        var fileContent = await handler.HandleAsync(new GetChatAttachmentQuery(id), ct);
        return fileContent is null
            ? NotFound()
            : File(fileContent.Content, fileContent.ContentType, fileContent.FileName);
    }

    [HttpGet("conversations/{id:guid}/files")]
    public async Task<IReadOnlyList<ConversationFileResponse>> GetConversationFiles(
        Guid id,
        [FromServices] IQueryHandler<GetConversationFilesQuery, IReadOnlyList<ConversationFileResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetConversationFilesQuery(id), ct);

    [HttpPost("projects/{projectId:guid}/conversation")]
    public async Task<ActionResult<Guid>> OpenProjectConversation(
        Guid projectId,
        [FromServices] ICommandHandler<OpenProjectConversationCommand, Guid> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new OpenProjectConversationCommand(projectId), ct);

    public sealed record SendMessageRequest(string? Body, IReadOnlyList<Guid>? AttachmentIds);
    public sealed record MarkReadRequest(Guid? LastReadMessageId);
}
