using Proposly.Application.Abstractions;
using Proposly.Application.Notifications.Responses;
using Proposly.Domain.Notifications;

namespace Proposly.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(
    INotificationRepository repository,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationResponse>>
{
    public async Task<IReadOnlyList<NotificationResponse>> HandleAsync(
        GetNotificationsQuery query, CancellationToken cancellationToken = default)
    {
        var notifications = await repository.GetForUserAsync(currentUserService.UserId, query.Limit, cancellationToken);

        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse(n.Id, n.Title, n.Link, n.IsRead, n.CreatedAt))
            .ToList();
    }
}
