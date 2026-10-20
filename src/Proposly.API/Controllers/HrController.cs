using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.API.Authorization;
using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Commands.AddWorker;
using Proposly.Application.Hr.Commands.AddWorkerAbsence;
using Proposly.Application.Hr.Commands.AddWorkerNote;
using Proposly.Application.Hr.Commands.DeleteWorkerFile;
using Proposly.Application.Hr.Commands.DeleteWorkerNote;
using Proposly.Application.Hr.Commands.InviteWorker;
using Proposly.Application.Hr.Commands.SetWorkerDocumentCategory;
using Proposly.Application.Hr.Commands.SetWorkerNotePinned;
using Proposly.Application.Hr.Commands.UpdateWorker;
using Proposly.Application.Hr.Commands.UploadWorkerFile;
using Proposly.Application.Hr.Queries.GetWorker;
using Proposly.Application.Hr.Queries.GetWorkerFile;
using Proposly.Application.Hr.Queries.GetWorkers;
using Proposly.Application.Hr.Responses;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.API.Controllers;

// The HR record carries the most sensitive data in the app (SVNR, birthday, internal notes),
// so the whole surface is Owner/Admin only. Workers never reach these endpoints.
[Authorize(Policy = Policies.ManageUsers)]
[ApiController]
[Route("api/[controller]")]
public sealed class HrController : ControllerBase
{
    [HttpGet("workers")]
    public async Task<IReadOnlyList<WorkerListItemResponse>> GetWorkers(
        [FromServices] IQueryHandler<GetWorkersQuery, IReadOnlyList<WorkerListItemResponse>> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new GetWorkersQuery(), ct);

    [HttpGet("workers/{userId:guid}")]
    public async Task<ActionResult<WorkerResponse>> GetWorker(
        Guid userId,
        [FromServices] IQueryHandler<GetWorkerQuery, WorkerResponse?> handler,
        CancellationToken ct)
    {
        var worker = await handler.HandleAsync(new GetWorkerQuery(userId), ct);
        return worker is null ? NotFound() : worker;
    }

    [HttpPost("workers")]
    public async Task<ActionResult<Guid>> AddWorker(
        [FromBody] AddWorkerCommand command,
        [FromServices] ICommandHandler<AddWorkerCommand, Guid> handler,
        CancellationToken ct)
    {
        var userId = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetWorker), new { userId }, userId);
    }

    [HttpPost("workers/{userId:guid}/invite")]
    public async Task<IActionResult> InviteWorker(
        Guid userId,
        [FromBody] InviteWorkerRequest request,
        [FromServices] ICommandHandler<InviteWorkerCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new InviteWorkerCommand(userId, request.Email, request.Role), ct);
        return NoContent();
    }

    [HttpPut("workers/{userId:guid}")]
    public async Task<IActionResult> UpdateWorker(
        Guid userId,
        [FromBody] UpdateWorkerCommand command,
        [FromServices] ICommandHandler<UpdateWorkerCommand> handler,
        CancellationToken ct)
    {
        if (userId != command.UserId)
            throw new ArgumentException("Route and payload user ids do not match.");
        await handler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPost("workers/{userId:guid}/absences")]
    public async Task<ActionResult<Guid>> AddAbsence(
        Guid userId,
        [FromBody] AddWorkerAbsenceRequest request,
        [FromServices] ICommandHandler<AddWorkerAbsenceCommand, Guid> handler,
        CancellationToken ct)
        => await handler.HandleAsync(
            new AddWorkerAbsenceCommand(userId, request.Type, request.StartDate, request.EndDate), ct);

    [HttpPost("workers/{userId:guid}/notes")]
    public async Task<ActionResult<WorkerNoteResponse>> AddNote(
        Guid userId,
        [FromBody] AddNoteRequest request,
        [FromServices] ICommandHandler<AddWorkerNoteCommand, WorkerNoteResponse> handler,
        CancellationToken ct)
        => await handler.HandleAsync(new AddWorkerNoteCommand(userId, request.Tag, request.Text), ct);

    [HttpPut("workers/{userId:guid}/notes/{noteId:guid}/pin")]
    public async Task<IActionResult> PinNote(
        Guid userId,
        Guid noteId,
        [FromBody] PinNoteRequest request,
        [FromServices] ICommandHandler<SetWorkerNotePinnedCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new SetWorkerNotePinnedCommand(userId, noteId, request.Pinned), ct);
        return NoContent();
    }

    [HttpDelete("workers/{userId:guid}/notes/{noteId:guid}")]
    public async Task<IActionResult> DeleteNote(
        Guid userId,
        Guid noteId,
        [FromServices] ICommandHandler<DeleteWorkerNoteCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteWorkerNoteCommand(userId, noteId), ct);
        return NoContent();
    }

    [HttpPut("workers/{userId:guid}/documents/{documentId:guid}/category")]
    public async Task<IActionResult> SetDocumentCategory(
        Guid userId,
        Guid documentId,
        [FromBody] SetCategoryRequest request,
        [FromServices] ICommandHandler<SetWorkerDocumentCategoryCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new SetWorkerDocumentCategoryCommand(userId, documentId, request.Category), ct);
        return NoContent();
    }

    [HttpPost("workers/{userId:guid}/files")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<ActionResult<WorkerDocumentResponse?>> UploadFile(
        Guid userId,
        IFormFile file,
        [FromForm] WorkerFileKind kind,
        [FromForm] string? category,
        [FromServices] ICommandHandler<UploadWorkerFileCommand, WorkerDocumentResponse?> handler,
        CancellationToken ct)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);

        var command = new UploadWorkerFileCommand(
            userId, kind, category,
            Path.GetFileName(file.FileName),
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            stream.ToArray());

        return await handler.HandleAsync(command, ct);
    }

    [HttpGet("workers/{userId:guid}/files/{kind}")]
    public async Task<IActionResult> DownloadFile(
        Guid userId,
        WorkerFileKind kind,
        [FromServices] IQueryHandler<GetWorkerFileQuery, HrFileResponse?> handler,
        CancellationToken ct,
        [FromQuery] Guid? documentId = null)
    {
        var file = await handler.HandleAsync(new GetWorkerFileQuery(userId, kind, documentId), ct);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
    }

    [HttpDelete("workers/{userId:guid}/files/{kind}")]
    public async Task<IActionResult> DeleteFile(
        Guid userId,
        WorkerFileKind kind,
        [FromServices] ICommandHandler<DeleteWorkerFileCommand> handler,
        CancellationToken ct,
        [FromQuery] Guid? documentId = null)
    {
        await handler.HandleAsync(new DeleteWorkerFileCommand(userId, kind, documentId), ct);
        return NoContent();
    }

    public sealed record InviteWorkerRequest(string Email, string Role);
    public sealed record AddWorkerAbsenceRequest(AbsenceType Type, DateOnly StartDate, DateOnly EndDate);
    public sealed record AddNoteRequest(string Tag, string Text);
    public sealed record PinNoteRequest(bool Pinned);
    public sealed record SetCategoryRequest(string Category);
}
