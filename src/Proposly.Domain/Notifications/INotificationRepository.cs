namespace Proposly.Domain.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetForUserAsync(Guid userId, int? limit = 50, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task MarkAllReadForUserAsync(Guid userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
