using Proposly.Application.Abstractions;
using Proposly.Domain.Notifications;

namespace Proposly.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(INotificationRepository repository)
    : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task HandleAsync(MarkNotificationReadCommand command, CancellationToken cancellationToken = default)
    {
        var notification = await repository.GetByIdAsync(command.NotificationId, cancellationToken)
            ?? throw new InvalidOperationException("Notification not found.");

        notification.MarkAsRead();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
