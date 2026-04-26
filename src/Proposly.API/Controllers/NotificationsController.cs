using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposly.Application.Abstractions;
using Proposly.Application.Notifications.Commands.MarkAllNotificationsRead;
using Proposly.Application.Notifications.Commands.MarkNotificationRead;
using Proposly.Application.Notifications.Queries.GetNotifications;
using Proposly.Application.Notifications.Responses;

namespace Proposly.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<NotificationResponse>> GetAll(
        [FromServices] IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationResponse>> handler,
        CancellationToken ct,
        [FromQuery] int? limit = 50)
        => await handler.HandleAsync(new GetNotificationsQuery(limit), ct);

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(
        Guid id,
        [FromServices] ICommandHandler<MarkNotificationReadCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new MarkNotificationReadCommand(id), ct);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(
        [FromServices] ICommandHandler<MarkAllNotificationsReadCommand> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new MarkAllNotificationsReadCommand(), ct);
        return NoContent();
    }
}
