using Proposly.Application.Abstractions;

namespace Proposly.Application.Notifications.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand;
