using Proposly.Application.Abstractions;
using Proposly.Domain.Notifications;

namespace Proposly.Application.Notifications.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadCommandHandler(
    INotificationRepository repository,
    ICurrentUserService currentUserService)
    : ICommandHandler<MarkAllNotificationsReadCommand>
{
    public async Task HandleAsync(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken = default)
    {
        await repository.MarkAllReadForUserAsync(currentUserService.UserId, cancellationToken);
    }
}
