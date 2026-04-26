using Microsoft.EntityFrameworkCore;
using Proposly.Domain.Notifications;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(AppDbContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct = default)
        => await context.Notifications.AddAsync(notification, ct);

    public async Task<IReadOnlyList<Notification>> GetForUserAsync(Guid userId, int? limit = 50, CancellationToken ct = default)
    {
        var query = context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);
        return await (limit.HasValue ? query.Take(limit.Value) : query).ToListAsync(ct);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task MarkAllReadForUserAsync(Guid userId, CancellationToken ct = default)
        => await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
