using Proposly.Application.Abstractions;
using Proposly.Application.Notifications.Responses;

namespace Proposly.Application.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(int? Limit = 50) : IQuery<IReadOnlyList<NotificationResponse>>;
