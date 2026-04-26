namespace Proposly.Application.Notifications.Responses;

public sealed record NotificationResponse(
    Guid Id,
    string Title,
    string? Link,
    bool IsRead,
    DateTime CreatedAt);
